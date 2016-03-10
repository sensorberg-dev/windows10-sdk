// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Diagnostics;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal.Services
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
        Task FlushHistory();
    }
}