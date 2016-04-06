// Created by Kay Czarnotta on 16.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Services
{
    public interface IStorage
    {
        /// <summary>
        /// Initializes the storage, e.g. create or folders.
        /// </summary>
        /// <returns></returns>
        Task InitStorage();

        Task<IList<HistoryEvent>> GetUndeliveredEvents();
        Task<IList<HistoryAction>> GetUndeliveredActions();
        Task SetEventsAsDelivered();
        Task SetActionsAsDelivered();
        Task SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType);
        Task SaveHistoryEvents(string pid, DateTimeOffset timestamp, BeaconEventType eventType);
        Task<IList<HistoryAction>> GetActions(string uuid);
        Task<HistoryAction> GetAction(string uuid);

        /// <summary>
        /// Cleans old entries from the database
        /// </summary>
        /// <returns></returns>
        Task CleanDatabase();

        /// <summary>
        /// Returns delayed actions which should be executed now or maxDelayFromNowInSeconds
        /// seconds in the future.
        /// </summary>
        /// <param name="maxDelayFromNowInSeconds"></param>
        /// <returns></returns>
        Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds);
        Task SetDelayedActionAsExecuted(string id);
        Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice);
        Task SaveBeaconEventState(string pid, BeaconEventType enter);
        Task<BackgroundEvent> GetLastEventStateForBeacon(string pid);
        Task SaveActionForForeground(BeaconAction beaconAction);
    }
}