// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Diagnostics;
using Windows.Devices.Bluetooth.Advertisement;
using MetroLog;
using SensorbergSDK.Internal.Utils;

namespace SensorbergSDK
{
    /// <summary>
    /// Manages publishing BLE advertisements.
    /// </summary>
    public sealed class Advertiser
    {
        private static ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<Advertiser>();
        private const int DefaultMeasuredPower = -59;
        private BluetoothLEAdvertisementPublisher _advertisementPublisher;
        private Beacon _beacon;

        public string BeaconId1 { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public ushort ManufacturerId { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public ushort BeaconCode { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public ushort BeaconId2 { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public ushort BeaconId3 { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public bool IsStarted { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }


        /// <summary>
        /// Starts advertizing based on the set values (beacon ID 1, ID 2 and ID 3).
        /// Note that this method does not validate the values and will throw exception, if the
        /// values are invalid.
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                _beacon = new Beacon();
                _beacon.Id1 = BeaconId1;
                _beacon.ManufacturerId = ManufacturerId;
                _beacon.Code = BeaconCode;
                _beacon.Id2 = BeaconId2;
                _beacon.Id3 = BeaconId3;
                _beacon.MeasuredPower = DefaultMeasuredPower;

                _advertisementPublisher = new BluetoothLEAdvertisementPublisher();

                BluetoothLEAdvertisementDataSection dataSection = BeaconFactory.BeaconToSecondDataSection(_beacon);
                _logger.Debug("Advertiser.Start(): " + BeaconFactory.DataSectionToRawString(dataSection));
                _advertisementPublisher.Advertisement.DataSections.Add(dataSection);
                _advertisementPublisher.Start();

                IsStarted = true;
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                _advertisementPublisher.Stop();
                _advertisementPublisher = null;
                IsStarted = false;
            }
            _logger.Debug("Advertiser.Stoped");
        }
    }
}