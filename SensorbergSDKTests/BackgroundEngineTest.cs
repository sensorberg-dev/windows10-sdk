// Created by Kay Czarnotta on 20.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKBackground;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class BackgroundEngineTest
    {
        private const int OUT_OF_RANGE_DB = -128;

        [TestInitialize]
        public async Task Setup()
        {
            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("sensorberg-storage");
                await folder.DeleteAsync();
            }
            catch (FileNotFoundException)
            {

            }
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveBackgroundEvent()
        {
            LayoutManager layoutManager = (LayoutManager)ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);
            BeaconAction orgAction = layoutManager.Layout.ResolvedActions.FirstOrDefault(ra => ra.BeaconAction.Uuid == "9ded63644e424d758b0218f7c70f2473").BeaconAction;

            List<Beacon> list = new List<Beacon>() { new Beacon() { Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 } };
            BackgroundEngine engine = new BackgroundEngine();
            TaskCompletionSource<BeaconAction> action = new TaskCompletionSource<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) =>
            {
                action.SetResult(args);
            };
            await engine.InitializeAsync();
            await engine.ResolveBeaconActionsAsync(list, OUT_OF_RANGE_DB);


            BeaconAction result = await action.Task;

            Assert.AreEqual(orgAction, result, "action not found");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveMultipleAction()
        {
            LayoutManager layoutManager = (LayoutManager)ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);

            BackgroundEngine engine = new BackgroundEngine();
            TaskCompletionSource<IList<BeaconAction>> action = new TaskCompletionSource<IList<BeaconAction>>();
            IList<BeaconAction> actions = new List<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) =>
            {
                actions.Add(args);
                if (actions.Count >= 3)
                {
                    action.SetResult(actions);
                }
            };
            List<Beacon> list = new List<Beacon>() { new Beacon() { Id1 = "7367672374000000ffff0000ffff0003", Id2 = 48869, Id3 = 21321 } };

            await engine.InitializeAsync();
            await engine.ResolveBeaconActionsAsync(list, OUT_OF_RANGE_DB);

            IList<BeaconAction> result = await action.Task;

            Assert.AreEqual(3, result.Count, "Not 3 action found");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveSingleActionNoResult()
        {
            LayoutManager layoutManager = (LayoutManager)ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);

            BackgroundEngine engine = new BackgroundEngine();
            TaskCompletionSource<IList<BeaconAction>> action = new TaskCompletionSource<IList<BeaconAction>>();
            IList<BeaconAction> actions = new List<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) =>
            {
                actions.Add(args);
                if (actions.Count >= 3)
                {
                    action.SetResult(actions);
                }
            };
            List<Beacon> list = new List<Beacon>() { new Beacon() { Id1 = "7367672374000000ffff0000ffff1234", Id2 = 39178, Id3 = 30929 } };

            if (await Task.WhenAny(action.Task, Task.Delay(500)) == action.Task)
            {
                Assert.AreEqual(0, action.Task.Result, "Not 0 action found");
            }
            else
            {
                //timeout is fine
            }
        }
    }
}