// Created by Kay Czarnotta on 16.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class StorageServiceExtend : StorageService
    {
        public void SetStorage(IStorage storage)
        {
            Storage = storage;
        }
    }
}