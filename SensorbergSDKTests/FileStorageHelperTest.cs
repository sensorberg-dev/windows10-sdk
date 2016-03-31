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
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;

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

            Assert.AreEqual(beaconId, e.pid);
            Assert.AreEqual(eventTime, e.dt);
            Assert.AreEqual((int) beaconEventType, e.trigger);
        }

        [TestMethod]
        public void TestHistoryEventToString()
        {
            string s = FileStorageHelper.EventToString("1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter);
            Assert.AreEqual("1,1429192800000,1,False\n", s);
        }

        [TestMethod]
        public void TestActionToString()
        {
            string s = FileStorageHelper.ActionToString("1", "1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter);
            Assert.AreEqual("1,1,1429192800000,1,False\n", s);
            s = FileStorageHelper.ActionToString(new HistoryAction() { eid = "1", pid = "1", dt = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000").ToString(History.TIMEFORMAT), trigger =  (int)BeaconEventType.Enter, Delivered = false});
            Assert.AreEqual("1,1,1429192800000,1,False\n", s);
        }

        [DataTestMethod]
        [DataRow("1,2,1429192800000,1,False", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,2,1429192800000,1", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,2,1429192800000,1,True", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, true)]
        [DataRow("1,2,1429192800000,1,true", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, true)]
        [DataRow("1,2,1429192800000,1,False,", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1,2,1429192800000,1,Faslse,", "1", "2", "2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1sdafsadf3423r2352twet,asdasdasdag321rqwf-dsafdsg-sadg,1429192800000,1,false", "1sdafsadf3423r2352twet", "asdasdasdag321rqwf-dsafdsg-sadg","2015-04-16T14:00:00.000+00:00", BeaconEventType.Enter, false)]
        [DataRow("1sdafsadf3423r2352twet,sadfasdf23e-rfr12e-2wrweafr21-asd,63596412-000000-23423sdfgdfgs0000,1,false", null,null, null, null, null)]
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

            Assert.AreEqual(beaconId, a.pid);
            Assert.AreEqual(eventTime, a.dt);
            Assert.AreEqual(uuid, a.eid);
            Assert.AreEqual((int)beaconEventType, a.trigger);
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
            action.SupressionTime = 321;
            action.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2015-04-15T12:00:00.000+0000")}
            };

            Guid guid = Guid.NewGuid();
            string s = FileStorageHelper.DelayedActionToString(action, DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), "123", BeaconEventType.Enter, guid);
            Assert.AreEqual(guid+",1429192800000,False,eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJVdWlkIjoidXVpZCIsIlN1YmplY3QiOiJTdWJqZWN0IiwiQm9keSI6ImJvZHkiLCJVcmwiOiJodHRwOi8vc2Vuc29yYmVyZy5jb20iLCJQYXlsb2FkU3RyaW5nIjoie1wicGF5XCI6XCJsb2FkXCJ9In0sIkJlYWNvblBpZHMiOlsiMSIsIjIiLCIzIiwiNCJdLCJFdmVudFR5cGVEZXRlY3RlZEJ5RGV2aWNlIjozLCJEZWxheSI6MTIzLCJTZW5kT25seU9uY2UiOnRydWUsIlN1cHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=\n", s);
        }

        [TestMethod]
        public void TestDelayedActionHelperToString()
        {
            Guid guid = Guid.NewGuid();
            FileStorageHelper.DelayedActionHelper helper = new FileStorageHelper.DelayedActionHelper();
            helper.Id = guid.ToString();
            helper.Executed = false;
            helper.Offset = DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000");
            helper.Content =
                "eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJVdWlkIjoidXVpZCIsIlN1YmplY3QiOiJTdWJqZWN0IiwiQm9keSI6ImJvZHkiLCJVcmwiOiJodHRwOi8vc2Vuc29yYmVyZy5jb20iLCJQYXlsb2FkU3RyaW5nIjoie1wicGF5XCI6XCJsb2FkXCJ9In0sIkJlYWNvblBpZHMiOlsiMSIsIjIiLCIzIiwiNCJdLCJFdmVudFR5cGVEZXRlY3RlZEJ5RGV2aWNlIjozLCJEZWxheSI6MTIzLCJTZW5kT25seU9uY2UiOnRydWUsIlN1cHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=";

            string s = FileStorageHelper.DelayedActionToString(helper);
            Assert.AreEqual(guid + ",1429192800000,False,eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJVdWlkIjoidXVpZCIsIlN1YmplY3QiOiJTdWJqZWN0IiwiQm9keSI6ImJvZHkiLCJVcmwiOiJodHRwOi8vc2Vuc29yYmVyZy5jb20iLCJQYXlsb2FkU3RyaW5nIjoie1wicGF5XCI6XCJsb2FkXCJ9In0sIkJlYWNvblBpZHMiOlsiMSIsIjIiLCIzIiwiNCJdLCJFdmVudFR5cGVEZXRlY3RlZEJ5RGV2aWNlIjozLCJEZWxheSI6MTIzLCJTZW5kT25seU9uY2UiOnRydWUsIlN1cHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=\n", s);
        }
        [TestMethod]
        public void TestDelayedActionFromString()
        {
            Guid guid = Guid.NewGuid();
            FileStorageHelper.DelayedActionHelper simpleDelayedActionFromString = FileStorageHelper.SimpleDelayedActionFromString(guid+",1429192800000,False,eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJVdWlkIjoidXVpZCIsIlN1YmplY3QiOiJTdWJqZWN0IiwiQm9keSI6ImJvZHkiLCJVcmwiOiJodHRwOi8vc2Vuc29yYmVyZy5jb20iLCJQYXlsb2FkU3RyaW5nIjoie1wicGF5XCI6XCJsb2FkXCJ9In0sIkJlYWNvblBpZHMiOlsiMSIsIjIiLCIzIiwiNCJdLCJFdmVudFR5cGVEZXRlY3RlZEJ5RGV2aWNlIjozLCJEZWxheSI6MTIzLCJTZW5kT25seU9uY2UiOnRydWUsIlN1cHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=");

            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), simpleDelayedActionFromString.Offset, "Wrong offset");
            Assert.IsFalse(simpleDelayedActionFromString.Executed, "Is executed");
            string s =
                "eyJBY3Rpb24iOnsiQmVhY29uQWN0aW9uIjp7IklkIjoxLCJUeXBlIjozLCJVdWlkIjoidXVpZCIsIlN1YmplY3QiOiJTdWJqZWN0IiwiQm9keSI6ImJvZHkiLCJVcmwiOiJodHRwOi8vc2Vuc29yYmVyZy5jb20iLCJQYXlsb2FkU3RyaW5nIjoie1wicGF5XCI6XCJsb2FkXCJ9In0sIkJlYWNvblBpZHMiOlsiMSIsIjIiLCIzIiwiNCJdLCJFdmVudFR5cGVEZXRlY3RlZEJ5RGV2aWNlIjozLCJEZWxheSI6MTIzLCJTZW5kT25seU9uY2UiOnRydWUsIlN1cHJlc3Npb25UaW1lIjozMjEsIlJlcG9ydEltbWVkaWF0ZWx5Ijp0cnVlLCJUaW1lZnJhbWVzIjpbeyJTdGFydCI6IjIwMTUtMDQtMTVUMTI6MDA6MDArMDA6MDAiLCJFbmQiOiIyMDE1LTA0LTE2VDEyOjAwOjAwKzAwOjAwIn1dfSwiVGltZSI6IjIwMTUtMDQtMTZUMTQ6MDA6MDArMDA6MDAiLCJCZWFjb24iOiIxMjMiLCJFdmVudCI6MX0=";
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
            action.SupressionTime = 321;
            action.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2015-04-15T12:00:00.000+0000")}
            };
            DelayedActionData data = FileStorageHelper.DelayedActionFromHelper(simpleDelayedActionFromString);
            Assert.AreEqual("123", data.beaconPid, "Wrong beacon pid");
            Assert.AreEqual(BeaconEventType.Enter, data.eventTypeDetectedByDevice, "Wrong event type");
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), data.dueTime, "Wrong time");
            Assert.AreEqual(guid.ToString(), data.Id, "ID isnt set");
            Assert.AreEqual(action, data.resolvedAction);
        }
    }
}