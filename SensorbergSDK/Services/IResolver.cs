// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using SensorbergSDK.Internal;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction for the resolver. The resolver processed the requests and fires the events for the resolved requests.
    /// </summary>
    public interface IResolver:IDisposable
    {
        /// <summary>
        /// Create a new request and process it.
        /// </summary>
        /// <param name="beaconEventArgs">Beacon event to process.</param>
        Task<int> CreateRequest(BeaconEventArgs beaconEventArgs);

        /// <summary>
        /// Event thats fired on resolved actions.
        /// </summary>
        event EventHandler<ResolvedActionsEventArgs> ActionsResolved;

        /// <summary>
        /// Event thats fired on failed actions.
        /// </summary>
        event EventHandler<string> FailedToResolveActions;

        /// <summary>
        /// Sets the timeout for exit events.
        /// </summary>
        ulong BeaconExitTimeout { get; set; }
    }
}