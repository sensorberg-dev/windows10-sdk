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
            await TestHelper.ClearFiles("sensorberg-storage");
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection() {MockSettings = "{\"revision\":0,\"settings\":{\"scanner.backgroundWaitTime\":120000}}"};
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task LoadSettingsFromResponse()
        {
            AppSettings appSettings = await ServiceManager.SettingsManager.GetSettings(true);


        }
    }
}