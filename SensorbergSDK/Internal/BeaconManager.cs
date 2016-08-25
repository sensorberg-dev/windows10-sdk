// Created by Kay Czarnotta on 25.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Manager to handle beacon enter and exit.
    /// </summary>
    public class BeaconManager
    {
        protected Dictionary<string, DateTimeOffset> KnownBeacons { get; } = new Dictionary<string, DateTimeOffset>();

        public BeaconEventType ResolveBeaconState(Beacon b)
        {
            return BeaconEventType.Unknown;
        }
    }
}