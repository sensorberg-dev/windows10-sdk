using SensorbergSDK;
using System;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.HockeyApp;

namespace SensorbergShowcase
{
	/// <summary>
	/// Construction and navigation related main page implementation reside here.
    /// We also manage the global main page components here such as the dialog
    /// control.
	/// </summary>
	public sealed partial class MainPage : Page
	{
        /*
         * Insert the manufacturer ID and beacon code for filtering beacons below.
         */
        private const UInt16 ManufacturerId = 0x004c;
        private const UInt16 BeaconCode = 0x0215;

        private SDKManager _sdkManager;
        private bool _appIsOnForeground;

        public bool IsBigScreen
        {
            get
            {
                return (bool)GetValue(IsBigScreenProperty);
            }
            private set
            {
                SetValue(IsBigScreenProperty, value);
            }
        }
        public static readonly DependencyProperty IsBigScreenProperty =
            DependencyProperty.Register("IsBigScreen", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
		{
			this.InitializeComponent();
            BeaconModel = new BeaconDetailsModel();

            double displaySize = ResolveDisplaySizeInInches();
            System.Diagnostics.Debug.WriteLine("Display size is " + displaySize + " inches");
            IsBigScreen = displaySize > 6d ? true : false;

            hub.Background.Opacity = 0.6d;
            pivot.Background.Opacity = 0.6d;
            
            SizeChanged += OnMainPageSizeChanged;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainPage.OnNavigatedTo()");
            base.OnNavigatedTo(e);

            if (_sdkManager == null)
            {
                _sdkManager = SDKManager.Instance(ManufacturerId, BeaconCode);
                _sdkManager.ScannerStatusChanged += OnScannerStatusChangedAsync;
                _sdkManager.LayoutValidityChanged += OnBeaconLayoutValidityChanged;
                _sdkManager.BackgroundFiltersUpdated += OnBackgroundFiltersUpdatedAsync;
            }

            BeaconAction pendingBeaconAction = BeaconAction.FromNavigationEventArgs(e);

            if (pendingBeaconAction != null)
            {
                _sdkManager.ClearPendingActions();

                if (await pendingBeaconAction.LaunchWebBrowserAsync())
                {
                    Application.Current.Exit();
                }
                else
                {
                    OnBeaconActionResolvedAsync(this, pendingBeaconAction);
                }
            }

            LoadApplicationSettings();
            ValidateApiKeyAsync();

            if (_advertiser == null)
            {
                _advertiser = new Advertiser();
                _advertiser.ManufacturerId = ManufacturerId;
                _advertiser.BeaconCode = BeaconCode;
            }

            toggleScanButton.IsEnabled = false;
            HookScannerEvents();
            _sdkManager.StartScanner();

            Window.Current.VisibilityChanged += OnVisibilityChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainPage.OnNavigatedFrom()");

            Window.Current.VisibilityChanged -= OnVisibilityChanged;

            _sdkManager.LayoutValidityChanged -= OnBeaconLayoutValidityChanged;
            _sdkManager.BackgroundFiltersUpdated -= OnBackgroundFiltersUpdatedAsync;

            UnhookScannerEvents();

            if (_sdkManager.IsScannerStarted)
            {
                _sdkManager.StopScanner();
            }

            SaveApplicationSettings();

            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Called when the app is brought on foreground or put in background.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            _appIsOnForeground = e.Visible;
            _sdkManager.OnApplicationVisibilityChanged(sender, e);

            if (!ScannerAreEventsHooked)
            {
                HookScannerEvents();
            }
        }

        /// <summary>
        /// Helper method for showing informational message dialogs, which do not require command handling.
        /// </summary>
        /// <param name="message">The message to show.</param>
        /// <param name="title">The title of the message dialog.</param>
        private async void ShowInformationalMessageDialogAsync(string message, string title = null)
        {
            MessageDialog messageDialog = (title == null) ? new MessageDialog(message) : new MessageDialog(message, title);
            messageDialog.Commands.Add(new UICommand("OK"));
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// Resolves the display size of the device running this app.
        /// </summary>
        /// <returns>The display size in inches or less than zero if unable to resolve.</returns>
        private double ResolveDisplaySizeInInches()
        {
            double displaySize = -1d;

            DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
            double rawPixelsPerViewPixel = displayInformation.RawPixelsPerViewPixel;
            float rawDpiX = displayInformation.RawDpiX;
            float rawDpiY = displayInformation.RawDpiY;
            double screenResolutionX = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Bounds.Width * rawPixelsPerViewPixel;
            double screenResolutionY = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Bounds.Height * rawPixelsPerViewPixel;

            if (rawDpiX > 0 && rawDpiY > 0)
            {
                displaySize = Math.Sqrt(
                    Math.Pow(screenResolutionX / rawDpiX, 2) +
                    Math.Pow(screenResolutionY / rawDpiY, 2));
                displaySize = Math.Round(displaySize, 1); // One decimal is enough
            }

            return displaySize;
        }

        private async void OnBackgroundFiltersUpdatedAsync(object sender, EventArgs e)
        {
            MessageDialog messageDialog = new MessageDialog(
                "Background task filters updated successfully with the new beacon beacon ID1s!",
                "Update successful");
            await messageDialog.ShowAsync();
        }

        private void OnMainPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsBigScreen)
            {
                // No implementation required
            }
            else
            {
                if (layoutGrid.ActualWidth > 0)
                {
                    BeaconDetailsControlWidth = layoutGrid.ActualWidth - 40d;
                }
            }
        }
    }
}
