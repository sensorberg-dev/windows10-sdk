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
        protected Dictionary<Beacon, DateTimeOffset> KnownBeacons { get; } = new Dictionary<Beacon, DateTimeOffset>();
        private long ExitTimeout { get; set; }

        public BeaconManager(long exitTimeout)
        {
            ExitTimeout = exitTimeout;
        }

        public BeaconEventType ResolveBeaconState(Beacon b)
        {
            if (KnownBeacons.ContainsKey(b))
            {
                KnownBeacons[b] = DateTimeOffset.Now;
                return BeaconEventType.None;
            }
            KnownBeacons[b] = DateTimeOffset.Now;
            return BeaconEventType.Enter;
        }

        public List<Beacon> ResolveBeaconExits()
        {
            DateTimeOffset end = DateTimeOffset.Now.AddMilliseconds(-ExitTimeout);
            List<Beacon> removeBeacons = new List<Beacon>();
            foreach (KeyValuePair<Beacon, DateTimeOffset> beacon in KnownBeacons)
            {
                if (beacon.Value > end)
                {
                    removeBeacons.Add(beacon.Key);
                }
            }

            foreach (Beacon b in removeBeacons)
            {
                KnownBeacons.Remove(b);
            }

            return removeBeacons;
        }
    }
}