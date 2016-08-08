// Created by Kay Czarnotta on 08.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using MetroLog;

namespace SensorbergSDK.Internal.Services
{
    public class QueuedFileWriter: IQueuedFileWriter
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<QueuedFileWriter>();

        private readonly IStorageFolder _folder;
        private readonly string _fileName;
        private StorageFile _storageFile;
        private List<string> Queue { get; set; } = new List<string>();
        private Task _runningTask;
        private CancellationTokenSource CancelToken { get; set; }
        private object _lockObject = new object();

        public QueuedFileWriter(IStorageFolder folder, string fileName)
        {
            _folder = folder;
            _fileName = fileName;
        }

        public async Task WriteLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }
            Queue.Add(line);
            if (_runningTask == null || _runningTask.Status == TaskStatus.Canceled || _runningTask.Status == TaskStatus.Faulted || _runningTask.Status == TaskStatus.RanToCompletion)
            {
                CancelToken = new CancellationTokenSource();
                (_runningTask = Task.Run(WriteLines, CancelToken.Token)).ConfigureAwait(false);
            }
        }

        private async Task WriteLines()
        {
            if (_storageFile == null)
            {
                _storageFile = await _folder.CreateFileAsync(_fileName, CreationCollisionOption.OpenIfExists);
            }
            do
            {
                List<string> listToWrite = Queue;
                try
                {
                    lock (_lockObject)
                    {
                        //if can locked, no read operation waiting
                        Queue = new List<string>();
                    }
                    await FileIO.AppendLinesAsync(_storageFile, listToWrite);
                }
                catch (Exception e)
                {
                    Logger.Error("Error while writing", e);
                    Queue.AddRange(listToWrite);
                }

            } while (Queue.Count != 0);
        }

        public async Task<List<string>> ReadLines()
        {
            List<string> queue;
            lock (_lockObject)
            {
                //if can locked, no write operation steals the queue
                queue = Queue;
            }
            queue.AddRange(await FileIO.ReadLinesAsync(_storageFile));

            return queue;
        }

        public Task Clear()
        {
            throw new System.NotImplementedException();
        }

        public Task RewriteFile(Action<List<string>, List<string>> action)
        {
            throw new NotImplementedException();
        }
    }
}