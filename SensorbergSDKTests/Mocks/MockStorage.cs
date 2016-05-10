// Created by Kay Czarnotta on 16.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockStorage:IStorage
    {
        public IList<HistoryAction> UndeliveredActions { get; set; } = new List<HistoryAction>();
        public IList<HistoryEvent> UndeliveredEvents { get; set; }= new List<HistoryEvent>();
        public Dictionary<string, BackgroundEvent> LastEventState { get; set; } = new Dictionary<string, BackgroundEvent>();
        public List<DelayedActionData> DelayedActions { get; set; } = new List<DelayedActionData>();

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

        public async Task<bool> SaveHistoryAction(HistoryAction action)
        {
            UndeliveredActions.Add(action);
            return true;
        }

        
        public async Task<bool> SaveHistoryEvents(HistoryEvent he)
        {
            UndeliveredEvents.Add(he);
            return true;
        }

        public async Task<IList<HistoryAction>> GetActions(string uuid)
        {
            return UndeliveredActions.Where(a => a.EventId == uuid).ToList();
        }

        public async Task<HistoryAction> GetAction(string uuid)
        {
            return UndeliveredActions.FirstOrDefault(a => a.EventId == uuid);
        }

        public Task CleanDatabase()
        {
            throw new NotImplementedException();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            DateTimeOffset maxDelayfromNow = DateTimeOffset.Now.AddSeconds(maxDelayFromNowInSeconds);
            return DelayedActions.Where(da => da.DueTime < maxDelayfromNow).ToList();
        }

        public async Task SetDelayedActionAsExecuted(string id)
        {
            DelayedActions.Remove(DelayedActions.FirstOrDefault(d => d.Id == id));
        }

        public async Task<bool> SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            DelayedActions.Add(new DelayedActionData() {BeaconPid = beaconPid,DueTime = dueTime, EventTypeDetectedByDevice =  eventTypeDetectedByDevice, Id = Guid.NewGuid().ToString(), ResolvedAction = action});
            return true;
        }

        public Task SaveHistoryAction(BeaconAction beaconAction)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SaveBeaconEventState(string pid, BeaconEventType enter)
        {
            LastEventState[pid] = new BackgroundEvent() {BeaconId = pid, EventTime = DateTimeOffset.Now, LastEvent = enter};
            return true;
        }

        public async Task<BackgroundEvent> GetLastEventStateForBeacon(string pid)
        {
            return LastEventState.ContainsKey(pid) ? LastEventState[pid] : null;
        }

        public Task SaveActionForForeground(BeaconAction beaconAction)
        {
            throw new NotImplementedException();
        }

        public Task<List<HistoryAction>> GetActionsForForeground(bool doNotDelete = false)
        {
            throw new NotImplementedException();
        }
    }
}