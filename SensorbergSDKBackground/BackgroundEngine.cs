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

namespace SensorbergSDKBackground
{
    /// <summary>
    /// BackgroundEngine resolves actions from BluetoothLEAdvertisementWatcherTriggerDetails object
    /// and resolves delayed actions. This is not part of the public API. Making modifications into
    /// background tasks is not required in order to use the SDK.
    /// </summary>
    class BackgroundEngine
    {
        private const int ExitEventDelayInSeconds = 13;
        private const int KillTimerDelayInMilliseconds = 200;

        public event EventHandler<int> Finished;

        private SDKEngine _sdkEngine;
        private BackgroundTaskDeferral _deferral;
        private IBackgroundTaskInstance _backgroundTaskInstance;
        private IList<Beacon> _beacons;
        private IList<BeaconEventArgs> _beaconArgs;
        private Timer _killTimer;
        private int _unsolvedCounter;
        private bool _newActionsFromBackground;
        private bool _readyToFinish = false;
        private int _finishingRounds = 5;

        public BackgroundEngine()
        {
            _sdkEngine = new SDKEngine(false);
            _beacons = new List<Beacon>();
            _beaconArgs = new List<BeaconEventArgs>();
            _sdkEngine.Resolver.RequestQueueCountChanged += OnRequestQueueCountChanged;
            _sdkEngine.BeaconActionResolved += OnBeaconActionResolvedAsync;
        }

        /// <summary>
        /// Initializes BackgroundEngine
        /// </summary>
        public async Task InitializeAsync(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            _backgroundTaskInstance = taskInstance;
            await _sdkEngine.InitializeAsync();

            if (BackgroundTaskManager.CheckIfBackgroundFilterUpdateIsRequired())
            {
                ToastNotification toastNotification =
                    NotificationUtils.CreateToastNotification(
                        "New beacon signature available", "Launch the application to update");
                NotificationUtils.ShowToastNotification(toastNotification);
            }
        }

        /// <summary>
        /// Resolves beacons, which triggered the background task.
        /// </summary>
        public async Task ResolveBeaconActionsAsync()
        {
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
                        await _sdkEngine.ResolveBeaconAction(beaconArg);
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
            await _sdkEngine.ProcessDelayedActionsAsync();
            Finish();
        }

        /// <summary>
        /// Finishes background processing and releases all resources
        /// </summary>
        /// <param name="triggerDetails"></param>
        private void Finish()
        {
            if (_killTimer != null)
            {
                _killTimer.Dispose();
                _killTimer = null;
            }

            if (Finished != null)
            {
                Finished(this, 0);
            }

            if (_newActionsFromBackground)
            {
                // Signal the main app that new actions have been resolved on background
                SDKData.Instance.NewActionsFromBackground = true;
            }

            _sdkEngine.Resolver.RequestQueueCountChanged -= OnRequestQueueCountChanged;
            _sdkEngine.BeaconActionResolved -= OnBeaconActionResolvedAsync;

            _sdkEngine.Deinitialize();
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
            foreach (var beacon in _beacons)
            {
                IList<DBBackgroundEventsHistory> history = await Storage.Instance.GetBeaconBackgroundEventsHistory(beacon.Pid);

                if (history.Count == 0)
                {
                    // No history for this beacon. Let's save it and add it to event args array for solving.
                    AddBeaconArgs(beacon, BeaconEventType.Enter);
                    await Storage.Instance.SaveBeaconBackgroundEvent(beacon.Pid, BeaconEventType.Enter);
#if LOUD_DEBUG
                    ToastNotification toastNotification = NotificationUtils.CreateToastNotification("Enter Beacon", _beacons[0].Id1 + " " + _beacons[0].BeaconId2 + " " + _beacons[0].BeaconId3);
                    NotificationUtils.ShowToastNotification(toastNotification);
#endif
                }
                else if (history.Count == 1)
                {
                    if (history[0].EventType == (int)BeaconEventType.Enter)
                    {
                        if (beacon.RawSignalStrengthInDBm == filter.OutOfRangeThresholdInDBm)
                        {
                            // Exit event
                            AddBeaconArgs(beacon, BeaconEventType.Exit);
                            await Storage.Instance.DeleteBackgroundEventAsync(beacon.Pid);
#if LOUD_DEBUG
                            ToastNotification toastNotification = NotificationUtils.CreateToastNotification("Exit Beacon", _beacons[0].Id1 + " " + _beacons[0].BeaconId2 + " " + _beacons[0].BeaconId3);
                            NotificationUtils.ShowToastNotification(toastNotification);
#endif
                        }
                    }
                }
            }
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
            System.Diagnostics.Debug.WriteLine("BackgroundEngine.OnRequestQueueCountChanged(): " + e);

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
        private async void OnBeaconActionResolvedAsync(object sender, BeaconAction beaconAction)
        {
            System.Diagnostics.Debug.WriteLine("BackgroundEngine.OnBeaconActionResolvedAsync()");
            await Storage.Instance.SaveBeaconActionFromBackgroundAsync(beaconAction);
            _newActionsFromBackground = true;

            if (SDKData.Instance.ShowNotificationsOnBackground())
            {
                // Toast notifications are shown only if the app is not visible
                ToastNotification toastNotification = beaconAction.ToToastNotification();
                NotificationUtils.ShowToastNotification(toastNotification);
            }
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
            if (_sdkEngine.UnresolvedActionCount <= 0 || _finishingRounds-- <= 0)
            {
                // Signals that we are ready to finish. Waits one more cycle to ensure everything
                // has been finished.
                _readyToFinish = true;
            }
        }
    }
}
