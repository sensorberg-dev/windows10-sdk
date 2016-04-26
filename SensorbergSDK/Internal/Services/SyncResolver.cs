// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task<int> CreateRequest(BeaconEventArgs beaconEventArgs)
        {
            int requestId = SDKData.Instance.NextId();
            logger.Debug("Resolver: Beacon " + beaconEventArgs.Beacon.Id1 + " " + beaconEventArgs.Beacon.Id2 + " " + beaconEventArgs.Beacon.Id3 + " ---> Request: " + requestId);
            Request request = new Request(beaconEventArgs, requestId);
            await Resolve(request);
            return requestId;
        }

        private async Task Resolve(Request request)
        {
            logger.Trace("take next request " + request.RequestId);
            request.TryCount++;
            RequestResultState requestResult = RequestResultState.None;

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

                }
                    break;
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