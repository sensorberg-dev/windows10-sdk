using SensorbergSDK;
using SensorbergSDK.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Background;
using Windows.UI.Notifications;
using MetroLog;
using SensorbergSDK.Data;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDKBackground
{
    /// <summary>
    /// BackgroundEngine resolves actions from BluetoothLEAdvertisementWatcherTriggerDetails object
    /// and resolves delayed actions. This is not part of the public API. Making modifications into
    /// background tasks is not required in order to use the SDK.
    /// </summary>
    public class BackgroundEngine : IDisposable
    {
        private static readonly ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<BackgroundEngine>();
        private const int ExitEventDelayInSeconds = 13;
        private const int KillTimerDelayInMilliseconds = 200;

        public event EventHandler<int> Finished;

        private SDKEngine SdkEngine { get; }
        private BackgroundTaskDeferral _deferral;
        private IBackgroundTaskInstance _backgroundTaskInstance;
        private readonly IList<Beacon> _beacons;
        private readonly IList<BeaconEventArgs> _beaconArgs;
        private Timer _killTimer;
        private int _unsolvedCounter;
        private bool _readyToFinish = false;
        private int _finishingRounds = 5;
        private AppSettings AppSettings { get; set; }

        public event EventHandler<BeaconAction> BeaconActionResolved
        {
            add { SdkEngine.BeaconActionResolved += value; }
            remove { SdkEngine.BeaconActionResolved -= value; }
        }

        public event EventHandler<string> FailedToResolveBeaconAction
        {
            add { SdkEngine.FailedToResolveBeaconAction += value; }
            remove { SdkEngine.FailedToResolveBeaconAction -= value; }
        }

        public event EventHandler<bool> LayoutValidityChanged
        {
            add { SdkEngine.LayoutValidityChanged += value; }
            remove { SdkEngine.LayoutValidityChanged -= value; }
        }

        public BackgroundEngine()
        {
            SdkEngine = new SDKEngine(false);
            _beacons = new List<Beacon>();
            _beaconArgs = new List<BeaconEventArgs>();
            SdkEngine.Resolver.RequestQueueCountChanged += OnRequestQueueCountChanged;
            SdkEngine.BeaconActionResolved += OnBeaconActionResolvedAsync;
        }

        /// <summary>
        /// Initializes BackgroundEngine
        /// </summary>
        public async Task InitializeAsync(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            _backgroundTaskInstance = taskInstance;
            await SdkEngine.InitializeAsync();
            AppSettings = await ServiceManager.SettingsManager.GetSettings();

            //TODO verfiy
            if (BackgroundTaskManager.CheckIfBackgroundFilterUpdateIsRequired())
            {
                ToastNotification toastNotification = NotificationUtils.CreateToastNotification("New beacon signature available", "Launch the application to update");
                NotificationUtils.ShowToastNotification(toastNotification);
            }
        }

        /// <summary>
        /// Resolves beacons, which triggered the background task.
        /// </summary>
        public async Task ResolveBeaconActionsAsync()
        {
            logger.Trace("ResolveBeaconActionsAsync");
            var triggerDetails = _backgroundTaskInstance.TriggerDetails as BluetoothLEAdvertisementWatcherTriggerDetails;

            if (triggerDetails != null)
            {
                TriggerDetailsToBeacons(triggerDetails);

                if (_beacons.Count > 0)
                {
                    await AddBeaconsToBeaconArgsAsync(triggerDetails.SignalStrengthFilter);
                }

                if (_beaconArgs.Count > 0)
                {
                    // Resolve new events
                    _unsolvedCounter = _beaconArgs.Count;

                    foreach (var beaconArg in _beaconArgs)
                    {
                        await SdkEngine.ResolveBeaconAction(beaconArg);
                    }
                }
                else
                {
                    Finish();
                } 
            }
        }
        
        /// <summary>
        /// Processes the delayed actions and executes them as necessary.
        /// </summary>
        public async Task ProcessDelayedActionsAsync()
        {
            await SdkEngine.ProcessDelayedActionsAsync();
            Finish();
        }

        /// <summary>
        /// Finishes background processing and releases all resources
        /// </summary>
        private void Finish()
        {
            if (_killTimer != null)
            {
                _killTimer.Dispose();
                _killTimer = null;
            }

            Finished?.Invoke(this, 0);

            SdkEngine.BeaconActionResolved -= OnBeaconActionResolvedAsync;

            SdkEngine.Deinitialize();
            _deferral.Complete();
        }

        /// <summary>
        /// Constructs Beacon instances from the trigger data and adds recognized beacons to the _beacons list
        /// </summary>
        /// <param name="triggerDetails"></param>
        private void TriggerDetailsToBeacons(BluetoothLEAdvertisementWatcherTriggerDetails triggerDetails)
        {
            if (triggerDetails != null)
            {
                foreach (var bleAdvertisementReceivedEventArgs in triggerDetails.Advertisements)
                {
                    Beacon beacon = BeaconFactory.BeaconFromBluetoothLEAdvertisementReceivedEventArgs(bleAdvertisementReceivedEventArgs);
                    _beacons.Add(beacon);
                }
            }
        }

        /// <summary>
        /// Generates BeaconArgs from beacon events.
        /// For instance if a beacon is seen for the first time, BeaconArgs with enter type is generated
        /// </summary>
        private async Task AddBeaconsToBeaconArgsAsync(BluetoothSignalStrengthFilter filter)
        {
            logger.Trace("AddBeaconsToBeaconArgsAsync");
            foreach (var beacon in _beacons)
            {
                BackgroundEvent history = await ServiceManager.StorageService.GetLastEventStateForBeacon(beacon.Pid);

                if (history == null || history.LastEvent == BeaconEventType.Exit ||
                    (!IsOutOfRange(filter, beacon) && history.EventTime.AddMilliseconds(AppSettings.BeaconExitTimeout) < DateTimeOffset.Now))
                {
                    // No history for this beacon. Let's save it and add it to event args array for solving.
                    AddBeaconArgs(beacon, BeaconEventType.Enter);
                    await ServiceManager.StorageService.SaveBeaconEventState(beacon.Pid, BeaconEventType.Enter);
#if LOUD_DEBUG
                    ToastNotification toastNotification = NotificationUtils.CreateToastNotification("Enter Beacon", _beacons[0].Id1 + " " + _beacons[0].BeaconId2 + " " + _beacons[0].BeaconId3);
                    NotificationUtils.ShowToastNotification(toastNotification);
#endif
                }
                else if (history.LastEvent == BeaconEventType.Enter)
                {
                    if (IsOutOfRange(filter, beacon))
                    {
                        // Exit event
                        AddBeaconArgs(beacon, BeaconEventType.Exit);
                        await ServiceManager.StorageService.SaveBeaconEventState(beacon.Pid, BeaconEventType.Exit);
#if LOUD_DEBUG
                            ToastNotification toastNotification = NotificationUtils.CreateToastNotification("Exit Beacon", _beacons[0].Id1 + " " + _beacons[0].BeaconId2 + " " + _beacons[0].BeaconId3);
                            NotificationUtils.ShowToastNotification(toastNotification);
#endif
                    }
                }
            }
        }

        private static bool IsOutOfRange(BluetoothSignalStrengthFilter filter, Beacon beacon)
        {
            return beacon.RawSignalStrengthInDBm == filter.OutOfRangeThresholdInDBm;
        }

        private void AddBeaconArgs(Beacon beacon, BeaconEventType eventType)
        {
            var args = new BeaconEventArgs();
            args.Beacon = beacon;
            args.EventType = eventType;
            _beaconArgs.Add(args);
        }

        /// <summary>
        /// Observers changes in the RequestQueue. When the queue is empty, kill timer is started which will finish background task
        /// </summary>
        private void OnRequestQueueCountChanged(object sender, int e)
        {
            logger.Trace("BackgroundEngine.OnRequestQueueCountChanged(): " + e);

            if (e > 0)
            {
                if (_killTimer != null)
                {
                    _killTimer.Dispose();
                    _killTimer = null;
                }
            }
            else
            {
                _killTimer = new Timer(OnKill, null, KillTimerDelayInMilliseconds, KillTimerDelayInMilliseconds);  
            }
        }

        /// <summary>
        /// Handles BeaconActions that are resolved in the SDKEngine.
        /// All resolved actions are stored into local database. And the UI app will show actions to the user.
        /// When the UI app is not running, toast notification is shown for the user.
        /// </summary>
        private void OnBeaconActionResolvedAsync(object sender, BeaconAction beaconAction)
        {
            logger.Trace("BackgroundEngine.OnBeaconActionResolvedAsync()");
        }

        private void OnKill(object state)
        {
            if (_readyToFinish)
            {
                Finish();
                return;
            }
            
            // Finish when there are no more unresolved actions or timer has been called 5 times
            // (1 second in total)
            if (SdkEngine.UnresolvedActionCount <= 0 || _finishingRounds-- <= 0)
            {
                // Signals that we are ready to finish. Waits one more cycle to ensure everything
                // has been finished.
                _readyToFinish = true;
            }
        }

        public void Dispose()
        {
            _killTimer?.Dispose();
        }
    }
}
