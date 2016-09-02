// Created by Kay Czarnotta on 01.09.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class SdkStatusTest
    {
        private SdkConfiguration SdkConfiguration { get; set; }

        [TestInitialize]
        public void TestSetup()
        {
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            SdkConfiguration = new SdkConfiguration() {ApiKey = "1234567890"};
            ServiceManager.ApiConnction = new MockApiConnection()
            {
                MockSettings = "{\"revision\":0,\"settings\":{\"scanner.backgroundWaitTime\":120000, \"scanner.exitTimeoutMillis\":123, \"network.historyUploadInterval\":321}}",
                Configuration = SdkConfiguration,
                ValidApiKey = "1234567890"
            };
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task TestApiKeyValidation()
        {
            SdkStatus status = new SdkStatus();
            Assert.IsTrue(await status.IsApiKeyValid(), "ApiKey is not valid");
        }

        [TestMethod]
        public async Task TestInvalidApiKeyValidation()
        {
            SdkStatus status = new SdkStatus();
            SdkConfiguration.ApiKey = "123";
            Assert.IsFalse(await status.IsApiKeyValid(), "ApiKey is not invalid");
        }

        [TestMethod]
        public async Task TestBackendStatus()
        {
            SdkStatus status = new SdkStatus();
            Assert.IsTrue(await status.IsResolverReachable(), "Resolver reachable failed");
            ((MockApiConnection) ServiceManager.ApiConnction).FailNetwork = true;
            try { await ServiceManager.ApiConnction.LoadSettings(); } catch (Exception) { }
            Assert.IsFalse(await status.IsResolverReachable(), "Unreachable Resolver reachable failed");
            ((MockApiConnection) ServiceManager.ApiConnction).FailNetwork = false;
            await ServiceManager.ApiConnction.LoadSettings();
            Assert.IsTrue(await status.IsResolverReachable(), "Resolver reachable failed");
        }
    }
}