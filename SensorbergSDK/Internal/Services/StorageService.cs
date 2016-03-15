// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using SensorbergSDK.Data;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Utils;

namespace SensorbergSDK.Internal.Services
{
    public class StorageService : IStorageService
    {
        private const string KeyLayoutHeaders = "layout_headers";
        private const string KeyLayoutContent = "layout_content.cache"; // Cache file
        private const string KeyLayoutRetrievedTime = "layout_retrieved_time";

        public int RetryCount { get; set; }

        private Storage Storage { [DebuggerStepThrough] get; [DebuggerStepThrough] set; } = Internal.Storage.Instance;

        /// <summary>
        /// Checks whether the given API key is valid or not.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>The validation result.</returns>
        public async Task<ApiKeyValidationResult> ValidateApiKey(string apiKey)
        {
            ResponseMessage responseMessage = null;

            responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.RetrieveLayoutResponseAsync(SDKData.Instance, apiKey));

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
            await Task.Delay((int) Math.Pow(100*currentRetries + 1, currentRetries + 1));
        }

        public async Task<LayoutResult> RetrieveLayout()
        {
            ResponseMessage responseMessage = await ExecuteCall(async () => await ServiceManager.ApiConnction.RetrieveLayoutResponseAsync(SDKData.Instance));
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
                        Debug.WriteLine("LayoutManager.RetrieveLayoutAsync(): Failed to parse layout: " + ex.ToString());
                        layout = null;
                    }
                }

                if (layout != null)
                {
                    // Store the parsed layout
                    await SaveLayoutToLocalStorageAsync(headersAsString, contentAsString, layoutRetrievedTime);
                    return new LayoutResult() {Layout = layout, Result = NetworkResult.Success};
                }
            }

            return new LayoutResult() {Result = responseMessage != null ? responseMessage.NetworResult : NetworkResult.UnknownError};
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
        private async Task SaveLayoutToLocalStorageAsync(string headers, string content, DateTimeOffset layoutRetrievedTime)
        {
            if (await StoreDataAsync(KeyLayoutContent, content))
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
        private async Task<bool> StoreDataAsync(string fileName, string data)
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
                Debug.WriteLine("LayoutManager.StoreDataAsync(): Failed to save content: " + ex);
            }

            return success;
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