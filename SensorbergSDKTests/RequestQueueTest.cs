// Created by Kay Czarnotta on 09.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class RequestQueueTest
    {
        [TestInitialize]
        public void TestSetup()
        {
            ServiceManager.LayoutManager = new MockLayoutManager();
        }

        [TestMethod]
        public async Task SimpleRequestQueueTest()
        {
            RequestQueue queue = new RequestQueue();
            Request req = new Request(new BeaconEventArgs(), 1);
            TaskCompletionSource<RequestResultState> requestReady = new TaskCompletionSource<RequestResultState>();
            req.Result += (sender, state1) => { requestReady.SetResult(state1); };
            queue.Add(req);

            RequestResultState state = await requestReady.Task;
            Assert.AreEqual(RequestResultState.Success, state, "Request not successfull");
        }

        [TestMethod]
        public async Task MultipleRequestQueueTest()
        {
            RequestQueue queue = new RequestQueue();
            TaskCompletionSource<RequestResultState> requestReady = new TaskCompletionSource<RequestResultState>();
            List<RequestResultState> requestsList = new List<RequestResultState>();
            for (int i = 0; i < 10; i++)
            {
                Request req = new Request(new BeaconEventArgs(), i);
                if (i == 9)
                {
                    req.Result += (sender, state1) =>
                    {
                        Debug.WriteLine("MultipleRequestQueueTest - "+ ((Request)sender).RequestId+" - " + state1);
                        requestsList.Add(state1);
                        requestReady.SetResult(state1);
                    };
                }
                else
                {
                    req.Result += (sender, state1) =>
                    {
                        Debug.WriteLine("MultipleRequestQueueTest - " + ((Request)sender).RequestId + " - " + state1);
                        requestsList.Add(state1);
                    };
                }
                queue.Add(req);
            }
            if (await Task.WhenAny(requestReady.Task, Task.Delay(500000)) == requestReady.Task)
            {
                Assert.AreEqual(RequestResultState.Success, requestReady.Task.Result, "Request not successfull");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
                Assert.Fail("Timout");
        }


        [TestMethod]
        public async Task MultipleRequestWithFailuresQueueTest()
        {
            ((MockLayoutManager) ServiceManager.LayoutManager).ShouldFail += (r, fail) =>
            {
                if (r.RequestId == 5 && r.TryCount == 1)
                {
                    fail.Fail = true;
                    return;
                }

                if(r.RequestId == 6)
                {
                    fail.Fail = true;
                    return;
                }
            };

            RequestQueue queue = new RequestQueue();
            TaskCompletionSource<RequestResultState> requestReady = new TaskCompletionSource<RequestResultState>();
            List<RequestResultState> requestsList = new List<RequestResultState>();
            for (int i = 0; i < 10; i++)
            {
                Request req = new Request(new BeaconEventArgs(), i);
                req.Result += (sender, state1) =>
                {
                        Debug.WriteLine("MultipleRequestWithFailuresQueueTest - " + ((Request)sender).RequestId+" - " + state1);
                    if (((Request)sender).RequestId == 5 && ((Request)sender).TryCount != 2)
                    {
                        return;
                    }
                    if (((Request)sender).RequestId == 6 && ((Request)sender).TryCount != 3)
                    {
                        return;
                    }
                    requestsList.Add(state1);
                    if (queue.QueueSize == 0)
                    {
                        requestReady.SetResult(state1);
                    }
                };
                queue.Add(req);
            }
            if (await Task.WhenAny(requestReady.Task, Task.Delay(500000)) == requestReady.Task)
            {
                Assert.AreEqual(RequestResultState.Failed, requestReady.Task.Result, "Request successfull (last should fail)");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
                Assert.Fail("Timout");

        }

        [TestMethod]
        public async Task MultipleRequestBlocksQueueTest()
        {
            RequestQueue queue = new RequestQueue();
            TaskCompletionSource<RequestResultState> requestReady = new TaskCompletionSource<RequestResultState>();
            List<RequestResultState> requestsList = new List<RequestResultState>();
            for (int i = 0; i < 10; i++)
            {
                Request req = new Request(new BeaconEventArgs(), i);
                if (i == 9)
                {
                    req.Result += (sender, state1) =>
                    {
                        Debug.WriteLine("MultipleRequestBlocksQueueTest - " + ((Request)sender).RequestId+" - " + state1);
                        requestsList.Add(state1);
                        requestReady.SetResult(state1);
                    };
                }
                else
                {
                    req.Result += (sender, state1) =>
                    {
                        Debug.WriteLine("MultipleRequestBlocksQueueTest - " + ((Request)sender).RequestId+" - " + state1);
                        requestsList.Add(state1);
                    };
                }
                queue.Add(req);
            }
            if (await Task.WhenAny(requestReady.Task, Task.Delay(500000)) == requestReady.Task)
            {
                Assert.AreEqual(RequestResultState.Success, requestReady.Task.Result, "Request not successfull");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
                Assert.Fail("Timout");



            requestReady = new TaskCompletionSource<RequestResultState>();
            requestsList = new List<RequestResultState>();
            for (int i = 0; i < 10; i++)
            {
                Request req = new Request(new BeaconEventArgs(), i);
                if (i == 9)
                {
                    req.Result += (sender, state1) =>
                    {
                        Debug.WriteLine("MultipleRequestBlocksQueueTest#2 - " + ((Request)sender).RequestId+" - " + state1);
                        requestsList.Add(state1);
                        requestReady.SetResult(state1);
                    };
                }
                else
                {
                    req.Result += (sender, state1) =>
                    {
                        Debug.WriteLine("MultipleRequestBlocksQueueTest#2 - " + ((Request)sender).RequestId+" - " + state1);
                        requestsList.Add(state1);
                    };
                }
                queue.Add(req);
            }
            if (await Task.WhenAny(requestReady.Task, Task.Delay(500000)) == requestReady.Task)
            {
                Assert.AreEqual(RequestResultState.Success, requestReady.Task.Result, "Request not successfull");

                Assert.AreEqual(10, requestsList.Count, "Not 10 request results");
            }
            else
                Assert.Fail("Timout2");
        }
    }
}