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
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDK.Internal
{
    [TestClass]
    public class FullSDKTest
    {
        private const string ApiKey = "af24473d3ccb1d7a34307747531f06c25f08de361a5349389bbbe39274bf08cd";
        private const UInt16 ManufacturerId = 0x004c;
        private const UInt16 BeaconCode = 0x0215;

        [TestInitialize]
        public void Setup()
        {
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.BeaconScanner = new MockBeaconScanner();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestCleanup]
        public void Teardown()
        {
            SDKManager sdkManager = SDKManager.Instance(ManufacturerId, BeaconCode);
            sdkManager.Deinitialize(true);
        }

        [TestMethod]
        public async Task BeaconEntered()
        {
            MockBeaconScanner scanner = (MockBeaconScanner) ServiceManager.BeaconScanner;
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";

            SDKManager sdkManager = SDKManager.Instance(ManufacturerId, BeaconCode);
            sdkManager.ScannerStatusChanged += (sender, status) => {};
            TaskCompletionSource<BeaconAction> actionResolved = new TaskCompletionSource<BeaconAction>();
            sdkManager.BeaconActionResolved += (sender, action) =>
            {
                actionResolved.SetResult(action);
            };


            await sdkManager.InitializeAsync(ApiKey);

            // Listening to the following events is not necessary, but provides interesting data for our log
            sdkManager.Scanner.BeaconEvent += (sender, args) => {};
            sdkManager.FailedToResolveBeaconAction += (sender, s) => {};

            scanner.FireBeaconEvent(new Beacon() {Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018 }, BeaconEventType.Enter);

            BeaconAction action1 = await actionResolved.Task;

            Assert.AreEqual("4224871362624826b510141da0d4fc5d", action1.Uuid, "Wrong id in action");
            Assert.AreEqual("payload://is.awesome", action1.Url, "Wrong url in action");
            Assert.AreEqual(string.Empty, action1.Subject, "beacon 8 - Different action subject");
            Assert.AreEqual(string.Empty, action1.Body, "beacon 8 - Different action body");
            Assert.AreEqual("payload://is.awesome", action1.Url, "beacon 8 - wrong url is set");
            Assert.IsNotNull(action1.Payload, "beacon 8 - Payload is null");
            Assert.AreEqual(JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString(), action1.Payload.ToString());
        }

        [TestMethod]
        public async Task BeaconMultipleEntered()
        {
            MockBeaconScanner scanner = (MockBeaconScanner)ServiceManager.BeaconScanner;
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";

            SDKManager sdkManager = SDKManager.Instance(ManufacturerId, BeaconCode);
            sdkManager.ScannerStatusChanged += (sender, status) => { };
            TaskCompletionSource<BeaconAction> actionResolved = new TaskCompletionSource<BeaconAction>();
            List<BeaconAction> actions = new List<BeaconAction>();
            sdkManager.BeaconActionResolved += (sender, action) =>
            {
                actions.Add(action);
                actionResolved.SetResult(action);
            };


            await sdkManager.InitializeAsync(ApiKey);

            // Listening to the following events is not necessary, but provides interesting data for our log
            sdkManager.Scanner.BeaconEvent += (sender, args) => { };
            sdkManager.FailedToResolveBeaconAction += (sender, s) => { };

            scanner.FireBeaconEvent(new Beacon() { Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018 }, BeaconEventType.Enter);

            BeaconAction action1 = await actionResolved.Task;

            actionResolved = new TaskCompletionSource<BeaconAction>();

            Assert.AreEqual("4224871362624826b510141da0d4fc5d", action1.Uuid, "Wrong id in action");
            Assert.AreEqual("payload://is.awesome", action1.Url, "Wrong url in action");
            Assert.AreEqual(string.Empty, action1.Subject, "beacon 8 - Different action subject");
            Assert.AreEqual(string.Empty, action1.Body, "beacon 8 - Different action body");
            Assert.AreEqual("payload://is.awesome", action1.Url, "beacon 8 - wrong url is set");
            Assert.IsNotNull(action1.Payload, "beacon 8 - Payload is null");
            Assert.AreEqual(JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString(), action1.Payload.ToString());

            scanner.FireBeaconEvent(new Beacon() { Id1 = "7367672374000000ffff0000ffff0006", Id2 = 23430, Id3 = 28018 }, BeaconEventType.Enter);

            await Task.Delay(200);

            Assert.AreEqual(2, actions.Count, "Action missing");
        }

        [TestMethod]
        public async Task BeaconMultipleEnteredOneFired()
        {
            MockBeaconScanner scanner = (MockBeaconScanner)ServiceManager.BeaconScanner;
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";

            SDKManager sdkManager = SDKManager.Instance(ManufacturerId, BeaconCode);
            sdkManager.ScannerStatusChanged += (sender, status) => { };
            TaskCompletionSource<BeaconAction> actionResolved = new TaskCompletionSource<BeaconAction>();
            List<BeaconAction> actions = new List<BeaconAction>();
            sdkManager.BeaconActionResolved += (sender, action) =>
            {
                actions.Add(action);
                actionResolved.SetResult(action);
            };


            await sdkManager.InitializeAsync(ApiKey);

            // Listening to the following events is not necessary, but provides interesting data for our log
            sdkManager.Scanner.BeaconEvent += (sender, args) => { };
            sdkManager.FailedToResolveBeaconAction += (sender, s) => { };

            scanner.FireBeaconEvent(new Beacon() { Id1 = "7367672374000000ffff0000ffff0008", Id2 = 23430, Id3 = 28018 }, BeaconEventType.Enter);

            BeaconAction action1 = await actionResolved.Task;
            actionResolved = new TaskCompletionSource<BeaconAction>();

            Assert.AreEqual("4224871362624826b510141da0d4fc65d", action1.Uuid, "Wrong id in action");
            Assert.AreEqual("payload://is.awesome", action1.Url, "Wrong url in action");
            Assert.AreEqual(string.Empty, action1.Subject, "beacon 8 - Different action subject");
            Assert.AreEqual(string.Empty, action1.Body, "beacon 8 - Different action body");
            Assert.AreEqual("payload://is.awesome", action1.Url, "beacon 8 - wrong url is set");
            Assert.IsNotNull(action1.Payload, "beacon 8 - Payload is null");
            Assert.AreEqual(JsonObject.Parse("{\"payload\":\"is\",\"awesome\":true}").ToString(), action1.Payload.ToString());

            scanner.FireBeaconEvent(new Beacon() { Id1 = "7367672374000000ffff0000ffff0008", Id2 = 23430, Id3 = 28018 }, BeaconEventType.Enter);

            Debug.WriteLine("Waiting");
            await Task.Delay(200);
            Debug.WriteLine("Waiting done");
            Assert.AreEqual(1, actions.Count, "Action missing or to many");
        }
    }
}