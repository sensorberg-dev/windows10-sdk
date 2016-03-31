// Created by Kay Czarnotta on 16.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SensorbergSDK.Internal;

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
        Task<IList<DBHistoryAction>> GetActions(string uuid);
        Task<DBHistoryAction> GetAction(string uuid);

        /// <summary>
        /// Cleans old entries from the database
        /// </summary>
        /// <returns></returns>
        Task CleanDatabase();

        /// <summary>
        /// Returns the beacon actions, which have been resolved in the background, but not handled
        /// yet by the user. The returned actions are deleted from the database.
        /// </summary>
        /// <returns>The pending beacon actions resolved by the background task.</returns>
        Task<IList<BeaconAction>> GetBeaconActionsFromBackground();

        /// <summary>
        /// Returns delayed actions which should be executed now or maxDelayFromNowInSeconds
        /// seconds in the future.
        /// </summary>
        /// <param name="maxDelayFromNowInSeconds"></param>
        /// <returns></returns>
        Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds);
        Task SetDelayedActionAsExecuted(string id);
        Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice);
        Task<IList<DBBackgroundEventsHistory>> GetBeaconBackgroundEventsHistory(string pid);
        Task SaveBeaconBackgroundEvent(string pid, BeaconEventType enter);
        Task DeleteBackgroundEvent(string pid);
        Task SaveBeaconActionFromBackground(BeaconAction beaconAction);
        Task UpdateBeaconBackgroundEvent(string pidIn, BeaconEventType triggerIn);
        Task UpdateBackgroundEvent(string pidIn, BeaconEventType eventType);
    }
}