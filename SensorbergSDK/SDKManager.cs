using SensorbergSDK.Internal;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using MetroLog;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;

namespace SensorbergSDK
{
    /// <summary>
    /// The main interface of Sensorberg SDK.
    /// </summary>
    public sealed class SDKManager
    {
        private static ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<SDKManager>();
        public static readonly string DemoApiKey = Constants.DemoApiKey;
        private readonly int StartScannerIntervalInMilliseconds = 2000;
        private AppSettings _appSettings;
        private static SDKManager _instance;

        private readonly BackgroundTaskManager _backgroundTaskManager;
        private Timer _startScannerTimer;
        private bool _scannerShouldBeRunning;


        /// <summary>
        /// Fired when a beacon action has been successfully resolved and is ready to be exeuted.
        /// </summary>
        public event EventHandler<BeaconAction> BeaconActionResolved
        {
            add
            {
                SdkEngine.BeaconActionResolved += value;
                _backgroundTaskManager.BackgroundBeaconActionResolved += value;
            }
            remove
            {
                SdkEngine.BeaconActionResolved -= value;
                _backgroundTaskManager.BackgroundBeaconActionResolved -= value;
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
                SdkEngine.FailedToResolveBeaconAction += value;
            }
            remove
            {
                SdkEngine.FailedToResolveBeaconAction -= value;
            }
        }

        /// <summary>
        /// Fired, when the layout becomes valid/invalid.
        /// </summary>
        public event EventHandler<bool> LayoutValidityChanged
        {
            add
            {
                SdkEngine.LayoutValidityChanged += value;
            }
            remove
            {
                SdkEngine.LayoutValidityChanged -= value;
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

        /// <summary>
        /// Instance of the SDKEngine.
        /// </summary>
        private SDKEngine SdkEngine
        {
            [DebuggerStepThrough]
            get;
        }
        /// <summary>
        /// The scanner instance.
        /// </summary>
        public IBeaconScanner Scanner
        {
            [DebuggerStepThrough]
            get
            {
                return ServiceManager.BeaconScanner;
            }
        }

        /// <summary>
        /// Current configuration of the sdk.
        /// </summary>
        public SdkConfiguration Configuration { get; set; }

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
            [DebuggerStepThrough]
            get
            {
                return SdkEngine.IsInitialized;
            }
        }

        /// <summary>
        /// Property for checking if the background task is enabled (allowed to be registered).
        /// By registering or unregistering the background task, you change this value.
        /// </summary>
        public bool IsBackgroundTaskEnabled
        {
            [DebuggerStepThrough]
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
            [DebuggerStepThrough]
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
            [DebuggerStepThrough]
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
            [DebuggerStepThrough]
            get
            {
                return ServiceManager.LayoutManager.IsLayoutValid;
            }
        }

        public AppSettings DefaultAppSettings
        {
            [DebuggerStepThrough]
            get { return SdkEngine.DefaultAppSettings; }
            [DebuggerStepThrough]
            set { SdkEngine.DefaultAppSettings = value; }
        }

        public static string UserId
        {
            [DebuggerStepThrough]
            get
            {
                Instance();
                return _instance.SdkEngine.UserId;
            }
            [DebuggerStepThrough]
            set
            {
                Instance();
                _instance.SdkEngine.UserId = value;
            }
        }

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The singleton instance of this class.</returns>
        [Obsolete("Use new version without parameters")]
		public static SDKManager Instance(UInt16 manufacturerId, UInt16 beaconCode)
        {
            Instance();
            _instance.Configuration.ManufacturerId = manufacturerId;
            _instance.Configuration.BeaconCode = beaconCode;

            return _instance;
        }

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID of beacons to watch.</param>
        /// <param name="beaconCode">The beacon code of beacons to watch.</param>
        /// <returns>The singleton instance of this class.</returns>
        public static SDKManager Instance()
        {
            logger.Debug("Instance");
            if (_instance == null)
            {
                _instance = new SDKManager();
            }
            if (_instance.Configuration == null)
            {
                _instance.Configuration = new SdkConfiguration();
            }
            return _instance;
        }

        /// <summary>
        /// Uninitialize the complete SDK.
        /// </summary>
        public static void Dispose()
        {
            ServiceManager.BeaconScanner.StopWatcher();
            _instance?.UnregisterBackgroundTask();
            _instance?.SdkEngine.Dispose();
            _instance = null;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private SDKManager()
        {
            SdkEngine = new SDKEngine(true);
            _backgroundTaskManager = new BackgroundTaskManager();
            _backgroundTaskManager.RegisterOnProgressEventHandler();
        }

        /// <summary>
        /// Utility method for launching bluetooth settings on device.
        /// </summary>
        public async Task LaunchBluetoothSettingsAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
        }


        /// <summary>
        /// Initializes the SDK using the given configuration. The scanner can be used separately, but
        /// the resolving beacon actions cannot be done unless the SDK is initialized.
        /// If background task is enabled, this method check if there are updates for the
        /// background task filters available and updates them if so.
        /// </summary>
        public async Task InitializeAsync(SdkConfiguration configuration)
        {
            await InitializeInternal(configuration);
        }

        /// <summary>
        /// Initializes the SDK using the given API key. The scanner can be used separately, but
        /// the resolving beacon actions cannot be done unless the SDK is initialized.
        /// If background task is enabled, this method check if there are updates for the
        /// background task filters available and updates them if so.
        /// </summary>
        /// <param name="apiKey">The API key for the Sensorberg service.</param>
        /// <param name="timerClassName">Full class name of the timer background process, if needed.</param>
        /// <param name="advertisementClassName">Full class name of the advertisement background process, if needed.</param>
        /// <param name="uuidSpace">UUID space for the background task, default value is Constants.SensorbergUuidSpace.</param>
        /// <param name="startScanning">Start the background scanner</param>
        [Obsolete("The new method should be used")]
        public async Task InitializeAsync(string apiKey, string timerClassName = null, string advertisementClassName = null, string uuidSpace = Constants.SensorbergUuidSpace,
            bool startScanning = true)
        {
            await InitializeInternal(new SdkConfiguration()
            {
                ApiKey = apiKey,
                BackgroundTimerClassName = timerClassName,
                BackgroundAdvertisementClassName = advertisementClassName,
                BackgroundBeaconUuidSpace = uuidSpace,
                AutoStartScanner = startScanning
            });
        }

        private async Task InitializeInternal(SdkConfiguration configuration)
        {
            logger.Debug("InitializeAsync");
            Configuration = configuration;
            SDKData sdkData = SDKData.Instance;

            if (!IsInitialized)
            {
                sdkData.ApiKey = configuration.ApiKey;
                await SdkEngine.InitializeAsync();
                await InitializeSettingsAsync();
            }

            if (sdkData.BackgroundTaskEnabled)
            {
                logger.Debug("InitializeAsync#InitializeBackgground");
                await UpdateBackgroundTaskIfNeededAsync();
            }

            if (configuration.AutoStartScanner)
            {
                StartScanner();
            }
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

            if (_scannerShouldBeRunning)
            {
                StopScanner();
                StartScanner();
            }
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

                SdkEngine.Dispose();
            }
            Dispose();
        }

