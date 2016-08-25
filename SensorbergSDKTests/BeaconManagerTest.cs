// Created by Kay Czarnotta on 25.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;

namespace SensorbergSDKTests
{
    [TestClass]
    public class BeaconManagerTest
    {
        [TestMethod]
        public void TestDetectEnterExit()
        {
            BeaconManager manager = new BeaconManager();
            Assert.AreEqual(BeaconEventType.Enter,manager.ResolveBeaconState(new Beacon() { Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 }));
        }
    }
}