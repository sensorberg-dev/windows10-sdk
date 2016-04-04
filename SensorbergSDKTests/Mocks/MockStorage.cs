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
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockStorage:IStorage
    {
        public IList<HistoryAction> UndeliveredActions { get; set; }
        public IList<HistoryEvent> UndeliveredEvents { get; set; }
        public async Task InitStorage()
        {
        }
        public async Task<IList<HistoryEvent>> GetUndeliveredEvents()
        {
            return UndeliveredEvents;
        }

        public async Task<IList<HistoryAction>> GetUndeliveredActions()
        {
            return UndeliveredActions;
        }

        public async Task SetEventsAsDelivered()
        {
            UndeliveredEvents?.Clear();
        }

        public async Task SetActionsAsDelivered()
        {
            UndeliveredActions?.Clear();
        }

        public Task SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType)
        {
            throw new NotImplementedException();
        }

        public Task SaveHistoryEvents(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
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

        public Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            throw new NotImplementedException();
        }

        public Task SetDelayedActionAsExecuted(string id)
        {
            throw new NotImplementedException();
        }

        public Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            throw new NotImplementedException();
        }

        public Task SaveHistoryAction(BeaconAction beaconAction)
        {
            throw new NotImplementedException();
        }

        public Task SaveBeaconEventState(string pid, BeaconEventType enter)
        {
            throw new NotImplementedException();
        }

        public Task<BackgroundEvent> GetLastEventStateForBeacon(string pid)
        {
            throw new NotImplementedException();
        }
    }
}