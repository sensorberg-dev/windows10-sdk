using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class UnitTestResponse
    {
        [TestInitialize]
        public async Task TestSetup()
        {
            await TestHelper.Clear();
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.StorageService = new StorageService() {Storage = new MockStorage()};
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task Response_latest_response()
        {
            var uri = new Uri("ms-appx:///Assets/raw/latest_response.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(2, resp.AccountBeaconId1S.Count);
            Assert.AreEqual(5, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff13370133701337", BeaconEventType.Enter);
            Assert.IsNotNull(list);
            Assert.AreEqual(3, list.Count);
        }

        [TestMethod]
        public async Task Response_reportImmediately_false()
        {
            var uri = new Uri("ms-appx:///Assets/raw/reportImmediately_false.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1S.Count);
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
            var uri = new Uri("ms-appx:///Assets/raw/reportImmediately_true.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1S.Count);
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
            var uri = new Uri("ms-appx:///Assets/raw/reportImmediately_not_set.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1S.Count);
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
            var uri = new Uri("ms-appx:///Assets/raw/response_sendOnlyOnce_true.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1S.Count);
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
            var uri = new Uri("ms-appx:///Assets/raw/response_supressionTime.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            var val = JsonValue.Parse(text);

            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1S.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            foreach (var item in list)
            {
                Assert.IsTrue(item.SuppressionTime == 30);
            }
        }

        [TestMethod]
        public async Task Response_ResolvedAction_serialization()
        {
            var uri = new Uri("ms-appx:///Assets/raw/response_sendOnlyOnce_true.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);
            Layout resp = JsonConvert.DeserializeObject<Layout>(text, new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });
            resp?.FromJson(null, DateTimeOffset.Now);

            Assert.IsNotNull(resp);
            Assert.AreEqual(1, resp.AccountBeaconId1S.Count);
            Assert.AreEqual(1, resp.ResolvedActions.Count);

            IList<ResolvedAction> list = resp.GetResolvedActionsForPidAndEvent("7367672374000000ffff0000ffff00070800800005", BeaconEventType.Exit);
        }
    }
}
