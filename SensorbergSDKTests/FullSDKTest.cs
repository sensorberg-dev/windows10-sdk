// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Json;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class FullSDKTest
    {
        private const string ApiKey = "af24473d3ccb1d7a34307747531f06c25f08de361a5349389bbbe39274bf08cd";
        private const ushort ManufacturerId = 0x004c;
        private const ushort BeaconCode = 0x0215;

        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.Clear();
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.BeaconScanner = new MockBeaconScanner();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService() {Storage = new MockStorage()};
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestCleanup]
        public void Teardown()
        {
            SDKManager sdkManager = SDKManager.Instance();
            sdkManager.Deinitialize(true);
        }

        [TestMethod]
        public async Task BeaconEntered()
        {
            try
            {
                MockBeaconScanner scanner = (MockBeaconScanner) ServiceManager.BeaconScanner;

                SDKManager sdkManager = SDKManager.Instance();
                sdkManager.ScannerStatusChanged += (sender, status) => { };
                TaskCompletionSource<BeaconAction> actionResolved = new TaskCompletionSource<BeaconAction>();
                sdkManager.BeaconActionResolved += (sender, action) =>
                {
                    actionResolved.SetResult(action);
                };


                await sdkManager.InitializeAsync(new SdkConfiguration() {ApiKey = ApiKey, ManufacturerId = ManufacturerId, BeaconCode = BeaconCode});

                // Listening to the following events is not necessary, but provides interesting data for our log
                sdkManager.Scanner.BeaconEvent += (sender, args) => { };
                sdkManager.FailedToResolveBeaconAction += (sender, s) => { };

                scanner.FireBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018}, BeaconEventType.Enter);

                BeaconAction action1 = await actionResolved.Task;

                Assert.AreEqual("4224871362624826b510141da0d4fc5d", action1.Uuid, "Wrong id in action");
                Assert.AreEqual("payload://is.awesome", action1.Url, "Wrong url in action");
                Assert.AreEqual(string.Empty, action1.Subject, "beacon 8 - Different action subject");
                Assert.AreEqual(string.Empty, action1.Body, "beacon 8 - Different action body");
                Assert.AreEqual("payload://is.awesome", action1.Url, "beacon 8 - wrong url is set");
                Assert.IsNotNull(action1.Payload, "beacon 8 - Payload is null");
                Assert.AreEqual(JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString(), action1.Payload.ToString());
            }
            catch (AssertFailedException a)
            {
                throw a;
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public async Task BeaconMultipleEntered()
        {
            MockBeaconScanner scanner = (MockBeaconScanner) ServiceManager.BeaconScanner;

            SDKManager sdkManager = SDKManager.Instance();
            sdkManager.ScannerStatusChanged += (sender, status) => { };
            TaskCompletionSource<BeaconAction> actionResolved = new TaskCompletionSource<BeaconAction>();
            List<BeaconAction> actions = new List<BeaconAction>();
            sdkManager.BeaconActionResolved += (sender, action) =>
            {
                actions.Add(action);
                actionResolved.SetResult(action);
            };


            await sdkManager.InitializeAsync(new SdkConfiguration() {ApiKey = ApiKey, ManufacturerId = ManufacturerId, BeaconCode = BeaconCode});

            // Listening to the following events is not necessary, but provides interesting data for our log
            sdkManager.Scanner.BeaconEvent += (sender, args) => { };
            sdkManager.FailedToResolveBeaconAction += (sender, s) => { };

            scanner.FireBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018}, BeaconEventType.Enter);

            BeaconAction action1 = await actionResolved.Task;

            actionResolved = new TaskCompletionSource<BeaconAction>();

            Assert.AreEqual("4224871362624826b510141da0d4fc5d", action1.Uuid, "Wrong id in action");
            Assert.AreEqual("payload://is.awesome", action1.Url, "Wrong url in action");
            Assert.AreEqual(string.Empty, action1.Subject, "beacon 8 - Different action subject");
            Assert.AreEqual(string.Empty, action1.Body, "beacon 8 - Different action body");
            Assert.AreEqual("payload://is.awesome", action1.Url, "beacon 8 - wrong url is set");
            Assert.IsNotNull(action1.Payload, "beacon 8 - Payload is null");
            Assert.AreEqual(JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString(), action1.Payload.ToString());

            scanner.FireBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018}, BeaconEventType.Enter);

            await Task.Delay(200);

            Assert.AreEqual(2, actions.Count, "Action missing");
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task BeaconMultipleEnteredOneFired()
        {
            MockBeaconScanner scanner = (MockBeaconScanner) ServiceManager.BeaconScanner;

            SDKManager sdkManager = SDKManager.Instance();
            sdkManager.ScannerStatusChanged += (sender, status) => { };
            TaskCompletionSource<BeaconAction> actionResolved = new TaskCompletionSource<BeaconAction>();
            List<BeaconAction> actions = new List<BeaconAction>();
            sdkManager.BeaconActionResolved += (sender, action) =>
            {
                actions.Add(action);
                actionResolved.SetResult(action);
            };


            await sdkManager.InitializeAsync(new SdkConfiguration() {ApiKey = ApiKey, ManufacturerId = ManufacturerId, BeaconCode = BeaconCode});

            // Listening to the following events is not necessary, but provides interesting data for our log
            sdkManager.Scanner.BeaconEvent += (sender, args) => { };
            sdkManager.FailedToResolveBeaconAction += (sender, s) => { };

            scanner.FireBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0008", Id2 = 23430, Id3 = 28018}, BeaconEventType.Enter);

            BeaconAction action1 = await actionResolved.Task;
            actionResolved = new TaskCompletionSource<BeaconAction>();

            Assert.AreEqual("4224871362624826b510141da0d4fc65d", action1.Uuid, "Wrong id in action");
            Assert.AreEqual("payload://is.awesome", action1.Url, "Wrong url in action");
            Assert.AreEqual(string.Empty, action1.Subject, "beacon 8 - Different action subject");
            Assert.AreEqual(string.Empty, action1.Body, "beacon 8 - Different action body");
            Assert.AreEqual("payload://is.awesome", action1.Url, "beacon 8 - wrong url is set");
            Assert.IsNotNull(action1.Payload, "beacon 8 - Payload is null");
            Assert.AreEqual(JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString(), action1.Payload.ToString());

            scanner.FireBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0008", Id2 = 23430, Id3 = 28018}, BeaconEventType.Enter);

            Debug.WriteLine("Waiting");
            await Task.Delay(2000);
            Debug.WriteLine("Waiting done");
            Assert.AreEqual(1, actions.Count, "Action missing or to many");
        }



        [TestMethod]
        [Timeout(10000)]
        public async Task MultipleEventsFired()
        {
            SDKManager sdkManager = SDKManager.Instance();
            int resolvedAction = 0;
            sdkManager.BeaconActionResolved += (sender, action) => resolvedAction++;
            await sdkManager.InitializeAsync(new SdkConfiguration() {ApiKey = ApiKey, ManufacturerId = ManufacturerId, BeaconCode = BeaconCode});

            TaskCompletionSource<bool> actionResolved = new TaskCompletionSource<bool>();
            int requestCount = 0;
            int REQUEST_COUNT = 1000;
            ((Resolver) sdkManager.SdkEngine.Resolver).Finished += () =>
            {
                if (requestCount >= REQUEST_COUNT)
                {
                    actionResolved.SetResult(true);
                }
            };

            MockBeaconScanner scanner = (MockBeaconScanner) ServiceManager.BeaconScanner;
            for (; requestCount < REQUEST_COUNT; requestCount++)
            {
                scanner.NotifyBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018},
                    requestCount%2 == 0 ? BeaconEventType.Enter : BeaconEventType.Exit);
            }

            bool result = await actionResolved.Task;

            await ServiceManager.StorageService.FlushHistory();

            MockApiConnection connection = (MockApiConnection) ServiceManager.ApiConnction;
            Assert.AreEqual(REQUEST_COUNT, requestCount);
            Assert.AreEqual(REQUEST_COUNT, connection.HistoryEvents.Count);
            Assert.AreEqual(REQUEST_COUNT/2, connection.HistoryActions.Count);
        }
    }
}