        /// <summary>
        /// Registers the background task or in the case of a pending background filter update,
        /// re-registers the task.
        /// </summary>
        /// <returns>The registration result.</returns>
        public async Task<BackgroundTaskRegistrationResult> RegisterBackgroundTaskAsync()
        {
            SDKData.Instance.BackgroundTaskEnabled = true;
            return await _backgroundTaskManager.RegisterBackgroundTaskAsync(Configuration);
        }

        public async Task<BackgroundTaskRegistrationResult> UpdateBackgroundTaskIfNeededAsync()
        {
            BackgroundTaskRegistrationResult result = new BackgroundTaskRegistrationResult()
            {
                Success = true,
                Exception = null
            };

            if (BackgroundTaskManager.CheckIfBackgroundFilterUpdateIsRequired())
            {
                result = await _backgroundTaskManager.UpdateBackgroundTaskAsync(Configuration);
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
                        Scanner.StartWatcher(Configuration.ManufacturerId, Configuration.BeaconCode, _appSettings.BeaconExitTimeout, _appSettings.RssiEnterThreshold, _appSettings.EnterDistanceThreshold);
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
        /// Invalidates the current layout cache.
        /// </summary>
        /// <returns></returns>
        public async Task InvalidateCacheAsync()
        {
            await ServiceManager.LayoutManager.InvalidateLayout();
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
                await SdkEngine.ResolveBeaconAction(e);
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
                _appSettings = await ServiceManager.SettingsManager.GetSettings();
                ServiceManager.SettingsManager.SettingsUpdated += OnSettingsUpdated;
            }
        }
        /// <summary>
        /// Sends all pending history elements.
        /// </summary>
        public async Task FlushHistory()
        {
            await SdkEngine.FlushHistory();
        }
    }
}
