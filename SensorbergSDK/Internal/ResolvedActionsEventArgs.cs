using SensorbergSDK.Internal;
using System.Collections.Generic;

namespace SensorbergSDK
{
    public sealed class ResolvedActionsEventArgs
    {
        public int RequestID
        {
            get;
            set;
        }

        /// <summary>
        /// The beacon event type associated with the resolved action(s).
        /// </summary>
        public BeaconEventType BeaconEventType
        {
            get;
            set;
        }

        /// <summary>
        /// Beacon PID from the resolve action request.
        /// Makes it easier to track complete requests and reporting the history to the server.
        /// </summary>
        public string BeaconPid
        {
            get;
            set;
        }

        public IList<ResolvedAction> ResolvedActions
        {
            get;
            set;
        }
    }
}
