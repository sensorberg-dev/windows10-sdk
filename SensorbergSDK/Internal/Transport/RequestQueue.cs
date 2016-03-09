using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Simple queue for requests. When a request is added, it is handled automatically in due time.
    /// </summary>
    public sealed class RequestQueue : IDisposable
    {
        public event EventHandler<int> QueueCountChanged;
        private Task _workerTask;

        private Queue<Request> _requestQueue;
        private CancellationTokenSource _cancelToken;

        public RequestQueue()
        {
            _requestQueue = new Queue<Request>();
        }

        /// <summary>
        /// Shuts down the internal timer and after failing all pending requests clears the queue.
        /// </summary>
        public void Dispose()
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

        public void Add(Request request)
        {
            _requestQueue.Enqueue(request);

            if (_requestQueue.Count > 0 && _workerTask == null)
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

                                    Debug.WriteLine("RequestQueue.ServeNextRequestAsync(): Request with ID "
                                                    + currentRequest.RequestId + " failed, will try "
                                                    + numberOfTriesLeft + " more " + (numberOfTriesLeft > 1 ? "times" : "time"));

                                    _requestQueue.Enqueue(currentRequest);
                                }

                            }
                                break;
                            case RequestResultState.Success:
                                OnRequestServed(currentRequest, requestResult);
                                break;
                        }

                        currentRequest.IsBeingProcessed = false;
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

    }
}
