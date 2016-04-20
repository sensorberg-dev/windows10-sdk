// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class ResolverTest
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
        [Timeout(1000)]
        public async Task ResolveSingleAction()
        {
            IResolver resolver = new Resolver();
            TaskCompletionSource<IList<ResolvedAction>> action = new TaskCompletionSource<IList<ResolvedAction>>();
            resolver.ActionsResolved += (sender, args) =>
            {
                action.SetResult(args.ResolvedActions);
            };
            await resolver.CreateRequest(new BeaconEventArgs() {Beacon = new Beacon() {Id1= "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929 }, EventType = BeaconEventType.Enter});

            IList<ResolvedAction> result = await action.Task;

            Assert.AreEqual(1, result.Count, "Not 1 action found");
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task ResolveMultipleAction()
        {
            IResolver resolver = new Resolver();
            TaskCompletionSource<IList<ResolvedAction>> action = new TaskCompletionSource<IList<ResolvedAction>>();
            resolver.ActionsResolved += (sender, args) =>
            {
                action.SetResult(args.ResolvedActions);
            };
            await resolver.CreateRequest(new BeaconEventArgs() { Beacon = new Beacon() { Id1 = "7367672374000000ffff0000ffff0003", Id2 = 48869, Id3 = 21321 }, EventType = BeaconEventType.Enter });

            IList<ResolvedAction> result = await action.Task;

            Assert.AreEqual(3, result.Count, "Not 3 action found");
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task ResolveSingleActionNoResult()
        {
            IResolver resolver = new Resolver();
            TaskCompletionSource<IList<ResolvedAction>> action = new TaskCompletionSource<IList<ResolvedAction>>();
            resolver.ActionsResolved += (sender, args) =>
            {
                action.SetResult(args.ResolvedActions);
            };
            await resolver.CreateRequest(new BeaconEventArgs() { Beacon = new Beacon() { Id1 = "7367672374000000ffff0000ffff1234", Id2 = 39178, Id3 = 30929 }, EventType = BeaconEventType.Enter });

            IList<ResolvedAction> result = await action.Task;

            Assert.AreEqual(0, result.Count, "Not 0 action found");
        }
    }
}