﻿// Created by Kay Czarnotta on 19.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class ResolverTest
    {
        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.ClearFiles("sensorberg-storage");
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService() {Storage = new MockStorage()};
            ServiceManager.ReadOnlyForTests = true;
            ApplicationData.Current.LocalSettings.Values.Remove(SdkData.KeyIncrementalId);
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task ResolveSingleAction()
        {
            IResolver resolver = new Resolver(true);
            TaskCompletionSource<IList<ResolvedAction>> action = new TaskCompletionSource<IList<ResolvedAction>>();
            resolver.ActionsResolved += (sender, args) =>
            {
                action.SetResult(args.ResolvedActions);
            };
            await resolver.CreateRequest(new BeaconEventArgs()
            {
                Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                EventType = BeaconEventType.Enter
            });

            IList<ResolvedAction> result = await action.Task;

            Assert.AreEqual(1, result.Count, "Not 1 action found");
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task ResolveMultipleAction()
        {
            IResolver resolver = new Resolver(true);
            TaskCompletionSource<IList<ResolvedAction>> action = new TaskCompletionSource<IList<ResolvedAction>>();
            resolver.ActionsResolved += (sender, args) =>
            {
                action.SetResult(args.ResolvedActions);
            };
            await resolver.CreateRequest(new BeaconEventArgs()
            {
                Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0003", Id2 = 48869, Id3 = 21321},
                EventType = BeaconEventType.Enter
            });

            IList<ResolvedAction> result = await action.Task;

            Assert.AreEqual(4, result.Count, "Not 4 action found");
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task ResolveSingleActionNoResult()
        {
            IResolver resolver = new Resolver(true);
            TaskCompletionSource<IList<ResolvedAction>> action = new TaskCompletionSource<IList<ResolvedAction>>();
            resolver.ActionsResolved += (sender, args) =>
            {
                action.SetResult(args.ResolvedActions);
            };
            await resolver.CreateRequest(new BeaconEventArgs()
            {
                Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff1234", Id2 = 39178, Id3 = 30929},
                EventType = BeaconEventType.Enter
            });

            IList<ResolvedAction> result = await action.Task;

            Assert.AreEqual(0, result.Count, "Not 0 action found");
        }

        /***************************** Async Part ************************************/

        [TestMethod]
        [Timeout(5000)]
        public async Task SimpleRequestQueueTest()
        {
            MockLayoutManager layoutManager = new MockLayoutManager();
            layoutManager.FindOneAction = true;
            IResolver resolver = new Resolver(false);
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.LayoutManager = layoutManager;
            ServiceManager.ReadOnlyForTests = true;

            TaskCompletionSource<ResolvedActionsEventArgs> requestReady = new TaskCompletionSource<ResolvedActionsEventArgs>();
            resolver.ActionsResolved += (sender, args) =>
            {
                requestReady.TrySetResult(args);
            };
            await resolver.CreateRequest(new BeaconEventArgs()
            {
                Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                EventType = BeaconEventType.Enter
            });

            ResolvedActionsEventArgs state = await requestReady.Task;
            Assert.AreEqual(1, state.ResolvedActions.Count, "Request not successfull");
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task MultipleRequestQueueTest()
        {
            MockLayoutManager layoutManager = new MockLayoutManager();
            layoutManager.FindOneAction = true;
            IResolver resolver = new Resolver(false);
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.LayoutManager = layoutManager;
            ServiceManager.ReadOnlyForTests = true;

            TaskCompletionSource<List<ResolvedActionsEventArgs>> requestReady = new TaskCompletionSource<List<ResolvedActionsEventArgs>>();
            List<ResolvedActionsEventArgs> requestsList = new List<ResolvedActionsEventArgs>();
            resolver.ActionsResolved += (sender, args) =>
            {
                requestsList.Add(args);
            };
            ((Resolver) resolver).Finished += () =>
            {
                if (requestsList.Count == 10)
                {
                    requestReady.TrySetResult(requestsList);
                }
            };
            for (int i = 0; i < 10; i++)
            {
                await resolver.CreateRequest(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });
            }
            if (await Task.WhenAny(requestReady.Task, Task.Delay(500000)) == requestReady.Task)
            {
                Assert.AreEqual(10, requestReady.Task.Result.Count, "Request not successfull");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
            {
                Assert.Fail("Timout");
            }
        }


        [TestMethod]
        [Timeout(5000)]
        public async Task MultipleRequestWithFailuresQueueTest()
        {
            MockLayoutManager layoutManager = new MockLayoutManager();
            layoutManager.FindOneAction = true;
            IResolver resolver = new Resolver(false);
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.LayoutManager = layoutManager;
            ServiceManager.ReadOnlyForTests = true;

            layoutManager.ShouldFail += (r, fail) =>
            {
                if (r.RequestId == 5 && r.TryCount == 1)
                {
                    fail.Fail = true;
                    return;
                }

                if (r.RequestId == 6)
                {
                    fail.Fail = true;
                    return;
                }
            };

            TaskCompletionSource<List<ResolvedActionsEventArgs>> requestReady = new TaskCompletionSource<List<ResolvedActionsEventArgs>>();
            List<ResolvedActionsEventArgs> requestsList = new List<ResolvedActionsEventArgs>();
            resolver.ActionsResolved += (sender, args) =>
            {
                requestsList.Add(args);
            };
            ((Resolver) resolver).Finished += () => requestReady.TrySetResult(requestsList);

            for (int i = 0; i < 10; i++)
            {
                await resolver.CreateRequest(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });
            }

            if (await Task.WhenAny(requestReady.Task, Task.Delay(5000)) == requestReady.Task)
            {
                Assert.AreEqual(9, requestReady.Task.Result.Count, "Request successfull (last should fail)");

                Assert.AreEqual(9, requestsList.Count, "Not 9 request results");
            }
            else
            {
                Assert.Fail("Timout");
            }
        }

        [TestMethod]
        [Timeout(500000)]
        public async Task MultipleRequestBlocksQueueTest()
        {
            MockLayoutManager layoutManager = new MockLayoutManager();
            layoutManager.FindOneAction = true;
            IResolver resolver = new Resolver(false);
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.LayoutManager = layoutManager;
            ServiceManager.ReadOnlyForTests = true;


            TaskCompletionSource<List<ResolvedActionsEventArgs>> requestReady = new TaskCompletionSource<List<ResolvedActionsEventArgs>>();
            List<ResolvedActionsEventArgs> requestsList = new List<ResolvedActionsEventArgs>();
            EventHandler<ResolvedActionsEventArgs> resolverOnActionsResolved = (sender, args) =>
            {
                requestsList.Add(args);
            };
            resolver.ActionsResolved += resolverOnActionsResolved;
            ((Resolver) resolver).Finished += () =>
            {
                if (requestsList.Count == 10)
                {
                    requestReady.TrySetResult(requestsList);
                }
            };

            for (int i = 0; i < 10; i++)
            {
                await resolver.CreateRequest(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });
            }

            if (await Task.WhenAny(requestReady.Task, Task.Delay(500000)) == requestReady.Task)
            {
                Assert.AreEqual(10, requestReady.Task.Result.Count, "Request not successfull");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
            {
                Assert.Fail("Timout");
            }
            resolver.ActionsResolved -= resolverOnActionsResolved;

            requestsList = new List<ResolvedActionsEventArgs>();
            TaskCompletionSource<List<ResolvedActionsEventArgs>> requestReady2 = new TaskCompletionSource<List<ResolvedActionsEventArgs>>();

            resolver.ActionsResolved += (sender, args) =>
            {
                requestsList.Add(args);
            };
            ((Resolver) resolver).Finished += () =>
            {
                if (requestsList.Count == 10)
                {
                    requestReady2.TrySetResult(requestsList);
                }
            };
            for (int i = 0; i < 10; i++)
            {
                await resolver.CreateRequest(new BeaconEventArgs()
                {
                    Beacon = new Beacon() {Id1 = "7367672374000000ffff0000ffff0004", Id2 = 39178, Id3 = 30929},
                    EventType = BeaconEventType.Enter
                });
            }

            if (await Task.WhenAny(requestReady2.Task, Task.Delay(500000)) == requestReady2.Task)
            {
                Assert.AreEqual(10, requestReady2.Task.Result.Count, "Request not successfull");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
            {
                Assert.Fail("Timout2");
            }
        }
    }
}