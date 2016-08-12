// Created by Kay Czarnotta on 08.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.Storage;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    public class WriterFactory : IWriterFactory
    {
        public IQueuedFileWriter CreateNew(StorageFolder foregroundActions, string file)
        {
            return new QueuedFileWriter(foregroundActions, file);
        }
    }
}