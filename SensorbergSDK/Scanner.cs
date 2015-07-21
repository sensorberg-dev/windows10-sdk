using SensorbergSDK.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;

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
		private const int TimerIntervalInMilliseconds = 1000;
		private const int BeaconNotSeenDelayInMilliseconds = 2000;

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

		private static Scanner _instance;
		private BluetoothLEAdvertisementWatcher _bluetoothLEAdvertisemenetWatcher;
        private BeaconContainer _beaconsContainer;
		private Timer _beaconListRefreshTimer;
        private Timer _notifyStartedDelayTimer;

		public static Scanner Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Scanner();
				}

				return _instance;
			}
		}

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
                                TimerIntervalInMilliseconds, TimerIntervalInMilliseconds);
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
		public void StartWatcher(UInt16 manufacturerId, UInt16 beaconCode)
		{
			if (Status != ScannerStatus.Started)
			{
				if (_bluetoothLEAdvertisemenetWatcher == null)
				{
					_bluetoothLEAdvertisemenetWatcher = new BluetoothLEAdvertisementWatcher();
                    BluetoothLEManufacturerData manufacturerData = BeaconFactory.BeaconManufacturerData(manufacturerId, beaconCode);
                    _bluetoothLEAdvertisemenetWatcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
                }

				_bluetoothLEAdvertisemenetWatcher.Received += OnAdvertisementReceived;
				_bluetoothLEAdvertisemenetWatcher.Stopped += OnWatcherStopped;
				_bluetoothLEAdvertisemenetWatcher.Start();

				Status = ScannerStatus.Started;
                System.Diagnostics.Debug.WriteLine("Scanner.StartWatcher(): Watcher started");
            }
		}

		/// <summary>
		/// Stops the watcher. The events are unhooked in the callback (OnWatcherStopped).
		/// </summary>
		public void StopWatcher()
		{
			if (Status == ScannerStatus.Started)
			{
				if (_bluetoothLEAdvertisemenetWatcher != null)
				{
					_bluetoothLEAdvertisemenetWatcher.Stop();
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
            List<Beacon> beacons = _beaconsContainer.RemoveBeaconsBasedOnAge(Constants.BeaconExitDelayInMilliseconds);

            foreach (Beacon beacon in beacons)
            {
                NotifyBeaconEvent(beacon, BeaconEventType.Exit);
            }

            beacons = _beaconsContainer.BeaconsBasedOnAge(BeaconNotSeenDelayInMilliseconds);

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
            Beacon beacon = BeaconFactory.BeaconFromBluetoothLEAdvertisementReceivedEventArgs(args);

			if (beacon != null)
			{
                bool isExistingBeacon = _beaconsContainer.Update(beacon);

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
            if (_bluetoothLEAdvertisemenetWatcher != null)
            {
                System.Diagnostics.Debug.WriteLine("Scanner.OnWatcherStopped(): Status: " + _bluetoothLEAdvertisemenetWatcher.Status);

                if (_bluetoothLEAdvertisemenetWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
                {
                    Status = ScannerStatus.Aborted;
                }
                else
                {
                    Status = ScannerStatus.Stopped;
                }

				_bluetoothLEAdvertisemenetWatcher.Received -= OnAdvertisementReceived;
				_bluetoothLEAdvertisemenetWatcher.Stopped -= OnWatcherStopped;
				_bluetoothLEAdvertisemenetWatcher = null;
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
