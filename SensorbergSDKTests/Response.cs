using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Json;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDKTests
{
    [TestClass]
    public class UnitTestResponse
    {
        [TestInitialize]
        public void TestSetup()
        {
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
        }
        [TestMethod]
        public async Task Response_latest_response()
        {
            var uri = new Uri("ms-appx:///Assets/raw/latest_response.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);
            
            Assert.IsNotNull(resp);
            Assert.AreEqual(2,resp.AccountBeaconId1s.Count);
            Assert.AreEqual(5,resp.ResolvedActions.Count );

            IList<ResolvedAction> list =  resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff13370133701337", BeaconEventType.Enter);
            Assert.IsNotNull(list);
            Assert.AreEqual(3, list.Count);
        }

        [TestMethod]
        public async Task Response_reportImmediately_false()
        {
            var uri = new Uri("ms-appx:///Assets/raw/reportImmediately_false.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1s.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            foreach (var item in list)
            {
                Assert.IsFalse(item.ReportImmediately); 
            }
            
        }

        [TestMethod]
        public async Task Response_reportImmediately_true()
        {
            
            var uri = new System.Uri("ms-appx:///Assets/raw/reportImmediately_true.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1s.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            foreach (var item in list)
            {
                Assert.IsTrue(item.ReportImmediately);
            }
            
        }

        [TestMethod]
        public async Task Response_reportImmediately_not_set()
        {
            var uri = new System.Uri("ms-appx:///Assets/raw/reportImmediately_not_set.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1s.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            foreach (var item in list)
            {
                Assert.IsFalse(item.ReportImmediately);
            }

        }
        [TestMethod]
        public async Task Response_sendOnlyOnce_set()
        {
            var uri = new System.Uri("ms-appx:///Assets/raw/response_sendOnlyOnce_true.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1s.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            foreach (var item in list)
            {
                Assert.IsTrue(item.SendOnlyOnce);
            }

        }

        [TestMethod]
        public async Task Response_supressionTime()
        {
            var uri = new System.Uri("ms-appx:///Assets/raw/response_supressionTime.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1s.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            foreach (var item in list)
            {
                Assert.IsTrue(item.SupressionTime == 30);
            }

        }
        [TestMethod]
        public async Task Response_ResolvedAction_serialization()
        {
            var uri = new System.Uri("ms-appx:///Assets/raw/response_sendOnlyOnce_true.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = Layout.FromJson(null, val.GetObject(), DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1s.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);

            foreach (var item in list)
            {
                string strObj = ResolvedAction.Serialize(item);

                ResolvedAction obj = ResolvedAction.Deserialize(strObj);

                Assert.AreEqual(obj.BeaconAction.Url, item.BeaconAction.Url);
                Assert.AreEqual(obj.BeaconAction.Subject, item.BeaconAction.Subject);
                Assert.AreEqual(obj.ReportImmediately, item.ReportImmediately);
            }

        }




    }
}
