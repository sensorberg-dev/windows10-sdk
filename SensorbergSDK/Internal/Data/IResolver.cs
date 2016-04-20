// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;

namespace SensorbergSDK.Internal.Data
{
    public interface IResolver:IDisposable
    {
        Task<int> CreateRequest(BeaconEventArgs beaconEventArgs);
        event EventHandler<ResolvedActionsEventArgs> ActionsResolved;
        event EventHandler<string> FailedToResolveActions;
    }
}