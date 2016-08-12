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
    /// <summary>
    /// Abstraction for the storage of all SDK related data.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Initializes the storage, e.g. create folders or database. This depends on the implementation, but needs to be called during startup of the SDK.
        /// </summary>
        /// <returns></returns>
        Task InitStorage();

        /// <summary>
        /// Gets all not delivered HistoryEvents.
        /// </summary>
        Task<IList<HistoryEvent>> GetUndeliveredEvents();

        /// <summary>
        /// Get all not delivered HistoryActions.
        /// </summary>
        Task<IList<HistoryAction>> GetUndeliveredActions();

        /// <summary>
        /// Marks all delivered HistoryEvents as delivered.
        /// </summary>
        /// <returns></returns>
        Task SetEventsAsDelivered(IList<HistoryEvent> sendEvents);

        /// <summary>
        /// Marks all delivered HistoryActions as delivered.
        /// </summary>
        Task SetActionsAsDelivered(IList<HistoryAction> sendActions);

        /// <summary>
        /// Store the given HistoryAction.
        /// </summary>
        /// <param name="action">Action to store.</param>
        Task<bool> SaveHistoryAction(HistoryAction action);

        /// <summary>
        /// Store the given HistoryEvent.
        /// </summary>
        /// <param name="he">Event to store.</param>
        Task<bool> SaveHistoryEvents(HistoryEvent he);

        /// <summary>
        /// Cleans old entries from the database.
        /// </summary>
        Task CleanupDatabase();

        /// <summary>
        /// Removes all from database.
        /// </summary>
        /// <returns></returns>
        Task CleanDatabase();

        /// <summary>
        /// Returns delayed actions which should be executed now.
        /// </summary>
        /// <returns></returns>
        Task<IList<DelayedActionData>> GetDelayedActions();

        /// <summary>
        /// Mark the action by the given uuid as executed.
        /// </summary>
        /// <param name="uuid">UUID of the action.</param>
        Task SetDelayedActionAsExecuted(string uuid);

        /// <summary>
        /// Store a new delayed action.
        /// </summary>
        /// <param name="action">Action that should be delayed.</param>
        /// <param name="dueTime">Time of execution.</param>
        /// <param name="beaconPid">Beacon ID that the action triggered.</param>
        /// <param name="eventType">Type of event.</param>
        /// <param name="location">Geolocation of event.</param>
        Task<bool> SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventType, string location);

        /// <summary>
        /// Save the state of a beacon. This can be used by the background task to have the last state of a beacon. The method stores only the last state of a beacon.
        /// </summary>
        /// <param name="pid">ID of the beacon.</param>
        /// <param name="enter">Event type for that event.</param>
        Task<bool> SaveBeaconEventState(string pid, BeaconEventType enter);

        /// <summary>
        /// Gets the last state of the beacon.
        /// </summary>
        /// <param name="pid">ID of the beacon.</param>
        Task<BackgroundEvent> GetLastEventStateForBeacon(string pid);

        /// <summary>
        /// Returns all actions that are triggered by the background task.
        /// </summary>
        /// <param name="doNotDelete">Boolean to delete or not delete the returned actions.</param>
        Task<List<HistoryAction>> GetActionsForForeground(bool doNotDelete = false);

    }
}