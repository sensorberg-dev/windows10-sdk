// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SensorbergSDK.Internal
{
    public class DelayedActionData
    {
        public string Id { get; set; }
        public ResolvedAction ResolvedAction { get; set; }
        public DateTimeOffset DueTime { get; set; }
        public string BeaconPid { get; set; }
        public BeaconEventType EventTypeDetectedByDevice { get; set; }
    }

    /// <summary>
    /// History, HistoryEvent,HistoryAction are internal classes that used to construct json structure for an HTTP post.
    /// </summary>
    [DataContract]
    public class History
    {
        public const string Timeformat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

        [DataMember(Name = "deviceTimestamp")]
        public string DeviceTimestamp { get; set; } = DateTime.UtcNow.ToString(Timeformat);
        [DataMember(Name= "events")]
        public IList<HistoryEvent> Events { get; set; } = new List<HistoryEvent>();
        [DataMember(Name= "actions")]
        public IList<HistoryAction> Actions { get; set; } = new List<HistoryAction>();
    }

    [DataContract]
    public class HistoryEvent
    {

        [DataMember(Name = "pid")]
        public string BeaconId { get; set; }
        [DataMember(Name = "dt")]
        public string EventTime { get; set; }
        [DataMember(Name = "trigger")]
        public int Trigger { get; set; }

        public bool Delivered { get; set; }
    }

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
