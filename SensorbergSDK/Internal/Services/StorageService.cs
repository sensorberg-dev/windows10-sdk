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

namespace SensorbergSDK.Internal.Services
{
    public class StorageService : IStorageService
    {
        public int RetryCount { get; set; }

        private Storage Storage { [DebuggerStepThrough] get; [DebuggerStepThrough] set; } = Internal.Storage.Instance;

        /// <summary>
        /// Checks whether the given API key is valid or not.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>The validation result.</returns>
        public async Task<ApiKeyValidationResult> ValidateApiKey(string apiKey)
        {
            int retries = 0;
            bool networkError = false;
            do
            {
                ResponseMessage responseMessage = null;
                try
                {
                    responseMessage = await ServiceManager.ApiConnction.RetrieveLayoutResponseAsync(SDKData.Instance, apiKey);

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
                if (responseMessage != null && responseMessage.IsSuccess)
                {

                    return string.IsNullOrEmpty(responseMessage.Content) || responseMessage.Content.Length < Constants.MinimumLayoutContentLength
                        ? ApiKeyValidationResult.Invalid
                        : ApiKeyValidationResult.Valid;
                }
            } while (retries < RetryCount);

            return networkError ? ApiKeyValidationResult.NetworkError : ApiKeyValidationResult.UnknownError;
        }

        private async Task WaitBackoff(int currentRetries)
        {
            await Task.Delay((int) Math.Pow(100*currentRetries + 1, currentRetries + 1));
        }

        public Task<LayoutResult> RetrieveLayout()
        {
            throw new System.NotImplementedException();
        }

        public Task<AppSettings> RetrieveAppSettings()
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> FlushHistory()
        {
            try
            {
                History history = new History();
                history.actions = await Storage.GetUndeliveredActionsAsync();
                history.events = await Storage.GetUndeliveredEventsAsync();

                if ((history.events != null && history.events.Count > 0) || (history.actions != null && history.actions.Count > 0))
                {
                    var responseMessage = await ServiceManager.ApiConnction.SendHistory(history);

                    if (responseMessage.IsSuccess)
                    {
                        if ((history.events != null && history.events.Count > 0))
                        {
                            await Storage.SetEventsAsDeliveredAsync();
                        }

                        if (history.actions != null && history.actions.Count > 0)
                        {
                            await Storage.SetActionsAsDeliveredAsync();
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
        public async Task SaveHistoryAction(string uuid, string beaconPid, DateTime now, int beaconEventType)
        {
            await Storage.SaveHistoryAction(uuid, beaconPid, now, beaconEventType);
        }

        public async Task SaveHistoryEvent(string pid, DateTimeOffset timestamp, int eventType)
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

        public async Task<IList<BeaconAction>> GetBeaconActionsFromBackground()
        {
           return await Storage.GetBeaconActionsFromBackground();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds = 1000)
        {
           return await Storage.GetDelayedActions(maxDelayFromNowInSeconds);
        }

        public async Task SetDelayedActionAsExecuted(int id)
        {
            await Storage.SetDelayedActionAsExecuted(id);
        }

        public async Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            await Storage.SaveDelayedAction(action, dueTime, beaconPid, eventTypeDetectedByDevice);
        }

        public async Task<IList<DBBackgroundEventsHistory>> GetBeaconBackgroundEventsHistory(string pid)
        {
            return await Storage.GetBeaconBackgroundEventsHistory(pid);
        }

        public async Task SaveBeaconBackgroundEvent(string pid, BeaconEventType enter)
        {
            await Storage.SaveBeaconBackgroundEvent(pid, enter);
        }

        public async Task DeleteBackgroundEvent(string pid)
        {
            await Storage.DeleteBackgroundEvent(pid);
        }

        public async Task SaveBeaconActionFromBackground(BeaconAction beaconAction)
        {
            await Storage.SaveBeaconActionFromBackground(beaconAction);
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