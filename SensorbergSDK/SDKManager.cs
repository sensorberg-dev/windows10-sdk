using SensorbergSDK.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK
{
    /// <summary>
    /// The main interface of Sensorberg SDK.
    /// </summary>
    public sealed class SDKManager
    {
        public static readonly string DemoApiKey = Constants.DemoApiKey;
        private int StartScannerIntervalInMilliseconds = 2000;
        private AppSettings _appSettings;

        /// <summary>
        /// Fired when a beacon action has been successfully resolved and is ready to be exeuted.
        /// </summary>
        public event EventHandler<BeaconAction> BeaconActionResolved
        {
            add
            {
                _sdkEngine.BeaconActionResolved += value;
            }
            remove
            {
                _sdkEngine.BeaconActionResolved -= value;
            }
        }

        /// <summary>
        /// This event is fired, when a beacon actions could not be resolved.
        /// In most cases this event can be ignored.
        /// </summary>
        public event EventHandler<string> FailedToResolveBeaconAction
        {
            add
            {
                _sdkEngine.FailedToResolveBeaconAction += value;
            }
            remove
            {
                _sdkEngine.FailedToResolveBeaconAction -= value;
            }
        }

        /// <summary>
        /// Fired, when the layout becomes valid/invalid.
        /// </summary>
        public event EventHandler<bool> LayoutValidityChanged
        {
            add
            {
                _sdkEngine.LayoutValidityChanged += value;
            }
            remove
            {
                _sdkEngine.LayoutValidityChanged -= value;
            }
        }

        /// <summary>
        /// Triggered when the scanner is either started, stopped or aborted.
        /// Aborted status may indicate that the bluetooth has not been turned on on the device.
        /// </summary>
        public event EventHandler<ScannerStatus> ScannerStatusChanged
        {
            add
            {
                Scanner.StatusChanged += value;
            }
            remove
            {
                Scanner.StatusChanged -= value;
            }
        }

        public event EventHandler BackgroundFiltersUpdated
        {
            add
            {
                _backgroundTaskManager.BackgroundFiltersUpdated += value;
            }
            remove
            {
                _backgroundTaskManager.BackgroundFiltersUpdated -= value;
            }
        }

        private static SDKManager _instance;
        internal static SDKManager InternalInstance { get { return _instance; } }
        private SDKEngine _sdkEngine;
        private BackgroundTaskManager _backgroundTaskManager;
        private Timer _startScannerTimer;
        private bool _scannerShouldBeRunning;

        /// <summary>
        /// The scanner instance.
        /// </summary>
        public Scanner Scanner
        {
            get
            {
                return Scanner.Instance;
            }
        }

        /// <summary>
        /// The API key for the Sensorberg service.
        /// The recommended way is to set the API key when initializing the SDK.
        /// </summary>
        public string ApiKey
        {
            get
            {
                return SDKData.Instance.ApiKey;
            }
            set
            {
                SDKData.Instance.ApiKey = value;
            }
        }

        /// <summary>
        /// The manufacturer ID to filter beacons that are being watched.
        /// </summary>
        public UInt16 ManufacturerId
        {
            get;
            private set;
        }

        /// <summary>
        /// The beacon code to filter beacons that are being watched.
        /// </summary>
        public UInt16 BeaconCode
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates whether the SDK is initialized and ready to function or not.
        /// 
        /// The scanner will work even if the SDK has not been initialized. However, the resolver
        /// requires a valid API key to generate proper requests to server.
        /// 
        /// Sensorberg SDK is initialized by calling the Initialize method with a valid API key.
        /// </summary>
		public bool IsInitialized
        {
            get
            {
                return _sdkEngine.IsInitialized;
            }
        }

        /// <summary>
        /// Property for checking if the background task is enabled (allowed to be registered).
        /// By registering or unregistering the background task, you change this value.
        /// </summary>
        public bool IsBackgroundTaskEnabled
        {
            get
            {
                return SDKData.Instance.BackgroundTaskEnabled;
            }
        }

        /// <summary>
        /// Property for checking whether the background task is registered or not.
        /// </summary>
        public bool IsBackgroundTaskRegistered
        {
            get
            {
                return (_backgroundTaskManager != null && _backgroundTaskManager.IsBackgroundTaskRegistered);
            }
        }

        /// <summary>
        /// True, if the scanner is running. False otherwise.
        /// </summary>
		public bool IsScannerStarted
        {
            get
            {
                return (Scanner.Status == ScannerStatus.Started);
            }
        }

        /// <summary>
        /// True, if a layout has been retrieved and is valid.
        /// </summary>
        public bool IsLayoutValid
        {
            get
            {
                return _sdkEngine.LayoutManager.IsLayoutValid;
            }
        }

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The singleton instance of this class.</returns>
		public static SDKManager Instance(UInt16 manufacturerId, UInt16 beaconCode)
        {
            if (_instance == null)
            {
                _instance = new SDKManager();
            }

            _instance.ManufacturerId = manufacturerId;
            _instance.BeaconCode = beaconCode;

            return _instance;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private SDKManager()
        {
            _sdkEngine = new SDKEngine(true);
            _backgroundTaskManager = new BackgroundTaskManager();
        }

        /// <summary>
        /// Utility method for launching bluetooth settings on device.
        /// </summary>
        public async void LaunchBluetoothSettingsAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
        }

        /// <summary>
        /// Initializes the SDK using the given API key. The scanner can be used separately, but
        /// the resolving beacon actions cannot be done unless the SDK is initialized.
        /// 
        /// If background task is enabled, this method check if there are updates for the
        /// background task filters available and updates them if so.
        /// </summary>
        /// <param name="apiKey">The API key for the Sensorberg service.</param>
        public async Task InitializeAsync(string apiKey)
        {
            SDKData sdkData = SDKData.Instance;

            if (!IsInitialized)
            {
                sdkData.ApiKey = apiKey;
                await _sdkEngine.InitializeAsync();
                await InitializeSettingsAsync();
            }

            if (sdkData.BackgroundTaskEnabled)
            {
                await UpdateBackgroundTaskIfNeededAsync();
            }

            StartScanner();
        }

        private void OnSettingsUpdated(object sender, SettingsEventArgs settingsEventArgs)
        {
            var oldTimeout = _appSettings.BeaconExitTimeout;
            var oldRssiThreshold = _appSettings.RssiEnterThreshold;
            var oldDistanceThreshold = _appSettings.EnterDistanceThreshold;

            _appSettings = settingsEventArgs.Settings;

            bool settingsAreTheSame = _appSettings.BeaconExitTimeout == oldTimeout && _appSettings.RssiEnterThreshold == oldRssiThreshold && _appSettings.EnterDistanceThreshold == oldDistanceThreshold;

            if (settingsAreTheSame)
            {
                return;
            }

            StopScanner();
            StartScanner();
        }

        /// <summary>
        /// De-initializes the SDK.
        /// </summary>
        /// <param name="stopScanner">If true, will stop scanner if running.</param>
        public void Deinitialize(bool stopScanner)
        {
            if (IsInitialized)
            {
                if (stopScanner)
                {
                    StopScanner();
                }

                _sdkEngine.Deinitialize();
            }
        }

        /// <summary>
        /// Registers the background task or in the case of a pending background filter update,
        /// re-registers the task.
        /// </summary>
        /// <returns>The registration result.</returns>
        public async Task<BackgroundTaskRegistrationResult> RegisterBackgroundTaskAsync()
        {
            SDKData.Instance.BackgroundTaskEnabled = true;
            return await _backgroundTaskManager.RegisterBackgroundTaskAsync(ManufacturerId, BeaconCode);
        }

        public async Task<BackgroundTaskRegistrationResult> UpdateBackgroundTaskIfNeededAsync()
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                success = true,
                exception = null
            };

            if (BackgroundTaskManager.CheckIfBackgroundFilterUpdateIsRequired())
            {
                result = await _backgroundTaskManager.UpdateBackgroundTaskAsync(ManufacturerId, BeaconCode);
            }

            SDKData.Instance.BackgroundTaskEnabled = true;
            return result;
        }

        /// <summary>
        /// Unregisters the background task.
        /// </summary>
        public void UnregisterBackgroundTask()
        {
            _backgroundTaskManager.UnregisterBackgroundTask();
            SDKData.Instance.BackgroundTaskEnabled = false;
        }

        /// <summary>
        /// Starts the scanner and starts to listen to beacon events.
        /// </summary>
        public void StartScanner()
        {
            Scanner.StatusChanged += OnScannerStatusChanged;

            if (Scanner.Status != ScannerStatus.Started)
            {
                _scannerShouldBeRunning = true;
                Scanner.BeaconEvent += OnBeaconEventAsync;
                InitializeSettingsAsync().ContinueWith(task =>
                    {
                        Scanner.StartWatcher(ManufacturerId, BeaconCode, _appSettings.BeaconExitTimeout, _appSettings.RssiEnterThreshold, _appSettings.EnterDistanceThreshold);
                    });
            }
        }

        /// <summary>
        /// Stops the scanner and stops listening to beacon events.
        /// </summary>
        public void StopScanner()
        {
            _scannerShouldBeRunning = false;
            Scanner.StatusChanged -= OnScannerStatusChanged;

            if (Scanner.Status == ScannerStatus.Started)
            {
                Scanner.BeaconEvent -= OnBeaconEventAsync;
                Scanner.StopWatcher();
            }
        }

        /// <summary>
        /// Clears the pending beacon actions, which have been resolved by the background task.
        /// </summary>
        public void ClearPendingActions()
        {
            _sdkEngine.DismissPendingBeaconActionsResolvedOnBackgroundAsync();
        }

        /// <summary>
        /// Invalidates the current layout cache.
        /// </summary>
        /// <returns></returns>
        public IAsyncAction InvalidateCacheAsync()
        {
            Func<Task> action = async () =>
            {
                await _sdkEngine.LayoutManager.InvalidateLayoutAsync();
            };

            return action().AsAsyncAction();
        }

        /// <summary>
        /// Sets the SDK preferences based on the application visibility.
        /// Hook this event handler to Window.Current.VisibilityChanged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnApplicationVisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SDKManager.OnApplicationVisibilityChanged(): "
                + (e.Visible ? "To visible" : "To not visible"));
            SDKData.Instance.AppIsVisible = e.Visible;
        }

        /// <summary>
        /// Called, when scanner sends a beacon event. If the background task is registered,
        /// it will resolve the actions and we do nothing here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The beacon event.</param>
        private async void OnBeaconEventAsync(object sender, BeaconEventArgs e)
        {
            if (!IsBackgroundTaskRegistered)
            {
                await _sdkEngine.ResolveBeaconAction(e);
            }
        }

        private void OnScannerStatusChanged(object sender, ScannerStatus e)
        {
            if (_startScannerTimer != null)
            {
                _startScannerTimer.Dispose();
                _startScannerTimer = null;
            }

            if (e != ScannerStatus.Started)
            {
                Scanner.BeaconEvent -= OnBeaconEventAsync;

                if (_scannerShouldBeRunning)
                {
                    _startScannerTimer = new Timer(StartScannerTimerCallback, null, StartScannerIntervalInMilliseconds, Timeout.Infinite);
                }
            }
        }

        private void StartScannerTimerCallback(object state)
        {
            if (_startScannerTimer != null)
            {
                _startScannerTimer.Dispose();
                _startScannerTimer = null;
            }

            if (_scannerShouldBeRunning)
            {
                StartScanner();
            }
        }

        private async Task InitializeSettingsAsync()
        {
            if (_appSettings == null)
            {
                _appSettings = await SettingsManager.Instance.GetSettingsAsync();
                SettingsManager.Instance.SettingsUpdated += OnSettingsUpdated;
            }
        }
    }
}
