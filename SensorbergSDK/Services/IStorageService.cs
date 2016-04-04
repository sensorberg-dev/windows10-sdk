// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SensorbergSDK.Data;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Services
{
    public interface IStorageService
    {
        /// <summary>
        /// Sets the Retrycount for API calls, default Value is 3
        /// </summary>
        int RetryCount { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// Validates the given API key.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        Task<ApiKeyValidationResult> ValidateApiKey(string apiKey);


        /// <summary>
        /// Loads the layout from cloud or storage.
        /// </summary>
        /// <returns></returns>
        Task<LayoutResult> RetrieveLayout();


        /// <summary>
        /// Loads the appsettings.
        /// </summary>
        /// <returns></returns>
        Task<AppSettings> RetrieveAppSettings();

        /// <summary>
        /// Sends the full history in the cloud.
        /// </summary>
        /// <returns></returns>
        Task<bool> FlushHistory();

        /// <summary>
        /// Invalidates both the current and cached layout.
        /// </summary>
        Task InvalidateLayout();

        Task<Layout> LoadLayoutFromLocalStorage();

        /// <summary>
        /// Stores a resolved and executed action to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <param name="beaconAction"></param>
        Task SaveHistoryAction(string uuid, string beaconPid, DateTime now, BeaconEventType beaconEventType);

        /// <summary>
        /// Stores a beacon event to the database.
        /// </summary>
        Task SaveHistoryEvent(string pid, DateTimeOffset timestamp, BeaconEventType eventType);

        Task<IList<DBHistoryAction>> GetActions(string uuid);
        Task<DBHistoryAction> GetAction(string uuid);
        Task CleanDatabase();
        Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds = 1000);
        Task SetDelayedActionAsExecuted(string id);

        /// <summary>
        /// Initializes the StorageService, e.g. creates database.
        /// </summary>
        /// <returns></returns>
        Task InitStorage();

        Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice);
        Task<BackgroundEvent> GetLastEventStateForBeacon(string pid);
        Task SaveBeaconEventState(string pid, BeaconEventType enter);
        Task SaveHistoryAction(BeaconAction beaconAction);
    }
}