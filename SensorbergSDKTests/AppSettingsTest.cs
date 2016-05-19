// Created by Kay Czarnotta on 03.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class AppSettingsTest
    {

        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.ClearFiles("sensorberg-storage");
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection() {MockSettings = "{\"revision\":0,\"settings\":{\"scanner.backgroundWaitTime\":120000, \"scanner.exitTimeoutMillis\":123, \"network.historyUploadInterval\":321}}" };
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task LoadSettingsFromResponse()
        {
            AppSettings appSettings = await ServiceManager.SettingsManager.GetSettings(true);

            Assert.AreEqual((ulong)123, appSettings.BeaconExitTimeout);
            Assert.AreEqual((ulong)321, appSettings.HistoryUploadInterval);
        }
    }
}