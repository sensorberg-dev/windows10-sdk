// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Data.Json;
using Newtonsoft.Json;

namespace SensorbergSDK.Internal.Data
{
    public static class FileStorageHelper
    {
        /// <summary>
        /// Creates from the given parameters a string.
        /// </summary>
        /// <param name="he">Historyevent to convert.</param>
        /// <returns>String representing the HistoryEvent.</returns>
        public static string EventToString(HistoryEvent he)
        {
            return string.Format("{0},{1},{2},{3}\n", he.pid, DateTimeOffset.Parse(he.dt).ToUnixTimeMilliseconds(), he.trigger, false);
        }

        /// <summary>
        /// Parses the list of strings to a List of HistoryEvents.
        /// </summary>
        /// <param name="strings">List of string representing a HistoryEvent.</param>
        /// <returns></returns>
        public static List<HistoryEvent> EventsFromStrings(IList<string> strings)
        {
            if (strings == null || strings.Count == 0)
            {
                return new List<HistoryEvent>();
            }
            List<HistoryEvent> events = new List<HistoryEvent>();
            foreach (string s in strings)
            {
                HistoryEvent he = EventFromString(s);
                if(he!= null)
                {
                    events.Add(he);
                }
            }
            return events;
        }

        /// <summary>
        /// Parse the given string to a HistoryEvent.
        /// </summary>
        /// <param name="s">Comma separated string representing a HistoryEvent.</param>
        /// <returns></returns>
        public static HistoryEvent EventFromString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] ss = s.Split(new char[] {','});
            if (ss.Length < 3)
            {
                return null;
            }

            HistoryEvent he = new HistoryEvent();
            he.pid = ss[0];

