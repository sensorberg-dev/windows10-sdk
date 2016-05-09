// Created by Kay Czarnotta on 23.03.2016
// 
// Copyright (c) 2016,  Senorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class StorageClassTest
    {
        private IStorage storage;

        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.ClearFiles("sensorberg-storage");
            storage = new FileStorage();
        }

        [TestMethod]
        public async Task Cleanup()
        {
            await storage.CleanDatabase();
        }


        /// <summary>
        /// Should not fail.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InitTest()
        {
            await storage.InitStorage();
        }

        [TestMethod]
        public async Task DeleayedActionTest()
        {
            ResolvedAction action = new ResolvedAction();
            action.BeaconAction = new BeaconAction();
            action.BeaconAction.Body = "body";
            action.BeaconAction.Id = 1;
            action.BeaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            action.BeaconAction.Subject = "Subject";
            action.BeaconAction.Type = BeaconActionType.InApp;
            action.BeaconAction.Url = "http://sensorberg.com";
            action.BeaconAction.Uuid = "uuid";
            action.Delay = 123;
            action.BeaconPids = new List<string>() {"1", "2", "3", "4"};
            action.EventTypeDetectedByDevice = BeaconEventType.EnterExit;
            action.ReportImmediately = true;
            action.SendOnlyOnce = true;
            action.SuppressionTime = 321;
            action.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2015-04-15T12:00:00.000+0000")}
            };

            await storage.InitStorage();

            Assert.IsTrue(await storage.SaveDelayedAction(action, DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), "1", BeaconEventType.Enter));

            IList<DelayedActionData> delayedActions = await storage.GetDelayedActions(int.MaxValue);
            Assert.AreEqual(1, delayedActions.Count, "to many actions found");

            DelayedActionData delayAction = delayedActions[0];
            Assert.AreEqual("1", delayAction.BeaconPid, "Not same beacon id");
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), delayAction.DueTime, "not same delay time");
            Assert.AreEqual(BeaconEventType.Enter, delayAction.EventTypeDetectedByDevice, "not same event type");

            Assert.AreEqual(action.Delay, delayAction.ResolvedAction.Delay, "not same action delay");
            Assert.AreEqual(action.EventTypeDetectedByDevice, delayAction.ResolvedAction.EventTypeDetectedByDevice, "not same action event type");
            Assert.AreEqual(action.ReportImmediately, delayAction.ResolvedAction.ReportImmediately, "not same ReportImmediately");
            Assert.AreEqual(action.SendOnlyOnce, delayAction.ResolvedAction.SendOnlyOnce, "not same SendOnlyOnce");
            Assert.AreEqual(action.SuppressionTime, delayAction.ResolvedAction.SuppressionTime, "not same SendOnlyOnce");
            Assert.AreEqual(action.Timeframes.Count, delayAction.ResolvedAction.Timeframes.Count, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].Start, delayAction.ResolvedAction.Timeframes[0].Start, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].End, delayAction.ResolvedAction.Timeframes[0].End, "not same Timeframes count");

            Assert.AreEqual(action.BeaconPids.Count, delayAction.ResolvedAction.BeaconPids.Count, "not same beacon count");
            Assert.AreEqual(action.BeaconAction.Body, delayAction.ResolvedAction.BeaconAction.Body, "not same beacon action body");
            Assert.AreEqual(action.BeaconAction.Subject, delayAction.ResolvedAction.BeaconAction.Subject, "not same beacon action Subject");
            Assert.AreEqual(action.BeaconAction.Id, delayAction.ResolvedAction.BeaconAction.Id, "not same beacon action Id");
            Assert.AreEqual(action.BeaconAction.Type, delayAction.ResolvedAction.BeaconAction.Type, "not same beacon action Type");
            Assert.AreEqual(action.BeaconAction.Uuid, delayAction.ResolvedAction.BeaconAction.Uuid, "not same beacon action Uuid");
            Assert.AreEqual(action.BeaconAction.Payload.ToString(), delayAction.ResolvedAction.BeaconAction.Payload.ToString(), "not same beacon action Payload");


            Assert.AreEqual(action, delayAction.ResolvedAction, "not same action");



            ResolvedAction action2 = new ResolvedAction();
            action2.BeaconAction = new BeaconAction();
            action2.BeaconAction.Body = "body2";
            action2.BeaconAction.Id = 2;
            action2.BeaconAction.Payload = JsonObject.Parse("{\"pay\":\"load2\"}");
            action2.BeaconAction.Subject = "Subject2";
            action2.BeaconAction.Type = BeaconActionType.UrlMessage;
            action2.BeaconAction.Url = "http://sensorberg.com";
            action2.BeaconAction.Uuid = "uuid2";
            action2.Delay = 1234;
            action2.BeaconPids = new List<string>() {"1", "2", "3", "4", "5"};
            action2.EventTypeDetectedByDevice = BeaconEventType.EnterExit;
            action2.ReportImmediately = false;
            action2.SendOnlyOnce = false;
            action2.SuppressionTime = 3210;
            action2.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2014-04-15T12:00:00.000+0000")}
            };

            Assert.IsTrue(await storage.SaveDelayedAction(action, DateTimeOffset.Parse("2015-05-16T12:00:00.000+0000"), "2", BeaconEventType.EnterExit));


            delayedActions = await storage.GetDelayedActions(int.MaxValue);
            Assert.AreEqual(2, delayedActions.Count, "to many actions found");

            delayAction = delayedActions.FirstOrDefault(d => d.BeaconPid == "1");
            string idToDelete = delayAction.Id;
            Assert.AreEqual("1", delayAction.BeaconPid, "Not same beacon id");
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), delayAction.DueTime, "not same delay time");
            Assert.AreEqual(BeaconEventType.Enter, delayAction.EventTypeDetectedByDevice, "not same event type");

            Assert.AreEqual(action.Delay, delayAction.ResolvedAction.Delay, "not same action delay");
            Assert.AreEqual(action.EventTypeDetectedByDevice, delayAction.ResolvedAction.EventTypeDetectedByDevice, "not same action event type");
            Assert.AreEqual(action.ReportImmediately, delayAction.ResolvedAction.ReportImmediately, "not same ReportImmediately");
            Assert.AreEqual(action.SendOnlyOnce, delayAction.ResolvedAction.SendOnlyOnce, "not same SendOnlyOnce");
            Assert.AreEqual(action.SuppressionTime, delayAction.ResolvedAction.SuppressionTime, "not same SendOnlyOnce");
            Assert.AreEqual(action.Timeframes.Count, delayAction.ResolvedAction.Timeframes.Count, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].Start, delayAction.ResolvedAction.Timeframes[0].Start, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].End, delayAction.ResolvedAction.Timeframes[0].End, "not same Timeframes count");

            Assert.AreEqual(action.BeaconPids.Count, delayAction.ResolvedAction.BeaconPids.Count, "not same beacon count");
            Assert.AreEqual(action.BeaconAction.Body, delayAction.ResolvedAction.BeaconAction.Body, "not same beacon action body");
            Assert.AreEqual(action.BeaconAction.Subject, delayAction.ResolvedAction.BeaconAction.Subject, "not same beacon action Subject");
            Assert.AreEqual(action.BeaconAction.Id, delayAction.ResolvedAction.BeaconAction.Id, "not same beacon action Id");
            Assert.AreEqual(action.BeaconAction.Type, delayAction.ResolvedAction.BeaconAction.Type, "not same beacon action Type");
            Assert.AreEqual(action.BeaconAction.Uuid, delayAction.ResolvedAction.BeaconAction.Uuid, "not same beacon action Uuid");
            Assert.AreEqual(action.BeaconAction.Payload.ToString(), delayAction.ResolvedAction.BeaconAction.Payload.ToString(), "not same beacon action Payload");


            Assert.AreEqual(action, delayAction.ResolvedAction, "not same action");



            delayAction = delayedActions.FirstOrDefault(d => d.BeaconPid == "2");
            Assert.AreEqual("2", delayAction.BeaconPid, "Not same beacon id");
            Assert.AreEqual(DateTimeOffset.Parse("2015-05-16T12:00:00.000+0000"), delayAction.DueTime, "not same delay time");
            Assert.AreEqual(BeaconEventType.EnterExit, delayAction.EventTypeDetectedByDevice, "not same event type");

            Assert.AreEqual(action.Delay, delayAction.ResolvedAction.Delay, "not same action delay");
            Assert.AreEqual(action.EventTypeDetectedByDevice, delayAction.ResolvedAction.EventTypeDetectedByDevice, "not same action event type");
            Assert.AreEqual(action.ReportImmediately, delayAction.ResolvedAction.ReportImmediately, "not same ReportImmediately");
            Assert.AreEqual(action.SendOnlyOnce, delayAction.ResolvedAction.SendOnlyOnce, "not same SendOnlyOnce");
            Assert.AreEqual(action.SuppressionTime, delayAction.ResolvedAction.SuppressionTime, "not same SendOnlyOnce");
            Assert.AreEqual(action.Timeframes.Count, delayAction.ResolvedAction.Timeframes.Count, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].Start, delayAction.ResolvedAction.Timeframes[0].Start, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].End, delayAction.ResolvedAction.Timeframes[0].End, "not same Timeframes count");

            Assert.AreEqual(action.BeaconPids.Count, delayAction.ResolvedAction.BeaconPids.Count, "not same beacon count");
            Assert.AreEqual(action.BeaconAction.Body, delayAction.ResolvedAction.BeaconAction.Body, "not same beacon action body");
            Assert.AreEqual(action.BeaconAction.Subject, delayAction.ResolvedAction.BeaconAction.Subject, "not same beacon action Subject");
            Assert.AreEqual(action.BeaconAction.Id, delayAction.ResolvedAction.BeaconAction.Id, "not same beacon action Id");
            Assert.AreEqual(action.BeaconAction.Type, delayAction.ResolvedAction.BeaconAction.Type, "not same beacon action Type");
            Assert.AreEqual(action.BeaconAction.Uuid, delayAction.ResolvedAction.BeaconAction.Uuid, "not same beacon action Uuid");
            Assert.AreEqual(action.BeaconAction.Payload.ToString(), delayAction.ResolvedAction.BeaconAction.Payload.ToString(), "not same beacon action Payload");


            Assert.AreEqual(action, delayAction.ResolvedAction, "not same action");


            await storage.SetDelayedActionAsExecuted(idToDelete);

            delayedActions = await storage.GetDelayedActions(int.MaxValue);
            Assert.AreEqual(1, delayedActions.Count, "to many actions found after executing action");

            Assert.AreEqual("2", delayedActions[0].BeaconPid, "Not same beacon id");
        }


        [TestMethod]
        public async Task HistoryActionTest()
        {
            await storage.InitStorage();

            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            IList<HistoryAction> historyActions = await storage.GetUndeliveredActions();
            Assert.AreEqual(4, historyActions.Count, "Not 4 actions");

            HistoryAction action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Enter);
            Assert.AreEqual("1", action.BeaconId, "not same pid");
            Assert.AreEqual("1", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T12:00:00.000+00:00", action.ActionTime, "not same date");


            action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Exit);
            Assert.AreEqual("2", action.BeaconId, "not same pid");
            Assert.AreEqual("2", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T13:00:00.000+00:00", action.ActionTime, "not same date");


            action = historyActions.First(t => t.Trigger == (int) BeaconEventType.EnterExit && t.BeaconId == "3");
            Assert.AreEqual("3", action.BeaconId, "not same pid");
            Assert.AreEqual("3", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T14:00:00.000+00:00", action.ActionTime, "not same date");


            HistoryAction dbHistoryAction = await storage.GetAction("2");
            Assert.AreEqual((int) BeaconEventType.Exit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("2", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");


            IList<HistoryAction> dbHistoryActions = await storage.GetActions("3");
            Assert.AreEqual(2, dbHistoryActions.Count, "Not 2 actions");

            dbHistoryAction = dbHistoryActions.First(t => t.BeaconId == "3");
            Assert.AreEqual((int) BeaconEventType.EnterExit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("3", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("3", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");

            dbHistoryAction = dbHistoryActions.First(t => t.BeaconId == "2");
            Assert.AreEqual((int) BeaconEventType.EnterExit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("3", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");

            Assert.AreEqual(0, (await storage.GetActions("")).Count, "fails on empty id");
            Assert.AreEqual(0, (await storage.GetActions("1231312312")).Count, "fails on unkown id");

            await storage.SetActionsAsDelivered();

            historyActions = await storage.GetUndeliveredActions();
            Assert.AreEqual(0, historyActions.Count, "not all actions as delivered marked");


            Assert.IsNotNull(await storage.GetActions("3"), "no delivered message found");
            Assert.AreEqual(2, (await storage.GetActions("3")).Count, "not all actions as delivered marked from eid 3 found");
        }

        [TestMethod]
        public async Task HistoryEventTest()
        {
            await storage.InitStorage();

            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("2", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));

            IList<HistoryEvent> historyEvents = await storage.GetUndeliveredEvents();
            HistoryEvent historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-16T15:00:00.000+00:00");
            Assert.AreEqual("1", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-16T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");



            await storage.SetEventsAsDelivered();

            historyEvents = await storage.GetUndeliveredEvents();
            Assert.AreEqual(0, historyEvents.Count, "not all events as delivered marked");

        }

        [TestMethod]
        public async Task EventHistoryIssueTest()
        {
            await storage.InitStorage();

            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("2", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));

            IList<HistoryEvent> historyEvents = await storage.GetUndeliveredEvents();
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));
            await storage.SetEventsAsDelivered();

            historyEvents = await storage.GetUndeliveredEvents();
            Assert.AreEqual(1, historyEvents.Count, "the new event is missing");
        }

        [TestMethod]
        public async Task StorageClearTest()
        {
            await storage.InitStorage();

            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2015-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2015-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("2", DateTimeOffset.Parse("2015-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));

            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2015-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            Assert.AreEqual(4, (await storage.GetUndeliveredEvents()).Count, "not enough Undelivered Events found");
            Assert.AreEqual(4, (await storage.GetUndeliveredActions()).Count, "not enough Undelivered Actions found");

            await storage.SetEventsAsDelivered();
            await storage.SetActionsAsDelivered();


            Assert.AreEqual(0, (await storage.GetUndeliveredEvents()).Count, "Undelivered Events found");
            Assert.AreEqual(0, (await storage.GetUndeliveredActions()).Count, "Undelivered Actions found");

            await storage.CleanDatabase();

            Assert.AreEqual(0, (await storage.GetActions("1")).Count, "remaining Actions found");


            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2015-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2015-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("2", DateTimeOffset.Parse("2015-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));

            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2015-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            await storage.CleanDatabase();

            Assert.AreEqual(0, (await storage.GetUndeliveredEvents()).Count, "Undelivered Events found");
            Assert.AreEqual(0, (await storage.GetUndeliveredActions()).Count, "Undelivered Actions found");
        }

        [TestMethod]
        public async Task BackgroundEventStorageTest()
        {
            IStorage foregroundStorage = storage;
            await foregroundStorage.InitStorage();

            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("2", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));

            IList<HistoryEvent> historyEvents = await foregroundStorage.GetUndeliveredEvents();
            HistoryEvent historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-16T15:00:00.000+00:00");
            Assert.AreEqual("1", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-16T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");

            await foregroundStorage.SetEventsAsDelivered();

            historyEvents = await foregroundStorage.GetUndeliveredEvents();
            Assert.AreEqual(0, historyEvents.Count, "not all events as delivered marked");


            IStorage backgroundStorage = new FileStorage() {Background = true};
            await backgroundStorage.InitStorage();

            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-15T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-15T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-15T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("4", DateTimeOffset.Parse("2016-04-15T17:00:00.000+0000"), BeaconEventType.Enter)));

            historyEvents = await backgroundStorage.GetUndeliveredEvents();
            historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-15T15:00:00.000+00:00");
            Assert.AreEqual("3", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-15T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");

            await backgroundStorage.SetEventsAsDelivered();

            historyEvents = await backgroundStorage.GetUndeliveredEvents();
            Assert.AreEqual(0, historyEvents.Count, "not all events as delivered marked");
        }

        [TestMethod]
        public async Task GetEventInForegroundFromBackgroundStorageTest()
        {
            IStorage foregroundStorage = storage;
            await foregroundStorage.InitStorage();
            IStorage backgroundStorage = new FileStorage() {Background = true};
            await backgroundStorage.InitStorage();

            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("2", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter)));

            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-15T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-15T15:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("3", DateTimeOffset.Parse("2016-04-15T16:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("4", DateTimeOffset.Parse("2016-04-15T17:00:00.000+0000"), BeaconEventType.Enter)));

            //verify every storage api accesses every data
            IList<HistoryEvent> historyEvents = await backgroundStorage.GetUndeliveredEvents();
            HistoryEvent historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-15T15:00:00.000+00:00");
            Assert.AreEqual("3", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-15T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");

            historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-16T15:00:00.000+00:00");
            Assert.AreEqual("1", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-16T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");


            historyEvents = await foregroundStorage.GetUndeliveredEvents();
            historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-15T15:00:00.000+00:00");
            Assert.AreEqual("3", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-15T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");

            historyEvent = historyEvents.FirstOrDefault(h => h.EventTime == "2016-04-16T15:00:00.000+00:00");
            Assert.AreEqual("1", historyEvent.BeaconId, "Wrong pid");
            Assert.AreEqual("2016-04-16T15:00:00.000+00:00", historyEvent.EventTime, "Wrong date");
            Assert.AreEqual((int) BeaconEventType.Exit, historyEvent.Trigger, "Wrong trigger");


            await backgroundStorage.SetEventsAsDelivered();
            historyEvents = await backgroundStorage.GetUndeliveredEvents();
            Assert.AreEqual(0, historyEvents.Count, "not all events as delivered marked - background");

            historyEvents = await foregroundStorage.GetUndeliveredEvents();
            Assert.AreEqual(0, historyEvents.Count, "not all events as delivered marked - foreground");
        }

        [TestMethod]
        public async Task BackgroundActionStorageTest()
        {
            IStorage foregroundStorage = storage;
            await foregroundStorage.InitStorage();
            IStorage backgroundStorage = new FileStorage() {Background = true};
            await backgroundStorage.InitStorage();

            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            IList<HistoryAction> historyActions = await foregroundStorage.GetUndeliveredActions();
            Assert.AreEqual(4, historyActions.Count, "Not 4 actions");

            HistoryAction action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Enter);
            Assert.AreEqual("1", action.BeaconId, "not same pid");
            Assert.AreEqual("1", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T12:00:00.000+00:00", action.ActionTime, "not same date");


            action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Exit);
            Assert.AreEqual("2", action.BeaconId, "not same pid");
            Assert.AreEqual("2", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T13:00:00.000+00:00", action.ActionTime, "not same date");

            HistoryAction dbHistoryAction = await foregroundStorage.GetAction("2");
            Assert.AreEqual((int) BeaconEventType.Exit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("2", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");


            IList<HistoryAction> dbHistoryActions = await foregroundStorage.GetActions("3");
            Assert.AreEqual(2, dbHistoryActions.Count, "Not 2 actions");

            Assert.AreEqual(0, (await foregroundStorage.GetActions("")).Count, "fails on empty id");
            Assert.AreEqual(0, (await foregroundStorage.GetActions("1231312312")).Count, "fails on unkown id");

            await foregroundStorage.SetActionsAsDelivered();
            historyActions = await foregroundStorage.GetUndeliveredActions();
            Assert.AreEqual(0, historyActions.Count, "not all actions as delivered marked");


            Assert.IsNotNull(await foregroundStorage.GetActions("3"), "no delivered message found");

            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("4", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("5", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("6", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("6", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            historyActions = await backgroundStorage.GetUndeliveredActions();
            Assert.AreEqual(4, historyActions.Count, "Not 4 actions");

            action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Enter);
            Assert.AreEqual("1", action.BeaconId, "not same pid");
            Assert.AreEqual("4", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T12:00:00.000+00:00", action.ActionTime, "not same date");


            action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Exit);
            Assert.AreEqual("2", action.BeaconId, "not same pid");
            Assert.AreEqual("5", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T13:00:00.000+00:00", action.ActionTime, "not same date");


            dbHistoryAction = await backgroundStorage.GetAction("5");
            Assert.AreEqual((int) BeaconEventType.Exit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("5", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");


            dbHistoryActions = await backgroundStorage.GetActions("6");
            Assert.AreEqual(2, dbHistoryActions.Count, "Not 2 actions");

            Assert.AreEqual(0, (await backgroundStorage.GetActions("")).Count, "fails on empty id");
            Assert.AreEqual(0, (await backgroundStorage.GetActions("1231312312")).Count, "fails on unkown id");

            await backgroundStorage.SetActionsAsDelivered();
            historyActions = await backgroundStorage.GetUndeliveredActions();
            Assert.AreEqual(0, historyActions.Count, "not all actions as delivered marked");
        }

        [TestMethod]
        public async Task GetBackgroundActionsFromBackgroundinForegroundTest()
        {
            IStorage foregroundStorage = storage;
            IStorage backgroundStorage = new FileStorage() {Background = true};
            await backgroundStorage.InitStorage();

            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("4", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("5", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("6", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await backgroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("6", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("4", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("5", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("6", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await foregroundStorage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("6", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));


            IList<HistoryAction> historyActions = await backgroundStorage.GetUndeliveredActions();
            Assert.AreEqual(16, historyActions.Count, "Not 16 actions");

            historyActions = await foregroundStorage.GetUndeliveredActions();
            Assert.AreEqual(16, historyActions.Count, "Not 16 actions");

            HistoryAction dbHistoryAction = await backgroundStorage.GetAction("5");
            Assert.AreEqual((int) BeaconEventType.Exit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("5", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");

            dbHistoryAction = await foregroundStorage.GetAction("5");
            Assert.AreEqual((int) BeaconEventType.Exit, dbHistoryAction.Trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.BeaconId, "not same pid");
            Assert.AreEqual("5", dbHistoryAction.EventId, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), DateTimeOffset.Parse(dbHistoryAction.ActionTime), "not same date");


            IList<HistoryAction> dbHistoryActions = await backgroundStorage.GetActions("6");
            Assert.AreEqual(4, dbHistoryActions.Count, "Not 4 actions");

            dbHistoryActions = await foregroundStorage.GetActions("6");
            Assert.AreEqual(4, dbHistoryActions.Count, "Not 4 actions");


            await foregroundStorage.SetActionsAsDelivered();
            historyActions = await backgroundStorage.GetUndeliveredActions();
            Assert.AreEqual(0, historyActions.Count, "not all actions as delivered marked");

        }

        [TestMethod]
        public async Task TestFileLock()
        {
            await storage.InitStorage();
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));

            StorageFolder folder = await ((FileStorage) storage).GetFolder(FileStorage.ForegroundActionsFolder);
            StorageFile file = await folder.CreateFileAsync(FileStorage.ActionsFileName, CreationCollisionOption.OpenIfExists);
            IRandomAccessStream randomAccessStream;
            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                Task.Run(() =>
                {
                    Task.Delay(200);
                    randomAccessStream.Dispose();
                }).ConfigureAwait(false);
                await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter));
            }
            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                Assert.IsFalse(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
                randomAccessStream.Dispose();
            }
            folder = await ((FileStorage) storage).GetFolder(FileStorage.ForegroundEventsFolder);
            file = await folder.CreateFileAsync("1", CreationCollisionOption.OpenIfExists);
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));

            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                Task.Run(() =>
                {
                    Task.Delay(200);
                    randomAccessStream.Dispose();
                }).ConfigureAwait(false);
                await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter));
            }
            using (randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders))
            {
                Assert.IsFalse(await storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit)));
                randomAccessStream.Dispose();
            }
        }

        [TestMethod]
        public async Task EventStateTest()
        {
            await storage.InitStorage();
            Assert.IsTrue(await storage.SaveBeaconEventState("1", BeaconEventType.Enter));
            Assert.IsTrue(await storage.SaveBeaconEventState("2", BeaconEventType.Enter));
            Assert.IsTrue(await storage.SaveBeaconEventState("3", BeaconEventType.Enter));
            Assert.IsTrue(await storage.SaveBeaconEventState("1", BeaconEventType.Exit));
            Assert.IsTrue(await storage.SaveBeaconEventState("4", BeaconEventType.Exit));


            Assert.AreEqual(BeaconEventType.Exit, (await storage.GetLastEventStateForBeacon("1")).LastEvent, "Last event was not exit for 1");
            Assert.AreEqual(BeaconEventType.Enter, (await storage.GetLastEventStateForBeacon("2")).LastEvent, "Last event was not enter for 2");
            Assert.AreEqual(BeaconEventType.Enter, (await storage.GetLastEventStateForBeacon("3")).LastEvent, "Last event was not enter for 3");
            Assert.AreEqual(BeaconEventType.Exit, (await storage.GetLastEventStateForBeacon("4")).LastEvent, "Last event was not exit for 4");
            Assert.IsNull(await storage.GetLastEventStateForBeacon("5"), "not null object");
        }

        [TestMethod]
        public async Task HistoryBeaconActionTest()
        {
            await storage.InitStorage();
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));
            Assert.IsTrue(await storage.SaveHistoryAction(FileStorageHelper.ToHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit)));

            IList<HistoryAction> historyActions = await storage.GetUndeliveredActions();
            Assert.AreEqual(4, historyActions.Count, "Not 4 actions");

            HistoryAction action = historyActions.First(t => t.Trigger == (int) BeaconEventType.Enter);
            Assert.AreEqual("1", action.BeaconId, "not same pid");
            Assert.AreEqual("1", action.EventId, "not same eid");
            Assert.AreEqual("2016-04-16T12:00:00.000+00:00", action.ActionTime, "not same date");
        }


        [TestMethod]
        public async Task ReadEmptyFolderFilesTest()
        {
            await storage.InitStorage();
            await storage.GetActionsForForeground();
            await storage.GetAction("1");
            await storage.GetActions("asd");
            await storage.GetDelayedActions(123);
            await storage.GetLastEventStateForBeacon("123");
            await storage.GetUndeliveredActions();
            await storage.GetUndeliveredEvents();
            await storage.CleanDatabase();
        }
    }
}