// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MetroLog;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal
{
    public class SdkEngine : IDisposable
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<SdkEngine>();
        private const int DelayedActionExecutionTimeframeInSeconds = 60;
        private const int UpdateVisibilityTimerIntervalInMilliseconds = 60000;
        private const int DatabaseExpirationInHours = 1;

        /// <summary>
        /// Fired when a beacon action has been successfully resolved and is ready to be exeuted.
        /// </summary>
        public event EventHandler<BeaconAction> BeaconActionResolved;

        /// <summary>
        /// This event is fired, when a beacon actions could not be resolved.
        /// In most cases this event can be ignored.
        /// </summary>
        public event EventHandler<string> FailedToResolveBeaconAction;

        /// <summary>
        /// Fired, when the layout becomes valid/invalid.
        /// </summary>
        public event EventHandler<bool> LayoutValidityChanged;

        private readonly EventHistory _eventHistory;
        private Timer _processDelayedActionsTimer;
        private Timer _flushHistoryTimer;
        private Timer _updateVisibilityTimer;
        private Timer _getLayoutTimer;
        private DateTimeOffset _nextTimeToProcessDelayedActions;
        private readonly bool _appIsOnForeground;
        private SdkConfiguration _configuration;
        private ILocationService _locationService;
        public AppSettings AppSettings { get; set; }

        public AppSettings DefaultAppSettings
        {
            [DebuggerStepThrough] get { return ServiceManager.SettingsManager.DefaultAppSettings; }
            [DebuggerStepThrough] set { ServiceManager.SettingsManager.DefaultAppSettings = value; }
        }

        /// <summary>
        /// Indicates whether the SDK engine is initialized and ready to function or not.
        /// </summary>
        public bool IsInitialized { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }

        /// <summary>
        /// The Resolver instance.
        /// </summary>
        public IResolver Resolver { [DebuggerStepThrough] get; }

        /// <summary>
        /// Current count of unresolved actions.
        /// </summary>
        public int UnresolvedActionCount { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }

        public string UserId
        {
            [DebuggerStepThrough] get { return SdkData.UserId; }
            [DebuggerStepThrough] set { SdkData.UserId = value; }
        }

        public SdkConfiguration Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                ServiceManager.LocationService.Configuration = _configuration;
            }
        }

        /// <summary>
        /// Creates a new Engine object.
        /// </summary>
        /// <param name="createdOnForeground">bool for indication if the engine works on foreground.</param>
        /// <param name="configuration">Configuration used for the engine.</param>
        public SdkEngine(bool createdOnForeground)
        {
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new ApiConnection();
            ServiceManager.BeaconScanner = new Scanner();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.StorageService = new StorageService(createdOnForeground);
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.LocationService = _locationService = new LocationService();
            ServiceManager.WriterFactory = new WriterFactory();

            _appIsOnForeground = createdOnForeground;
            Resolver = new Resolver(!createdOnForeground, createdOnForeground);
            _eventHistory = new EventHistory();
            _nextTimeToProcessDelayedActions = DateTimeOffset.MaxValue;
            UnresolvedActionCount = 0;
        }

        /// <summary>
        /// Initializes the SDK engine. 
        /// </summary>
        public async Task InitializeAsync()
        {
            Logger.Debug("InitializeAsync");
            if (!IsInitialized)
            {
                await ServiceManager.StorageService.InitStorage();

                ServiceManager.LayoutManager.LayoutValidityChanged += LayoutValidityChanged;

                // We force to update the cache on the foreground only
                await UpdateCacheAsync(_appIsOnForeground);

                Resolver.ActionsResolved -= OnBeaconActionResolvedAsync;
                Resolver.ActionsResolved += OnBeaconActionResolvedAsync;
                Resolver.FailedToResolveActions -= OnResolverFailedToResolveActions;
                Resolver.FailedToResolveActions += OnResolverFailedToResolveActions;

                if (_appIsOnForeground)
                {
                    Logger.Debug("InitializeAsync#Foreground");
                    AppSettings = await ServiceManager.SettingsManager.GetSettings();
                    ServiceManager.SettingsManager.SettingsUpdated += OnSettingsUpdated;

                    var historyTimeSpan = TimeSpan.FromMilliseconds(AppSettings.HistoryUploadInterval);

                    _flushHistoryTimer =
                        new Timer(OnFlushHistoryTimerTimeoutAsync, null, historyTimeSpan, historyTimeSpan);

                    _updateVisibilityTimer =
                        new Timer(OnUpdateVisibilityTimerTimeout, null,
                            UpdateVisibilityTimerIntervalInMilliseconds,
                            UpdateVisibilityTimerIntervalInMilliseconds);

                    var layoutTimeSpam = TimeSpan.FromMilliseconds(AppSettings.LayoutUpdateInterval);
                    _getLayoutTimer = new Timer(OnLayoutUpdatedAsync, null, layoutTimeSpam, layoutTimeSpam);

                    await _locationService.Initialize();

                    // Check for possible delayed actions
                    await ProcessDelayedActionsAsync();
                    await CleanDatabaseAsync();
                }

                IsInitialized = true;
            }
        }

        private void OnSettingsUpdated(object sender, SettingsEventArgs settingsEventArgs)
        {
            AppSettings = settingsEventArgs.Settings;

            var historyIntervalTimeSpan = TimeSpan.FromMilliseconds(AppSettings.HistoryUploadInterval);
            _flushHistoryTimer?.Change(historyIntervalTimeSpan, historyIntervalTimeSpan);

            var layoutUploadIntervalTimespan = TimeSpan.FromMilliseconds(AppSettings.LayoutUpdateInterval);
            _getLayoutTimer?.Change(layoutUploadIntervalTimespan, layoutUploadIntervalTimespan);
        }

        /// <summary>
        /// Updates the layout cache.
        /// </summary>
        public async Task UpdateCacheAsync(bool forceUpdate)
        {
            await ServiceManager.LayoutManager.VerifyLayoutAsync(forceUpdate);
        }

        /// <summary>
        /// Tries to find a beacon action based on the given beacon event arguments.
        /// </summary>
        /// <param name="eventArgs">The arguments of a beacon event.</param>
        /// <returns></returns>
        public async Task ResolveBeaconAction(BeaconEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return;
            }
            Logger.Debug("SDKEngine: resolve beacon " + eventArgs.Beacon.Id1 + " " + eventArgs.Beacon.Id2 + " " + eventArgs.Beacon.Id3 + " " + eventArgs.EventType);
            if (IsInitialized && eventArgs.EventType != BeaconEventType.None)
            {
                UnresolvedActionCount++;
                string location = await _locationService.GetGeoHashedLocation();
                await _eventHistory.SaveBeaconEventAsync(eventArgs, location);
                await Resolver.CreateRequest(eventArgs);
            }
        }

        /// <summary>
        /// Handles delayed beacon actions resolved earlier.
        /// </summary>
        public async Task ProcessDelayedActionsAsync()
        {
            DateTimeOffset nearestDueTime = DateTimeOffset.MaxValue;

            IList<DelayedActionData> delayedActionDataList = await ServiceManager.StorageService.GetDelayedActions();

            foreach (DelayedActionData delayedActionData in delayedActionDataList)
            {
                if (delayedActionData.DueTime < DateTimeOffset.Now.AddSeconds(DelayedActionExecutionTimeframeInSeconds))
                {
                    // Time to execute
                    await ExecuteActionAsync(delayedActionData.ResolvedAction, delayedActionData.BeaconPid, delayedActionData.EventTypeDetectedByDevice, delayedActionData.Location);
                    await ServiceManager.StorageService.SetDelayedActionAsExecuted(delayedActionData.Id);
                }
                else if (delayedActionData.DueTime < nearestDueTime)
                {
                    nearestDueTime = delayedActionData.DueTime;
                }
            }
            if (_appIsOnForeground)
            {
                if (nearestDueTime < DateTimeOffset.MaxValue)
                {
                    ResetProcessDelayedActionsTimer(nearestDueTime);
                }
                else if (_processDelayedActionsTimer != null)
                {
                    _processDelayedActionsTimer.Dispose();
                    _processDelayedActionsTimer = null;
                }
            }
        }

        /// <summary>
        /// Executes the given action, stores the event in event history and notifies the listeners.
        /// </summary>
        private async Task ExecuteActionAsync(ResolvedAction resolvedAction, string beaconPid, BeaconEventType beaconEventType, string location)
        {
            try
            {
                Logger.Debug("SDKEngine: ExecuteActionAsync " + beaconPid + " BeaconEventType: " + beaconEventType);
                bool checkOnlyOnce = _eventHistory.CheckSendOnlyOnceAsync(resolvedAction);
                bool shouldSupress = _eventHistory.ShouldSupressAsync(resolvedAction);

                Logger.Trace("SDKEngine: ExecuteActionAsync " + beaconPid + " checkOnlyOnce: " + checkOnlyOnce + " shouldSupress:" + shouldSupress);
                if (!shouldSupress && !checkOnlyOnce && resolvedAction.IsInsideTimeframes(DateTimeOffset.Now))
                {
                    Logger.Trace("SDKEngine: ExecuteActionAsync " + beaconPid + " action resolved");
                    await _eventHistory.SaveExecutedResolvedActionAsync(resolvedAction.BeaconAction, beaconPid, beaconEventType, location);

                    BeaconActionResolved?.Invoke(this, resolvedAction.BeaconAction);
                }
                else
                {
                    Logger.Trace("SDKEngine: ExecuteActionAsync " + beaconPid + " action not resolved");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error during ExecuteActionAsync", e);
            }
        }

        /// <summary>
        /// (Re)sets the process delayed actions timer to trigger based on the given due time.
        /// </summary>
        /// <param name="nextDueTime">Time when the timer should timeout.</param>
        private void ResetProcessDelayedActionsTimer(DateTimeOffset nextDueTime)
        {
            _processDelayedActionsTimer?.Dispose();

            int millisecondsToNextProcessingOfDelayedActions = (int) nextDueTime.Subtract(DateTimeOffset.Now).TotalMilliseconds;

            Logger.Debug("SDKManager.ResetProcessDelayedActionsTimer(): "
                                               + Math.Round((double) millisecondsToNextProcessingOfDelayedActions/1000, 0)
                                               + " second(s) to next processing of delayed actions");

            _processDelayedActionsTimer =
                new Timer(OnProcessDelayedActionsTimerTimeoutAsync, null,
                    millisecondsToNextProcessingOfDelayedActions, Timeout.Infinite);
        }

        /// <summary>
        /// Cleans old entries from database. Called when the UI application is started.
        /// </summary>
        private async Task CleanDatabaseAsync()
        {
            var dateTimeOffset = DateTimeOffset.Now.AddHours(-DatabaseExpirationInHours);
            if (SdkData.DatabaseCleaningTime < dateTimeOffset)
            {
                SdkData.DatabaseCleaningTime = DateTimeOffset.Now;
                await ServiceManager.StorageService.CleanupDatabase();
            }
        }

        #region Private event handlers

        private async void OnBeaconActionResolvedAsync(object sender, ResolvedActionsEventArgs e)
        {
            UnresolvedActionCount--;
            if (e == null || e.ResolvedActions == null || e.ResolvedActions.Count == 0)
            {
                return;
            }
            Logger.Debug("SDKEngine: OnBeaconActionResolvedAsync " + e.RequestId + " BeaconEventType:" + e.BeaconEventType);
            foreach (ResolvedAction action in e.ResolvedActions)
            {
                try
                {
                    if (action.Delay > 0 && !action.ReportImmediately)
                    {
                        Logger.Debug("SDKEngine: OnBeaconActionResolvedAsync " + e.RequestId + " delay");
                        // Delay action execution
                        DateTimeOffset dueTime = DateTimeOffset.Now.AddSeconds((int) action.Delay);

                        await ServiceManager.StorageService.SaveDelayedAction(action, dueTime, e.BeaconPid, action.EventTypeDetectedByDevice, e.Location);

                        if (_appIsOnForeground && (_processDelayedActionsTimer == null || _nextTimeToProcessDelayedActions > dueTime))
                        {
                            if (_nextTimeToProcessDelayedActions > dueTime)
                            {
                                _nextTimeToProcessDelayedActions = dueTime;
                            }

                            ResetProcessDelayedActionsTimer(dueTime);
                        }
                    }
                    else
                    {
                        Logger.Debug("SDKEngine: OnBeaconActionResolvedAsync/ExecuteActionAsync " + e.RequestId + " -> Beacon Pid " + e.BeaconPid);
                        // Execute action immediately
                        await ExecuteActionAsync(action, e.BeaconPid, e.BeaconEventType, e.Location);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while handling resolved action", ex);
                }
            }
        }

        /// <summary>
        /// This method simply forwards the event to listeners.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The error message.</param>
        private void OnResolverFailedToResolveActions(object sender, string e)
        {
            FailedToResolveBeaconAction?.Invoke(this, e);
        }

        #endregion

        #region Timer callbacks
        private async void OnProcessDelayedActionsTimerTimeoutAsync(object state)
        {
            await ProcessDelayedActionsAsync();
        }

        private async void OnFlushHistoryTimerTimeoutAsync(object state)
        {
            Logger.Debug("History flushed.");
            await _eventHistory.FlushHistoryAsync();
        }

        private async void OnLayoutUpdatedAsync(object state)
        {
            await UpdateCacheAsync(true);
        }

        private void OnUpdateVisibilityTimerTimeout(object state)
        {
            SdkData.AppIsVisible = SdkData.AppIsVisible;
        }

        #endregion

        public async Task FlushHistory()
        {
            await _eventHistory.FlushHistoryAsync();
        }

        public void Dispose()
        {
            _flushHistoryTimer?.Dispose();
            _getLayoutTimer?.Dispose();
            _processDelayedActionsTimer?.Dispose();
            _updateVisibilityTimer?.Dispose();

            if (IsInitialized)
            {
                ServiceManager.LayoutManager.LayoutValidityChanged -= LayoutValidityChanged;

                Resolver.ActionsResolved -= OnBeaconActionResolvedAsync;
                Resolver.FailedToResolveActions -= OnResolverFailedToResolveActions;
                Resolver.Dispose();

                IsInitialized = false;
            }
        }
    }
}