﻿// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using MetroLog;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Utils;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal
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
    public sealed class Scanner : IBeaconScanner, IDisposable
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<Scanner>();

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
        private BluetoothLEAdvertisementWatcher _bluetoothLeAdvertisementWatcher;
        private BluetoothLEManufacturerData _bluetoothLeManufacturerData;
        private Timer _beaconListRefreshTimer;
        private Timer _notifyStartedDelayTimer;

        private ulong _beaconExitTimeout;
        private ulong? _enterDistanceThreshold;

        private ScannerStatus _status;

        /// <summary>
        /// Defines whether the scanner (bluetooth advertisement watcher) has been started or not.
        /// When the watcher is started, the timer for checking up on the list of beacons is
        /// started as well.
        /// </summary>
        public ScannerStatus Status
        {
            get { return _status; }
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
        /// Create new Scanner Object.
        /// </summary>
        public Scanner()
        {
            _status = ScannerStatus.Stopped;
            _beaconsContainer = new BeaconContainer();
        }

        /// <summary>
        /// Starts the watcher and hooks its events to callbacks.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID.</param>
        /// <param name="beaconCode">The beacon code.</param>
        /// <param name="beaconExitTimeoutInMiliseconds">Time in miliseconds after beacon will be trated as lost.</param>
        /// <param name="rssiEnterThreshold">Optional rssi threshold which will trigger beacon discover event. Value must be between -128 and 127.</param>
        /// <param name="enterDistanceThreshold">Optional minimal distance in meters that will trigger beacon discover event.</param>
        public void StartWatcher(ushort manufacturerId, ushort beaconCode, ulong beaconExitTimeoutInMiliseconds, short? rssiEnterThreshold = null,
            ulong? enterDistanceThreshold = null)
        {
            _beaconExitTimeout = beaconExitTimeoutInMiliseconds;
            _enterDistanceThreshold = enterDistanceThreshold;

            if (_beaconExitTimeout < 1000)
            {
                _beaconExitTimeout = 1000;
            }

            if (Status != ScannerStatus.Started)
            {
                if (_bluetoothLeAdvertisementWatcher == null)
                {
                    _bluetoothLeManufacturerData = BeaconFactory.BeaconManufacturerData(manufacturerId, beaconCode);
                    _bluetoothLeAdvertisementWatcher = new BluetoothLEAdvertisementWatcher();
                    _bluetoothLeAdvertisementWatcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(_bluetoothLeManufacturerData);
                    _bluetoothLeAdvertisementWatcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(0);
                    _bluetoothLeAdvertisementWatcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(_beaconExitTimeout);
                    _bluetoothLeAdvertisementWatcher.ScanningMode = BluetoothLEScanningMode.Active;

                    if (rssiEnterThreshold != null && rssiEnterThreshold.Value >= -128 && rssiEnterThreshold.Value <= 127)
                    {
                        _bluetoothLeAdvertisementWatcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter() {InRangeThresholdInDBm = rssiEnterThreshold.Value};
                    }
                }

                _bluetoothLeAdvertisementWatcher.Received += OnAdvertisementReceived;
                _bluetoothLeAdvertisementWatcher.Stopped += OnWatcherStopped;
                _bluetoothLeAdvertisementWatcher.Start();

                Status = ScannerStatus.Started;
                Logger.Debug("Scanner.StartWatcher(): Watcher started");
            }
        }

        /// <summary>
        /// Stops the watcher. The events are unhooked in the callback (OnWatcherStopped).
        /// </summary>
        public void StopWatcher()
        {
            if (Status == ScannerStatus.Started)
            {
                _bluetoothLeAdvertisementWatcher?.Stop();
            }
        }

        /// <summary>
        /// Notifies any listeners about the beacon event.
        /// </summary>
        /// <param name="beacon"></param>
        /// <param name="eventType"></param>
        public void NotifyBeaconEvent(Beacon beacon, BeaconEventType eventType)
        {
            BeaconEvent?.Invoke(this, new BeaconEventArgs() {Beacon = beacon, EventType = eventType});
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

            if (Status != ScannerStatus.Started && _beaconsContainer.Count == 0 && _beaconListRefreshTimer != null)
            {
                _beaconListRefreshTimer.Dispose();
                _beaconListRefreshTimer = null;
            }
        }

        /// <summary>
        /// Triggered when the watcher receives an advertisement.
        /// If the advertisement came from a beacon, a Beacon instance is created based on the
        /// received data. A new beacon is added to the list and an existing one is only updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Logger.Debug("Scanner: Advertisement received " + args.Timestamp.ToString("HH:mm:ss.fff"));
            Beacon beacon = BeaconFactory.BeaconFromBluetoothLeAdvertisementReceivedEventArgs(args);

            if (beacon != null)
            {
                if (_enterDistanceThreshold != null && beacon.Distance > _enterDistanceThreshold.Value)
                {
                    return;
                }

                bool isExistingBeacon = _beaconsContainer.TryUpdate(beacon);
                Logger.Trace("Scanner: beacon exists:" + isExistingBeacon + " " + beacon.Id1 + " " + beacon.Id2 + " " + beacon.Id3);
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
            if (_bluetoothLeAdvertisementWatcher != null)
            {
                Logger.Debug("Scanner: .OnWatcherStopped(): Status: " + _bluetoothLeAdvertisementWatcher.Status);

                Status = _bluetoothLeAdvertisementWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted ? ScannerStatus.Aborted : ScannerStatus.Stopped;

                _bluetoothLeAdvertisementWatcher.Received -= OnAdvertisementReceived;
                _bluetoothLeAdvertisementWatcher.Stopped -= OnWatcherStopped;
                _bluetoothLeAdvertisementWatcher = null;
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

            StatusChanged?.Invoke(this, _status);
        }

        public void Dispose()
        {
            _beaconListRefreshTimer?.Dispose();
            _notifyStartedDelayTimer?.Dispose();
        }
    }
}
