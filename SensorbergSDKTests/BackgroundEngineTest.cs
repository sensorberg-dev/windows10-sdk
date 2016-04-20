// Created by Kay Czarnotta on 20.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKBackground;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class BackgroundEngineTest
    {
        [TestInitialize]
        public void Setup()
        {
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task ResolveBackgroundEvent()
        {
            BackgroundEngine engine = new BackgroundEngine();
            await engine.InitializeAsync(null);
            await engine.ResolveBeaconActionsAsync();
        }
    }
}