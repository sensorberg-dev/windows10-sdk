using System;
using System.Collections.Generic;

namespace SensorbergSDK.Internal
{
    public enum RequestResultState
    {
        None,
        Failed,
        Success
    };

    public sealed class Request
    {
        private const int DefaultMaxNumberOfRetries = 3;

        public event EventHandler<RequestResultState> Result;

        public BeaconEventArgs BeaconEventArgs
        {
            get;
            private set;
        }

        public IList<ResolvedAction> ResolvedActions
        {
            get;
            set;
        }

        public RequestResultState ResultState
        {
            get;
            private set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }

        /// <summary>
        /// This ID can be used to track this specific request (to match the outgoing request
        /// with the incoming result) if needed. The value can be arbitrary, but should naturally
        /// be unique.
        /// </summary>
        public int RequestId
        {
            get;
            private set;
        }

        public bool IsBeingProcessed
        {
            get;
            set;
        }

        public int TryCount
        {
            get;
            set;
        }

        public int MaxNumberOfRetries
        {
            get
            {
                return DefaultMaxNumberOfRetries;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="beaconEventArgs">The beacon event details.</param>
        /// <param name="requestId">The request ID (can be arbitrary).</param>
        public Request(BeaconEventArgs beaconEventArgs, int requestId)
        {
            BeaconEventArgs = beaconEventArgs;
            RequestId = requestId;
            ResultState = RequestResultState.None;
        }
         
        private Request()
        {
        }

        /// <summary>
        /// Called when this request has been handled. Notifies any listeners
        /// of the result.
        /// </summary>
        /// <param name="resultState">The request result.</param>
        public void NotifyResult(RequestResultState resultState)
        {
            ResultState = resultState;

            if (Result != null)
            {
                Result(this, ResultState);
            }
        }
    }
}
