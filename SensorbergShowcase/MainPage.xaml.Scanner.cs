using SensorbergSDK;
using System;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SensorbergSDK.Internal.Services;

namespace SensorbergShowcase
{
    /// <summary>
    /// Contains the scanner specific UI implementation.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const double DefaultBeaconDetailsControlWidth = 350d;
        private IAsyncOperation<IUICommand> _bluetoothNotOnDialogOperation;

        public BeaconDetailsModel BeaconModel
        {
            get
            {
                return (BeaconDetailsModel)GetValue(BeaconModelProperty);
            }
            private set
            {
                SetValue(BeaconModelProperty, value);
            }
        }
        public static readonly DependencyProperty BeaconModelProperty =
            DependencyProperty.Register("BeaconModel", typeof(BeaconDetailsModel), typeof(MainPage),
                new PropertyMetadata(null));

        public double BeaconDetailsControlWidth
        {
            get
            {
                return (double)GetValue(BeaconDetailsControlWidthProperty);
            }
            private set
            {
                SetValue(BeaconDetailsControlWidthProperty, value);
            }
        }
        public static readonly DependencyProperty BeaconDetailsControlWidthProperty =
            DependencyProperty.Register("BeaconDetailsControlWidth", typeof(double), typeof(MainPage),
                new PropertyMetadata(DefaultBeaconDetailsControlWidth));
 
        public bool ScannerAreEventsHooked
        {
            get;
            private set;
        }

        public bool IsScanning
        {
            get
            {
                return (bool)GetValue(IsScanningProperty);
            }
            private set
            {
                SetValue(IsScanningProperty, value);
            }
        }
        public static readonly DependencyProperty IsScanningProperty =
            DependencyProperty.Register("IsScanning", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        public bool BeaconsInRange
        {
            get
            {
                return (bool)GetValue(BeaconsInRangeProperty);
            }
            private set
            {
                SetValue(BeaconsInRangeProperty, value);
            }
        }
        public static readonly DependencyProperty BeaconsInRangeProperty =
            DependencyProperty.Register("BeaconsInRange", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        /// <summary>
        /// Hooks the scanner events.
        /// </summary>
        private void HookScannerEvents()
        {
            if (!ScannerAreEventsHooked)
            {
                IBeaconScanner scanner = _sdkManager.Scanner;
                scanner.BeaconEvent += OnBeaconEventAsync;
                scanner.BeaconNotSeenForAWhile += OnBeaconNotSeenForAWhileAsync;
                ScannerAreEventsHooked = true;
            }
        }

        /// <summary>
        /// Unhooks the scanner events.
        /// </summary>
        private void UnhookScannerEvents()
        {
            if (ScannerAreEventsHooked)
            {
                IBeaconScanner scanner = _sdkManager.Scanner;
                scanner.BeaconEvent -= OnBeaconEventAsync;
                scanner.BeaconNotSeenForAWhile -= OnBeaconNotSeenForAWhileAsync;
                ScannerAreEventsHooked = false;
            }
        }

		private async void OnBeaconEventAsync(object sender, BeaconEventArgs eventArgs)
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				Beacon beacon = eventArgs.Beacon;

				if (eventArgs.EventType != BeaconEventType.None)
				{
					System.Diagnostics.Debug.WriteLine("MainPage.OnBeaconEventAsync(): '"
                        + eventArgs.EventType + "' event from " + beacon.ToString());
				}

				bool isExistingBeacon = false;

                if (BeaconModel.Contains(beacon))
                {
                    if (eventArgs.EventType == BeaconEventType.Exit)
                    {
                        BeaconModel.Remove(beacon);
                    }
                    else
                    {
                        BeaconModel.AddOrReplace(beacon);
                    }

                    BeaconModel.SortBeaconsBasedOnDistance();
                    isExistingBeacon = true;
                }


                if (!isExistingBeacon)
				{
                    BeaconModel.AddOrReplace(beacon);
                    BeaconModel.SortBeaconsBasedOnDistance();
                }

				if (BeaconModel.Count() > 0)
				{
                    BeaconsInRange = true;
				}
				else
				{
                    BeaconsInRange = false;
                }
            });
		}

		private async void OnBeaconNotSeenForAWhileAsync(object sender, Beacon e)
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
                BeaconModel.SetBeaconRange(e, 0);
			});
		}

        private async void OnToggleScanButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                toggleScanButton.IsEnabled = false;

                if (_sdkManager.IsScannerStarted)
                {
                    // Unhook the events only after receiving scanner stopped event
                    _sdkManager.StopScanner();
                }
                else
                {
                    HookScannerEvents();
                    _sdkManager.StartScanner();
                }
            });
        }

        private async void OnScannerStatusChangedAsync(object sender, ScannerStatus e)
        {
            System.Diagnostics.Debug.WriteLine("MainPage.OnScannerStatusChangedAsync(): " + e);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IsScanning = (e == ScannerStatus.Started);

                switch (e)
                {
                    case ScannerStatus.Stopped:
                        toggleScanButton.IsChecked = true;
                        toggleScanButton.Label = "start scanner";
                        UnhookScannerEvents();
                        break;

                    case ScannerStatus.Started:
                        if (_bluetoothNotOnDialogOperation != null)
                        {
                            _bluetoothNotOnDialogOperation.Cancel();
                            _bluetoothNotOnDialogOperation = null;
                        }

                        toggleScanButton.IsChecked = false;
                        toggleScanButton.Label = "stop scanner";
                        break;

                    case ScannerStatus.Aborted:
                        toggleScanButton.IsChecked = true;
                        toggleScanButton.Label = "start scanner";

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
                                    _bluetoothNotOnDialogOperation = null;
                                })));

                            _bluetoothNotOnDialogOperation = messageDialog.ShowAsync();
                        }

                        break;
                }

                toggleScanButton.IsEnabled = true;
            });
        }
    }
}
