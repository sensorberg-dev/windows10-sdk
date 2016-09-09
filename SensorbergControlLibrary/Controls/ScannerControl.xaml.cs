using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MetroLog;
using SensorbergControlLibrary.Model;
using SensorbergSDK;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergControlLibrary.Controls
{
    public sealed partial class ScannerControl : UserControl
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<ScannerControl>();
        public static readonly DependencyProperty EnableScannerProperty = DependencyProperty.Register("EnableScanner", typeof(bool), typeof(ScannerControl), new PropertyMetadata(default(bool)));
        private BeaconDetailsModel BeaconModel { get; } = new BeaconDetailsModel();
        public event NotifyCollectionChangedEventHandler BeaconCollectionChanged
        {
            add { BeaconModel.BeaconDetailsCollection.CollectionChanged += value; }
            remove { BeaconModel.BeaconDetailsCollection.CollectionChanged -= value; }
        }

        public bool EnableScanner
        {
            get { return (bool) GetValue(EnableScannerProperty); }
            set
            {
                SetValue(EnableScannerProperty, value);
                if (!value)
                {
                    BeaconModel.Clear();
                }
            }
        }

        public ScannerControl()
        {
            InitializeComponent();
        }

        public async Task OnBeaconEvent(BeaconEventArgs eventArgs)
        {
            try
            {
                Beacon beacon = eventArgs.Beacon;

                if (eventArgs.EventType != BeaconEventType.None)
                {
                    Logger.Debug("MainPage.OnBeaconEventAsync: '" + eventArgs.EventType + "' event from " + beacon);
                }

                bool isExistingBeacon = false;

                if (BeaconModel.Contains(beacon))
                {
                    if (eventArgs.EventType == BeaconEventType.Exit)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => BeaconModel.Remove(beacon));
                    }
                    else
                    {
                        BeaconModel.AddOrReplace(beacon);
                    }

                    isExistingBeacon = true;
                }

                if (!isExistingBeacon)
                {
                    BeaconModel.AddOrReplace(beacon);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error while add/update beacon", e);
            }
        }
    }
}
