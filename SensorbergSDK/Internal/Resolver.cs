using SensorbergSDK.Internal;
using System;
using System.Collections.Generic;

namespace SensorbergSDK
{
    /// <summary>
    /// Manages resolving the actions associated to beacon events.
    /// </summary>
	public sealed class Resolver
	{
        public event EventHandler<ResolvedActionsEventArgs> ActionsResolved;
        public event EventHandler<string> FailedToResolveActions;

        public event EventHandler<int> RequestQueueCountChanged
        {
            add { _requestQueue.QueueCountChanged += value; }
            remove { _requestQueue.QueueCountChanged -= value; }
        }

        private RequestQueue _requestQueue;
        private Dictionary<string, string> _filter = new Dictionary<string, string>();

        public Resolver()
        {
            _requestQueue = new RequestQueue();
        }

        public void ClearRequests()
        {
            _requestQueue.Dispose();
        }

        /// <summary>
        /// Creates and schedules an execution of a request for the given beacon event.
        /// </summary>
        /// <param name="beaconEventArgs">The beacon event details.</param>
        /// <returns>The request ID.</returns>
        public int CreateRequest(BeaconEventArgs beaconEventArgs)
        {
            int requestId = SDKData.Instance.NextId();
            Request request = new Request(beaconEventArgs, requestId);
            request.Result += OnRequestResult;
            _requestQueue.Add(request);
            return requestId;
        }

        /// <summary>
        /// Handles a completed (or failed) request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRequestResult(object sender, RequestResultState e)
        {
            Request request = sender as Request;

            if (request != null)
            {
                request.Result -= OnRequestResult;

                if (e == RequestResultState.Success)
                {
                    System.Diagnostics.Debug.WriteLine("Resolver.OnRequestResult(): Request with ID " + request.RequestId + " was successful");

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
                    System.Diagnostics.Debug.WriteLine("Resolver.OnRequestResult(): Request with ID " + request.RequestId + " failed");

                    if (FailedToResolveActions != null)
                    {
                        FailedToResolveActions(this, request.ErrorMessage);
                    }
                }

                request = null;        
            }
        }
    }
}


