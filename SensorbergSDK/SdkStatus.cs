// Created by Kay Czarnotta on 01.09.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using Windows.Media.Streaming.Adaptive;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK
{
    public class SdkStatus
    {
        public async Task<bool> IsApiKeyValid()
        {
            ApiKeyHelper helper = new ApiKeyHelper();
            return await helper.ValidateApiKey(null) == ApiKeyValidationResult.Valid;
        }

        public async Task<bool> IsResolverReachable()
        {
            if (ServiceManager.ApiConnction == null)
            {
                return false;
            }

            NetworkResult result = ServiceManager.ApiConnction.LastCallResult;

            if (result == NetworkResult.UnknownError)
            {
                await ServiceManager.ApiConnction.LoadSettings();
                result = ServiceManager.ApiConnction.LastCallResult;
            }
            return result != NetworkResult.NetworkError && result != NetworkResult.UnknownError;
        }
    }
}