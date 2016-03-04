// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
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
            
        }
    }
}