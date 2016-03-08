// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDK.Internal
{
    [TestClass]
    public class LayoutManagerTest
    {
        [TestInitialize]
        public void Setup()
        {
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
        }

        [TestMethod]
        public async Task TestValidLayout()
        {
            await ValidateBaseMockLayout(ServiceManager.LayoutManager);
        }

        [TestMethod]
        public async Task TestValidInvalidateLayout()
        {
            ILayoutManager manager = ServiceManager.LayoutManager;
            await ValidateBaseMockLayout(manager);
            await manager.InvalidateLayoutAsync();
            Assert.IsFalse(manager.IsLayoutValid, "Layout still valid");
            Assert.IsNull(manager.Layout, "Layout still exists");
            Assert.IsTrue(await manager.VerifyLayoutAsync(true), "Verification failed");
            Assert.IsTrue(manager.IsLayoutValid, "Layout still invalid");
        }

        private static async Task ValidateBaseMockLayout(ILayoutManager manager)
        {
            Assert.IsTrue(await manager.VerifyLayoutAsync(true), "Verification failed");
            Layout layout = manager.Layout;
            Assert.IsNotNull(layout, "No Layout avialable");
            Assert.AreEqual(5, layout.AccountBeaconId1s.Count, "Number of proximity beacons not matching");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0003"), "Beacon 1 not found");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0006"), "Beacon 2 not found");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0004"), "Beacon 3 not found");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0007"), "Beacon 4 not found");
            Assert.AreEqual(9, layout.ResolvedActions.Count, "not 9 actions");

            ResolvedAction a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "9ded63644e424d758b0218f7c70f2473");
            Assert.AreEqual(3, (int) a.EventTypeDetectedByDevice, "beacon 1 - Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 1 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00043917830929"), "beacon 1 - No Beacon found!");

            Assert.AreEqual(43200, a.SupressionTime, "beacon 1 - Wrong supression time!");
            Assert.AreEqual(0, a.Delay, "beacon 1 - Different delay is set");

            Assert.AreEqual(string.Empty, a.BeaconAction.Subject, "beacon 1 - Different action subject");
            Assert.AreEqual(string.Empty, a.BeaconAction.Body, "beacon 1 - Different action body");
            Assert.AreEqual("http://www.visitfinland.com/", a.BeaconAction.Url, "beacon 1 - wrong url is set");
            Assert.IsNull(a.BeaconAction.Payload, "beacon 1 - Payload is not null");

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 1 - More timeframes are set");
            Assert.AreEqual(new DateTime(2015, 04, 16, 12, 46, 19, 627), a.Timeframes[0].Start.Value.DateTime, "beacon 1 - Different timesetting");

            Assert.AreEqual(3, (int) a.BeaconAction.Type, "beacon 1 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 1 - Send only once is set");


            a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "3f30be2605524f82a9bf0ccb4a81618f");
            Assert.AreEqual(1, (int) a.EventTypeDetectedByDevice, "beacon 2 - Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 2 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00034886921321"), "beacon 2 - No Beacon found!");

            Assert.AreEqual(900, a.SupressionTime, "beacon 2 - Wrong supression time!");
            Assert.AreEqual(0, a.Delay, "beacon 2 - Different delay is set");

            Assert.AreEqual(string.Empty, a.BeaconAction.Subject, "beacon 2 - Different action subject");
            Assert.AreEqual(string.Empty, a.BeaconAction.Body, "beacon 2 - Different action body");
            Assert.AreEqual("http://www.visitfinland.com/", a.BeaconAction.Url, "beacon 2 - wrong url is set");
            Assert.IsNull(a.BeaconAction.Payload, "beacon 2 - Payload is not null");

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 2 - More timeframes are set");
            Assert.AreEqual(new DateTime(2015, 04, 16, 12, 33, 48, 627), a.Timeframes[0].Start.Value.DateTime, "beacon 2 - Different timesetting");

            Assert.AreEqual(3, (int) a.BeaconAction.Type, "beacon 2 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 2 - Send only once is set");


            a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "312a8594e07542bd814ecdd17f76538e");
            Assert.AreEqual(1, (int) a.EventTypeDetectedByDevice, "beacon 3 - Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 3 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00034886921321"), "beacon 3 - No Beacon found!");

            Assert.AreEqual(900, a.SupressionTime, "beacon 3 - Wrong supression time!");
            Assert.AreEqual(0, a.Delay, "beacon 3 - Different delay is set");

            Assert.AreEqual(string.Empty, a.BeaconAction.Subject, "beacon 3 - Different action subject");
            Assert.AreEqual(string.Empty, a.BeaconAction.Body, "beacon 3 - Different action body");
            Assert.AreEqual("http://www.visitfinland.com/", a.BeaconAction.Url, "beacon 3 - wrong url is set");
            Assert.IsNull(a.BeaconAction.Payload, "beacon 3 - Payload is not null");

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 3 - More timeframes are set");
            Assert.AreEqual(new DateTime(2015, 04, 16, 12, 34, 22, 596), a.Timeframes[0].Start.Value.DateTime, "beacon 3 - Different timesetting");

            Assert.AreEqual(3, (int) a.BeaconAction.Type, "beacon 3 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 3 - Send only once is set");


            a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "959ea393e3424ab7ad53584a8b789197");
            Assert.AreEqual(1, (int) a.EventTypeDetectedByDevice, "beacon 4 - Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 4 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00034895330988"), "beacon 4 - No Beacon found!");

            Assert.AreEqual(900, a.SupressionTime, "beacon 4 - Wrong supression time!");
            Assert.AreEqual(60, a.Delay, "beacon 4 - Different delay is set");

            Assert.AreEqual("Delay 1 minute", a.BeaconAction.Subject, "beacon 4 - Different action subject");
            Assert.AreEqual("Delay 1 minute", a.BeaconAction.Body, "beacon 4 - Different action body");
            Assert.AreEqual("http://www.microsoft.com", a.BeaconAction.Url, "beacon 4 - wrong url is set");
            Assert.IsNull(a.BeaconAction.Payload, "beacon 4 - Payload is not null");

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 4 - More timeframes are set");
            Assert.AreEqual(new DateTime(2015, 04, 30, 08, 05, 54, 432), a.Timeframes[0].Start.Value.DateTime, "beacon 4 - Different timesetting");

            Assert.AreEqual(1, (int) a.BeaconAction.Type, "beacon 4 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 4 - Send only once is set");


            a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "351fd4b8b1c34da6b827e53acd79ff17");
            Assert.AreEqual(1, (int) a.EventTypeDetectedByDevice, "beacon 5 - Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 5 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00034886921321"), "beacon 5 - No Beacon found!");

            Assert.AreEqual(900, a.SupressionTime, "beacon 5 - Wrong supression time!");
            Assert.AreEqual(0, a.Delay, "beacon 5 - Different delay is set");

            Assert.AreEqual(string.Empty, a.BeaconAction.Subject, "beacon 5 - Different action subject");
            Assert.AreEqual(string.Empty, a.BeaconAction.Body, "beacon 5 - Different action body");
            Assert.AreEqual("http://www.visitfinland.com/", a.BeaconAction.Url, "beacon 5 - wrong url is set");
            Assert.IsNull(a.BeaconAction.Payload, "beacon 5 - Payload is not null");

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 5 - More timeframes are set");
            Assert.AreEqual(new DateTime(2015, 04, 16, 12, 33, 28, 264), a.Timeframes[0].Start.Value.DateTime, "beacon 5 - Different timesetting");

            Assert.AreEqual(3, (int) a.BeaconAction.Type, "beacon 5 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 5 - Send only once is set");


            a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "a5009f851ded4ce68d9b1b4ff6db6137");
            Assert.AreEqual(1, (int) a.EventTypeDetectedByDevice, "beacon 7- Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 7 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00073918758763"), "beacon 7 - No Beacon found!");

            Assert.AreEqual(31536000, a.SupressionTime, "beacon 7 - Wrong supression time!");
            Assert.AreEqual(0, a.Delay, "beacon 7 - Different delay is set");

            Assert.AreEqual("You´re in the year 2017!", a.BeaconAction.Subject, "beacon 7 - Different action subject");
            Assert.AreEqual("It´s a great year", a.BeaconAction.Body, "beacon 7 - Different action body");
            Assert.AreEqual("http://www.visitfinland.com/", a.BeaconAction.Url, "beacon 7 - wrong url is set");
            Assert.IsNull(a.BeaconAction.Payload, "beacon 7 - Payload is not null");

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 7 - More timeframes are set");
            Assert.AreEqual(new DateTime(2016, 12, 31, 11, 00, 00, 00), a.Timeframes[0].Start.Value.DateTime, "beacon 7 - Different timesetting");
            Assert.AreEqual(new DateTime(2017, 12, 31, 11, 00, 00, 00), a.Timeframes[0].End.Value.DateTime, "beacon 7 - Different timesetting");

            Assert.AreEqual(1, (int) a.BeaconAction.Type, "beacon 7 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 7 - Send only once is set");


            a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "4224871362624826b510141da0d4fc5d");
            Assert.AreEqual(1, (int) a.EventTypeDetectedByDevice, "beacon 8- Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count, "beacon 8 - Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00062343028018"), "beacon 8 - No Beacon found!");

            Assert.AreEqual(-1, a.SupressionTime, "beacon 8 - Wrong supression time!");
            Assert.AreEqual(0, a.Delay, "beacon 8 - Different delay is set");

            Assert.AreEqual(string.Empty, a.BeaconAction.Subject, "beacon 8 - Different action subject");
            Assert.AreEqual(string.Empty, a.BeaconAction.Body, "beacon 8 - Different action body");
            Assert.AreEqual("payload://is.awesome", a.BeaconAction.Url, "beacon 8 - wrong url is set");
            Assert.IsNotNull(a.BeaconAction.Payload, "beacon 8 - Payload is null");
            Assert.AreEqual(a.BeaconAction.Payload.ToString(), JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString());

            Assert.AreEqual(1, a.Timeframes.Count, "beacon 8 - More timeframes are set");
            Assert.AreEqual(new DateTime(2015, 04, 16, 12, 48, 51, 828), a.Timeframes[0].Start.Value.DateTime, "beacon 8 - Different timesetting");

            Assert.AreEqual(3, (int) a.BeaconAction.Type, "beacon 8 - Different type");
            Assert.IsFalse(a.SendOnlyOnce, "beacon 8 - Send only once is set");
        }
    }
}