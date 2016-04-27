// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetroLog;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal.Services
{
    public class SyncResolver : IResolver
    {
        private static readonly ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<SyncResolver>();
        public event EventHandler<ResolvedActionsEventArgs> ActionsResolved;
        public event EventHandler<string> FailedToResolveActions;
        public event Action Finished;
        private Task WorkerTask { get; set; }

        public Queue<Request> RequestQueue { get;}
        private CancellationTokenSource CancelToken { get; set; }
        public bool SynchronResolver { get; }

        public SyncResolver(bool synchron)
        {
            SynchronResolver = synchron;

            if (!SynchronResolver)
            {
                RequestQueue= new Queue<Request>();
            }
        }
        public void Dispose()
        {
            CancelToken?.Dispose();
        }

        public async Task<int> CreateRequest(BeaconEventArgs beaconEventArgs)
        {
            int requestId = SDKData.Instance.NextId();
            logger.Debug("Resolver: Beacon " + beaconEventArgs.Beacon.Id1 + " " + beaconEventArgs.Beacon.Id2 + " " + beaconEventArgs.Beacon.Id3 + " ---> Request: " + requestId);
            Request request = new Request(beaconEventArgs, requestId);
            if (SynchronResolver)
            {
                await Resolve(request);
                Finished?.Invoke();
            }
            else
            {
                AddAsynchronRequest(request);
            }
            return requestId;
        }

        private void AddAsynchronRequest(Request request)
        {
            RequestQueue.Enqueue(request);
            logger.Trace("Add new request {0}", request.RequestId);
            if (RequestQueue.Count > 0 &&
                (WorkerTask == null || WorkerTask.Status == TaskStatus.Canceled || WorkerTask.Status == TaskStatus.Faulted || WorkerTask.Status == TaskStatus.RanToCompletion))
            {
                CancelToken = new CancellationTokenSource();
                (WorkerTask = Task.Run(ServeNextRequest, CancelToken.Token)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Serves the request in the current index.
        /// </summary>
        private async Task ServeNextRequest()
        {
            try
            {
                while (RequestQueue.Count != 0)
                {
                    Request request = RequestQueue.Dequeue();
                    await Resolve(request);
                }
            }
            finally
            {
                Cancel();
            }
        }
        private void Cancel()
        {
            CancelToken?.Cancel();
            CancelToken?.Dispose();
            CancelToken = null;
            WorkerTask = null;
                Finished?.Invoke();
        }

        private async Task Resolve(Request request)
        {
            logger.Trace("take next request " + request.RequestId);
            request.TryCount++;
            RequestResultState requestResult;

            try
            {
                requestResult = await ServiceManager.LayoutManager.ExecuteRequestAsync(request);
            }
            catch (ArgumentNullException ex)
            {
                request.ErrorMessage = ex.Message;
                requestResult = RequestResultState.Failed;
            }
            catch (Exception ex)
            {
                request.ErrorMessage = ex.Message;
                requestResult = RequestResultState.Failed;
            }
            logger.Debug("request result " + request.RequestId + " " + requestResult);

            switch (requestResult)
            {
                case RequestResultState.Failed:
                {
                    if (request.TryCount >= request.MaxNumberOfRetries)
                    {
                        // The maximum number of retries has been exceeded => fail
                        OnRequestServed(request, requestResult);
                    }
                    else
                    {
                        int numberOfTriesLeft = request.MaxNumberOfRetries - request.TryCount;

                        logger.Debug("RequestQueue.ServeNextRequestAsync(): Request with ID "
                                     + request.RequestId + " failed, will try "
                                     + numberOfTriesLeft + " more " + (numberOfTriesLeft > 1 ? "times" : "time"));

                        await Resolve(request);
                    }

                    break;
                }
                case RequestResultState.Success:
                {
                    OnRequestServed(request, requestResult);
                    break;
                }
            }
        }

        private void OnRequestServed(object sender, RequestResultState e)
        {
            Request request = sender as Request;

            if (request != null)
            {
                logger.Debug("OnRequestServed: Request with ID " + request.RequestId + " was " + e);
                if (e == RequestResultState.Success)
                {

                    if (ActionsResolved != null)
                    {
                        ResolvedActionsEventArgs eventArgs = new ResolvedActionsEventArgs();
                        eventArgs.ResolvedActions = request.ResolvedActions;
                        eventArgs.RequestID = request.RequestId;
                        eventArgs.BeaconEventType = request.BeaconEventArgs.EventType;

                        if (request.BeaconEventArgs != null && request.BeaconEventArgs.Beacon != null)
                        {
                            eventArgs.BeaconPid = request.BeaconEventArgs.Beacon.Pid;
                        }

                        ActionsResolved(this, eventArgs);
                    }
                }
                else if (e == RequestResultState.Failed)
                {
                    logger.Info("OnRequestServed: Request with ID " + request.RequestId + " failed");

                    FailedToResolveActions?.Invoke(this, request.ErrorMessage);
                }
            }
        }
    }
}