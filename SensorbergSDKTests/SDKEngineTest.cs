// Created by Kay Czarnotta on 20.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class SDKEngineTest
    {
        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.Clear();
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService() {};
            ServiceManager.WriterFactory = new WriterFactory();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task ResolveBackgroundBeaconsSingleAction()
        {
            LayoutManager layoutManager = (LayoutManager) ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);
            SdkEngine engine = new SdkEngine(false);
            await engine.InitializeAsync();

            BeaconAction orgAction = layoutManager.Layout.ResolvedActions.FirstOrDefault(ra => ra.BeaconAction.Uuid == "9ded63644e424d758b0218f7c70f2473").BeaconAction;

            TaskCompletionSource<BeaconAction> action = new TaskCompletionSource<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) => { action.SetResult(args); };
            await
                engine.ResolveBeaconAction(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });

            BeaconAction result = await action.Task;

            Assert.AreEqual(orgAction, result, "action not found");
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task ResolveMultipleAction()
        {
            LayoutManager layoutManager = (LayoutManager) ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);
            SdkEngine engine = new SdkEngine(false);
            await engine.InitializeAsync();

            TaskCompletionSource<IList<BeaconAction>> action = new TaskCompletionSource<IList<BeaconAction>>();
            IList<BeaconAction> list = new List<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) =>
            {
                list.Add(args);
                if (list.Count >= 3)
                {
                    action.TrySetResult(list);
                }
            };
            await
                engine.ResolveBeaconAction(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0003", Id2 = 48869, Id3 = 21321},
                    EventType = BeaconEventType.Enter
                });

            IList<BeaconAction> result = await action.Task;

            Assert.AreEqual(4, result.Count, "Not 4 action found");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveSingleActionNoResult()
        {
            LayoutManager layoutManager = (LayoutManager) ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);
            SdkEngine engine = new SdkEngine(false);
            await engine.InitializeAsync();

            TaskCompletionSource<IList<BeaconAction>> action = new TaskCompletionSource<IList<BeaconAction>>();
            IList<BeaconAction> list = new List<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) =>
            {
                list.Add(args);
                if (list.Count >= 3)
                {
                    action.SetResult(list);
                }
            };
            await
                engine.ResolveBeaconAction(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff1234", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });

            if (await Task.WhenAny(action.Task, Task.Delay(500)) == action.Task)
            {
                Assert.AreEqual(0, action.Task.Result, "Not 0 action found");
            }
            else
            {
                //timeout is fine
            }
        }


        [TestMethod]
        public async Task TestSilentCampaign()
        {
            ((MockApiConnection) ServiceManager.ApiConnction).LayoutFile = "mock/mock_silent_layout.json";
            ServiceManager.ReadOnlyForTests = false;
            MockStorage storage = new MockStorage();
            ServiceManager.StorageService = new StorageService() {Storage = storage};
            ServiceManager.ReadOnlyForTests = true;

            SdkEngine engine = new SdkEngine(false);
            await engine.InitializeAsync();

            TaskCompletionSource<bool> action = new TaskCompletionSource<bool>();
            engine.BeaconActionResolved += (sender, args) => { action.SetResult(true); };
            await
                engine.ResolveBeaconAction(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff4321", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });

            if (await Task.WhenAny(action.Task, Task.Delay(500)) == action.Task)
            {
                Assert.Fail("no action should fired");
            }
            else
            {
                Assert.AreEqual(1, storage.UndeliveredActions.Count, "Not 1 undlivered action");
                Assert.AreEqual(1, storage.UndeliveredEvents.Count, "Not 1 undlivered event");
            }
        }


        [TestMethod]
        public async Task TestInfiniteCampaign()
        {
            ServiceManager.ReadOnlyForTests = false;
            MockStorage storage = new MockStorage();
            ServiceManager.StorageService = new StorageService() {Storage = storage};
            ServiceManager.ReadOnlyForTests = true;

            SdkEngine engine = new SdkEngine(false);
            await engine.InitializeAsync();

            TaskCompletionSource<bool> action = new TaskCompletionSource<bool>();
            engine.BeaconActionResolved += (sender, args) => { action.SetResult(true); };
            await
                engine.ResolveBeaconAction(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0012", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });

            if (await Task.WhenAny(action.Task, Task.Delay(500)) == action.Task)
            {
                Assert.IsTrue(action.Task.Result, "Not 1 action found");
                Assert.AreEqual(1, storage.UndeliveredActions.Count, "Not 1 undlivered action");
                Assert.AreEqual(1, storage.UndeliveredEvents.Count, "Not 1 undlivered event");
            }
            else
            {
                Assert.Fail("timeout");
            }
        }
    }
}