// Created by Kay Czarnotta on 16.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockStorage:IStorage
    {
        public async Task InitStorage()
        {
        }

        public Task<IList<HistoryEvent>> GetUndeliveredEvents()
        {
            throw new NotImplementedException();
        }

        public Task<IList<HistoryAction>> GetUndeliveredActions()
        {
            throw new NotImplementedException();
        }

        public Task SetEventsAsDelivered()
        {
            throw new NotImplementedException();
        }

        public Task SetActionsAsDelivered()
        {
            throw new NotImplementedException();
        }

        public Task SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, int beaconEventType)
        {
            throw new NotImplementedException();
        }

        public Task SaveHistoryEvents(string pid, DateTimeOffset timestamp, int eventType)
        {
            throw new NotImplementedException();
        }

        public Task<IList<DBHistoryAction>> GetActions(string uuid)
        {
            throw new NotImplementedException();
        }

        public Task<DBHistoryAction> GetAction(string uuid)
        {
            throw new NotImplementedException();
        }

        public Task CleanDatabase()
        {
            throw new NotImplementedException();
        }

        public Task<IList<BeaconAction>> GetBeaconActionsFromBackground()
        {
            throw new NotImplementedException();
        }

        public Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            throw new NotImplementedException();
        }

        public Task SetDelayedActionAsExecuted(int id)
        {
            throw new NotImplementedException();
        }

        public Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            throw new NotImplementedException();
        }

        public Task<IList<DBBackgroundEventsHistory>> GetBeaconBackgroundEventsHistory(string pid)
        {
            throw new NotImplementedException();
        }

        public Task SaveBeaconBackgroundEvent(string pid, BeaconEventType enter)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBackgroundEvent(string pid)
        {
            throw new NotImplementedException();
        }

        public Task SaveBeaconActionFromBackground(BeaconAction beaconAction)
        {
            throw new NotImplementedException();
        }
    }
}