// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using SensorbergSDK.Internal;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction for the beacon scanner.
    /// </summary>
    public interface IBeaconScanner
    {
        /// <summary>
        /// Triggered when beacon hasn't been seen for a time defined in BeaconNotSeenDelayInMilliseconds.
        /// </summary>
        event EventHandler<Beacon> BeaconNotSeenForAWhile;
        /// <summary>
        /// Triggered when the scanner is either started, stopped or aborted.
        /// Aborted status may indicate that the bluetooth has not been turned on on the device.
        /// </summary>
        event EventHandler<ScannerStatus> StatusChanged;

        event EventHandler<BeaconEventArgs> BeaconEvent;

        /// <summary>
        /// Defines whether the scanner (bluetooth advertisement watcher) has been started or not.
        /// When the watcher is started, the timer for checking up on the list of beacons is
        /// started as well.
        /// </summary>
        ScannerStatus Status { get; }

        /// <summary>
        /// Starts the watcher and hooks its events to callbacks.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID.</param>
        /// <param name="beaconCode">The beacon code.</param>
        /// <param name="beaconExitTimeout">Time in miliseconds after beacon will be trated as lost.</param>
        /// <param name="rssiEnterThreshold">Optional rssi threshold which will trigger beacon discover event. Value must be between -128 and 127.</param>
        /// <param name="enterDistanceThreshold">Optional minimal distance in meters that will trigger beacon discover event.</param>
        void StartWatcher(ushort manufacturerId, ushort beaconCode, ulong beaconExitTimeout, short? rssiEnterThreshold = null, ulong? enterDistanceThreshold = null);

        /// <summary>
        /// Stops the watcher. The events are unhooked in the callback (OnWatcherStopped).
        /// </summary>
        void StopWatcher();

        /// <summary>
        /// Reset all states of the beacons, so every beacon gets a new ENTER event.
        /// </summary>
        void ResetBeaconState();
    }
}