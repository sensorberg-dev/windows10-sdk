// Created by Kay Czarnotta on 25.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Diagnostics;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK
{
    /// <summary>
    /// Defines the configuration for the sdk.
    /// </summary>
    public class SdkConfiguration
    {
        /// <summary>
        /// ApiKey to connect to the Sensorberg Backend. See https://manage.sensorberg.com/#/applications for the key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Class name to register the background timer.
        /// </summary>
        public string BackgroundTimerClassName { get; set; }

        /// <summary>
        /// class name to register the advertismend task, this task receives the bluetooth events.
        /// </summary>
        public string BackgroundAdvertisementClassName { get; set; }

        /// <summary>
        /// Defines an differend bacground beacon uuid space. See https://msdn.microsoft.com/de-de/library/windows/apps/windows.devices.bluetooth.advertisement.bluetoothleadvertisementfilter.bytepatterns.aspx for further information.
        /// </summary>
        public string BackgroundBeaconUuidSpace { get; set; }

        /// <summary>
        /// Defines if the beacon scanner should automaticly start.
        /// </summary>
        public bool AutoStartScanner { get; set; }

        /// <summary>
        /// Defines an user or advertisement id for collection all the events from a specific user or device.
        /// </summary>
        public string UserId
        {
            get { return SdkData.Instance.UserId; }
            set { SdkData.Instance.UserId = value; }
        }

        /// <summary>
        /// The manufacturer ID to filter beacons that are being watched.
        /// </summary>
        public ushort ManufacturerId
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        /// <summary>
        /// The beacon code to filter beacons that are being watched.
        /// </summary>
        public ushort BeaconCode
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        /// <summary>
        /// Creates a new object that starts automatic the scanner and collect every beacon contains the sensorberg uuids.
        /// </summary>
        public SdkConfiguration()
        {
            AutoStartScanner = true;
            BackgroundBeaconUuidSpace = Constants.SensorbergUuidSpace;
        }
    }
}