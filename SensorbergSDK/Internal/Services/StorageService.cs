// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal.Services
{
    public class StorageService : IStorageService
    {
        public int RetryCount { get; set; }

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
            await Task.Delay((int)Math.Pow(100*currentRetries + 1, currentRetries + 1));
        }

        public Task<LayoutResult> RetrieveLayout()
        {
            throw new System.NotImplementedException();
        }

        public Task<AppSettings> RetrieveAppSettings()
        {
            throw new System.NotImplementedException();
        }

        public Task FlushHistory()
        {
            throw new System.NotImplementedException();
        }
    }
}