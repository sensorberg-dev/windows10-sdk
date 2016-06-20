// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using MetroLog;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    public class Resolver : IResolver
    {
        private readonly bool _createdOnForeground;
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<Resolver>();
        public event EventHandler<ResolvedActionsEventArgs> ActionsResolved;
        public event EventHandler<string> FailedToResolveActions;
        public event Action Finished;
        private Task WorkerTask { get; set; }

        public Queue<Request> RequestQueue { get;}
        private CancellationTokenSource CancelToken { get; set; }
        public bool SynchronResolver { get; }

        public Resolver(bool synchron, bool createdOnForeground)
        {
            _createdOnForeground = createdOnForeground;
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
            int requestId = SdkData.NextId();
            Logger.Debug("Resolver: Beacon " + beaconEventArgs.Beacon.Id1 + " " + beaconEventArgs.Beacon.Id2 + " " + beaconEventArgs.Beacon.Id3 + " ---> Request: " + requestId);
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
            Logger.Trace("Add new request {0}", request.RequestId);
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
            Logger.Trace("take next request " + request.RequestId);
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
            Logger.Debug("request result " + request.RequestId + " " + requestResult);

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

                        Logger.Debug("RequestQueue.ServeNextRequestAsync(): Request with ID "
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

        private async void OnRequestServed(object sender, RequestResultState e)
        {
            Request request = sender as Request;

            if (request != null)
            {
                Logger.Debug("OnRequestServed: Request with ID " + request.RequestId + " was " + e);
                if (e == RequestResultState.Success)
                {

                    if (ActionsResolved != null)
                    {
                        ResolvedActionsEventArgs eventArgs = new ResolvedActionsEventArgs();
                        eventArgs.ResolvedActions = request.ResolvedActions;
                        eventArgs.RequestId = request.RequestId;
                        eventArgs.BeaconEventType = request.BeaconEventArgs.EventType;
                        eventArgs.Location = await ServiceManager.LocationService.GetGeoHashedLocation();
                        if (request.BeaconEventArgs != null && request.BeaconEventArgs.Beacon != null)
                        {
                            eventArgs.BeaconPid = request.BeaconEventArgs.Beacon.Pid;
                        }

                        ActionsResolved(this, eventArgs);
                    }
                }
                else if (e == RequestResultState.Failed)
                {
                    Logger.Info("OnRequestServed: Request with ID " + request.RequestId + " failed");

                    FailedToResolveActions?.Invoke(this, request.ErrorMessage);
                }
            }
        }
    }
}