// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class LayoutManagerTest
    {
        [TestInitialize]
        public void Setup()
        {
            ServiceManager.ApiConnction = new MockApiConnection();
        }

        [TestMethod]
        public async Task TestValidLayout()
        {
            LayoutManager manager = LayoutManager.Instance;
            Assert.IsTrue(await manager.VerifyLayoutAsync(true), "Verification failed" );
            Layout layout = manager.Layout;
            Assert.IsNotNull(layout, "No Layout avialable");
            Assert.AreEqual(4, layout.AccountBeaconId1s.Count, "Number of proximity beacons not matching");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0003"), "Beacon 1 not found");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0006"), "Beacon 2 not found");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0004"), "Beacon 3 not found");
            Assert.IsTrue(layout.AccountBeaconId1s.Contains("7367672374000000ffff0000ffff0007"), "Beacon 4 not found");
            Assert.AreEqual(8, layout.ResolvedActions.Count, "not 8 actions");

            ResolvedAction a = layout.ResolvedActions.FirstOrDefault(t => t.BeaconAction.Uuid == "3f30be2605524f82a9bf0ccb4a81618f");
            Assert.AreEqual(1, (int)a.EventTypeDetectedByDevice, "Wrong trigger type");
            Assert.AreEqual(1, a.BeaconPids.Count,"Beacon count wrong");
            Assert.IsTrue(a.BeaconPids.ContainsKey("7367672374000000ffff0000ffff00043917830929"), "No Beacon found!");

            Assert.AreEqual(43200, a.SupressionTime, "Wrong supression time!");
        }
    }
}