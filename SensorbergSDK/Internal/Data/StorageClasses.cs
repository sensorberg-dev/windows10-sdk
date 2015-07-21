using SQLite;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SensorbergSDK.Internal
{
    public struct DelayedActionData
    {
        public int Id;
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
        public History()
        {
            events = new List<HistoryEvent>();
            actions = new List<HistoryAction>();
        }

        [DataMember]
        public string deviceTimestamp { get; set; }
        [DataMember]
        public IList<HistoryEvent> events { get; set; }
        [DataMember]
        public IList<HistoryAction> actions { get; set; }
    }

    [DataContract]
    public class HistoryEvent
    {
        public HistoryEvent(DBHistoryEvent dbEvent)
        {
            pid = dbEvent.pid;
            dt = dbEvent.dt.ToString();
            trigger = dbEvent.trigger;
        }
        [DataMember]
        public string pid { get; set; } //beaconId
        [DataMember]
        public string dt { get; set; } //eventDate
        [DataMember]
        public int trigger { get; set; }
    }

    [DataContract]
    public class HistoryAction
    {
        public HistoryAction(DBHistoryAction dbAction)
        {
            eid = dbAction.eid;
            pid = dbAction.pid;
            dt = dbAction.dt.ToString();
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
        [PrimaryKey, AutoIncrement]
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
        [PrimaryKey]
        public string BeaconPid { get; set; }

        public DateTimeOffset EventTime { get; set; } // Time when this action should be executed
        
        public int EventType { get; set; }

    }

    //These actions are solved on background. Foreground app should deliver them to the listener app
    public class DBBeaconActionFromBackground
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string BeaconAction { get; set; }
        public string Payload { get; set; }

    }
}