            try
            {
                he.dt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(ss[1])).ToString(History.TIMEFORMAT);
            }
            catch (FormatException)
            {
                Debug.WriteLine("ERROR: parsing event: "+ s);
                return null;
            }

            he.trigger = int.Parse(ss[2]);
            if (ss.Length > 3)
            {
                try
                {
                    he.Delivered = bool.Parse(ss[3]);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("ERROR: parsing event: " + s);
                }
            }
            return he;
        }

        public static string ActionToString(HistoryAction historyAction)
        {
            return ActionToString(historyAction.eid, historyAction.pid, DateTimeOffset.Parse(historyAction.dt), historyAction.trigger, historyAction.Delivered, historyAction.Background);
        }
        public static string ActionToString(string uuid, string beaconPid, DateTimeOffset timestamp, BeaconEventType beaconEventType)
        {
            return ActionToString(uuid, beaconPid, timestamp, (int) beaconEventType, false, false);
        }

        internal static string ActionToString(string uuid, string beaconPid, DateTimeOffset timestamp, int beaconEventType, bool delivered, bool background)
        {
            return string.Format("{0},{1},{2},{3},{4},{5}\n", uuid, beaconPid, timestamp.ToUnixTimeMilliseconds(), beaconEventType, delivered, background);
        }

        public static List<HistoryAction> ActionsFromStrings(IList<string> strings)
        {
            if (strings == null || strings.Count == 0)
            {
                return new List<HistoryAction>();
            }
            List<HistoryAction> actions = new List<HistoryAction>();
            foreach (string s in strings)
            {
                HistoryAction ha = ActionFromString(s);
                if (ha != null)
                {
                    actions.Add(ha);
                }
            }
            return actions;
        }

        /// <summary>
        /// Parse the given string to a HistoryAction.
        /// </summary>
        /// <param name="s">Comma separated string representing a HistoryAction.</param>
        /// <returns></returns>
        public static HistoryAction ActionFromString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] ss = s.Split(new char[] {','});
            if (ss.Length < 5)
            {
                return null;
            }

            HistoryAction ha = new HistoryAction();
            ha.eid = ss[0];
            ha.pid = ss[1];

            try
            {
                ha.dt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(ss[2])).ToString(History.TIMEFORMAT);
            }
            catch (FormatException)
            {
                Debug.WriteLine("ERROR: parsing action: " + s);
                return null;
            }

            ha.trigger = int.Parse(ss[3]);
            try
            {
                if (ss.Length > 5)
                {
                    ha.Delivered = bool.Parse(ss[4]);
                }
            }
            catch (FormatException)
            {
                Debug.WriteLine("ERROR: parsing action: " + s);
            }
            try
            {
                ha.Background = bool.Parse(ss[ss.Length - 1]);
            }
            catch (FormatException)
            {
                Debug.WriteLine("ERROR: parsing action: " + s);
            }

            return ha;
        }

        public static string DelayedActionToString(DelayedActionHelper delayedActionHelper)
        {
            return DelayedActionToString(delayedActionHelper.Content, delayedActionHelper.Offset, delayedActionHelper.Executed, delayedActionHelper.Id);
        }

        public static string DelayedActionToString(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType beaconEventType)
        {
            return DelayedActionToString(action, dueTime, beaconPid, beaconEventType, Guid.NewGuid());
        }

        public static string DelayedActionToString(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType beaconEventType, Guid guid)
        {
            string serializeObject = JsonConvert.SerializeObject(new SerializedAction() {Action = action, Time = dueTime, Beacon = beaconPid, Event = beaconEventType});
            return DelayedActionToString(Convert.ToBase64String(Encoding.UTF8.GetBytes(serializeObject)), dueTime, false, guid.ToString());
        }

        public static string DelayedActionToString(string action, DateTimeOffset dueTime, bool executed, string guid)
        {
            return string.Format("{0},{1},{2},{3}\n", guid, dueTime.ToUnixTimeMilliseconds(), executed, action);
        }


        public static List<DelayedActionHelper> DelayedActionsFromStrings(IList<string> strings)
        {
            if (strings == null || strings.Count == 0)
            {
                return new List<DelayedActionHelper>();
            }
            List<DelayedActionHelper> actions = new List<DelayedActionHelper>();
            foreach (string s in strings)
            {
                DelayedActionHelper ha = SimpleDelayedActionFromString(s);
                if (ha != null)
                {
                    actions.Add(ha);
                }
            }
            return actions;
        }

        public static DelayedActionHelper SimpleDelayedActionFromString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] ss = s.Split(new char[] { ',' });
            if (ss.Length < 4)
            {
                return null;
            }

            DelayedActionHelper dah = new DelayedActionHelper();
            dah.Id = ss[0];
            dah.Offset = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(ss[1]));
            dah.Executed = bool.Parse(ss[2]);
            dah.Content = ss[3];
            return dah;
        }

        public static DelayedActionData DelayedActionFromHelper(DelayedActionHelper delayedActionHelper)
        {
            if (delayedActionHelper == null)
            {
                return null;
            }
            string s = Encoding.UTF8.GetString(Convert.FromBase64String(delayedActionHelper.Content));
            SerializedAction deserializeObject = JsonConvert.DeserializeObject<SerializedAction>(s);
            DelayedActionData data = new DelayedActionData();
            data.Id = delayedActionHelper.Id;
            data.beaconPid = deserializeObject.Beacon;
            data.dueTime = deserializeObject.Time;
            data.eventTypeDetectedByDevice = deserializeObject.Event;

            data.resolvedAction = deserializeObject.Action;
            if (!string.IsNullOrEmpty(deserializeObject.Action.BeaconAction.PayloadString))
            {
                data.resolvedAction.BeaconAction.Payload = JsonObject.Parse(deserializeObject.Action.BeaconAction.PayloadString);
            }
            data.resolvedAction = deserializeObject.Action;

            return data;
        }

        public static Dictionary<string, Dictionary<string, long>> BackoundEventsFromString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            Dictionary<string, Dictionary<string, long>> dict = JsonConvert.DeserializeObject< Dictionary<string, Dictionary<string, long>>>(s);
            if (dict == null)
            {
                dict = new Dictionary<string, Dictionary<string, long>>();
            }
            return dict;
        }

        public static string BackoundEventsToString(Dictionary<string, Dictionary<string, long>> dic)
        {
            return JsonConvert.SerializeObject(dic);
        }

        public static string BeaconEventStateToString(string pid, BeaconEventType type, DateTimeOffset now)
        {
            return string.Format("{0},{1},{2}", pid, (int)type, now.ToUnixTimeMilliseconds());
        }

        public static BackgroundEvent BeaconEventStateFromString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] ss = s.Split(new char[] { ',' });
            if (ss.Length < 3)
            {
                return null;
            }

            BackgroundEvent be = new BackgroundEvent();
            be.BeaconID = ss[0];
            be.LastEvent = (BeaconEventType)int.Parse(ss[1]);
            be.EventTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(ss[2]));
            return be;
        }

        public static string BeaconActionToString(BeaconAction action)
        {
            return JsonConvert.SerializeObject(action);
        }

        public static BeaconAction BeaconActionFromString(string s)
        {
            BeaconAction action = JsonConvert.DeserializeObject<BeaconAction>(s);
            if (!string.IsNullOrEmpty(action?.PayloadString))
            {
                action.Payload = JsonObject.Parse(action.PayloadString);
            }
            return action;
        }

        public static HistoryAction ToHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType)
        {
            return new HistoryAction() { pid = beaconPid, dt = now.ToString(History.TIMEFORMAT), eid = uuid, trigger = (int)beaconEventType };
        }

        public static HistoryEvent ToHistoryEvent(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
        {
            return new HistoryEvent() { pid = pid, dt = timestamp.ToString(History.TIMEFORMAT), trigger = (int)eventType };
        }

        public class SerializedAction
        {
            public ResolvedAction Action { get; set; }
            public DateTimeOffset Time { get; set; }
            public string Beacon { get; set; }
            public BeaconEventType Event { get; set; }
        }

        public class DelayedActionHelper
        {
            public string Id { get; set; }
            public DateTimeOffset Offset { get; set; }
            public string Content { get; set; }

            public bool Executed { get; set; }
        }

    }
}