﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SensorbergSDK;
using SensorbergTrafficLightApp.Helpers;
using SensorbergTrafficLightApp.Models;

namespace SensorbergTrafficLightApp
{
    public sealed partial class MainPage : Page
    {
        private const string API_KEY = "8c1a6f4a18640bbcedd36e942f5d741da1cba18e71e8cc090078beef97f1b619";//TODO: Enter API key
        private const string COLOR_KEY = "color";
        private const ushort MANUFACTURER_ID = 0x004c;
        private const ushort BEACON_CODE = 0x0215;

        private readonly SDKManager _sdkManager;
      
        private IAsyncOperation<IUICommand> _bluetoothNotOnDialogOperation;

        public MainPage()
        {
            this.InitializeComponent();
            _sdkManager = SDKManager.Instance(MANUFACTURER_ID,BEACON_CODE);
            _sdkManager.BeaconActionResolved += OnBeaconActionResolved;
            _sdkManager.ScannerStatusChanged += OnScannerStatusChangedAsync;
        }

        private void OnScannerStatusChangedAsync(object sender, ScannerStatus e)
        {
            if (e == ScannerStatus.Aborted &&  _bluetoothNotOnDialogOperation == null )
            {
                MessageDialog messageDialog = new MessageDialog(
                    "Do you wish to enable Bluetooth on this device?",
                    "Failed to start Bluetooth LE advertisement watcher");

                messageDialog.Commands.Add(new UICommand("Yes",
                    (command) =>
                        {
                            _sdkManager.LaunchBluetoothSettingsAsync();
                        }));

                messageDialog.Commands.Add(new UICommand("No",
                    (command) =>
                        {
                            _sdkManager.StopScanner();
                        }));

                _bluetoothNotOnDialogOperation = messageDialog.ShowAsync();
            }

            if (e == ScannerStatus.Stopped)
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () =>
                        {
                            SetLightState(TrafficStates.Red);
                        });
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetLightState(TrafficStates.Red);
            await _sdkManager.InitializeAsync(API_KEY);
        }

        private void SetLightState(TrafficStates state)
        {
            switch (state)
            {
                case TrafficStates.Green:
                    {
                        GreenGrid.Visibility = Visibility.Visible;
                        break;
                    }

                case TrafficStates.Red:
                    {
                        GreenGrid.Visibility = Visibility.Collapsed;
                        break;
                    }
            }
        }

        private async void OnBeaconActionResolved(object sender, BeaconAction e)
        {
            Debug.WriteLine("Beacon action resolved" + e.Body + ", " + e.Payload);
            if (e.Payload != null && e.Payload.ContainsKey(COLOR_KEY))
            {
                TrafficStates state;
                var stringValue = e.Payload[COLOR_KEY].GetString();
                bool parseResult = Enum.TryParse(stringValue.ToFirstLetterUpper(), out state);

                if (parseResult)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () =>
                        {
                            SetLightState(state);
                        });
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

    }
}
