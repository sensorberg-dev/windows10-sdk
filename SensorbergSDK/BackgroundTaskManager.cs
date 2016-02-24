using SensorbergSDK.Internal;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK
{
    public struct BackgroundTaskRegistrationResult
    {
        public bool success;
        public Exception exception;
    }

	/// <summary>
	/// A helper class for managing the background task (registering and unregistering).
	/// The classes responsible for taking the action when the tasks are triggered are
    /// AdvertisementWatcherBackgroundTask and TimedBackgroundTask.
	/// </summary>
	public sealed class BackgroundTaskManager
	{
        private static readonly string BackgroundTaskProjectNamespace = "SensorbergSDKBackground";
		private static readonly string AdvertisementWatcherBackgroundTaskNameSuffix = "AdvertisementWatcherBackgroundTask";
        private static readonly string TimedBackgroundTaskNameSuffix = "TimedBackgroundTask";
        private static readonly string AdvertisementWatcherBackgroundTaskEntryPoint = BackgroundTaskProjectNamespace + ".AdvertisementWatcherBackgroundTask";
        private static readonly string TimedBackgroundTaskEntryPoint = BackgroundTaskProjectNamespace + ".TimedBackgroundTask";
        private const int TimeTriggerIntervalInMinutes = 15;
        private const int SignalStrengthFilterOutOfRangeThresholdInDBm = -127;
        private const int MaxBeaconId1FilterCount = 10;
	    private AppSettings _appSettings;

        public EventHandler BackgroundFiltersUpdated;

        /// <summary>
        /// Property for checking whether the background task is registered or not.
        /// </summary>
        public bool IsBackgroundTaskRegistered
		{
			get
			{
                return (BackgroundTaskRegistered(_advertisementWatcherBackgroundTaskName)
                    && BackgroundTaskRegistered(_timedBackgroundTaskName));
            }
		}

        private string _advertisementWatcherBackgroundTaskName;
        private string _timedBackgroundTaskName;

        /// <summary>
        /// Constructor.
        /// 
        /// Since the names of background tasks must be unique, we use the package ID as a prefix
        /// in the task names.
        /// 
        /// For more information, see https://msdn.microsoft.com/en-us/library/windows/apps/xaml/jj553413.aspx
        /// </summary>
        public BackgroundTaskManager()
        {
            string packageId = Windows.ApplicationModel.Package.Current.Id.Name;
            _advertisementWatcherBackgroundTaskName = packageId + AdvertisementWatcherBackgroundTaskNameSuffix;
            _timedBackgroundTaskName = packageId + TimedBackgroundTaskNameSuffix;
        }

		/// <summary>
		/// Unregisters the background task if registered.
		/// </summary>
        public void UnregisterBackgroundTask()
        {
            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(_advertisementWatcherBackgroundTaskName)
                    || taskValue.Name.Equals(_timedBackgroundTaskName))
                {
                    taskValue.Unregister(true);
                    System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.UnregisterBackgroundTask(): Unregistered task: " + taskValue.Name);
                }
            }
        }

        /// <summary>
        /// Registers the background task.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The registration result.</returns>
        public IAsyncOperation<BackgroundTaskRegistrationResult> RegisterBackgroundTaskAsync(UInt16 manufacturerId, UInt16 beaconCode)
        {
            return InternalRegisterBackgroundTaskAsync(manufacturerId, beaconCode).AsAsyncOperation<BackgroundTaskRegistrationResult>();
        }

        internal async Task<BackgroundTaskRegistrationResult> InternalRegisterBackgroundTaskAsync(UInt16 manufacturerId, UInt16 beaconCode)
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                success = IsBackgroundTaskRegistered,
                exception = null
            };

            if (!result.success)
            {
                // Prompt user to accept the request
                BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
                if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity
                    || backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
                {
                    result = RegisterTimedBackgroundTask();

                    if (result.success)
                    {
                        result = await RegisterAdvertisementWatcherBackgroundTaskAsync(manufacturerId, beaconCode);
                    }
                }

                if (result.success)
                {
                    System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.RegisterBackgroundTask(): Registration successful");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.RegisterBackgroundTask(): Already registered");
            }

			return result;
		}

	    /// <summary>
        /// Updates the background task e.g. when the filters should be changed.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The registration result.</returns>
        public async Task<BackgroundTaskRegistrationResult> UpdateBackgroundTaskAsync(UInt16 manufacturerId, UInt16 beaconCode)
        {
            UnregisterBackgroundTask();
            return await RegisterAdvertisementWatcherBackgroundTaskAsync(manufacturerId, beaconCode);
        }

        /// <summary>
        /// Checks if the background filters are up-to-date or not. To update the filters,
        /// unregister and register background task again
        /// (call BackgroundTaskManager.UpdateBackgroundTaskAsync()).
        /// </summary>
        /// <returns>True, if an update is required. False otherwise.</returns>
        public static bool CheckIfBackgroundFilterUpdateIsRequired()
        {
            SDKData sdkData = SDKData.Instance;
            bool isRequired = sdkData.BackgroundFilterUpdateRequired;

            if (!isRequired && !string.IsNullOrEmpty(sdkData.LayoutBeaconId1Hash))
            {
                string upToDateHash = LayoutManager.CreateHashOfBeaconId1sInLayout(LayoutManager.Instance.Layout);

                if (!string.IsNullOrEmpty(upToDateHash)
                    && !sdkData.LayoutBeaconId1Hash.Equals(upToDateHash))
                {
                    sdkData.LayoutBeaconId1Hash = upToDateHash;
                    sdkData.BackgroundFilterUpdateRequired = true;
                    isRequired = true;
                }
            }

            return isRequired;
        }

        /// <summary>
        /// Registers the BLE advertisement watcher background task.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The registration result.</returns>
        private async Task<BackgroundTaskRegistrationResult> RegisterAdvertisementWatcherBackgroundTaskAsync(
            UInt16 manufacturerId, UInt16 beaconCode)
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                success = false,
                exception = null
            };

            if (BackgroundTaskRegistered(_advertisementWatcherBackgroundTaskName))
            {
                // Already registered
                System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.RegisterAdvertisementWatcherBackgroundTask(): Already registered");
                result.success = true;
            }
            else
            {
                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();

                backgroundTaskBuilder.Name = _advertisementWatcherBackgroundTaskName;
                backgroundTaskBuilder.TaskEntryPoint = AdvertisementWatcherBackgroundTaskEntryPoint;

                IBackgroundTrigger trigger = null;

                BluetoothLEAdvertisementWatcherTrigger advertisementWatcherTrigger =
                    new BluetoothLEAdvertisementWatcherTrigger();

                // This filter includes all Sensorberg beacons 
                var pattern = BeaconFactory.UUIDToAdvertisementBytePattern(Constants.SensorbergUuidSpace, manufacturerId, beaconCode);
                advertisementWatcherTrigger.AdvertisementFilter.BytePatterns.Add(pattern);

                LayoutManager layoutManager = LayoutManager.Instance;

#if FILTER_SUPPORTS_MORE_UUIDS
                // Only UUIDs that are registered to the app will be added into filter                      
                if (await layoutManager.VerifyLayoutAsync(false)
                    && layoutManager.Layout.ContainsOtherThanSensorbergBeaconId1s())
                {
                    int counter = 0;

                    foreach (string beaconId1 in LayoutManager.Instance.Layout.AccountBeaconId1s)
                    {
                        if (beaconId1.Length == Constants.BeaconId1LengthWithoutDashes && counter < MaxBeaconId1FilterCount)
                        {
                            if (!beaconId1.StartsWith(Constants.SensorbergUuidSpace, StringComparison.CurrentCultureIgnoreCase))
                            {
                                pattern = BeaconFactory.UUIDToAdvertisementBytePattern(beaconId1);
                                advertisementWatcherTrigger.AdvertisementFilter.BytePatterns.Add(pattern);
                                counter++;
                            }
                        }
                    }
                }
#endif

                _appSettings = await SettingsManager.Instance.GetSettingsAsync();

                // Using MaxSamplingInterval as SamplingInterval ensures that we get an event only
                // when entering or exiting from the range of the beacon
                advertisementWatcherTrigger.SignalStrengthFilter.SamplingInterval = advertisementWatcherTrigger.MaxSamplingInterval;
                if (_appSettings.RssiEnterThreshold != null && _appSettings.RssiEnterThreshold.Value >= -128 &&
                    _appSettings.RssiEnterThreshold.Value <= 127)
                {
                    advertisementWatcherTrigger.SignalStrengthFilter.InRangeThresholdInDBm = _appSettings.RssiEnterThreshold;
                }
                else
                {
                    advertisementWatcherTrigger.SignalStrengthFilter.InRangeThresholdInDBm = Constants.DefaultBackgroundScannerEnterThreshold;
                }

                advertisementWatcherTrigger.SignalStrengthFilter.OutOfRangeThresholdInDBm = SignalStrengthFilterOutOfRangeThresholdInDBm;
                advertisementWatcherTrigger.SignalStrengthFilter.OutOfRangeTimeout =  TimeSpan.FromMilliseconds(_appSettings.BeaconExitTimeout);
                
                trigger = advertisementWatcherTrigger;

                backgroundTaskBuilder.SetTrigger(trigger);

                try
                {
                    BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
                    backgroundTaskRegistration.Completed += new BackgroundTaskCompletedEventHandler(OnAdvertisementWatcherBackgroundTaskCompleted);
                    result.success = true;
                }
                catch (Exception ex)
                {
                    result.exception = ex;
                    System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.RegisterAdvertisementWatcherBackgroundTask(): Failed to register: " + ex);
                }

                if (result.success)
                {
                    SDKData sdkData = SDKData.Instance;

                    // Check if there was a pending filter update
                    if (sdkData.BackgroundFilterUpdateRequired)
                    {
                        string upToDateHash = LayoutManager.CreateHashOfBeaconId1sInLayout(layoutManager.Layout);

                        if (!string.IsNullOrEmpty(upToDateHash)
                            && sdkData.LayoutBeaconId1Hash.Equals(upToDateHash))
                        {
                            // Background filter updated successfully
                            sdkData.BackgroundFilterUpdateRequired = false;

                            if (BackgroundFiltersUpdated != null)
                            {
                                BackgroundFiltersUpdated(this, null);
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(sdkData.LayoutBeaconId1Hash))
                    {
                        // This is the first time the background task is registered with valid layout =>
                        // set the hash
                        string upToDateHash = LayoutManager.CreateHashOfBeaconId1sInLayout(layoutManager.Layout);

                        if (!string.IsNullOrEmpty(upToDateHash))
                        {
                            sdkData.LayoutBeaconId1Hash = upToDateHash;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Registers the timed background task.
        /// </summary>
        /// <returns>The registration result.</returns>
        public BackgroundTaskRegistrationResult RegisterTimedBackgroundTask()
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                success = false,
                exception = null
            };

            if (BackgroundTaskRegistered(_timedBackgroundTaskName))
            {
                // Already registered
                result.success = true;
            }
            else
            {
                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();
                backgroundTaskBuilder.Name = _timedBackgroundTaskName;
                backgroundTaskBuilder.TaskEntryPoint = TimedBackgroundTaskEntryPoint;
                TimeTrigger timeTrigger = new TimeTrigger(TimeTriggerIntervalInMinutes, false);
                backgroundTaskBuilder.SetTrigger(timeTrigger);

                try
                {
                    BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
                    backgroundTaskRegistration.Completed += new BackgroundTaskCompletedEventHandler(OnTimedBackgroundTaskCompleted);
                    result.success = true;
                }
                catch (Exception ex)
                {
                    result.exception = ex;
                    System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.RegisterTimedBackgroundTask(): Failed to register: " + ex);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a background task with the given name is registered.
        /// </summary>
        /// <param name="taskName">The name of the background task.</param>
        /// <returns>True, if registered. False otherwise.</returns>
        private bool BackgroundTaskRegistered(string taskName)
        {
            bool registered = false;

            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(taskName))
                {
                    registered = true;
                    break;
                }
            }

            return registered;
        }

        /// <summary>
        /// Note: This handler is called only if the task completed while the application was in the foreground. 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="args"></param>
        private void OnAdvertisementWatcherBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
		{
			System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.OnAdvertisementWatcherBackgroundTaskCompleted()");
        }

        /// <summary>
        /// Note: This handler is called only if the task completed while the application was in the foreground. 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="args"></param>
        private void OnTimedBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.OnTimedBackgroundTaskCompleted()");
        }

       

    }
}
