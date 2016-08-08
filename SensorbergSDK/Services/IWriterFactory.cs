// Created by Kay Czarnotta on 08.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.Storage;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Factory to create writer classes.
    /// </summary>
    public interface IWriterFactory
    {
        /// <summary>
        /// Creates a new queued writer for the given file.
        /// </summary>
        IQueuedFileWriter CreateNew(StorageFolder foregroundActions, string file);
    }
}