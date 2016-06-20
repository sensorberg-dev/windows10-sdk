// Created by Kay Czarnotta on 20.06.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK.Internal.Data
{
    public class SerializedAction
    {
        public ResolvedAction Action { get; set; }
        public DateTimeOffset Time { get; set; }
        public string Beacon { get; set; }
        public BeaconEventType Event { get; set; }
    }
}