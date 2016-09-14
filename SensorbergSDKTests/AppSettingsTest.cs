// Created by Kay Czarnotta on 03.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
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
            await TestHelper.Clear();
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

        [TestMethod]
        public async Task UpdateExitTimeoutTest()
        {
            SdkEngine engine = new SdkEngine(true);
            await engine.InitializeAsync();

            AppSettings appSettings = engine.AppSettings;

            Assert.AreEqual((ulong)123, appSettings.BeaconExitTimeout);
            Assert.AreEqual((ulong)123,engine.Resolver.BeaconExitTimeout);
            ((MockApiConnection) ServiceManager.ApiConnction).MockSettings =
                "{\"revision\":0,\"settings\":{\"scanner.backgroundWaitTime\":120000, \"scanner.exitTimeoutMillis\":123000, \"network.historyUploadInterval\":321}}";

            ((SettingsManager)ServiceManager.SettingsManager).OnTimerTick(null);

            appSettings = engine.AppSettings;
            Assert.AreEqual((ulong)123000, appSettings.BeaconExitTimeout);
            Assert.AreEqual((ulong)123000, engine.Resolver.BeaconExitTimeout);
        }
    }
}