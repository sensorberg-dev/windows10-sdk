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

        public async Task SaveHistoryAction(HistoryAction action)
        {
            UndeliveredActions.Add(action);
        }

        
        public async Task SaveHistoryEvents(HistoryEvent he)
        {
            UndeliveredEvents.Add(he);
        }

        public async Task<IList<HistoryAction>> GetActions(string uuid)
        {
            return UndeliveredActions.Where(a => a.eid == uuid).ToList();
        }

        public async Task<HistoryAction> GetAction(string uuid)
        {
            return UndeliveredActions.FirstOrDefault(a => a.eid == uuid);
        }

        public Task CleanDatabase()
        {
            throw new NotImplementedException();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            DateTimeOffset maxDelayfromNow = DateTimeOffset.Now.AddSeconds(maxDelayFromNowInSeconds);
            return DelayedActions.Where(da => da.dueTime < maxDelayfromNow).ToList();
        }

        public async Task SetDelayedActionAsExecuted(string id)
        {
            DelayedActions.Remove(DelayedActions.FirstOrDefault(d => d.Id == id));
        }

        public async Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            DelayedActions.Add(new DelayedActionData() {beaconPid = beaconPid,dueTime = dueTime, eventTypeDetectedByDevice =  eventTypeDetectedByDevice, Id = Guid.NewGuid().ToString(), resolvedAction = action});
        }

        public Task SaveHistoryAction(BeaconAction beaconAction)
        {
            throw new NotImplementedException();
        }

        public async Task SaveBeaconEventState(string pid, BeaconEventType enter)
        {
            LastEventState[pid] = new BackgroundEvent() {BeaconID = pid, EventTime = DateTimeOffset.Now, LastEvent = enter};
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