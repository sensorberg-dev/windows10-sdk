// Created by Kay Czarnotta on 24.08.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class ScannerTest
    {
        [TestInitialize]
        public void Setup()
        {
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.BeaconScanner = new Scanner();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.StorageService = new StorageService() { Storage = new MockStorage() };
            ServiceManager.ReadOnlyForTests = true;
        }
        [TestMethod]
        public async Task FilterTest()
        {
            await ServiceManager.LayoutManager.VerifyLayoutAsync();
            IBeaconScanner scanner = ServiceManager.BeaconScanner;
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() {Id1 = "7367672374000000ffff0000ffff0003", Id2 = 1,Id3 = 2}));
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() {Id1 = "7367672374000000ffff0000ffff00030", Id2 = 1,Id3 = 2}));
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() {Id1 = "7367672374000000ffff0000ffff000300", Id2 = 1,Id3 = 2}));
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() {Id1 = "7367672374000000ffff0000ffff00030001", Id2 = 1,Id3 = 2}));

            Assert.IsFalse(scanner.FilterBeaconByUuid(new Beacon() { Id1 = "7asd672374000000ffff0000ffff00030001", Id2 = 1, Id3 = 2 }));

            scanner.DisableFilter = true;
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() { Id1 = "7367672374000000ffff0000ffff0003", Id2 = 1, Id3 = 2 }));
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() { Id1 = "7367672374000000ffff0000ffff00030", Id2 = 1, Id3 = 2 }));
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() { Id1 = "7367672374000000ffff0000ffff000300", Id2 = 1, Id3 = 2 }));
            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() { Id1 = "7367672374000000ffff0000ffff00030001", Id2 = 1, Id3 = 2 }));

            Assert.IsTrue(scanner.FilterBeaconByUuid(new Beacon() { Id1 = "7asd672374000000ffff0000ffff00030001", Id2 = 1, Id3 = 2 }));
        }
    }
}