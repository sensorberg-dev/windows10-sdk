using SensorbergSDK.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK
{
    public enum ScannerStatus
    {
        Stopped,
        Started,
        Aborted
    }

    /// <summary>
    /// Bluetooth LE advertisement scanner helper class with beacon list management.
    /// </summary>
	public sealed class Scanner
    {
        /// <summary>
        /// Triggered when the scanner is either started, stopped or aborted.
        /// Aborted status may indicate that the bluetooth has not been turned on on the device.
        /// </summary>
        public event EventHandler<ScannerStatus> StatusChanged;

        public event EventHandler<BeaconEventArgs> BeaconEvent;

        /// <summary>
        /// Triggered when beacon hasn't been seen for a time defined in BeaconNotSeenDelayInMilliseconds.
        /// </summary>
        public event EventHandler<Beacon> BeaconNotSeenForAWhile;

        private readonly BeaconContainer _beaconsContainer;
        private static Scanner _instance;
        private BluetoothLEAdvertisementWatcher _bluetoothLEAdvertisementWatcher;
        private BluetoothLEManufacturerData _bluetoothLeManufacturerData;
        private Timer _beaconListRefreshTimer;
        private Timer _notifyStartedDelayTimer;

        private UInt64 _beaconExitTimeout;
        private UInt64? _enterDistanceThreshold;

        public static Scanner Instance => _instance ?? (_instance = new Scanner());

        private ScannerStatus _status;
        /// <summary>
        /// Defines whether the scanner (bluetooth advertisement watcher) has been started or not.
        /// When the watcher is started, the timer for checking up on the list of beacons is
        /// started as well.
        /// </summary>
        public ScannerStatus Status
        {
            get
            {
                return _status;
            }
            private set
            {
                if (_status != value)
                {
                    _status = value;

                    if (_notifyStartedDelayTimer != null)
                    {
                        _notifyStartedDelayTimer.Dispose();
                        _notifyStartedDelayTimer = null;
                    }

                    if (_status == ScannerStatus.Started)
                    {
                        if (_beaconListRefreshTimer == null)
                        {
                            _beaconListRefreshTimer = new Timer(CheckListForOldBeacons, null,
                                Constants.BeaconsListRefreshIntervalInMilliseconds, Constants.BeaconsListRefreshIntervalInMilliseconds);
                        }

                        // Delay the notification in case there is an immediate error (e.g. when
                        // the bluetooth is not turned on the device.
                        _notifyStartedDelayTimer = new Timer(OnNotifyStartedDelayTimeout, null, 500, Timeout.Infinite);
                    }
                    else
                    {
                        // Notify immediately
                        if (StatusChanged != null)
                        {
                            StatusChanged(this, _status);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        private Scanner()
        {
            _status = ScannerStatus.Stopped;
            _beaconsContainer = new BeaconContainer();
        }

        /// <summary>
        /// Starts the watcher and hooks its events to callbacks.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID.</param>
        /// <param name="beaconCode">The beacon code.</param>
        /// <param name="beaconExitTimeoutInMiliseconds">Time in miliseconds after beacon will be trated as lost</param>
        /// <param name="rssiEnterThreshold">Optional rssi threshold which will trigger beacon discover event. Value must be between -128 and 127</param>
        /// <param name="enterDistanceThreshold">Optional minimal distance in meters that will trigger beacon discover event</param>
        public void StartWatcher(UInt16 manufacturerId, UInt16 beaconCode, UInt64 beaconExitTimeoutInMiliseconds, Int16? rssiEnterThreshold = null, UInt64? enterDistanceThreshold = null)
        {
            _beaconExitTimeout = beaconExitTimeoutInMiliseconds;
            _enterDistanceThreshold = enterDistanceThreshold;

            if (_beaconExitTimeout < 1000)
            {
                _beaconExitTimeout = 1000;
            }

            if (Status != ScannerStatus.Started)
            {
                if (_bluetoothLEAdvertisementWatcher == null)
                {
                    _bluetoothLeManufacturerData = BeaconFactory.BeaconManufacturerData(manufacturerId, beaconCode);
                    _bluetoothLEAdvertisementWatcher = new BluetoothLEAdvertisementWatcher();
                    _bluetoothLEAdvertisementWatcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(_bluetoothLeManufacturerData);
                    _bluetoothLEAdvertisementWatcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(0);
                    _bluetoothLEAdvertisementWatcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(_beaconExitTimeout);
                    _bluetoothLEAdvertisementWatcher.ScanningMode = BluetoothLEScanningMode.Active;

                    if (rssiEnterThreshold != null  && rssiEnterThreshold.Value >= -128 && rssiEnterThreshold.Value <= 127)
                    {
                        _bluetoothLEAdvertisementWatcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter() { InRangeThresholdInDBm = rssiEnterThreshold.Value};
                    }
                }

                _bluetoothLEAdvertisementWatcher.Received += OnAdvertisementReceived;
                _bluetoothLEAdvertisementWatcher.Stopped += OnWatcherStopped;
                _bluetoothLEAdvertisementWatcher.Start();

                Status = ScannerStatus.Started;
                Debug.WriteLine("Scanner.StartWatcher(): Watcher started");
            }
        }
        
        /// <summary>
        /// Stops the watcher. The events are unhooked in the callback (OnWatcherStopped).
        /// </summary>
        public void StopWatcher()
        {
            if (Status == ScannerStatus.Started)
            {
                if (_bluetoothLEAdvertisementWatcher != null)
                {
                    _bluetoothLEAdvertisementWatcher.Stop();
                }
            }
        }

        /// <summary>
        /// Notifies any listeners about the beacon event.
        /// </summary>
        /// <param name="beacon"></param>
        /// <param name="eventType"></param>
        private void NotifyBeaconEvent(Beacon beacon, BeaconEventType eventType)
        {
            if (BeaconEvent != null)
            {
                BeaconEvent(this, new BeaconEventArgs() { Beacon = beacon, EventType = eventType });
            }
        }

        /// <summary>
        /// Checks the list of beacons for old beacons. If old enough beacons are found, an
        /// exit event for them is generated and they are removed from the list.
        /// </summary>
        /// <param name="state"></param>
        private void CheckListForOldBeacons(object state)
        {
            List<Beacon> beacons = _beaconsContainer.RemoveBeaconsBasedOnAge(_beaconExitTimeout);

            foreach (Beacon beacon in beacons)
            {
                NotifyBeaconEvent(beacon, BeaconEventType.Exit);
            }

            beacons = _beaconsContainer.BeaconsBasedOnAge(_beaconExitTimeout);

            if (BeaconNotSeenForAWhile != null)
            {
                foreach (Beacon beacon in beacons)
                {
                    BeaconNotSeenForAWhile(this, beacon);
                }
            }

            beacons = null;

            if (Status != ScannerStatus.Started && _beaconsContainer.Count == 0 && _beaconListRefreshTimer != null)
            {
                _beaconListRefreshTimer.Dispose();
                _beaconListRefreshTimer = null;
            }
        }

        /// <summary>
        /// Triggered when the watcher receives an advertisement.
        /// 
        /// If the advertisement came from a beacon, a Beacon instance is created based on the
        /// received data. A new beacon is added to the list and an existing one is only updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Debug.WriteLine("");
            Debug.WriteLine("");
            Debug.WriteLine("Advertisement received " + args.Timestamp.ToString("HH:mm:ss.fff"));
            Beacon beacon = BeaconFactory.BeaconFromBluetoothLEAdvertisementReceivedEventArgs(args);

            if (beacon != null)
            {
                if (_enterDistanceThreshold != null && beacon.Distance > _enterDistanceThreshold.Value)
                {
                    return;
                }

                bool isExistingBeacon = _beaconsContainer.TryUpdate(beacon);
                if (isExistingBeacon)
                {
                    NotifyBeaconEvent(beacon, BeaconEventType.None);

                }
                else
                {
                    _beaconsContainer.Add(beacon);
                    NotifyBeaconEvent(beacon, BeaconEventType.Enter);
                }
            }
        }

        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            if (_bluetoothLEAdvertisementWatcher != null)
            {
                Debug.WriteLine("Scanner.OnWatcherStopped(): Status: " + _bluetoothLEAdvertisementWatcher.Status);

                if (_bluetoothLEAdvertisementWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
                {
                    Status = ScannerStatus.Aborted;
                }
                else
                {
                    Status = ScannerStatus.Stopped;
                }

                _bluetoothLEAdvertisementWatcher.Received -= OnAdvertisementReceived;
                _bluetoothLEAdvertisementWatcher.Stopped -= OnWatcherStopped;
                _bluetoothLEAdvertisementWatcher = null;
            }
        }

        /// <summary>
        /// Sends a delayed notifications about watcher started event.
        /// </summary>
        /// <param name="state"></param>
        private void OnNotifyStartedDelayTimeout(object state)
        {
            if (_notifyStartedDelayTimer != null)
            {
                _notifyStartedDelayTimer.Dispose();
                _notifyStartedDelayTimer = null;
            }

            if (StatusChanged != null)
            {
                StatusChanged(this, _status);
            }
        }
    }
}
