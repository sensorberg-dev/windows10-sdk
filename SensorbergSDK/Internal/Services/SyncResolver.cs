// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal.Services
{
    public class SyncResolver:IResolver
    {
        public event EventHandler<ResolvedActionsEventArgs> ActionsResolved;
        public event EventHandler<string> FailedToResolveActions;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<int> CreateRequest(BeaconEventArgs beaconEventArgs)
        {
            throw new NotImplementedException();
        }
    }
}