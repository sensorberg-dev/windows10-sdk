// Created by Kay Czarnotta on 16.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

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
        Task<bool> SaveHistoryAction(HistoryAction action);
        Task<bool> SaveHistoryEvents(HistoryEvent he);
        Task<IList<HistoryAction>> GetActions(string uuid);
        Task<HistoryAction> GetAction(string uuid);

        /// <summary>
        /// Cleans old entries from the database.
        /// </summary>
        Task CleanDatabase();

        /// <summary>
        /// Returns delayed actions which should be executed now or maxDelayFromNowInSeconds
        /// seconds in the future.
        /// </summary>
        /// <param name="maxDelayFromNowInSeconds"></param>
        /// <returns></returns>
        Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds);
        Task SetDelayedActionAsExecuted(string id);
        Task<bool> SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice);
        Task<bool> SaveBeaconEventState(string pid, BeaconEventType enter);
        Task<BackgroundEvent> GetLastEventStateForBeacon(string pid);
        Task<List<HistoryAction>> GetActionsForForeground(bool doNotDelete = false);
    }
}