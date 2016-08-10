// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class StorageServiceTest
    {
        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.ClearFiles("sensorberg-storage");
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.StorageService = new StorageServiceExtend();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.WriterFactory = new WriterFactory();
            ServiceManager.ReadOnlyForTests = true;


            await ServiceManager.StorageService.InitStorage();
        }

        [TestMethod]
        public async Task ValidateAPIKeyTest()
        {
            MockApiConnection connection = (MockApiConnection) ServiceManager.ApiConnction;

            IStorageService service = ServiceManager.StorageService;
            Assert.AreEqual(ApiKeyValidationResult.Valid, await service.ValidateApiKey("true"), "Not successfull");
            connection.APIInvalid = true;
            Assert.AreEqual(ApiKeyValidationResult.Invalid, await service.ValidateApiKey("false"), "Not invalid");
            connection.APIInvalid = false;


            connection.FailNetwork = true;
            Assert.AreEqual(ApiKeyValidationResult.NetworkError, await service.ValidateApiKey("true"), "No network issue");
            connection.FailNetwork = false;

            connection.UnknownError = true;
            Assert.AreEqual(ApiKeyValidationResult.UnknownError, await service.ValidateApiKey("true"), "No unknown issue");
            connection.UnknownError = false;
        }

        [TestMethod]
        public async Task RetrieveLayoutTest()
        {
            MockApiConnection connection = (MockApiConnection) ServiceManager.ApiConnction;
            IStorageService service = ServiceManager.StorageService;

            ApplicationData.Current.LocalSettings.Values.Remove(LayoutManager.KeyLayoutHeaders);
            ApplicationData.Current.LocalSettings.Values.Remove(LayoutManager.KeyLayoutRetrievedTime);
            await TestHelper.RemoveFile(LayoutManager.KeyLayoutContent);

            connection.FailNetwork = true;
            LayoutResult layout = await service.RetrieveLayout();
            Assert.AreEqual(NetworkResult.NetworkError, layout.Result, "Not failed");
            connection.FailNetwork = false;

            layout = await service.RetrieveLayout();
            Assert.AreEqual(NetworkResult.Success, layout.Result, "Not successfull loaded");
            LayoutManagerTest.ValidateMockLayout(layout.Layout);

            connection.FailNetwork = true;
            //should be cached
            layout = await service.RetrieveLayout();
            Assert.AreEqual(NetworkResult.Success, layout.Result, "Not successfull loaded from cache");
            LayoutManagerTest.ValidateMockLayout(layout.Layout);
        }

        [TestMethod]
        public async Task FlushHistoryTest()
        {
            StorageServiceExtend sse = (StorageServiceExtend) ServiceManager.StorageService;
            MockStorage mockStorage = new MockStorage();
            sse.SetStorage(mockStorage);
            mockStorage.UndeliveredActions = new List<HistoryAction> {new HistoryAction()};
            mockStorage.UndeliveredEvents = new List<HistoryEvent> {new HistoryEvent()};

            MockApiConnection connection = (MockApiConnection) ServiceManager.ApiConnction;
            IStorageService service = ServiceManager.StorageService;


            connection.FailNetwork = true;
            Assert.IsFalse(await service.FlushHistory(), "Flushing History not failed");
            connection.FailNetwork = false;
            Assert.IsTrue(mockStorage.UndeliveredEvents.Count != 0, "Event were resetet");
            Assert.IsTrue(mockStorage.UndeliveredActions.Count != 0, "Actions were resetet");

            Assert.IsTrue(await service.FlushHistory(), "Flushing History not succeed");
            Assert.IsTrue(mockStorage.UndeliveredEvents.Count == 0, "Event were not marked as send");
            Assert.IsTrue(mockStorage.UndeliveredActions.Count == 0, "Actions were not marked as send");
        }

        [TestMethod]
        public async Task TestBackgroundActionsTest()
        {
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.LayoutManager = new LayoutManagerExtend();

            Layout resp =
                JsonConvert.DeserializeObject<Layout>(
                    await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/raw/mock/mock_layout.json", UriKind.RelativeOrAbsolute))),
                    new JsonSerializerSettings
                    {
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc
                    });
            resp?.FromJson(await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/raw/mock/layout_request_header.txt", UriKind.RelativeOrAbsolute))),
                DateTimeOffset.Now);

            ((LayoutManagerExtend) ServiceManager.LayoutManager).SetLayout(resp);
            ServiceManager.ReadOnlyForTests = true;

            FileStorage storage = new FileStorage {Background = true};

            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("9ded63644e424d758b0218f7c70f2473", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"),
                BeaconEventType.Enter, "1"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3f30be2605524f82a9bf0ccb4a81618f", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"),
                BeaconEventType.Exit, "2"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("312a8594e07542bd814ecdd17f76538e", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, ""));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("959ea393e3424ab7ad53584a8b789197", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, null));


            IStorageService storageService = ServiceManager.StorageService;
            List<BeaconAction> beaconActions = await storageService.GetActionsForForeground();

            Assert.AreEqual(4, beaconActions.Count, "Not 4 actions found");

            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("9ded63644e424d758b0218f7c70f2473", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"),
                BeaconEventType.Enter, "1"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3f30be2605524f82a9bf0ccb4a81618f", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"),
                BeaconEventType.Exit, "2"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("312a8594e07542bd814ecdd17f76538e", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, ""));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("959ea393e3424ab7ad53584a8b789197", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, null));

            beaconActions = await storageService.GetActionsForForeground();

            Assert.AreEqual(4, beaconActions.Count, "Not 4 actions found");

            StorageService service = (StorageService) ServiceManager.StorageService;
            IStorage foregroundStorage = service.Storage;
            await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("4", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"),
                BeaconEventType.Enter, "1"));
            await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"),
                BeaconEventType.Exit, "2"));
            await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, ""));
            await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, null));

            IList<HistoryAction> historyActions = await foregroundStorage.GetUndeliveredActions();

            Assert.AreEqual(12, historyActions.Count, "Not 12 history actions found");


            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("9ded63644e424d758b0218f7c70f2473", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"),
                BeaconEventType.Enter, "1"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3f30be2605524f82a9bf0ccb4a81618f", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"),
                BeaconEventType.Exit, "2"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("312a8594e07542bd814ecdd17f76538e", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, ""));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("959ea393e3424ab7ad53584a8b789197", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, null));

            await foregroundStorage.SetActionsAsDelivered();
            historyActions = await foregroundStorage.GetUndeliveredActions();
            Assert.AreEqual(4, historyActions.Count, "Not 4 history actions found");

            beaconActions = await storageService.GetActionsForForeground();

            Assert.AreEqual(4, beaconActions.Count, "Not 4 actions found");



            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("9ded63644e424d758b0218f7c70f2473", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"),
                BeaconEventType.Enter, "1"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3f30be2605524f82a9bf0ccb4a81618f", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"),
                BeaconEventType.Exit, "2"));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("312a8594e07542bd814ecdd17f76538e", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, ""));
            await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("959ea393e3424ab7ad53584a8b789197", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"),
                BeaconEventType.EnterExit, null));

            await foregroundStorage.GetUndeliveredActions();
            await foregroundStorage.SetActionsAsDelivered();
            historyActions = await foregroundStorage.GetUndeliveredActions();
            Assert.AreEqual(0, historyActions.Count, "Not 0 history actions found");

            beaconActions = await storageService.GetActionsForForeground();

            Assert.AreEqual(4, beaconActions.Count, "Not 4 actions found");
        }

        [TestMethod]
        public async Task TestFileLock()
        {
            IStorageService service = ServiceManager.StorageService;
            await service.SaveHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter, "1");
            await service.SaveHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit, "2");

            StorageFolder folder = await ((FileStorage) ((StorageService) service).Storage).GetFolder(FileStorage.ForegroundActionsFolder);
            StorageFile file = await folder.CreateFileAsync(FileStorage.ActionsFileName, CreationCollisionOption.OpenIfExists);
            IRandomAccessStream randomAccessStream;
            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                Task.Run(() =>
                {
                    Task.Delay(200);
                    randomAccessStream.Dispose();
                }).ConfigureAwait(false);
                await service.SaveHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter, "1");
            }
            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                await service.SaveHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit, "1");
            }
            folder = await ((FileStorage) ((StorageService) service).Storage).GetFolder(FileStorage.ForegroundEventsFolder);
            file = await folder.CreateFileAsync("1", CreationCollisionOption.OpenIfExists);
            await service.SaveHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter, "2");
            await service.SaveHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit, "3");

            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                Task.Run(() =>
                {
                    Task.Delay(200);
                    randomAccessStream.Dispose();
                }).ConfigureAwait(false);
                await service.SaveHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter, "12");
            }
            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                await service.SaveHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit, "123");
            }
        }
    }
}