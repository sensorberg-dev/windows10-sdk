using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Simple queue for requests. When a request is added, it is handled automatically in due time.
    /// </summary>
    public sealed class RequestQueue : IDisposable
    {
        public event EventHandler<int> QueueCountChanged;

        private const int ServeRequestIntervalInMilliseconds = 500;

        private List<Request> _requestList;
        private Timer _serveRequestTimer;
        private object _listLocker;
        private int _currentRequestIndex;

        public RequestQueue()
        {
            _listLocker = new object();
            _requestList = new List<Request>();
        }

        /// <summary>
        /// Shuts down the internal timer and after failing all pending requests clears the queue.
        /// </summary>
        public void Dispose()
        {
            if (_serveRequestTimer != null)
            {
                _serveRequestTimer.Dispose();
                _serveRequestTimer = null;
            }

            // Abort all pending requests
            while (_requestList.Count > 0)
            {
                OnRequestServed(_requestList[0], RequestResultState.None);
            }
        }

        public void Add(Request request)
        {
            lock(_listLocker)
            {
                _requestList.Add(request);

                if (_requestList.Count > 0 && _serveRequestTimer == null)
                {
                    //ServeNextRequestAsync(null);
                    _serveRequestTimer = new Timer(ServeNextRequestAsync, null, 0, ServeRequestIntervalInMilliseconds);
                }
                if (QueueCountChanged != null)
                {
                    QueueCountChanged(this, _requestList.Count);
                }
            }
        }

        /// <summary>
        /// Serves the request in the current index.
        /// </summary>
        /// <param name="state"></param>
        private async void ServeNextRequestAsync(object state)
        {
            int requestCount = 0;

            lock(_listLocker)
            {
                requestCount = _requestList.Count;
            }

            if (requestCount > 0)
            {
                if (_currentRequestIndex >= requestCount || _currentRequestIndex < 0)
                {
                    _currentRequestIndex = 0;
                }

                Request currentRequest = null;

                lock (_listLocker)
                {
                    if (_currentRequestIndex >= _requestList.Count)
                    {
                        Debug.WriteLine("More threads then requests ");
                        return;
                    }
                    currentRequest = _requestList.ElementAt(_currentRequestIndex++);
                }

                if (currentRequest != null && !currentRequest.IsBeingProcessed)
                {
                    currentRequest.IsBeingProcessed = true;
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

                    bool wasRemoved = false;

                    switch (requestResult)
                    {
                        case RequestResultState.Failed:
                            if (currentRequest.TryCount >= currentRequest.MaxNumberOfRetries)
                            {
                                // The maximum number of retries has been exceeded => fail
                                wasRemoved = OnRequestServed(currentRequest, requestResult);
                            }
                            else
                            {
                                int numberOfTriesLeft = currentRequest.MaxNumberOfRetries - currentRequest.TryCount;

                                System.Diagnostics.Debug.WriteLine("RequestQueue.ServeNextRequestAsync(): Request with ID "
                                    + currentRequest.RequestId + " failed, will try "
                                    + numberOfTriesLeft + " more " + (numberOfTriesLeft > 1 ? "times" : "time"));
                            }

                            break;
                        case RequestResultState.Success:
                            wasRemoved = OnRequestServed(currentRequest, requestResult);
                            break;
                    }

                    currentRequest.IsBeingProcessed = false;

                    if (wasRemoved && _currentRequestIndex > 0)
                    {
                        _currentRequestIndex--;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the request result and removes it from the queue.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="resultState"></param>
        /// <returns>True, if the request was successfully removed from the queue.</returns>
        private bool OnRequestServed(Request request, RequestResultState resultState)
        {
            bool wasRemoved = false;

            if (request != null)
            {
                lock (_listLocker)
                {
                    wasRemoved = _requestList.Remove(request);
                    request.NotifyResult(resultState);

                    if (_requestList.Count == 0 && _serveRequestTimer != null)
                    {
                        _serveRequestTimer.Dispose();
                        _serveRequestTimer = null;
                    }
                    if (QueueCountChanged != null)
                    {
                        QueueCountChanged(this, _requestList.Count);
                    }
                }
            }

            return wasRemoved;
        }
    }
}
