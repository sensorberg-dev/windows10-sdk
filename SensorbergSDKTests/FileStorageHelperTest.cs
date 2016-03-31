// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
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
    }
}