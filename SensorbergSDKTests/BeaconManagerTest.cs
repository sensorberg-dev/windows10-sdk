// Created by Kay Czarnotta on 25.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class BeaconManagerTest
    {
        [TestMethod]
        public async Task TestDetectEnterExit()
        {
            BeaconManager manager = new BeaconManager(200);
            Beacon beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929};
            Assert.AreEqual(BeaconEventType.Enter, manager.ResolveBeaconState(beacon));

            await Task.Delay(1000);
            Assert.AreEqual(1, manager.ResolveBeaconExits().Count);
            Assert.AreEqual(0, manager.ResolveBeaconExits().Count);


            Assert.AreEqual(BeaconEventType.Enter, manager.ResolveBeaconState(beacon));

            await Task.Delay(1000);
            Assert.AreEqual(1, manager.ResolveBeaconExits().Count);
            Assert.AreEqual(0, manager.ResolveBeaconExits().Count);
        }

        [TestMethod]
        public async Task TestDetectMultipleEnterExit()
        {
            BeaconManager manager = new BeaconManager(200);
            Beacon beacon = new Beacon() { Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 };
            Assert.AreEqual(BeaconEventType.Enter, manager.ResolveBeaconState(beacon));
            Assert.AreEqual(BeaconEventType.None, manager.ResolveBeaconState(beacon));
            Assert.AreEqual(BeaconEventType.None, manager.ResolveBeaconState(beacon));
            Assert.AreEqual(BeaconEventType.None, manager.ResolveBeaconState(beacon));

            await Task.Delay(1000);
            Assert.AreEqual(1, manager.ResolveBeaconExits().Count);
            Assert.AreEqual(0, manager.ResolveBeaconExits().Count);


            Assert.AreEqual(BeaconEventType.Enter, manager.ResolveBeaconState(beacon));
            Assert.AreEqual(BeaconEventType.None, manager.ResolveBeaconState(beacon));
            Assert.AreEqual(BeaconEventType.None, manager.ResolveBeaconState(beacon));
            Assert.AreEqual(BeaconEventType.None, manager.ResolveBeaconState(beacon));

            await Task.Delay(1000);
            Assert.AreEqual(1, manager.ResolveBeaconExits().Count);
            Assert.AreEqual(0, manager.ResolveBeaconExits().Count);
        }
    }
}