using SensorbergSDK;
using System;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;

namespace SensorbergSimpleApp
{
    /// <summary>
    /// Application main page.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /*
         * Insert your API key for Sensorberg service and the manufacturer ID and beacon code for
         * filtering beacons below.
         */
        private const string ApiKey = "af24473d3ccb1d7a34307747531f06c25f08de361a5349389bbbe39274bf08cd";
        private const ushort ManufacturerId = 0x004c;
        private const ushort BeaconCode = 0x0215;

        private const string KeyIsBackgroundTaskEnabled = "background_task_enabled";
        private SDKManager _sdkManager;
        private IAsyncOperation<IUICommand> _bluetoothNotOnDialogOperation;

        private ObservableCollection<LogEntryItem> LogEntryItemCollection
        {
            get
            {
                return (ObservableCollection<LogEntryItem>)GetValue(LogEntryItemCollectionProperty);
            }
            set
            {
                SetValue(LogEntryItemCollectionProperty, value);
            }
        }
        public static readonly DependencyProperty LogEntryItemCollectionProperty =
            DependencyProperty.Register("LogEntryItemCollection", typeof(ObservableCollection<LogEntryItem>), typeof(MainPage),
                new PropertyMetadata(null));

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            LogEntryItemCollection = new ObservableCollection<LogEntryItem>();

            _sdkManager = SDKManager.Instance(ManufacturerId, BeaconCode);
            _sdkManager.ScannerStatusChanged += OnScannerStatusChangedAsync;
            _sdkManager.BeaconActionResolved += OnBeaconActionResolvedAsync;

            // Listening to the following events is not necessary, but provides interesting data for our log
            _sdkManager.Scanner.BeaconEvent += OnBeaconEventAsync;
            _sdkManager.FailedToResolveBeaconAction += OnFailedToResolveBeaconActionAsync;

            Window.Current.VisibilityChanged += _sdkManager.OnApplicationVisibilityChanged;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            BeaconAction pendingBeaconAction = BeaconAction.FromNavigationEventArgs(e);

            if (pendingBeaconAction != null)
            {
                if (await pendingBeaconAction.LaunchWebBrowserAsync())
                {
                    Application.Current.Exit();
                }
                else
                {
                    OnBeaconActionResolvedAsync(this, pendingBeaconAction);
                }
            }

            _sdkManager.InitializeAsync(ApiKey);

            bool backgroundTaskEnabledAndRegistered =
                (_sdkManager.IsBackgroundTaskEnabled && _sdkManager.IsBackgroundTaskRegistered);

            AddLogEntry(backgroundTaskEnabledAndRegistered ? "Background task is registered." : "Background task is not registerd");
            toggleBackgroundTaskButton.Label = backgroundTaskEnabledAndRegistered ? "turn off notifications" : "turn on notifications";

