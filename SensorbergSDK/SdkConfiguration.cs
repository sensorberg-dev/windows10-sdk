// Created by Kay Czarnotta on 25.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using SensorbergSDK.Internal;

namespace SensorbergSDK
{
    public class SdkConfiguration
    {
        public string ApiKey { get; set; }
        public string BackgroundTimerClassName { get; set; }
        public string BackgroundAdvertisementClassName { get; set; }
        public string BackgroundBeaconUuidSpace { get; set; }
        public bool AutoStartScanner { get; set; }
        /// <summary>
        /// The manufacturer ID to filter beacons that are being watched.
        /// </summary>
        public UInt16 ManufacturerId
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        /// <summary>
        /// The beacon code to filter beacons that are being watched.
        /// </summary>
        public UInt16 BeaconCode
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        public SdkConfiguration()
        {
            AutoStartScanner = true;
            BackgroundBeaconUuidSpace = Constants.SensorbergUuidSpace;
        }
    }
}