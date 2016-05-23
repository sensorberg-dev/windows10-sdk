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
        [DataMember(Name = "trigger")]
        public int Trigger { get; set; }
        public bool Delivered { get; set; }
        public bool Background { get; set; }
    }

}
