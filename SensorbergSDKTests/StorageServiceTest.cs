﻿// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class StorageServiceTest
    {
        [TestInitialize]
        public void Setup()
        {
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.StorageService = new StorageServiceExtend();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task ValidateAPIKeyTest()
        {
            MockApiConnection connection = (MockApiConnection) ServiceManager.ApiConnction;

            IStorageService service = ServiceManager.StorageService;
            Assert.AreEqual(ApiKeyValidationResult.Valid,  await service.ValidateApiKey("true"), "Not successfull");
            connection.APIInvalid = true;
            Assert.AreEqual(ApiKeyValidationResult.Invalid, await service.ValidateApiKey("false"), "Not invalid");
            connection.APIInvalid = false;


            connection.FailNetwork = true;
            Assert.AreEqual(ApiKeyValidationResult.NetworkError,  await service.ValidateApiKey("true"), "No network issue");
            connection.FailNetwork = false;

            connection.UnknownError = true;
            Assert.AreEqual(ApiKeyValidationResult.UnknownError, await service.ValidateApiKey("true"), "No unknown issue");
            connection.UnknownError = false;
        }

        [TestMethod]
        public async Task RetrieveLayoutTest()
        {
            MockApiConnection connection = (MockApiConnection) ServiceManager.ApiConnction;
            IStorageService service = ServiceManager.StorageService;


            connection.FailNetwork = true;
            LayoutResult layout = await service.RetrieveLayout();
            Assert.AreEqual(NetworkResult.NetworkError, layout.Result, "Not failed");
            connection.FailNetwork = false;

            layout = await service.RetrieveLayout();
            Assert.AreEqual(NetworkResult.Success, layout.Result, "Not successfull loaded");
            LayoutManagerTest.ValidateMockLayout(layout.Layout);


            connection.FailNetwork = true;

            //should be cached
            layout = await service.RetrieveLayout();
            Assert.AreEqual(NetworkResult.Success, layout.Result, "Not successfull loaded from cache");
            LayoutManagerTest.ValidateMockLayout(layout.Layout);
        }

        [TestMethod]
        public async Task FlushHistoryTest()
        {
            StorageServiceExtend sse = (StorageServiceExtend) ServiceManager.StorageService;
            MockStorage mockStorage = new MockStorage();
            sse.SetStorage(mockStorage);
            mockStorage.UndeliveredActions = new List<HistoryAction>() {new HistoryAction(new DBHistoryAction())};
            mockStorage.UndeliveredEvents = new List<HistoryEvent>() {new HistoryEvent(new DBHistoryEvent())};

            MockApiConnection connection = (MockApiConnection)ServiceManager.ApiConnction;
            IStorageService service = ServiceManager.StorageService;


            connection.FailNetwork = true;
            Assert.IsFalse(await service.FlushHistory(), "Flushing History not failed");
            connection.FailNetwork = false;
            Assert.IsTrue(mockStorage.UndeliveredEvents.Count!= 0, "Event were resetet");
            Assert.IsTrue(mockStorage.UndeliveredActions.Count != 0, "Actions were resetet");

            Assert.IsTrue(await service.FlushHistory(), "Flushing History not succeed");
            Assert.IsTrue(mockStorage.UndeliveredEvents.Count == 0, "Event were not marked as send");
            Assert.IsTrue(mockStorage.UndeliveredActions.Count == 0, "Actions were not marked as send");
        }
    }
}