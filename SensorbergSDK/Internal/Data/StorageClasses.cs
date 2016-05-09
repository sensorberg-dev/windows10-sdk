using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SensorbergSDK.Internal
{
    public class DelayedActionData
    {
        public string Id;
        public ResolvedAction resolvedAction;
        public DateTimeOffset dueTime;
        public string beaconPid;
        public BeaconEventType eventTypeDetectedByDevice;
    }

    /// <summary>
    /// History, HistoryEvent,HistoryAction are internal classes that used to construct json structure for an HTTP post
    /// </summary>
    /// 
    [DataContract]
    public class History
    {
        public const string TIMEFORMAT = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public string deviceTimestamp { get; set; }
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public IList<HistoryEvent> events { get; set; } = new List<HistoryEvent>();
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public IList<HistoryAction> actions { get; set; } = new List<HistoryAction>();
    }

    [DataContract]
    public class HistoryEvent
    {

        public HistoryEvent()
        {
        }

//        public HistoryEvent(DBHistoryEvent dbEvent)
//        {
//            pid = dbEvent.pid;
//            dt = dbEvent.dt.ToString(History.TIMEFORMAT);
////            dt = dbEvent.dt.ToString(History.Formater.FormatProvider);
//            trigger = dbEvent.trigger;
//        }
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public string pid { get; set; } //beaconId
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public string dt { get; set; } //eventDate
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public int trigger { get; set; }

        public bool Delivered { get; set; }
    }

    [DataContract]
    public class HistoryAction
    {
        public HistoryAction()
        {
        }
//        public HistoryAction(DBHistoryAction dbAction)
//        {
//            eid = dbAction.eid;
//            pid = dbAction.pid;
//            dt = dbAction.dt.ToString(History.TIMEFORMAT);
//            //            dt = dbAction.dt.ToString(History.Formater.FormatProvider);
//            trigger = dbAction.trigger;
//        }
        [DataMember]
        public string eid { get; set; } //eventId
        [DataMember]
        public string pid { get; set; } //beaconId
        [DataMember]
        public string dt { get; set; } //eventDate
        [DataMember]
        public int trigger { get; set; }
        public bool Delivered { get; set; }
        public bool Background { get; set; }
    }

}
