// Created by Kay Czarnotta on 05.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using SensorbergSDK.Data;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;

namespace SensorbergSDK
{
    public struct BackgroundTaskRegistrationResult
    {
        public bool success;
        public Exception exception;
    }
    public class BackgroundTaskManager
    {
        private const int TIME_TRIGGER_INTERVAL_IN_MINUTES = 15;
        private const int SIGNAL_STRENGTH_FILTER_OUT_OF_RANGE_THRESHOLD_IN_D_BM = -127;

        private const string TIMER_CLASS = "SENSORBERG_TIMER_CLASS";
        private const string ADVERTISEMENT_CLASS = "SENSORBERG_ADVERTISEMENT_CLASS";
        public event EventHandler BackgroundFiltersUpdated;
        public bool IsBackgroundTaskRegistered { get { return BackgroundTaskRegistered(TIMER_CLASS) && BackgroundTaskRegistered(ADVERTISEMENT_CLASS); } }
        public AppSettings AppSettings { get; set; }

        public void UnregisterBackgroundTask()
        {
            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(ADVERTISEMENT_CLASS) || taskValue.Name.Equals(TIMER_CLASS))
                {
                    taskValue.Unregister(true);
                    System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.UnregisterBackgroundTask(): Unregistered task: " + taskValue.Name);
                }
            }
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
                string upToDateHash = LayoutManager.CreateHashOfBeaconId1sInLayout(ServiceManager.LayoutManager.Layout);

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

        public async Task<BackgroundTaskRegistrationResult> UpdateBackgroundTaskAsync(string timerClassName, string advertisementClassName, ushort manufacturerId, ushort beaconCode)
        {
            UnregisterBackgroundTask();
            return await RegisterBackgroundTaskAsync(timerClassName, advertisementClassName, manufacturerId, beaconCode);
        }

        public async Task<BackgroundTaskRegistrationResult> RegisterBackgroundTaskAsync(string timerClassName, string advertisementClassName, ushort manufacturerId, ushort beaconCode)
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
                    result = RegisterTimedBackgroundTask(timerClassName);

                    if (result.success)
                    {
                        result = await RegisterAdvertisementWatcherBackgroundTaskAsync(advertisementClassName, manufacturerId, beaconCode);
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
        /// Registers the BLE advertisement watcher background task.
        /// </summary>
        /// <param name="advertisementClassName">Full class name of the advertisment background task</param>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The registration result.</returns>
        private async Task<BackgroundTaskRegistrationResult> RegisterAdvertisementWatcherBackgroundTaskAsync(string advertisementClassName, ushort manufacturerId, ushort beaconCode)
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                success = false,
                exception = null
            };

            if (BackgroundTaskRegistered(ADVERTISEMENT_CLASS))
            {
                // Already registered
                System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.RegisterAdvertisementWatcherBackgroundTask(): Already registered");
                result.success = true;
            }
            else
            {
                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();

                backgroundTaskBuilder.Name = ADVERTISEMENT_CLASS;
                backgroundTaskBuilder.TaskEntryPoint = advertisementClassName;

                IBackgroundTrigger trigger;

                BluetoothLEAdvertisementWatcherTrigger advertisementWatcherTrigger = new BluetoothLEAdvertisementWatcherTrigger();

                // This filter includes all Sensorberg beacons 
                var pattern = BeaconFactory.UUIDToAdvertisementBytePattern(Constants.SensorbergUuidSpace, manufacturerId, beaconCode);
                advertisementWatcherTrigger.AdvertisementFilter.BytePatterns.Add(pattern);

                ILayoutManager layoutManager = ServiceManager.LayoutManager;

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

                AppSettings = await ServiceManager.SettingsManager.GetSettings();

                // Using MaxSamplingInterval as SamplingInterval ensures that we get an event only
                // when entering or exiting from the range of the beacon
                advertisementWatcherTrigger.SignalStrengthFilter.SamplingInterval = advertisementWatcherTrigger.MaxSamplingInterval;
                if (AppSettings.RssiEnterThreshold != null && AppSettings.RssiEnterThreshold.Value >= -128 &&
                    AppSettings.RssiEnterThreshold.Value <= 127)
                {
                    advertisementWatcherTrigger.SignalStrengthFilter.InRangeThresholdInDBm = AppSettings.RssiEnterThreshold;
                }
                else
                {
                    advertisementWatcherTrigger.SignalStrengthFilter.InRangeThresholdInDBm = Constants.DefaultBackgroundScannerEnterThreshold;
                }

                advertisementWatcherTrigger.SignalStrengthFilter.OutOfRangeThresholdInDBm = SIGNAL_STRENGTH_FILTER_OUT_OF_RANGE_THRESHOLD_IN_D_BM;
                advertisementWatcherTrigger.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(AppSettings.BeaconExitTimeout);

                trigger = advertisementWatcherTrigger;

                backgroundTaskBuilder.SetTrigger(trigger);

                try
                {
                    BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
                    backgroundTaskRegistration.Completed += OnAdvertisementWatcherBackgroundTaskCompleted;
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

                        if (!string.IsNullOrEmpty(upToDateHash) && sdkData.LayoutBeaconId1Hash.Equals(upToDateHash))
                        {
                            // Background filter updated successfully
                            sdkData.BackgroundFilterUpdateRequired = false;

                            BackgroundFiltersUpdated?.Invoke(this, null);
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
        public BackgroundTaskRegistrationResult RegisterTimedBackgroundTask(string timerClassName)
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                success = false,
                exception = null
            };

            if (BackgroundTaskRegistered(TIMER_CLASS))
            {
                // Already registered
                result.success = true;
            }
            else
            {
                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();
                backgroundTaskBuilder.Name = TIMER_CLASS;
                backgroundTaskBuilder.TaskEntryPoint = timerClassName;
                TimeTrigger timeTrigger = new TimeTrigger(TIME_TRIGGER_INTERVAL_IN_MINUTES, false);
                backgroundTaskBuilder.SetTrigger(timeTrigger);

                try
                {
                    BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
                    backgroundTaskRegistration.Completed += OnTimedBackgroundTaskCompleted;
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
        private void OnTimedBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.OnTimedBackgroundTaskCompleted()");
        }

        /// <summary>
        /// Note: This handler is called only if the task completed while the application was in the foreground. 
        /// </summary>
        private void OnAdvertisementWatcherBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("BackgroundTaskManager.OnAdvertisementWatcherBackgroundTaskCompleted()");
        }
    }
}