// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using SensorbergSDK.Data;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Utils;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    public class StorageService : IStorageService
    {
        private const string KeyLayoutHeaders = "layout_headers";
        private const string KeyLayoutContent = "layout_content.cache"; // Cache file
        private const string KeyLayoutRetrievedTime = "layout_retrieved_time";

        public int RetryCount { get; set; } = 3;

        protected IStorage Storage { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public StorageService(bool createdOnForeground = true)
        {
            //Ensures that database tables are created
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
            ResponseMessage responseMessage = null;

            responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.RetrieveLayoutResponse(SDKData.Instance, apiKey));

            if (responseMessage != null && responseMessage.IsSuccess)
            {

                return string.IsNullOrEmpty(responseMessage.Content) || responseMessage.Content.Length < Constants.MinimumLayoutContentLength
                    ? ApiKeyValidationResult.Invalid
                    : ApiKeyValidationResult.Valid;
            }
            return responseMessage.NetworResult == NetworkResult.NetworkError ? ApiKeyValidationResult.NetworkError : ApiKeyValidationResult.UnknownError;
        }


        private async Task WaitBackoff(int currentRetries)
        {
            await Task.Delay((int) Math.Pow(2, currentRetries + 1)*100);
        }

        public async Task<LayoutResult> RetrieveLayout()
        {
            ResponseMessage responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.RetrieveLayoutResponse(SDKData.Instance));
            if (responseMessage != null && responseMessage.IsSuccess)
            {
                Layout layout = null;
                string headersAsString = Helper.StripLineBreaksAndExcessWhitespaces(responseMessage.Header);
                string contentAsString = Helper.StripLineBreaksAndExcessWhitespaces(responseMessage.Content);
                contentAsString = Helper.EnsureEncodingIsUTF8(contentAsString);
                DateTimeOffset layoutRetrievedTime = DateTimeOffset.Now;

                if (contentAsString.Length > Constants.MinimumLayoutContentLength)
                {
                    try
                    {
                        JsonValue content = JsonValue.Parse(contentAsString);
                        layout = Layout.FromJson(headersAsString, content.GetObject(), layoutRetrievedTime);
                        Debug.WriteLine("LayoutManager: new Layout received: Beacons: " + layout.AccountBeaconId1s.Count + " Actions :" + layout.ResolvedActions.Count);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("LayoutManager.RetrieveLayout(): Failed to parse layout: " + ex);
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
               Layout layout= await LoadLayoutFromLocalStorage();
                return new LayoutResult() {Result = layout != null ? NetworkResult.Success : NetworkResult.NetworkError, Layout = layout};
            }

            return new LayoutResult() {Result = responseMessage != null ? responseMessage.NetworResult : NetworkResult.UnknownError};
        }

        public Task<AppSettings> RetrieveAppSettings()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> FlushHistory()
        {
            try
            {
                History history = new History();
                history.actions = await Storage.GetUndeliveredActions();
                history.events = await Storage.GetUndeliveredEvents();

                if ((history.events != null && history.events.Count > 0) || (history.actions != null && history.actions.Count > 0))
                {
                    var responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.SendHistory(history));

                    if (responseMessage.IsSuccess)
                    {
                        if ((history.events != null && history.events.Count > 0))
                        {
                            await Storage.SetEventsAsDelivered();
                        }

                        if (history.actions != null && history.actions.Count > 0)
                        {
                            await Storage.SetActionsAsDelivered();
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while sending history: " + ex.Message);
            }
            return false;
        }



        /// <summary>
        /// Saves the strings that make up a layout.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <param name="layoutRetrievedTime"></param>
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
                Debug.WriteLine("LayoutManager.StoreData(): Failed to save content: " + ex);
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
                layoutRetrievedTime = (DateTimeOffset)ApplicationData.Current.LocalSettings.Values[KeyLayoutRetrievedTime];
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
                Debug.WriteLine("LayoutManager.LoadLayoutFromLocalStorage(): Failed to load content: " + ex);
            }

            if (!string.IsNullOrEmpty(content))
            {
                content = Helper.EnsureEncodingIsUTF8(content);
                try
                {
                    JsonValue contentAsJsonValue = JsonValue.Parse(content);
                    layout = Layout.FromJson(headers, contentAsJsonValue.GetObject(), layoutRetrievedTime);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("LayoutManager.LoadLayoutFromLocalStorage(): Failed to parse layout: " + ex);
                }
            }

            if (layout == null)
            {
                // Failed to parse the layout => invalidate it
                await InvalidateLayout();
            }

            return layout;
        }

#region pure storage methods (sqlstorage class delegates)
        public async Task SaveHistoryAction(string uuid, string beaconPid, DateTime now, BeaconEventType beaconEventType)
        {
            await Storage.SaveHistoryAction(uuid, beaconPid, now, beaconEventType);
        }

        public async Task SaveHistoryEvent(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
        {
            await Storage.SaveHistoryEvents(pid, timestamp, eventType);
        }

        public async Task<IList<DBHistoryAction>> GetActions(string uuid)
        {
           return await Storage.GetActions(uuid);
        }

        public async Task<DBHistoryAction> GetAction(string uuid)
        {
           return await Storage.GetAction(uuid);
        }

        public async Task CleanDatabase()
        {
            await Storage.CleanDatabase();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds = 1000)
        {
           return await Storage.GetDelayedActions(maxDelayFromNowInSeconds);
        }

        public async Task SetDelayedActionAsExecuted(string id)
        {
            await Storage.SetDelayedActionAsExecuted(id);
        }

        public async Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            await Storage.SaveDelayedAction(action, dueTime, beaconPid, eventTypeDetectedByDevice);
        }

        public async Task<BackgroundEvent> GetLastEventStateForBeacon(string pid)
        {
            return await Storage.GetLastEventStateForBeacon(pid);
        }

        public async Task SaveBeaconEventState(string pid, BeaconEventType enter)
        {
            await Storage.SaveBeaconEventState(pid, enter);
        }

        public async Task SaveHistoryAction(BeaconAction beaconAction)
        {
            await Storage.SaveHistoryAction(beaconAction);
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
            catch (Exception)
            {
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
                    Debug.WriteLine("timeout error while validation api key: " + e.Message);
                    await WaitBackoff(retries);
                }
                catch (IOException e)
                {
                    networkError = true;
                    Debug.WriteLine("Error while validation api key: " + e.Message);
                    await WaitBackoff(retries);
                }
                catch (HttpRequestException e)
                {
                    networkError = true;
                    Debug.WriteLine("Error while validation api key: " + e.Message);
                    await WaitBackoff(retries);
                }
                catch (Exception e)
                {
                    networkError = false;
                    Debug.WriteLine("Error while validation api key: " + e.Message);
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