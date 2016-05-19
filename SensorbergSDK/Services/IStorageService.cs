// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction to handle all storage related actions.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Sets the Retrycount for API calls, default Value is 3.
        /// </summary>
        int RetryCount { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// Initializes the StorageService, e.g. creates database.
        /// </summary>
        Task InitStorage();

        /// <summary>
        /// Validates the given API key.
        /// </summary>
        /// <param name="apiKey">Api Key to validate.</param>
        Task<ApiKeyValidationResult> ValidateApiKey(string apiKey);


        /// <summary>
        /// Loads the layout from cloud or storage.
        /// </summary>
        /// <returns></returns>
        Task<LayoutResult> RetrieveLayout();

        /// <summary>
        /// Sends the full history in the cloud.
        /// </summary>
        Task<bool> FlushHistory();

        /// <summary>
        /// Invalidates both the current and cached layout.
        /// </summary>
        Task InvalidateLayout();

        /// <summary>
        /// Loads the local stored Layout.
        /// </summary>
        Task<Layout> LoadLayoutFromLocalStorage();

        /// <summary>
        /// Stores a resolved and executed action to the database.
        /// </summary>
        Task<bool> SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType);

        /// <summary>
        /// Stores a beacon event to the database.
        /// </summary>
        Task<bool> SaveHistoryEvent(string pid, DateTimeOffset timestamp, BeaconEventType eventType);

        /// <summary>
        /// Get all triggered actions by the given action uuid.
        /// </summary>
        /// <param name="uuid">uuid from the action to search.</param>
        /// <param name="forceUpdate">Force to ignore any cache.</param>
        /// <returns>List of found actions.</returns>
        Task<IList<HistoryAction>> GetActions(string uuid, bool forceUpdate = false);

        /// <summary>
        /// Get the first triggered action by the given action uuid.
        /// </summary>
        /// <param name="uuid">uuid from the action to search.</param>
        /// <param name="forceUpdate">Force to ignore any cache.</param>
        /// <returns>The first found action or null.</returns>
        Task<HistoryAction> GetAction(string uuid, bool forceUpdate = false);

        /// <summary>
        /// Cleans old entries from the database.
        /// </summary>
        Task CleanupDatabase();

        /// <summary>
        /// Returns all deleayed Actions.
        /// </summary>
        Task<IList<DelayedActionData>> GetDelayedActions();

        /// <summary>
        /// Mark delayed action as executed.
        /// </summary>
        /// <param name="id">UUID of delayed action.</param>
        Task SetDelayedActionAsExecuted(string id);

        /// <summary>
        /// Store a new delayed action.
        /// </summary>
        /// <param name="action">Action that should be delayed.</param>
        /// <param name="dueTime">Time of execution.</param>
        /// <param name="beaconPid">Beacon ID that the action triggered.</param>
        /// <param name="eventType">Type of event.</param>
        Task<bool> SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventType);

        /// <summary>
        /// Gets the last state of the beacon.
        /// </summary>
        /// <param name="pid">ID of the beacon.</param>
        Task<BackgroundEvent> GetLastEventStateForBeacon(string pid);

        /// <summary>
        /// Save the state of a beacon. This can be used by the background task to have the last state of a beacon. The method stores only the last state of a beacon.
        /// </summary>
        /// <param name="pid">ID of the beacon.</param>
        /// <param name="enter">Event type for that event.</param>
        Task<bool> SaveBeaconEventState(string pid, BeaconEventType enter);

        /// <summary>
        /// Returns all actions that are triggered by the background task.
        /// </summary>
        /// <param name="doNotDelete">Boolean to delete or not delete the returned actions</param>
        Task<List<BeaconAction>> GetActionsForForeground(bool doNotDelete = false);
    }
}