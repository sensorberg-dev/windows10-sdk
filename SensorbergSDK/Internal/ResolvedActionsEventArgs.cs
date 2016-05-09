// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using SensorbergSDK.Internal;
using System.Collections.Generic;
using System.Diagnostics;

namespace SensorbergSDK
{
    public sealed class ResolvedActionsEventArgs
    {
        public int RequestId
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        /// <summary>
        /// The beacon event type associated with the resolved action(s).
        /// </summary>
        public BeaconEventType BeaconEventType
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        /// <summary>
        /// Beacon PID from the resolve action request.
        /// Makes it easier to track complete requests and reporting the history to the server.
        /// </summary>
        public string BeaconPid
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        public IList<ResolvedAction> ResolvedActions
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }
    }
}
