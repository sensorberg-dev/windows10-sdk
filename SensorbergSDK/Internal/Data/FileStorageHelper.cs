// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SensorbergSDK.Internal.Data
{
    public static class FileStorageHelper
    {
        public static string EventToString(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
        {
            return string.Format("{0},{1},{2},{3}\n", pid, timestamp.ToUnixTimeMilliseconds(), (int)eventType, false);
        }

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
            catch (FormatException e)
            {
                Debug.WriteLine("ERRO: parsing event: "+ s);
                return null;
            }
            he.trigger = int.Parse(ss[2]);
            if (ss.Length > 3)
            {
                try
                {
                    he.Delivered = bool.Parse(ss[3]);
                }
                catch (FormatException e)
                {
                    Debug.WriteLine("ERRO: parsing event: " + s);
                }
            }
            return he;
        }
    }
}