// Created by Kay Czarnotta on 04.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;

namespace SensorbergSDK.Internal.Data
{
    /// <summary>
    /// Event class for storage of each background event.
    /// </summary>
    public class BackgroundEvent
    {
        public string BeaconID { get; set; }
        public BeaconEventType LastEvent { get; set; }
        public DateTimeOffset EventTime { get; set; }
    }
}