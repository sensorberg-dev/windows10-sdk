// Created by Kay Czarnotta on 08.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SensorbergSDK.Internal.Services
{
    /// <summary>
    /// Interface to write many lines to one file to avoid concurrent issues.
    /// </summary>
    public interface IQueuedFileWriter: IDisposable
    {
        /// <summary>
        /// Event for notification on empty queue.
        /// </summary>
        event Action QueueEmpty;
        /// <summary>
        /// Write the given line to the file.
        /// </summary>
        Task WriteLine(string line);
        /// <summary>
        /// Return all lines from file and cached (not written) lines.
        /// </summary>
        /// <returns></returns>
        Task<List<string>> ReadLines();
        /// <summary>
        /// Removes all lines from file and cache.
        /// </summary>
        /// <returns></returns>
        Task Clear();

        /// <summary>
        /// Reads the file and cache and replacese both with the given lines.
        /// </summary>
        Task RewriteFile(Action<List<string>, List<string>> action);
    }
}