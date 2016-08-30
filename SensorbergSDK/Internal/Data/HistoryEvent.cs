// Created by Kay Czarnotta on 10.05.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System.Runtime.Serialization;

namespace SensorbergSDK.Internal.Data
{
    [DataContract]
    public class HistoryEvent
    {

        [DataMember(Name = "pid")]
        public string BeaconId { get; set; }
        [DataMember(Name = "dt")]
        public string EventTime { get; set; }
        [DataMember(Name = "trigger")]
        public int Trigger { get; set; }
        [DataMember(Name = "location")]
        public string Location { get; set; }

        public bool Delivered { get; set; }

        protected bool Equals(HistoryEvent other)
        {
            return string.Equals(BeaconId, other.BeaconId) && string.Equals(EventTime, other.EventTime) && Trigger == other.Trigger && string.Equals(Location, other.Location) && Delivered == other.Delivered;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((HistoryEvent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = BeaconId?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (EventTime?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ Trigger;
                hashCode = (hashCode*397) ^ (Location?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ Delivered.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(HistoryEvent left, HistoryEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HistoryEvent left, HistoryEvent right)
        {
            return !Equals(left, right);
        }
    }
}