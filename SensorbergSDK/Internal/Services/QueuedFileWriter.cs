// Created by Kay Czarnotta on 08.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using MetroLog;

namespace SensorbergSDK.Internal.Services
{
    /// <summary>
    /// Queued writer class to avoid exceptions on file locks while reading and writing.
    /// </summary>
    public class QueuedFileWriter : IQueuedFileWriter
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<QueuedFileWriter>();
        private static readonly IBuffer LINE_END = CryptographicBuffer.ConvertStringToBinary("\n", BinaryStringEncoding.Utf8);
        private Semaphore _semaphore = new Semaphore(1,1);

        public event Action QueueEmpty;

        private readonly IStorageFolder _folder;
        private readonly string _fileName;
        private StorageFile _storageFile;
        private List<string> Queue { get; set; } = new List<string>();
        private Task _runningTask;
        private CancellationTokenSource CancelToken { get; set; }

        public QueuedFileWriter(IStorageFolder folder, string fileName)
        {
            _folder = folder;
            _fileName = fileName;
        }

        public async Task WriteLine(string line)
        {
            Logger.Trace("Append line: {0}", line);
            if (string.IsNullOrEmpty(line))
            {
                return;
            }
            Queue.Add(line);
            StartWorker();
        }

        private void StartWorker()
        {
            if (_runningTask == null || _runningTask.Status == TaskStatus.Canceled || _runningTask.Status == TaskStatus.Faulted || _runningTask.Status == TaskStatus.RanToCompletion)
            {
                Logger.Trace("Start writer");
                CancelToken?.Dispose();
                CancelToken = new CancellationTokenSource();
                (_runningTask = Task.Run(WriteLines, CancelToken.Token)).ConfigureAwait(false);
            }
        }

        private async Task WriteLines()
        {
            while (Queue.Count != 0)
            {
                try
                {
                    _semaphore.WaitOne();
                    await CheckFileInitialization();
                    using (IRandomAccessStream stream = await _storageFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
                    {
                        stream.Seek(stream.Size);
                        for (int i = 0; i < 10 && Queue.Count != 0; i++)
                        {
                            await stream.WriteAsync(CryptographicBuffer.ConvertStringToBinary(Queue[0], BinaryStringEncoding.Utf8));
                            await stream.WriteAsync(LINE_END);
                            await stream.FlushAsync();
                            Queue.RemoveAt(0);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error while writing", e);
                }
                finally
                {
                    Logger.Trace("Write end");
                    _semaphore.Release();
                }
            }

            _runningTask = null;
            QueueEmpty?.Invoke();
        }

        private async Task CheckFileInitialization()
        {
            if (_storageFile == null)
            {
                _storageFile = await _folder.CreateFileAsync(_fileName, CreationCollisionOption.OpenIfExists);
            }
        }

        public async Task<List<string>> ReadLines()
        {
            return await InternalReadLines();
        }

        private async Task<List<string>> InternalReadLines(bool ignoreSemaphore = false, int retryCount = 3)
        {
            if (retryCount < 0)
            {
                return null;
            }
            await CheckFileInitialization();

            try
            {
                if (!ignoreSemaphore)
                {
                    _semaphore.WaitOne();
                }
                Logger.Trace("Read");
                List<string> queue = new List<string>(Queue);
                queue.AddRange(await FileIO.ReadLinesAsync(await _folder.CreateFileAsync(_fileName, CreationCollisionOption.OpenIfExists)));
                return queue;
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay((int)Math.Pow(10, 3 - retryCount)*10);
                return await InternalReadLines(true, --retryCount);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while reading lines", ex);
            }
            finally
            {
                Logger.Trace("Read end");
                if (!ignoreSemaphore)
                {
                    _semaphore.Release();
                }
            }
            return new List<string>();
        }

        public async Task Clear()
        {
            try
            {
                CancelToken?.Cancel();
                CancelToken?.Dispose();
                CancelToken = null;
                _semaphore.WaitOne();
                Queue = new List<string>();
                if (_storageFile != null)
                {
                    await _storageFile.DeleteAsync();
                }
                _storageFile = null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RewriteFile(Action<List<string>, List<string>> action)
        {
            CancelToken?.Cancel();
            List<string> list = await InternalReadLines(true);

            await Clear();
            List<string> newList = new List<string>();
            action(list, newList);

            foreach (string s in newList)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    Queue.Add(s);
                }
            }
//            Queue.AddRange(newList);
            StartWorker();
        }

        public void Dispose()
        {
            CancelToken?.Cancel();
            CancelToken?.Dispose();
        }
    }
}