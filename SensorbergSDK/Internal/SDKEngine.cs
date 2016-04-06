using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MetroLog;
using SensorbergSDK.Data;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Internal
{
    public class SDKEngine
    {
        private readonly ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<SDKEngine>();
        private const int DelayedActionExecutionTimeframeInSeconds = 60;
        private const int UpdateVisibilityTimerIntervalInMilliseconds = 60000;
        private const int CheckPendingBeaconActionsFromBackgroundIntervalInMilliseconds = 1000;
        private const int DatabaseExpirationInHours = 1;
        private const int WORKER_TIMER_PERIDODE = 1000;

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

        private EventHistory _eventHistory;
        private Timer _processDelayedActionsTimer;
        private Timer _flushHistoryTimer;
        private Timer _updateVisibilityTimer;
        private Timer _getLayoutTimer;
        private Timer PeriodicWorkerTimer { get; set; }
//        private Timer _fetchActionsResolvedByBackgroundTimer;
        private DateTimeOffset _nextTimeToProcessDelayedActions;
        private bool _appIsOnForeground;
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
        public Resolver Resolver { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }

        /// <summary>
        /// Current count of unresolved actions
        /// </summary>
        public int UnresolvedActionCount { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }

        public string UserId
        {
            [DebuggerStepThrough] get { return SDKData.Instance.UserId; }
            [DebuggerStepThrough] set { SDKData.Instance.UserId = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="createdOnForeground"></param>
        public SDKEngine(bool createdOnForeground)
        {
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new ApiConnection();
            ServiceManager.BeaconScanner = new Scanner();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.StorageService = new StorageService(createdOnForeground);
            ServiceManager.SettingsManager = new SettingsManager();

            _appIsOnForeground = createdOnForeground;
            Resolver = new Resolver();
            _eventHistory = new EventHistory();
            _nextTimeToProcessDelayedActions = DateTimeOffset.MaxValue;
            UnresolvedActionCount = 0;
        }

        /// <summary>
        /// Initializes the SDK engine. 
        /// </summary>
        public async Task InitializeAsync()
        {
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
                    AppSettings = await ServiceManager.SettingsManager.GetSettings();
                    ServiceManager.SettingsManager.SettingsUpdated += OnSettingsUpdated;

                    var historyTimeSpan = TimeSpan.FromMilliseconds(AppSettings.HistoryUploadInterval);

                    _flushHistoryTimer =
                        new Timer(OnFlushHistoryTimerTimeoutAsync, null, historyTimeSpan, historyTimeSpan);

                    _updateVisibilityTimer =
                        new Timer(OnUpdateVisibilityTimerTimeout, null,
                            UpdateVisibilityTimerIntervalInMilliseconds,
                            UpdateVisibilityTimerIntervalInMilliseconds);

                    PeriodicWorkerTimer = new Timer(OnWorkerTimerActivated, null, WORKER_TIMER_PERIDODE, WORKER_TIMER_PERIDODE);
//                    _fetchActionsResolvedByBackgroundTimer =
//                        new Timer(OnCheckActionsResolvedByBackground, null,
//                            CheckPendingBeaconActionsFromBackgroundIntervalInMilliseconds,
//                            CheckPendingBeaconActionsFromBackgroundIntervalInMilliseconds);

                    var layoutTimeSpam = TimeSpan.FromMilliseconds(AppSettings.LayoutUpdateInterval);
                    _getLayoutTimer = new Timer(OnLayoutUpdatedAsync, null, layoutTimeSpam, layoutTimeSpam);

                    // Check for possible delayed actions
                    await ProcessDelayedActionsAsync();
                    await CleanDatabaseAsync();
                    await _eventHistory.FlushHistoryAsync();
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
        /// De-initializes the SDK.
        /// </summary>
        public void Deinitialize()
        {
            if (IsInitialized)
            {
                ServiceManager.LayoutManager.LayoutValidityChanged -= LayoutValidityChanged;

                Resolver.ActionsResolved -= OnBeaconActionResolvedAsync;
                Resolver.FailedToResolveActions -= OnResolverFailedToResolveActions;
                Resolver.ClearRequests();

                if (_flushHistoryTimer != null)
                {
                    _flushHistoryTimer.Dispose();
                    _flushHistoryTimer = null;
                }

                if (_processDelayedActionsTimer != null)
                {
                    _processDelayedActionsTimer.Dispose();
                    _processDelayedActionsTimer = null;
                }

                PeriodicWorkerTimer?.Dispose();
                PeriodicWorkerTimer = null;
//                if (_fetchActionsResolvedByBackgroundTimer != null)
//                {
//                    _fetchActionsResolvedByBackgroundTimer.Dispose();
//                    _fetchActionsResolvedByBackgroundTimer = null;
//                }

                IsInitialized = false;
            }
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
            logger.Debug("SDKEngine: resolve beacon " + eventArgs.Beacon.Id1 + " " + eventArgs.Beacon.Id2 + " " + eventArgs.Beacon.Id3 + " " + eventArgs.EventType);
            if (IsInitialized && eventArgs.EventType != BeaconEventType.None)
            {
                UnresolvedActionCount++;
                Resolver.CreateRequest(eventArgs);
                await _eventHistory.SaveBeaconEventAsync(eventArgs);
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
                if (delayedActionData.dueTime < DateTimeOffset.Now.AddSeconds((double) DelayedActionExecutionTimeframeInSeconds))
                {
                    // Time to execute
                    await ExecuteActionAsync(delayedActionData.resolvedAction, delayedActionData.beaconPid, delayedActionData.eventTypeDetectedByDevice);
                    await ServiceManager.StorageService.SetDelayedActionAsExecuted(delayedActionData.Id);
                }
                else if (delayedActionData.dueTime < nearestDueTime)
                {
                    nearestDueTime = delayedActionData.dueTime;
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
        /// <param name="resolvedAction"></param>
        /// <param name="beaconPid"></param>
        /// <param name="beaconEventType"></param>
        private async Task ExecuteActionAsync(ResolvedAction resolvedAction, string beaconPid, BeaconEventType beaconEventType)
        {
            logger.Debug("SDKEngine: ExecuteActionAsync " + beaconPid + " BeaconEventType: " + beaconEventType);
            bool checkOnlyOnce = await _eventHistory.CheckSendOnlyOnceAsync(resolvedAction);
            bool shouldSupress = await _eventHistory.ShouldSupressAsync(resolvedAction);

            logger.Trace("SDKEngine: ExecuteActionAsync " + beaconPid + " checkOnlyOnce: " + checkOnlyOnce + " shouldSupress:" + shouldSupress);
            if (!shouldSupress && !checkOnlyOnce && resolvedAction.IsInsideTimeframes(DateTimeOffset.Now))
            {
                logger.Trace("SDKEngine: ExecuteActionAsync " + beaconPid + " action resolved");
                await _eventHistory.SaveExecutedResolvedActionAsync(resolvedAction.BeaconAction, beaconPid, beaconEventType);

                BeaconActionResolved?.Invoke(this, resolvedAction.BeaconAction);
            }
            else
            {
                logger.Trace("SDKEngine: ExecuteActionAsync " + beaconPid + " action not resolved");
            }
        }

        /// <summary>
        /// (Re)sets the process delayed actions timer to trigger based on the given due time.
        /// </summary>
        /// <param name="nextDueTime">Time when the timer should timeout.</param>
        private void ResetProcessDelayedActionsTimer(DateTimeOffset nextDueTime)
        {
            if (_processDelayedActionsTimer != null)
            {
                _processDelayedActionsTimer.Dispose();
            }

            int millisecondsToNextProcessingOfDelayedActions = (int) nextDueTime.Subtract(DateTimeOffset.Now).TotalMilliseconds;

            logger.Debug("SDKManager.ResetProcessDelayedActionsTimer(): "
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
            if (SDKData.Instance.DatabaseCleaningTime < DateTimeOffset.Now.AddHours(-DatabaseExpirationInHours))
            {
                SDKData.Instance.DatabaseCleaningTime = DateTimeOffset.Now;
                await ServiceManager.StorageService.CleanDatabase();
            }
        }

        #region Private event handlers

        private async void OnBeaconActionResolvedAsync(object sender, ResolvedActionsEventArgs e)
        {
            if (e == null)
            {
                return;
            }
            logger.Debug("SDKEngine: OnBeaconActionResolvedAsync " + e.RequestID + " BeaconEventType:" + e.BeaconEventType);
            //TODO verify event needed?
            if (e.ResolvedActions.Count > 0 && BeaconActionResolved != null)
            {
                foreach (ResolvedAction action in e.ResolvedActions)
                {
                    if (action.Delay > 0 && action.ReportImmediately == false)
                    {
                        logger.Debug("SDKEngine: OnBeaconActionResolvedAsync " + e.RequestID + " delay");
                        // Delay action execution
                        DateTimeOffset dueTime = DateTimeOffset.Now.AddSeconds((int) action.Delay);

                        await ServiceManager.StorageService.SaveDelayedAction(action, dueTime, e.BeaconPid, action.EventTypeDetectedByDevice);

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
                        logger.Debug("SDKEngine: OnBeaconActionResolvedAsync/ExecuteActionAsync " + e.RequestID + " -> Beacon Pid " + e.BeaconPid);
                        // Execute action immediately
                        await ExecuteActionAsync(action, e.BeaconPid, e.BeaconEventType);
                    }
                }
            }
            UnresolvedActionCount--;
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

        public async void OnWorkerTimerActivated(object state)
        {
//            await ServiceManager.StorageService.get
        }
       /* /// <summary>
        /// Checks, if there are pending beacon actions resolved by the background task.
        /// This callback is called only when the application is on foreground.
        /// </summary>
        /// <param name="state"></param>
        public async void OnCheckActionsResolvedByBackground(object state)
        {
            if (SDKData.Instance.NewActionsFromBackground)
            {
                SDKData.Instance.NewActionsFromBackground = false;
                IList<BeaconAction> list = await ServiceManager.StorageService.GetBeaconActionsFromBackground();

                foreach (var beaconAction in list)
                {
                    if (BeaconActionResolved != null)
                    {
                        BeaconActionResolved(this, beaconAction);
                    }
                }
            }
        }*/

        private async void OnProcessDelayedActionsTimerTimeoutAsync(object state)
        {
            await ProcessDelayedActionsAsync();
        }

        private async void OnFlushHistoryTimerTimeoutAsync(object state)
        {
            logger.Debug("History flushed.");
            await _eventHistory.FlushHistoryAsync();
        }

        private async void OnLayoutUpdatedAsync(object state)
        {
            await UpdateCacheAsync(true);
        }

        private void OnUpdateVisibilityTimerTimeout(object state)
        {
            SDKData.Instance.AppIsVisible = SDKData.Instance.AppIsVisible;
        }

        #endregion

        public async Task FlushHistory()
        {
            await _eventHistory.FlushHistoryAsync();
        }
    }
}