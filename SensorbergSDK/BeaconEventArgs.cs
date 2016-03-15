using System;

namespace SensorbergSDK
{
    /// <summary>
    /// Beacon event type.
    /// </summary>
    public enum BeaconEventType
    {
        None,
        Enter, // Entered to range of beacon event
        Exit, // Exit from beacon range event
        EnterExit // This we get from the server when the trigger can be both
    };

    public sealed class BeaconEventArgs
    {
        public BeaconEventType EventType
        {
            get;
            set;
        }

        public Beacon Beacon
        {
            get;
            set;
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get
            {
                if (Beacon != null)
                {
                    return Beacon.Timestamp;
                }
                return DateTimeOffset.MinValue;
            }
        }
    }
}