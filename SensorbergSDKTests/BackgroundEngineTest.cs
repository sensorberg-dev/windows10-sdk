// Created by Kay Czarnotta on 20.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Storage;
using MetroLog;
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
        private static readonly ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<BackgroundEngineTest>();
        private const int OUT_OF_RANGE_DB = -128;

        [TestInitialize]
        public async Task Setup()
        {
            logger.Debug("Setup - Start");
            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("sensorberg-storage");
                await TestHelper.ClearFiles(folder);
//                await folder.DeleteAsync();
//                await Task.Delay(200);
            }
            catch (FileNotFoundException)
            {

            }
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService() { Storage = new MockStorage() };
            ServiceManager.ReadOnlyForTests = true;
            logger.Debug("Setup - End");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveBackgroundEvent()
        {
            logger.Debug("ResolveBackgroundEvent - Start");
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
            logger.Debug("ResolveBackgroundEvent - End");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveBackgroundEventSingle()
        {
            logger.Debug("ResolveBackgroundEventSingle - Start");
            LayoutManager layoutManager = (LayoutManager)ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);
            BeaconAction orgAction = layoutManager.Layout.ResolvedActions.FirstOrDefault(ra => ra.BeaconAction.Uuid == "9ded63644e424d758b0218f7c70f2473").BeaconAction;

            List<Beacon> list = new List<Beacon>() { new Beacon() { Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 }/*, new Beacon() { Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 }*/ };
            BackgroundEngine engine = new BackgroundEngine();
            TaskCompletionSource<BeaconAction> action = new TaskCompletionSource<BeaconAction>();
            int resolveCount=0;
            engine.BeaconActionResolved += (sender, args) =>
            {
                resolveCount++;
                action.SetResult(args);
            };
            await engine.InitializeAsync();
            await engine.ResolveBeaconActionsAsync(list, OUT_OF_RANGE_DB);


            BeaconAction result = await action.Task;

            Assert.AreEqual(orgAction, result, "action not found");
            Assert.AreEqual(1,resolveCount, "More then onetime resolved");
            logger.Debug("ResolveBackgroundEventSingle - End");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveBackgroundEventSupress()
        {
            logger.Debug("ResolveBackgroundEventSupress - Start");
            LayoutManager layoutManager = (LayoutManager)ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);
            BeaconAction orgAction = layoutManager.Layout.ResolvedActions.FirstOrDefault(ra => ra.BeaconAction.Uuid == "9ded63644e424d758b0218f7c70f2473").BeaconAction;

            List<Beacon> list = new List<Beacon>() { new Beacon() { Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 }, new Beacon() { Id1 = "7367672374000000ffff0000ffff0003", Id2 = 48869, Id3 = 21321 } };
            BackgroundEngine engine = new BackgroundEngine();
            TaskCompletionSource<BeaconAction> action = new TaskCompletionSource<BeaconAction>();
            int resolveCount = 0;
            engine.BeaconActionResolved += (sender, args) =>
            {
                resolveCount++;
                action.TrySetResult(args);
            };
            await engine.InitializeAsync();
            await engine.ResolveBeaconActionsAsync(list, OUT_OF_RANGE_DB);


            BeaconAction result = await action.Task;

            Assert.AreEqual(orgAction, result, "action not found");
            Assert.AreEqual(1, resolveCount, "More then onetime resolved");
            logger.Debug("ResolveBackgroundEventSupress - End");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveMultipleAction()
        {
            logger.Debug("ResolveMultipleAction - Start");
            LayoutManager layoutManager = (LayoutManager)ServiceManager.LayoutManager;
            await layoutManager.VerifyLayoutAsync(true);

            BackgroundEngine engine = new BackgroundEngine();
            IList<BeaconAction> actions = new List<BeaconAction>();
            engine.BeaconActionResolved += (sender, args) =>
            {
                actions.Add(args);
            };
            List<Beacon> list = new List<Beacon>() { new Beacon() { Id1 = "7367672374000000ffff0000ffff0003", Id2 = 48869, Id3 = 21321 } };

            await engine.InitializeAsync();
            await engine.ResolveBeaconActionsAsync(list, OUT_OF_RANGE_DB);


            Assert.AreEqual(3, actions.Count, "Not 3 action found");
            logger.Debug("ResolveMultipleAction - End");
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task ResolveSingleActionNoResult()
        {
            logger.Debug("ResolveSingleActionNoResult - Start");
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
            logger.Debug("ResolveSingleActionNoResult - End");
        }
    }
}