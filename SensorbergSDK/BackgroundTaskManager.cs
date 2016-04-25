// Created by Kay Czarnotta on 05.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;
using System.Collections.Generic;
using MetroLog;

namespace SensorbergSDK
{
    public struct BackgroundTaskRegistrationResult
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }
    public class BackgroundTaskManager
    {
        private static ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<BackgroundTaskManager>();
        private const int TIME_TRIGGER_INTERVAL_IN_MINUTES = 15;
        private const int SIGNAL_STRENGTH_FILTER_OUT_OF_RANGE_THRESHOLD_IN_D_BM = -127;

        private const string TIMER_CLASS = "SENSORBERG_TIMER_CLASS";
        private const string ADVERTISEMENT_CLASS = "SENSORBERG_ADVERTISEMENT_CLASS";
        public event EventHandler BackgroundFiltersUpdated;
        public event EventHandler<BeaconAction> BackgroundBeaconActionResolved;
        public bool IsBackgroundTaskRegistered { get { return BackgroundTaskRegistered(TIMER_CLASS) && BackgroundTaskRegistered(ADVERTISEMENT_CLASS); } }
        public AppSettings AppSettings { get; set; }

        public void UnregisterBackgroundTask()
        {
            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(ADVERTISEMENT_CLASS) || taskValue.Name.Equals(TIMER_CLASS))
                {
                    taskValue.Unregister(true);
                    logger.Debug("BackgroundTaskManager.UnregisterBackgroundTask(): Unregistered task: " + taskValue.Name);
                }
            }
        }

        /// <summary>
        /// Will remove OnProgress event handlers from advertisement background task
        /// OnProgress events are used to indicate UI tasks on beacon actions resolved in background
        /// </summary>
        public void UnRegisterOnProgressEventHandler()
        {
            logger.Debug("UnRegisterOnProgressEventHandler");

            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(ADVERTISEMENT_CLASS))
                {
                    taskValue.Progress -= OnAdvertisementWatcherBackgroundTaskProgress;
                }
            }
        }
        /// <summary>
        /// Will remove OnProgress event handlers from advertisement background task
        /// OnProgress events are used to indicate UI tasks on beacon actions resolved in background
        /// </summary>
        public void RegisterOnProgressEventHandler()
        {
            logger.Debug("RegisterOnProgressEventHandler");

            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(ADVERTISEMENT_CLASS))
                {
                    taskValue.Progress += OnAdvertisementWatcherBackgroundTaskProgress;
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

        public async Task<BackgroundTaskRegistrationResult> UpdateBackgroundTaskAsync(SdkConfiguration configuration)
        {
            UnregisterBackgroundTask();
            return await RegisterBackgroundTaskAsync(configuration);
        }

        public async Task<BackgroundTaskRegistrationResult> RegisterBackgroundTaskAsync(SdkConfiguration configuration)
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                Success = IsBackgroundTaskRegistered,
                Exception = null
            };

            if (!result.Success)
            {
                // Prompt user to accept the request
                BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
                if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity
                    || backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
                {
                    result = RegisterTimedBackgroundTask(configuration.BackgroundTimerClassName);

                    if (result.Success)
                    {
                        result = await RegisterAdvertisementWatcherBackgroundTaskAsync(configuration);
                    }
                }

                if (result.Success)
                {
                    logger.Debug("BackgroundTaskManager.RegisterBackgroundTask(): Registration successful");
                }
            }
            else
            {
                logger.Debug("BackgroundTaskManager.RegisterBackgroundTask(): Already registered");
            }

            return result;
        }

        /// <summary>
        /// Registers the BLE advertisement watcher background task.
        /// </summary>
        /// <returns>The registration result.</returns>
        private async Task<BackgroundTaskRegistrationResult> RegisterAdvertisementWatcherBackgroundTaskAsync(SdkConfiguration configuration)
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                Success = false,
                Exception = null
            };

            if (BackgroundTaskRegistered(ADVERTISEMENT_CLASS))
            {
                // Already registered
                logger.Debug("BackgroundTaskManager.RegisterAdvertisementWatcherBackgroundTask(): Already registered");
                result.Success = true;
            }
            else
            {
                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();

                backgroundTaskBuilder.Name = ADVERTISEMENT_CLASS;
                backgroundTaskBuilder.TaskEntryPoint = configuration.BackgroundAdvertisementClassName;

                BluetoothLEAdvertisementWatcherTrigger advertisementWatcherTrigger = new BluetoothLEAdvertisementWatcherTrigger();

                // This filter includes all Sensorberg beacons 
                var pattern = BeaconFactory.UUIDToAdvertisementBytePattern(configuration.BackgroundBeaconUuidSpace, configuration.ManufacturerId, configuration.BeaconCode);
                advertisementWatcherTrigger.AdvertisementFilter.BytePatterns.Add(pattern);

                ILayoutManager layoutManager = ServiceManager.LayoutManager;

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

                IBackgroundTrigger trigger = advertisementWatcherTrigger;

                backgroundTaskBuilder.SetTrigger(trigger);

                try
                {
                    BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
                    backgroundTaskRegistration.Completed += OnAdvertisementWatcherBackgroundTaskCompleted;
                    backgroundTaskRegistration.Progress += OnAdvertisementWatcherBackgroundTaskProgress;

                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                    logger.Error("BackgroundTaskManager.RegisterAdvertisementWatcherBackgroundTask(): Failed to register: ", ex);
                }

                if (result.Success)
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

            //Load last events from background
            await LoadBackgroundActions();

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
                Success = false,
                Exception = null
            };

            if (BackgroundTaskRegistered(TIMER_CLASS))
            {
                // Already registered
                result.Success = true;
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
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                    logger.Error("BackgroundTaskManager.RegisterTimedBackgroundTask(): Failed to register: " , ex);
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

            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(taskName))
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Note: This handler is called only if the task completed while the application was in the foreground. 
        /// </summary>
        private void OnTimedBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.Debug("BackgroundTaskManager.OnTimedBackgroundTaskCompleted()");
        }

        /// <summary>
        /// Note: This handler is called only if the task completed while the application was in the foreground. 
        /// </summary>
        private void OnAdvertisementWatcherBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.Debug("BackgroundTaskManager.OnAdvertisementWatcherBackgroundTaskCompleted()");
        }

        private async void OnAdvertisementWatcherBackgroundTaskProgress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            await LoadBackgroundActions();
        }

        private async Task LoadBackgroundActions()
        {
            logger.Debug("BackgroundTaskManager.OnAdvertisementWatcherBackgroundTaskProgress()");

            List<BeaconAction> beaconActions = await ServiceManager.StorageService.GetActionsForForeground();
            foreach (var beaconAction in beaconActions)
            {
                BackgroundBeaconActionResolved?.Invoke(this, beaconAction);
            }
        }
    }
}