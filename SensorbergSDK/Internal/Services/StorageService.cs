// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using MetroLog;
using Newtonsoft.Json;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Internal.Utils;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    public class StorageService : IStorageService
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<StorageService>();
        private const string KeyLayoutHeaders = "layout_headers";
        private const string KeyLayoutContent = "layout_content.cache"; // Cache file
        private const string KeyLayoutRetrievedTime = "layout_retrieved_time";
        private const int MaxRetries = 2;

        public int RetryCount { get; set; } = 3;

        public IStorage Storage { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public StorageService(bool createdOnForeground = true)
        {
            Storage = new FileStorage() {Background = !createdOnForeground};
        }

        public async Task InitStorage()
        {
            await Storage.InitStorage();
        }


        /// <summary>
        /// Checks whether the given API key is valid or not.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>The validation result.</returns>
        public async Task<ApiKeyValidationResult> ValidateApiKey(string apiKey)
        {
            var responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.RetrieveLayoutResponse(apiKey));

            if (responseMessage != null && responseMessage.IsSuccess)
            {
                return string.IsNullOrEmpty(responseMessage.Content) || responseMessage.Content.Length < Constants.MinimumLayoutContentLength
                    ? ApiKeyValidationResult.Invalid
                    : ApiKeyValidationResult.Valid;
            }
            return responseMessage?.NetworResult == NetworkResult.NetworkError ? ApiKeyValidationResult.NetworkError : ApiKeyValidationResult.UnknownError;
        }


        private async Task WaitBackoff(int currentRetries)
        {
            await Task.Delay((int) Math.Pow(2, currentRetries + 1)*100);
        }

        public async Task<LayoutResult> RetrieveLayout()
        {
            ResponseMessage responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.RetrieveLayoutResponse());
            if (responseMessage != null && responseMessage.IsSuccess)
            {
                Layout layout = null;
                string headersAsString = Helper.StripLineBreaksAndExcessWhitespaces(responseMessage.Header);
                string contentAsString = Helper.StripLineBreaksAndExcessWhitespaces(responseMessage.Content);
                contentAsString = Helper.EnsureEncodingIsUtf8(contentAsString);
                DateTimeOffset layoutRetrievedTime = DateTimeOffset.Now;

                if (contentAsString.Length > Constants.MinimumLayoutContentLength)
                {
                    try
                    {
                        layout = JsonConvert.DeserializeObject<Layout>(contentAsString);
                        layout?.FromJson(headersAsString, layoutRetrievedTime);
                        Logger.Debug("LayoutManager: new Layout received: Beacons: " + layout?.AccountBeaconId1S.Count + " Actions :" + layout?.ResolvedActions.Count);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("LayoutManager.RetrieveLayout(): Failed to parse layout: " + ex);
                        layout = null;
                    }
                }

                if (layout != null)
                {
                    // Store the parsed layout
                    await SaveLayoutToLocalStorage(headersAsString, contentAsString, layoutRetrievedTime);
                    return new LayoutResult() {Layout = layout, Result = NetworkResult.Success};
                }
            }
            else
            {
                Layout layout = await LoadLayoutFromLocalStorage();
                return new LayoutResult() {Result = layout != null ? NetworkResult.Success : NetworkResult.NetworkError, Layout = layout};
            }

            return new LayoutResult() {Result = NetworkResult.UnknownError};
        }

        public async Task<bool> FlushHistory()
        {
            try
            {
                History history = new History();
                history.Actions = await Storage.GetUndeliveredActions();
                history.Events = await Storage.GetUndeliveredEvents();

                if ((history.Events != null && history.Events.Count > 0) || (history.Actions != null && history.Actions.Count > 0))
                {
                    var responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.SendHistory(history));

                    if (responseMessage.IsSuccess)
                    {
                        if (history.Events != null && history.Events.Count > 0)
                        {
                            await Storage.SetEventsAsDelivered(history.Events);
                        }

                        if (history.Actions != null && history.Actions.Count > 0)
                        {
                            await Storage.SetActionsAsDelivered(history.Actions);
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while sending history: " + ex.Message, ex);
            }
            return false;
        }



        /// <summary>
        /// Saves the strings that make up a layout.
        /// </summary>
        private async Task SaveLayoutToLocalStorage(string headers, string content, DateTimeOffset layoutRetrievedTime)
        {
            if (await StoreData(KeyLayoutContent, content))
            {
                ApplicationData.Current.LocalSettings.Values[KeyLayoutHeaders] = headers;
                ApplicationData.Current.LocalSettings.Values[KeyLayoutRetrievedTime] = layoutRetrievedTime;
            }
        }

        /// <summary>
        /// Saves the given data to the specified file.
        /// </summary>
        /// <param name="fileName">The file name of the storage file.</param>
        /// <param name="data">The data to save.</param>
        /// <returns>True, if successful. False otherwise.</returns>
        private async Task<bool> StoreData(string fileName, string data)
        {
            bool success = false;

            try
            {
                var storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.AppendTextAsync(storageFile, data);
                success = true;
            }
            catch (Exception ex)
            {
                Logger.Error("LayoutManager.StoreData(): Failed to save content: " + ex, ex);
            }

            return success;
        }

        /// <summary>
        /// Tries to load the layout from the local storage.
        /// </summary>
        /// <returns>A layout instance, if successful. Null, if not found.</returns>
        public async Task<Layout> LoadLayoutFromLocalStorage()
        {
            Layout layout = null;
            string headers = string.Empty;
            string content = string.Empty;
            DateTimeOffset layoutRetrievedTime = DateTimeOffset.Now;

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyLayoutHeaders))
            {
                headers = ApplicationData.Current.LocalSettings.Values[KeyLayoutHeaders].ToString();
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyLayoutRetrievedTime))
            {
                layoutRetrievedTime = (DateTimeOffset) ApplicationData.Current.LocalSettings.Values[KeyLayoutRetrievedTime];
            }

            try
            {
                var contentFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(KeyLayoutContent);

                if (contentFile != null)
                {
                    content = await FileIO.ReadTextAsync(contentFile as IStorageFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LayoutManager.LoadLayoutFromLocalStorage(): Failed to load content: " + ex, ex);
            }

            if (!string.IsNullOrEmpty(content))
            {
                content = Helper.EnsureEncodingIsUtf8(content);
                try
                {
                    layout = JsonConvert.DeserializeObject<Layout>(content);
                    layout?.FromJson(headers, layoutRetrievedTime);
                }
                catch (Exception ex)
                {
                    Logger.Error("LayoutManager.LoadLayoutFromLocalStorage(): Failed to parse layout: " + ex, ex);
                }
            }

            if (layout == null)
            {
                // Failed to parse the layout => invalidate it
                await InvalidateLayout();
            }

            return layout;
        }

        #region storage methods

        public async Task<bool> SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType, string location)
        {
            return await SaveHistoryActionRetry(uuid, beaconPid, now, beaconEventType, location, MaxRetries);
        }

        private async Task<bool> SaveHistoryActionRetry(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType, string location, int retry)
        {
            if (retry < 0)
            {
                return false;
            }
            try
            {
                HistoryAction action = FileStorageHelper.ToHistoryAction(uuid, beaconPid, now, beaconEventType, location);
                if (await Storage.SaveHistoryAction(action))
                {
                    return true;
                }
                return await SaveHistoryActionRetry(uuid, beaconPid, now, beaconEventType, location, --retry);
            }
            catch (UnauthorizedAccessException)
            {
                return await SaveHistoryActionRetry(uuid, beaconPid, now, beaconEventType, location, --retry);
            }
            catch (FileNotFoundException)
            {
                return await SaveHistoryActionRetry(uuid, beaconPid, now, beaconEventType, location, --retry);
            }
        }

        public async Task<bool> SaveHistoryEvent(string pid, DateTimeOffset timestamp, BeaconEventType eventType, string location)
        {
            return await SaveHistoryEventRetry(pid, timestamp, eventType, location, MaxRetries);
        }

        private async Task<bool> SaveHistoryEventRetry(string pid, DateTimeOffset timestamp, BeaconEventType eventType, string location, int retry)
        {
            if (retry < 0)
            {
                return false;
            }
            try
            {
                if (await Storage.SaveHistoryEvents(FileStorageHelper.ToHistoryEvent(pid, timestamp, eventType, location)))
                {
                    return true;
                }
                return await SaveHistoryEventRetry(pid, timestamp, eventType, location, --retry);
            }
            catch (UnauthorizedAccessException)
            {
                return await SaveHistoryEventRetry(pid, timestamp, eventType, location, --retry);
            }
            catch (FileNotFoundException)
            {
                return await SaveHistoryEventRetry(pid, timestamp, eventType, location, --retry);
            }
        }

        public async Task CleanupDatabase()
        {
            await Storage.CleanupDatabase();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions()
        {
            return await Storage.GetDelayedActions();
        }

        public async Task SetDelayedActionAsExecuted(string id)
        {
            await Storage.SetDelayedActionAsExecuted(id);
        }

        public async Task<bool> SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventType, string location)
        {
            return await SaveDelayedActionsRetry(action, dueTime, beaconPid, eventType, location, MaxRetries);
        }

        private async Task<bool> SaveDelayedActionsRetry(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice, string location, int retry)
        {
            if (retry < 0)
            {
                return false;
            }
            try
            {
                if (await Storage.SaveDelayedAction(action, dueTime, beaconPid, eventTypeDetectedByDevice, location))
                {
                    return true;
                }
                return await SaveDelayedActionsRetry(action, dueTime, beaconPid, eventTypeDetectedByDevice, location, --retry);
            }
            catch (UnauthorizedAccessException)
            {
                return await SaveDelayedActionsRetry(action, dueTime, beaconPid, eventTypeDetectedByDevice, location, --retry);
            }
            catch (FileNotFoundException)
            {
                return await SaveDelayedActionsRetry(action, dueTime, beaconPid, eventTypeDetectedByDevice, location, --retry);
            }
        }

        public async Task<BackgroundEvent> GetLastEventStateForBeacon(string pid)
        {
            return await Storage.GetLastEventStateForBeacon(pid);
        }

        public async Task<bool> SaveBeaconEventState(string pid, BeaconEventType enter)
        {
            return await SaveBeaconEventStateRetry(pid, enter,MaxRetries);
        }

        private async Task<bool> SaveBeaconEventStateRetry(string pid, BeaconEventType enter, int retry)
        {
            if (retry < 0)
            {
                return false;
            }
            try
            {
                if (await Storage.SaveBeaconEventState(pid, enter))
                {
                    return true;
                }
                return await SaveBeaconEventStateRetry(pid, enter, --retry);
            }
            catch (UnauthorizedAccessException)
            {
                return await SaveBeaconEventStateRetry(pid, enter, --retry);
            }
            catch (FileNotFoundException)
            {
                return await SaveBeaconEventStateRetry(pid, enter, --retry);
            }
        }

        public async Task<List<BeaconAction>> GetActionsForForeground(bool doNotDelete = false)
        {
            List<BeaconAction> beaconActions = new List<BeaconAction>();
            List<HistoryAction> historyActions = await Storage.GetActionsForForeground(doNotDelete);
            foreach (HistoryAction historyAction in historyActions)
            {
                ResolvedAction action = ServiceManager.LayoutManager.GetAction(historyAction.EventId);
                beaconActions.Add(action.BeaconAction);
            }

            return beaconActions;
        }

        #endregion

        /// <summary>
        /// Invalidates both the current and cached layout.
        /// </summary>
        public async Task InvalidateLayout()
        {
            ApplicationData.Current.LocalSettings.Values[KeyLayoutHeaders] = null;
            ApplicationData.Current.LocalSettings.Values[KeyLayoutRetrievedTime] = null;

            try
            {
                var contentFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(KeyLayoutContent);

                if (contentFile != null)
                {
                    await contentFile.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error invalidating layout", ex);
            }
        }

        private async Task<ResponseMessage> ExecuteCall(Func<Task<ResponseMessage>> action)
        {

            bool networkError;
            int retries = 0;
            do
            {
                try
                {
                    ResponseMessage responseMessage = await action();
                    responseMessage.NetworResult = NetworkResult.Success;
                    return responseMessage;
                }
                catch (TimeoutException e)
                {
                    networkError = true;
                    Logger.Error("timeout error while executing call: " + e.Message, e);
                    await WaitBackoff(retries);
                }
                catch (IOException e)
                {
                    networkError = true;
                    Logger.Error("Error while executing call: " + e.Message, e);
                    await WaitBackoff(retries);
                }
                catch (HttpRequestException e)
                {
                    networkError = true;
                    Logger.Error("Error while executing call: " + e.Message, e);
                    await WaitBackoff(retries);
                }
                catch (Exception e)
                {
                    networkError = false;
                    Logger.Error("Error while executing call: " + e.Message, e);
                    await WaitBackoff(retries);
                }
                finally
                {
                    retries++;
                }
            } while (retries < RetryCount);

            return new ResponseMessage() {NetworResult = networkError ? NetworkResult.NetworkError : NetworkResult.UnknownError};
        }
    }
}