            if (!_sdkManager.IsBackgroundTaskEnabled)
            {
                MessageDialog messageDialog = new MessageDialog(
                    "Would you like to receive beacon notifications, when the application is not running?",
                    "Enable notifications");

                messageDialog.Commands.Add(new UICommand("Yes",
                    new UICommandInvokedHandler((command) =>
                    {
                        SetBackgroundTaskEnabledAsync(true);
                    })));

                messageDialog.Commands.Add(new UICommand("No"));

                await messageDialog.ShowAsync();
            }
        }

        private async void SetBackgroundTaskEnabledAsync(bool enabled)
        {
            if (_sdkManager.IsBackgroundTaskEnabled != enabled)
            {
                if (enabled)
                {
                    BackgroundTaskRegistrationResult result = await _sdkManager.RegisterBackgroundTaskAsync();

                    if (result.Success)
                    {
                        toggleBackgroundTaskButton.Label = "turn off notifications";
                        AddLogEntry("Background task is registered.");
                    }
                    else
                    {
                        AddLogEntry("Background task registration failed: " + result.Exception.Message);
                    }
                }
                else
                {
                    _sdkManager.UnregisterBackgroundTask();
                    toggleBackgroundTaskButton.Label = "turn on notifications";
                    AddLogEntry("Background task is not registered.");
                }  
            }
        }

        private void AddLogEntry(string message)
        {
            LogEntryItem logEntryItem = new LogEntryItem(message);
            LogEntryItemCollection.Insert(0, logEntryItem);
        }

        /// <summary>
        /// Called when the status of the scanner is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnScannerStatusChangedAsync(object sender, ScannerStatus e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AddLogEntry("Scanner status changed: " + e);

                switch (e)
                {
                    case ScannerStatus.Stopped:
                        break;

                    case ScannerStatus.Started:
                        if (_bluetoothNotOnDialogOperation != null)
                        {
                            _bluetoothNotOnDialogOperation.Cancel();
                            _bluetoothNotOnDialogOperation = null;
                        }

                        break;

                    case ScannerStatus.Aborted:
                        if (_bluetoothNotOnDialogOperation == null)
                        {
                            MessageDialog messageDialog = new MessageDialog(
                                "Do you wish to enable Bluetooth on this device?",
                                "Failed to start Bluetooth LE advertisement watcher");

                            messageDialog.Commands.Add(new UICommand("Yes",
                                new UICommandInvokedHandler((command) =>
                                {
                                    _sdkManager.LaunchBluetoothSettingsAsync();
                                })));

                            messageDialog.Commands.Add(new UICommand("No",
                                new UICommandInvokedHandler((command) =>
                                {
                                    _sdkManager.StopScanner();
                                })));

                            _bluetoothNotOnDialogOperation = messageDialog.ShowAsync();
                        }

                        break;
                }
            });
        }

        /// <summary>
        /// Called when the scanner detects a beacon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnBeaconEventAsync(object sender, BeaconEventArgs e)
        {
            if (e.EventType != BeaconEventType.None)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                    {
                        AddLogEntry("Received event '" + e.EventType + "' from beacon " + e.Beacon.ToString());
                    });
            }
        }

        /// <summary>
        /// Handles incoming beacon actions, when the application is running.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnBeaconActionResolvedAsync(object sender, BeaconAction e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, async () =>
                {
                    string logEntryMessage = "Beacon action resolved:"
                        + "\n- ID: " + e.Id
                        + "\n- " + (string.IsNullOrEmpty(e.Subject) ? "(No subject)" : "Subject: " + e.Subject)
                        + "\n- " + (string.IsNullOrEmpty(e.Body) ? "(No body)" : "Body: " + e.Body)
                        + "\n- " + (string.IsNullOrEmpty(e.Url) ? "(No URL)" : "URL: " + e.Url);
                    AddLogEntry(logEntryMessage);

                    MessageDialog messageDialog = e.ToMessageDialog();

                    switch (e.Type)
                    {
                        case BeaconActionType.UrlMessage:
                        case BeaconActionType.VisitWebsite:
                            messageDialog.Commands.Add(new UICommand("Yes",
                                new UICommandInvokedHandler(async (command) =>
                                {
                                    await Windows.System.Launcher.LaunchUriAsync(new Uri(e.Url));
                                })));

                            messageDialog.Commands.Add(new UICommand("No")); 
                            break;

                        case BeaconActionType.InApp:
                            break;
                    }

                    await messageDialog.ShowAsync();
                });
        }

        /// <summary>
        /// Normally we would not care about this, but for demonstrative purposes lets log these
        /// events too.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnFailedToResolveBeaconActionAsync(object sender, string e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    AddLogEntry("Could not resolve action for beacon: " + e);
                });
        }

        /// <summary>
        /// Enables/disables the background task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnToggleBackgroundTaskButtonClicked(object sender, RoutedEventArgs e)
        {
            SetBackgroundTaskEnabledAsync(!_sdkManager.IsBackgroundTaskEnabled);
        }

        /// <summary>
        /// Contains data of a single log entry.
        /// </summary>
        public class LogEntryItem
        {
            public string Timestamp
            {
                get;
                private set;
            }

            public string Message
            {
                get;
                set;
            }

            public LogEntryItem(string message)
            {
                Timestamp = string.Format("{0:H:mm:ss}", DateTime.Now);
                Message = message;
            }
        }
    }
}
