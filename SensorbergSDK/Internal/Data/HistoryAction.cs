// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Runtime.Serialization;

namespace SensorbergSDK.Internal.Data
{
    [DataContract]
    public class HistoryAction
    {
        [DataMember(Name = "eid")]
        public string EventId { get; set; } 
        [DataMember(Name = "pid")]
        public string BeaconId { get; set; }
        [DataMember(Name = "dt")]
        public string ActionTime { get; set; }
        [DataMember(Name = "location")]
        public string Location { get; set; }
        [DataMember(Name = "trigger")]
        public int Trigger { get; set; }
        public bool Delivered { get; set; }
        public bool Background { get; set; }

        protected bool Equals(HistoryAction other)
        {
            return string.Equals(EventId, other.EventId) && string.Equals(BeaconId, other.BeaconId) && string.Equals(ActionTime, other.ActionTime) && string.Equals(Location, other.Location) && Trigger == other.Trigger && Delivered == other.Delivered && Background == other.Background;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HistoryAction) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EventId?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (BeaconId?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (ActionTime?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Location?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ Trigger;
                hashCode = (hashCode*397) ^ Delivered.GetHashCode();
                hashCode = (hashCode*397) ^ Background.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(HistoryAction left, HistoryAction right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HistoryAction left, HistoryAction right)
        {
            return !Equals(left, right);
        }
    }

}
