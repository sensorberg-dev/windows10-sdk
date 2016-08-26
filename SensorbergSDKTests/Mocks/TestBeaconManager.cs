// Created by Kay Czarnotta on 25.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using SensorbergSDK;
using SensorbergSDK.Internal;

namespace SensorbergSDKTests.Mocks
{
    public class TestBeaconManager : BeaconManager
    {
        public TestBeaconManager(long exitTimeout) : base(exitTimeout)
        {
        }

        public void SetTime(Beacon b, DateTimeOffset time)
        {
            KnownBeacons[b] = time;
        }
    }
}