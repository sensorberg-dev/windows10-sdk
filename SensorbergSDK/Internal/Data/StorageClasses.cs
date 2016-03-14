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
        [DataMember]
        public string DeviceTimestamp { get; set; }
        [DataMember]
        public IList<HistoryEvent> Events { get; set; } = new List<HistoryEvent>();
        [DataMember]
        public IList<HistoryAction> Actions { get; set; } = new List<HistoryAction>();
    }

    [DataContract]
    public class HistoryEvent
    {
        public HistoryEvent(DBHistoryEvent dbEvent)
        {
            Pid = dbEvent.pid;
            Dt = dbEvent.dt.ToString("s");
            Trigger = dbEvent.trigger;
        }
        [DataMember]
        public string Pid { get; set; } //beaconId
        [DataMember]
        public string Dt { get; set; } //eventDate
        [DataMember]
        public int Trigger { get; set; }
    }

    [DataContract]
    public class HistoryAction
    {
        public HistoryAction(DBHistoryAction dbAction)
        {
            Eid = dbAction.eid;
            Pid = dbAction.pid;
            Dt = dbAction.dt.ToString("s");
            Trigger = dbAction.trigger;
        }
        [DataMember]
        public string Eid { get; set; } //eventId
        [DataMember]
        public string Pid { get; set; } //beaconId
        [DataMember]
        public string Dt { get; set; } //eventDate
        [DataMember]
        public int Trigger { get; set; }
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
