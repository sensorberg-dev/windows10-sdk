// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetroLog;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Simple queue for requests. When a request is added, it is handled automatically in due time.
    /// </summary>
    public sealed class RequestQueue:IDisposable
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<RequestQueue>();
        public event EventHandler<int> QueueCountChanged;
        private Task _workerTask;

        private readonly Queue<Request> _requestQueue;
        private CancellationTokenSource _cancelToken;

        public RequestQueue()
        {
            _requestQueue = new Queue<Request>();
        }

        /// <summary>
        /// Returns the element count inside the queue.
        /// </summary>
        public int QueueSize
        {
            get { return _requestQueue.Count; }
        }

        /// <summary>
        /// Clears the queue while failing all pending requests.
        /// </summary>
        public void Clear()
        {
            Cancel();

            // Abort all pending requests
            while (_requestQueue.Count > 0)
            {
                OnRequestServed(_requestQueue.Dequeue(), RequestResultState.None);
            }
        }

        private void Cancel()
        {
            _cancelToken?.Cancel();
            _cancelToken?.Dispose();
            _cancelToken = null;
            _workerTask = null;
        }

        /// <summary>
        /// Adds an Requests to the queue.
        /// </summary>
        /// <param name="request"></param>
        public void Add(Request request)
        {
            _requestQueue.Enqueue(request);
            Logger.Trace("Add new request {0}", request.RequestId);
            if (_requestQueue.Count > 0 &&
                (_workerTask == null || _workerTask.Status == TaskStatus.Canceled || _workerTask.Status == TaskStatus.Faulted || _workerTask.Status == TaskStatus.RanToCompletion))
            {
                _cancelToken = new CancellationTokenSource();
                (_workerTask = Task.Run(ServeNextRequestAsync, _cancelToken.Token)).ConfigureAwait(false);
            }
            QueueCountChanged?.Invoke(this, _requestQueue.Count);
        }

        /// <summary>
        /// Serves the request in the current index.
        /// </summary>
        private async Task ServeNextRequestAsync()
        {
            try
            {
                while (_requestQueue.Count != 0)
                {
                    Request currentRequest = _requestQueue.Dequeue();

                    if (currentRequest != null)
                    {
                        Logger.Trace("RequestQueue: take next request " + currentRequest.RequestId);
                        currentRequest.TryCount++;
                        RequestResultState requestResult = RequestResultState.None;

                        try
                        {
                            requestResult = await ServiceManager.LayoutManager.ExecuteRequestAsync(currentRequest);
                        }
                        catch (ArgumentNullException ex)
                        {
                            currentRequest.ErrorMessage = ex.Message;
                            requestResult = RequestResultState.Failed;
                        }
                        catch (Exception ex)
                        {
                            currentRequest.ErrorMessage = ex.Message;
                            requestResult = RequestResultState.Failed;
                        }
                        Logger.Debug("RequestQueue: request result " + currentRequest.RequestId + " " + requestResult);

                        switch (requestResult)
                        {
                            case RequestResultState.Failed:
                            {
                                if (currentRequest.TryCount >= currentRequest.MaxNumberOfRetries)
                                {
                                    // The maximum number of retries has been exceeded => fail
                                    OnRequestServed(currentRequest, requestResult);
                                }
                                else
                                {
                                    int numberOfTriesLeft = currentRequest.MaxNumberOfRetries - currentRequest.TryCount;

                                    Logger.Debug("RequestQueue.ServeNextRequestAsync(): Request with ID "
                                                 + currentRequest.RequestId + " failed, will try "
                                                 + numberOfTriesLeft + " more " + (numberOfTriesLeft > 1 ? "times" : "time"));

                                    _requestQueue.Enqueue(currentRequest);
                                }

                                break;
                            }
                            case RequestResultState.Success:
                            {
                                OnRequestServed(currentRequest, requestResult);
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                Cancel();
            }
        }

        /// <summary>
        /// Sets the request result and removes it from the queue.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="resultState"></param>
        /// <returns>True, if the request was successfully removed from the queue.</returns>
        private void OnRequestServed(Request request, RequestResultState resultState)
        {
            if (request != null)
            {
                request.NotifyResult(resultState);

                if (_requestQueue.Count == 0 && _cancelToken != null)
                {
                    Cancel();
                }
                QueueCountChanged?.Invoke(this, _requestQueue.Count);
            }
        }

        public void Dispose()
        {
            _cancelToken?.Dispose();
        }
    }
}
