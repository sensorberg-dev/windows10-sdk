﻿// Created by Kay Czarnotta on 23.03.2016
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
using SensorbergSDK.Services;

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
            storage = new SqlStorage();
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
            //            Assert.AreEqual(action.BeaconAction.Payload, delayAction.resolvedAction.BeaconAction.Payload, "not same beacon action Payload");


            //Assert.AreEqual(action, delayAction.resolvedAction, "not same action");



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
            action2.BeaconPids = new List<string>() { "1", "2", "3", "4" ,"5"};
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
            //            Assert.AreEqual(action.BeaconAction.Payload, delayAction.resolvedAction.BeaconAction.Payload, "not same beacon action Payload");


            //Assert.AreEqual(action, delayAction.resolvedAction, "not same action");



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
            //            Assert.AreEqual(action.BeaconAction.Payload, delayAction.resolvedAction.BeaconAction.Payload, "not same beacon action Payload");


            //Assert.AreEqual(action, delayAction.resolvedAction, "not same action");

        }


    }
}