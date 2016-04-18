// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using SensorbergSDK;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockBeaconScanner : IBeaconScanner
    {
        public event EventHandler<Beacon> BeaconNotSeenForAWhile;
        public event EventHandler<ScannerStatus> StatusChanged;
        public event EventHandler<BeaconEventArgs> BeaconEvent;
        public ScannerStatus Status { get; }

        public void StartWatcher(ushort manufacturerId, ushort beaconCode, ulong beaconExitTimeout, short? rssiEnterThreshold, ulong? enterDistanceThreshold)
        {
        }

        public void StopWatcher()
        {
        }

        public void FireBeaconEvent(Beacon beacon, BeaconEventType eventType)
        {
            BeaconEvent?.Invoke(this, new BeaconEventArgs() {Beacon = beacon, EventType = eventType});
        }
    }
}