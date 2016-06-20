// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using Windows.Data.Json;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDKTests
{
    [TestClass]
    public class FileStorageHelperTest
    {
        [DataTestMethod]
        [DataRow("1,1429192800000,1,False", "1","2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,1429192800000,1", "1", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,1429192800000,1,True", "1", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, true)]
        [DataRow("1,1429192800000,1,true", "1", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, true)]
        [DataRow("1,1429192800000,1,False,", "1", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,1429192800000,1,Faslse,", "1", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1sdafsadf3423r2352twet,1429192800000,1,false", "1sdafsadf3423r2352twet", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1sdafsadf3423r2352twet,63596412-000000-23423sdfgdfgs0000,1,false", null, null, null, null)]
        [DataRow("1,", null, null, null, null)]
        [DataRow(",,", null, null, null, null)]
        [DataRow("", null, null, null, null)]
        [DataRow(null, null, null, null, null)]
        public void TestHistoryEventParsing(string query, string beaconId, string eventTime, BeaconEventType beaconEventType, bool send)
        {
            HistoryEvent e = FileStorageHelper.EventFromString(query);

            //fallback for unparsable data
            if (beaconId == null && eventTime == null)
            {
                Assert.IsNull(e);
                return;
            }

            Assert.AreEqual(beaconId, e.BeaconId);
            Assert.AreEqual(eventTime, e.EventTime);
            Assert.AreEqual((int) beaconEventType, e.Trigger);
        }

        [TestMethod]
        public void TestHistoryEventToString()
        {
            string s = FileStorageHelper.EventToString(new HistoryEvent()
            {
                BeaconId = "1",
                EventTime = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000").ToString(History.Timeformat),
                Trigger = (int) BeaconEventType.Enter
            });
            Assert.AreEqual("1,1429192800000,1,False,\n", s);
        }

        [TestMethod]
        public void TestActionToString()
        {
            string s = FileStorageHelper.ActionToString("1", "1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter,"1");
            Assert.AreEqual("1,1,1429192800000,1,False,False,1\n", s);
            s = FileStorageHelper.ActionToString("1", "1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter,"1234235235");
            Assert.AreEqual("1,1,1429192800000,1,False,False,1234235235\n", s);
            s = FileStorageHelper.ActionToString("1", "1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter,"");
            Assert.AreEqual("1,1,1429192800000,1,False,False,\n", s);
            s = FileStorageHelper.ActionToString("1", "1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter,null);
            Assert.AreEqual("1,1,1429192800000,1,False,False,\n", s);


            s = FileStorageHelper.ActionToString(new HistoryAction() { EventId = "1", BeaconId = "1", ActionTime = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000").ToString(History.Timeformat), Trigger =  (int)BeaconEventType.Enter, Delivered = false});
            Assert.AreEqual("1,1,1429192800000,1,False,False,\n", s);
            s = FileStorageHelper.ActionToString(new HistoryAction() { EventId = "1", BeaconId = "1", ActionTime = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000").ToString(History.Timeformat), Trigger = (int)BeaconEventType.Enter, Delivered = false , Location = ""});
            Assert.AreEqual("1,1,1429192800000,1,False,False,\n", s);
            s = FileStorageHelper.ActionToString(new HistoryAction() { EventId = "1", BeaconId = "1", ActionTime = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000").ToString(History.Timeformat), Trigger = (int)BeaconEventType.Enter, Delivered = false, Location = "1234567"});
            Assert.AreEqual("1,1,1429192800000,1,False,False,1234567\n", s);
        }

        [DataTestMethod]
        [DataRow("1,2,1429192800000,1,False,False", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,2,1429192800000,1,False", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,2,1429192800000,1,False,True", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, true)]
        [DataRow("1,2,1429192800000,1,False,true", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, true)]
        [DataRow("1,2,1429192800000,1,False,False,", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,2,1429192800000,1,False,Faslse,", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1sdafsadf3423r2352twet,asdasdasdag321rqwf-dsafdsg-sadg,1429192800000,1,false,False", "1sdafsadf3423r2352twet", "asdasdasdag321rqwf-dsafdsg-sadg","2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1sdafsadf3423r2352twet,sadfasdf23e-rfr12e-2wrweafr21-asd,63596412-000000-23423sdfgdfgs0000,1,false,False", null,null, null, null, null)]
        [DataRow("1,", null, null, null, null, null)]
        [DataRow(",,", null, null, null, null, null)]
        [DataRow("", null, null, null, null, null)]
        [DataRow(null, null, null, null, null, null)]
        public void TestHistoryActionParsing(string query, string uuid, string beaconId, string eventTime, BeaconEventType beaconEventType, bool send)
        {
            HistoryAction a = FileStorageHelper.ActionFromString(query);

            //fallback for unparsable data
            if (beaconId == null && eventTime == null)
            {
                Assert.IsNull(a);
                return;
            }

            Assert.AreEqual(beaconId, a.BeaconId);
            Assert.AreEqual(eventTime, a.ActionTime);
            Assert.AreEqual(uuid, a.EventId);
            Assert.AreEqual((int)beaconEventType, a.Trigger);
        }

        [TestMethod]
        public void TestDelayedActionToString()
        {
            ResolvedAction action = new ResolvedAction();
            action.BeaconAction = new BeaconAction();
            action.BeaconAction.Body = "body";
            action.BeaconAction.Id = 1;
            action.BeaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            action.BeaconAction.Subject = "Subject";
            action.BeaconAction.Type = BeaconActionType.InApp;
            action.BeaconAction.Url = "http://sensorberg.com";
            action.BeaconAction.Uuid = "uuid";
            action.Delay = 123;
            action.BeaconPids = new List<string>() { "1", "2", "3", "4" };
            action.EventTypeDetectedByDevice = BeaconEventType.EnterExit;
            action.ReportImmediately = true;
            action.SendOnlyOnce = true;
            action.SuppressionTime = 321;
            action.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2015-04-15T12:00:00.000+0000")}
            };

            Guid guid = Guid.NewGuid();
            string s = FileStorageHelper.DelayedActionToString(action, DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), "123", BeaconEventType.Enter, guid, "123");
            Assert.AreEqual(guid+ ",1429192800000,False,eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJlaWQiOiJ1dWlkIiwiU3ViamVjdCI6IlN1YmplY3QiLCJCb2R5IjoiYm9keSIsIlVybCI6Imh0dHA6Ly9zZW5zb3JiZXJnLmNvbSIsIlBheWxvYWRTdHJpbmciOiJ7XCJwYXlcIjpcImxvYWRcIn0ifSwiYmVhY29ucyI6WyIxIiwiMiIsIjMiLCI0Il0sInRyaWdnZXIiOjMsIkRlbGF5IjoxMjMsIlNlbmRPbmx5T25jZSI6dHJ1ZSwic3VwcHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=,123\n", s);
        }

        [TestMethod]
        public void TestDelayedActionHelperToString()
        {
            Guid guid = Guid.NewGuid();
            DelayedActionHelper helper = new DelayedActionHelper();
            helper.Id = guid.ToString();
            helper.Executed = false;
            helper.Offset = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000");
            helper.Content =
                "eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJlaWQiOiJ1dWlkIiwiU3ViamVjdCI6IlN1YmplY3QiLCJCb2R5IjoiYm9keSIsIlVybCI6Imh0dHA6Ly9zZW5zb3JiZXJnLmNvbSIsIlBheWxvYWRTdHJpbmciOiJ7XCJwYXlcIjpcImxvYWRcIn0ifSwiYmVhY29ucyI6WyIxIiwiMiIsIjMiLCI0Il0sInRyaWdnZXIiOjMsIkRlbGF5IjoxMjMsIlNlbmRPbmx5T25jZSI6dHJ1ZSwic3VwcHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=";

            string s = FileStorageHelper.DelayedActionToString(helper);
            Assert.AreEqual(guid + ",1429192800000,False,eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJlaWQiOiJ1dWlkIiwiU3ViamVjdCI6IlN1YmplY3QiLCJCb2R5IjoiYm9keSIsIlVybCI6Imh0dHA6Ly9zZW5zb3JiZXJnLmNvbSIsIlBheWxvYWRTdHJpbmciOiJ7XCJwYXlcIjpcImxvYWRcIn0ifSwiYmVhY29ucyI6WyIxIiwiMiIsIjMiLCI0Il0sInRyaWdnZXIiOjMsIkRlbGF5IjoxMjMsIlNlbmRPbmx5T25jZSI6dHJ1ZSwic3VwcHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=,\n", s);
        }
        [TestMethod]
        public void TestDelayedActionFromString()
        {
            Guid guid = Guid.NewGuid();
            DelayedActionHelper simpleDelayedActionFromString = FileStorageHelper.SimpleDelayedActionFromString(guid+ ",1429192800000,False,eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJlaWQiOiJ1dWlkIiwiU3ViamVjdCI6IlN1YmplY3QiLCJCb2R5IjoiYm9keSIsIlVybCI6Imh0dHA6Ly9zZW5zb3JiZXJnLmNvbSIsIlBheWxvYWRTdHJpbmciOiJ7XCJwYXlcIjpcImxvYWRcIn0ifSwiYmVhY29ucyI6WyIxIiwiMiIsIjMiLCI0Il0sInRyaWdnZXIiOjMsIkRlbGF5IjoxMjMsIlNlbmRPbmx5T25jZSI6dHJ1ZSwic3VwcHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=");

            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), simpleDelayedActionFromString.Offset, "Wrong offset");
            Assert.IsFalse(simpleDelayedActionFromString.Executed, "Is executed");
            string s =
                "eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJlaWQiOiJ1dWlkIiwiU3ViamVjdCI6IlN1YmplY3QiLCJCb2R5IjoiYm9keSIsIlVybCI6Imh0dHA6Ly9zZW5zb3JiZXJnLmNvbSIsIlBheWxvYWRTdHJpbmciOiJ7XCJwYXlcIjpcImxvYWRcIn0ifSwiYmVhY29ucyI6WyIxIiwiMiIsIjMiLCI0Il0sInRyaWdnZXIiOjMsIkRlbGF5IjoxMjMsIlNlbmRPbmx5T25jZSI6dHJ1ZSwic3VwcHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=";
            Assert.AreEqual(s, simpleDelayedActionFromString.Content, "Wrong content");

            ResolvedAction action = new ResolvedAction();
            action.BeaconAction = new BeaconAction();
            action.BeaconAction.Body = "body";
            action.BeaconAction.Id = 1;
            action.BeaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            action.BeaconAction.Subject = "Subject";
            action.BeaconAction.Type = BeaconActionType.InApp;
            action.BeaconAction.Url = "http://sensorberg.com";
            action.BeaconAction.Uuid = "uuid";
            action.Delay = 123;
            action.BeaconPids = new List<string>() { "1", "2", "3", "4" };
            action.EventTypeDetectedByDevice = BeaconEventType.EnterExit;
            action.ReportImmediately = true;
            action.SendOnlyOnce = true;
            action.SuppressionTime = 321;
            action.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2015-04-15T12:00:00.000+0000")}
            };
            DelayedActionData data = FileStorageHelper.DelayedActionFromHelper(simpleDelayedActionFromString);
            Assert.AreEqual("123", data.BeaconPid, "Wrong beacon pid");
            Assert.AreEqual(BeaconEventType.Enter, data.EventTypeDetectedByDevice, "Wrong event type");
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), data.DueTime, "Wrong time");
            Assert.AreEqual(guid.ToString(), data.Id, "ID isnt set");
            Assert.AreEqual(action, data.ResolvedAction);

            Assert.IsNull(FileStorageHelper.SimpleDelayedActionFromString(""));
            Assert.IsNull(FileStorageHelper.SimpleDelayedActionFromString(null));
            Assert.IsNull(FileStorageHelper.DelayedActionFromHelper(null));
        }

        [TestMethod]
        public void TestBackgroundEventFromString()
        {
            string s = "{\"1\":{\"time\":1429192800000,\"event\":1},\"2\":{\"time\":1429192800000,\"event\":1}}";
            Dictionary<string, Dictionary<string, long>> dict = new Dictionary<string, Dictionary<string, long>>
            {
                {
                    "1", new Dictionary<string, long> {{"time", 1429192800000}, {"event", (long) BeaconEventType.Enter}}
                },
                {
                    "2", new Dictionary<string, long> {{"time", 1429192800000}, {"event", (long) BeaconEventType.Enter}}
                }
            };
            Dictionary<string, Dictionary<string, long>> readDict = FileStorageHelper.BackoundEventsFromString(s);
            Assert.AreEqual(dict.Count, readDict.Count, "Not same element count");
                        CollectionAssert.AreEqual(dict["1"], readDict["1"]);
                        CollectionAssert.AreEqual(dict["2"], readDict["2"]);


            Assert.IsNull(FileStorageHelper.BackoundEventsFromString(null));
        }
        [TestMethod]
        public void TestBackgroundEventToString()
        {
            string s = "{\"1\":{\"time\":1429192800000,\"event\":1},\"2\":{\"time\":1429192800000,\"event\":1}}";
            Dictionary<string, Dictionary<string, long>> dict = new Dictionary<string, Dictionary<string, long>>
            {
                {
                    "1", new Dictionary<string, long> {{"time", 1429192800000}, {"event", (long) BeaconEventType.Enter}}
                },
                {
                    "2", new Dictionary<string, long> {{"time", 1429192800000}, {"event", (long) BeaconEventType.Enter}}
                }
            };
            Assert.AreEqual(s, FileStorageHelper.BackoundEventsToString(dict));
        }

        [TestMethod]
        public void TestEventStateStorage()
        {
            string s = FileStorageHelper.BeaconEventStateToString("1", BeaconEventType.Enter, DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"));
            Assert.AreEqual("1,1,1429192800000",s);

            BackgroundEvent be = FileStorageHelper.BeaconEventStateFromString(s);
            Assert.AreEqual("1",be.BeaconId);
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), be.EventTime);
            Assert.AreEqual(BeaconEventType.Enter,be.LastEvent);
            Assert.IsNull(FileStorageHelper.BeaconEventStateFromString(""));
            Assert.IsNull(FileStorageHelper.BeaconEventStateFromString(null));
        }

        [TestMethod]
        public void TestBeaconActionToString()
        {
            string s = "{\"Id\":1,\"Type\":3,\"eid\":\"uuid\",\"Subject\":\"Subject\",\"Body\":\"body\",\"Url\":\"http://sensorberg.com\",\"PayloadString\":\"{\\\"pay\\\":\\\"load\\\"}\"}";
            BeaconAction beaconAction = new BeaconAction();
            beaconAction.Body = "body";
            beaconAction.Id = 1;
            beaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            beaconAction.Subject = "Subject";
            beaconAction.Type = BeaconActionType.InApp;
            beaconAction.Url = "http://sensorberg.com";
            beaconAction.Uuid = "uuid";

            string beaconString = FileStorageHelper.BeaconActionToString(beaconAction);
            Assert.AreEqual(s,beaconString);
        }

        [TestMethod]
        public void TestBeaconActionFromString()
        {
            string s = "{\"Id\":1,\"Type\":3,\"eid\":\"uuid\",\"Subject\":\"Subject\",\"Body\":\"body\",\"Url\":\"http://sensorberg.com\",\"PayloadString\":\"{\\\"pay\\\":\\\"load\\\"}\"}";
            BeaconAction beaconAction = new BeaconAction();
            beaconAction.Body = "body";
            beaconAction.Id = 1;
            beaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            beaconAction.Subject = "Subject";
            beaconAction.Type = BeaconActionType.InApp;
            beaconAction.Url = "http://sensorberg.com";
            beaconAction.Uuid = "uuid";

            BeaconAction action = FileStorageHelper.BeaconActionFromString(s);

            Assert.AreEqual(beaconAction, action);
        }
    }
}