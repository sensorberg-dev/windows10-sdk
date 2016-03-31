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
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Services;
using SQLite;

namespace SensorbergSDKTests
{
    [TestClass]
    public class StorageClassTest
    {
        private IStorage storage;

        [TestInitialize]
        public async Task Setup()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("sensorberg.db");
                if (file.IsAvailable)
                {
                    await file.DeleteAsync();
                }
            }
            catch (FileNotFoundException)
            {

            }
            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("sensorberg-storage");
                await folder.DeleteAsync();
            }
            catch (FileNotFoundException)
            {

            }
            //            storage = new SqlStorage();
            storage = new FileStorage();
        }

        [TestCleanup]
        public void Cleanup()
        {
            (storage as SqlStorage)?.CloseConnection();
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
            action.SupressionTime = 321;
            action.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2015-04-15T12:00:00.000+0000")}
            };

            await storage.InitStorage();

            await storage.SaveDelayedAction(action, DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), "1", BeaconEventType.Enter);

            IList<DelayedActionData> delayedActions = await storage.GetDelayedActions(int.MaxValue);
            Assert.AreEqual(1, delayedActions.Count, "to many actions found");

            DelayedActionData delayAction = delayedActions[0];
            Assert.AreEqual("1", delayAction.beaconPid, "Not same beacon id");
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), delayAction.dueTime, "not same delay time");
            Assert.AreEqual(BeaconEventType.Enter, delayAction.eventTypeDetectedByDevice, "not same event type");

            Assert.AreEqual(action.Delay, delayAction.resolvedAction.Delay, "not same action delay");
            Assert.AreEqual(action.EventTypeDetectedByDevice, delayAction.resolvedAction.EventTypeDetectedByDevice, "not same action event type");
            Assert.AreEqual(action.ReportImmediately, delayAction.resolvedAction.ReportImmediately, "not same ReportImmediately");
            Assert.AreEqual(action.SendOnlyOnce, delayAction.resolvedAction.SendOnlyOnce, "not same SendOnlyOnce");
            Assert.AreEqual(action.SupressionTime, delayAction.resolvedAction.SupressionTime, "not same SendOnlyOnce");
            Assert.AreEqual(action.Timeframes.Count, delayAction.resolvedAction.Timeframes.Count, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].Start, delayAction.resolvedAction.Timeframes[0].Start, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].End, delayAction.resolvedAction.Timeframes[0].End, "not same Timeframes count");

            Assert.AreEqual(action.BeaconPids.Count, delayAction.resolvedAction.BeaconPids.Count, "not same beacon count");
            Assert.AreEqual(action.BeaconAction.Body, delayAction.resolvedAction.BeaconAction.Body, "not same beacon action body");
            Assert.AreEqual(action.BeaconAction.Subject, delayAction.resolvedAction.BeaconAction.Subject, "not same beacon action Subject");
            Assert.AreEqual(action.BeaconAction.Id, delayAction.resolvedAction.BeaconAction.Id, "not same beacon action Id");
            Assert.AreEqual(action.BeaconAction.Type, delayAction.resolvedAction.BeaconAction.Type, "not same beacon action Type");
            Assert.AreEqual(action.BeaconAction.Uuid, delayAction.resolvedAction.BeaconAction.Uuid, "not same beacon action Uuid");
            Assert.AreEqual(action.BeaconAction.Payload.ToString(), delayAction.resolvedAction.BeaconAction.Payload.ToString(), "not same beacon action Payload");


            Assert.AreEqual(action, delayAction.resolvedAction, "not same action");



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
            action2.SupressionTime = 3210;
            action2.Timeframes = new List<Timeframe>()
            {
                new Timeframe() {End = DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), Start = DateTimeOffset.Parse("2014-04-15T12:00:00.000+0000")}
            };

            await storage.SaveDelayedAction(action, DateTimeOffset.Parse("2015-05-16T12:00:00.000+0000"), "2", BeaconEventType.EnterExit);


            delayedActions = await storage.GetDelayedActions(int.MaxValue);
            Assert.AreEqual(2, delayedActions.Count, "to many actions found");

            delayAction = delayedActions.FirstOrDefault(d => d.beaconPid == "1");
            string idToDelete = delayAction.Id;
            Assert.AreEqual("1", delayAction.beaconPid, "Not same beacon id");
            Assert.AreEqual(DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), delayAction.dueTime, "not same delay time");
            Assert.AreEqual(BeaconEventType.Enter, delayAction.eventTypeDetectedByDevice, "not same event type");

            Assert.AreEqual(action.Delay, delayAction.resolvedAction.Delay, "not same action delay");
            Assert.AreEqual(action.EventTypeDetectedByDevice, delayAction.resolvedAction.EventTypeDetectedByDevice, "not same action event type");
            Assert.AreEqual(action.ReportImmediately, delayAction.resolvedAction.ReportImmediately, "not same ReportImmediately");
            Assert.AreEqual(action.SendOnlyOnce, delayAction.resolvedAction.SendOnlyOnce, "not same SendOnlyOnce");
            Assert.AreEqual(action.SupressionTime, delayAction.resolvedAction.SupressionTime, "not same SendOnlyOnce");
            Assert.AreEqual(action.Timeframes.Count, delayAction.resolvedAction.Timeframes.Count, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].Start, delayAction.resolvedAction.Timeframes[0].Start, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].End, delayAction.resolvedAction.Timeframes[0].End, "not same Timeframes count");

            Assert.AreEqual(action.BeaconPids.Count, delayAction.resolvedAction.BeaconPids.Count, "not same beacon count");
            Assert.AreEqual(action.BeaconAction.Body, delayAction.resolvedAction.BeaconAction.Body, "not same beacon action body");
            Assert.AreEqual(action.BeaconAction.Subject, delayAction.resolvedAction.BeaconAction.Subject, "not same beacon action Subject");
            Assert.AreEqual(action.BeaconAction.Id, delayAction.resolvedAction.BeaconAction.Id, "not same beacon action Id");
            Assert.AreEqual(action.BeaconAction.Type, delayAction.resolvedAction.BeaconAction.Type, "not same beacon action Type");
            Assert.AreEqual(action.BeaconAction.Uuid, delayAction.resolvedAction.BeaconAction.Uuid, "not same beacon action Uuid");
            Assert.AreEqual(action.BeaconAction.Payload.ToString(), delayAction.resolvedAction.BeaconAction.Payload.ToString(), "not same beacon action Payload");


            Assert.AreEqual(action, delayAction.resolvedAction, "not same action");



            delayAction = delayedActions.FirstOrDefault(d => d.beaconPid == "2");
            Assert.AreEqual("2", delayAction.beaconPid, "Not same beacon id");
            Assert.AreEqual(DateTimeOffset.Parse("2015-05-16T12:00:00.000+0000"), delayAction.dueTime, "not same delay time");
            Assert.AreEqual(BeaconEventType.EnterExit, delayAction.eventTypeDetectedByDevice, "not same event type");

            Assert.AreEqual(action.Delay, delayAction.resolvedAction.Delay, "not same action delay");
            Assert.AreEqual(action.EventTypeDetectedByDevice, delayAction.resolvedAction.EventTypeDetectedByDevice, "not same action event type");
            Assert.AreEqual(action.ReportImmediately, delayAction.resolvedAction.ReportImmediately, "not same ReportImmediately");
            Assert.AreEqual(action.SendOnlyOnce, delayAction.resolvedAction.SendOnlyOnce, "not same SendOnlyOnce");
            Assert.AreEqual(action.SupressionTime, delayAction.resolvedAction.SupressionTime, "not same SendOnlyOnce");
            Assert.AreEqual(action.Timeframes.Count, delayAction.resolvedAction.Timeframes.Count, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].Start, delayAction.resolvedAction.Timeframes[0].Start, "not same Timeframes count");
            Assert.AreEqual(action.Timeframes[0].End, delayAction.resolvedAction.Timeframes[0].End, "not same Timeframes count");

            Assert.AreEqual(action.BeaconPids.Count, delayAction.resolvedAction.BeaconPids.Count, "not same beacon count");
            Assert.AreEqual(action.BeaconAction.Body, delayAction.resolvedAction.BeaconAction.Body, "not same beacon action body");
            Assert.AreEqual(action.BeaconAction.Subject, delayAction.resolvedAction.BeaconAction.Subject, "not same beacon action Subject");
            Assert.AreEqual(action.BeaconAction.Id, delayAction.resolvedAction.BeaconAction.Id, "not same beacon action Id");
            Assert.AreEqual(action.BeaconAction.Type, delayAction.resolvedAction.BeaconAction.Type, "not same beacon action Type");
            Assert.AreEqual(action.BeaconAction.Uuid, delayAction.resolvedAction.BeaconAction.Uuid, "not same beacon action Uuid");
            Assert.AreEqual(action.BeaconAction.Payload.ToString(), delayAction.resolvedAction.BeaconAction.Payload.ToString(), "not same beacon action Payload");


            Assert.AreEqual(action, delayAction.resolvedAction, "not same action");


            await storage.SetDelayedActionAsExecuted(idToDelete);

            delayedActions = await storage.GetDelayedActions(int.MaxValue);
            Assert.AreEqual(1, delayedActions.Count, "to many actions found after executing action");

            Assert.AreEqual("2", delayedActions[0].beaconPid, "Not same beacon id");
        }


        [TestMethod]
        public async Task HistoryActionTest()
        {
            await storage.InitStorage();

            await storage.SaveHistoryAction("1", "1", DateTimeOffset.Parse("2016-04-16T12:00:00.000+0000"), BeaconEventType.Enter);
            await storage.SaveHistoryAction("2", "2", DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), BeaconEventType.Exit);
            await storage.SaveHistoryAction("3", "3", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit);
            await storage.SaveHistoryAction("3", "2", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit);

            IList<HistoryAction> historyActions = await storage.GetUndeliveredActions();
            Assert.AreEqual(4, historyActions.Count, "Not 4 actions");

            HistoryAction action = historyActions.First(t => t.trigger == (int) BeaconEventType.Enter);
            Assert.AreEqual("1", action.pid, "not same pid");
            Assert.AreEqual("1", action.eid, "not same eid");
            Assert.AreEqual("2016-04-16T12:00:00.000+00:00", action.dt, "not same date");


            action = historyActions.First(t => t.trigger == (int) BeaconEventType.Exit);
            Assert.AreEqual("2", action.pid, "not same pid");
            Assert.AreEqual("2", action.eid, "not same eid");
            Assert.AreEqual("2016-04-16T13:00:00.000+00:00", action.dt, "not same date");


            action = historyActions.First(t => t.trigger == (int) BeaconEventType.EnterExit && t.pid == "3");
            Assert.AreEqual("3", action.pid, "not same pid");
            Assert.AreEqual("3", action.eid, "not same eid");
            Assert.AreEqual("2016-04-16T14:00:00.000+00:00", action.dt, "not same date");


            DBHistoryAction dbHistoryAction = await storage.GetAction("2");
            Assert.AreEqual((int) BeaconEventType.Exit, dbHistoryAction.trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.pid, "not same pid");
            Assert.AreEqual("2", dbHistoryAction.eid, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T13:00:00.000+0000"), dbHistoryAction.dt, "not same date");


            IList<DBHistoryAction> dbHistoryActions = await storage.GetActions("3");
            Assert.AreEqual(2, dbHistoryActions.Count, "Not 2 actions");

            dbHistoryAction = dbHistoryActions.First(t => t.pid == "3");
            Assert.AreEqual((int) BeaconEventType.EnterExit, dbHistoryAction.trigger, "not same type");
            Assert.AreEqual("3", dbHistoryAction.pid, "not same pid");
            Assert.AreEqual("3", dbHistoryAction.eid, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), dbHistoryAction.dt, "not same date");

            dbHistoryAction = dbHistoryActions.First(t => t.pid == "2");
            Assert.AreEqual((int) BeaconEventType.EnterExit, dbHistoryAction.trigger, "not same type");
            Assert.AreEqual("2", dbHistoryAction.pid, "not same pid");
            Assert.AreEqual("3", dbHistoryAction.eid, "not same eid");
            Assert.AreEqual(DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), dbHistoryAction.dt, "not same date");

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

            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter);
            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit);
            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2016-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit);
            await storage.SaveHistoryEvents("2", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter);

            IList<HistoryEvent> historyEvents = await storage.GetUndeliveredEvents();
            HistoryEvent historyEvent = historyEvents.FirstOrDefault(h => h.dt == "2016-04-16T15:00:00.000+00:00");
            Assert.AreEqual("1", historyEvent.pid, "Wrong pid");
            Assert.AreEqual("2016-04-16T15:00:00.000+00:00", historyEvent.dt, "Wrong date");
            Assert.AreEqual((int)BeaconEventType.Exit, historyEvent.trigger, "Wrong trigger");



            await storage.SetEventsAsDelivered();

            historyEvents = await storage.GetUndeliveredEvents();
            Assert.AreEqual(0, historyEvents.Count, "not all events as delivered marked");

        }

        [TestMethod]
        public async Task EventHistoryIssueTest()
        {
            await storage.InitStorage();

            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2016-04-16T14:00:00.000+0000"), BeaconEventType.Enter);
            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2016-04-16T15:00:00.000+0000"), BeaconEventType.Exit);
            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2016-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit);
            await storage.SaveHistoryEvents("2", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter);

            IList<HistoryEvent> historyEvents = await storage.GetUndeliveredEvents();
            await storage.SaveHistoryEvents("3", DateTimeOffset.Parse("2016-04-16T17:00:00.000+0000"), BeaconEventType.Enter);
            await storage.SetEventsAsDelivered();

            historyEvents = await storage.GetUndeliveredEvents();
            Assert.AreEqual(1, historyEvents.Count, "the new event is missing");
        }

        [TestMethod]
        public async Task StorageClearTest()
        {
            await storage.InitStorage();

            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.Enter);
            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2015-04-16T15:00:00.000+0000"), BeaconEventType.Exit);
            await storage.SaveHistoryEvents("1", DateTimeOffset.Parse("2015-04-16T16:00:00.000+0000"), BeaconEventType.EnterExit);
            await storage.SaveHistoryEvents("2", DateTimeOffset.Parse("2015-04-16T17:00:00.000+0000"), BeaconEventType.Enter);

            await storage.SaveHistoryAction("1", "1", DateTimeOffset.Parse("2015-04-16T12:00:00.000+0000"), BeaconEventType.Enter);
            await storage.SaveHistoryAction("2", "2", DateTimeOffset.Parse("2015-04-16T13:00:00.000+0000"), BeaconEventType.Exit);
            await storage.SaveHistoryAction("3", "3", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit);
            await storage.SaveHistoryAction("3", "2", DateTimeOffset.Parse("2015-04-16T14:00:00.000+0000"), BeaconEventType.EnterExit);

            await storage.SetEventsAsDelivered();
            await storage.SetActionsAsDelivered();


            Assert.AreEqual(0, (await storage.GetUndeliveredEvents()).Count, "Undelivered Events found");
            Assert.AreEqual(0, (await storage.GetUndeliveredActions()).Count, "Undelivered Actions found");

            await storage.CleanDatabase();

            Assert.AreEqual(0, (await storage.GetActions("1")).Count, "remaining Actions found");

        }

        [TestMethod]
        public async Task BackgroundStorageTest()
        {
            await storage.InitStorage();

            await storage.SaveBeaconBackgroundEvent("1", BeaconEventType.Enter);
            try
            {
                await storage.SaveBeaconBackgroundEvent("1", BeaconEventType.Exit);
                Assert.Fail("no duplicate check");
            }
            catch (SQLiteException e)
            {
            }
            await storage.UpdateBackgroundEvent("1", BeaconEventType.Exit);
            await storage.SaveBeaconBackgroundEvent("2", BeaconEventType.Enter);
            await storage.SaveBeaconBackgroundEvent("3", BeaconEventType.Enter);

            IList<DBBackgroundEventsHistory> backgroundEventsHistories = await storage.GetBeaconBackgroundEventsHistory("1");
            Assert.AreEqual(1, backgroundEventsHistories.Count, "Not 1 events found");

            Assert.AreEqual(3, (await storage.GetUndeliveredEvents()).Count, "not 3 undelivered Events found");
            await storage.SetEventsAsDelivered();
            Assert.AreEqual(0, (await storage.GetUndeliveredEvents()).Count, "Undelivered Events found");



            BeaconAction beaconAction = new BeaconAction();
            beaconAction.Body = "body";
            beaconAction.Id = 1;
            beaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            beaconAction.Subject = "Subject";
            beaconAction.Type = BeaconActionType.InApp;
            beaconAction.Url = "http://sensorberg.com";
            beaconAction.Uuid = "uuid";

            await storage.SaveBeaconActionFromBackground(beaconAction);

            beaconAction = new BeaconAction();
            beaconAction.Body = "body";
            beaconAction.Id = 2;
            beaconAction.Payload = JsonObject.Parse("{\"pay\":\"load\"}");
            beaconAction.Subject = "Subject";
            beaconAction.Type = BeaconActionType.InApp;
            beaconAction.Url = "http://sensorberg.com";
            beaconAction.Uuid = "uuid1";

            await storage.SaveBeaconActionFromBackground(beaconAction);

            Assert.AreEqual(2, (await storage.GetBeaconActionsFromBackground()).Count, "no undelivered Actions found");
            Assert.AreEqual(0, (await storage.GetBeaconActionsFromBackground()).Count, "Undelivered Actions found");
        }
    }
}