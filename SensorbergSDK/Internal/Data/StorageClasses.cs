using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Windows.Data.Json;
using Newtonsoft.Json;

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

        public HistoryEvent(DBHistoryEvent dbEvent)
        {
            pid = dbEvent.pid;
            dt = dbEvent.dt.ToString(History.TIMEFORMAT);
//            dt = dbEvent.dt.ToString(History.Formater.FormatProvider);
            trigger = dbEvent.trigger;
        }
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
        public HistoryAction(DBHistoryAction dbAction)
        {
            eid = dbAction.eid;
            pid = dbAction.pid;
            dt = dbAction.dt.ToString(History.TIMEFORMAT);
            //            dt = dbAction.dt.ToString(History.Formater.FormatProvider);
            trigger = dbAction.trigger;
        }
        [DataMember]
        public string eid { get; set; } //eventId
        [DataMember]
        public string pid { get; set; } //beaconId
        [DataMember]
        public string dt { get; set; } //eventDate
        [DataMember]
        public int trigger { get; set; }
        public bool Delivered { get; set; }
    }

    /// <summary>
    /// DBHistoryEvent is an internal class that is used to store event into sqlite
    /// </summary>
    public class DBHistoryEvent
    {
        public string pid { get; set; }//beaconId
        public DateTimeOffset dt { get; set; } //eventDate
        public int trigger { get; set; }
        public bool delivered { get; set; } //true, if event is sent to the server

    }
    /// <summary>
    /// DBHistoryAction is an internal class that is used to store action history into sqlite
    /// </summary>
    public class DBHistoryAction
    {
        public string eid { get; set; } //eventId
        public string pid { get; set; } //beaconId
        public DateTimeOffset dt { get; set; } //eventDate
        public int trigger { get; set; }
        public bool delivered { get; set; } //true, if event is sent to the server
    }

    /// <summary>
    /// DBDelayedAction is an internal class that is used for storing delayed actions to be
    /// processed later.
    /// </summary>
    public class DBDelayedAction
    {
        public int Id { get; set; }
        public DateTimeOffset DueTime { get; set; } // Time when this action should be executed
        public string ResolvedAction { get; set; }
        public string BeaconPid { get; set; }
        public int EventTypeDetectedByDevice { get; set; } // Event detected by the device that caused the action
        public bool Executed { get; set; }
    }

    /// <summary>
    /// DBBackgroundEventsHistory is an internal class that is used to filter and regocnize beacon events on background
    /// task.
    /// </summary>
    public class DBBackgroundEventsHistory
    {
        public string BeaconPid { get; set; }

        public DateTimeOffset EventTime { get; set; } // Time when this action should be executed
        
        public int EventType { get; set; }

    }

    //These actions are solved on background. Foreground app should deliver them to the listener app
    public class DBBeaconActionFromBackground
    {
        public int Id { get; set; }
        public string BeaconAction { get; set; }
        public string Payload { get; set; }

    }
}
