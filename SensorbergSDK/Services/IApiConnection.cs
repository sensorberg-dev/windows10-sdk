// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Services
{
    public interface IApiConnection
    {
        /// <summary>
        /// Sends a layout request to server and returns the HTTP response, if any.
        /// </summary>
        /// <param name="data">api key and device id for the request.</param>
        /// <param name="apiId">optional api id, overrides the given id by SDKData.</param>
        /// <returns>A HttpResponseMessage containing the server response or null in case of an error.</returns>
        Task<ResponseMessage> RetrieveLayoutResponse(SdkData data, string apiId = null);

        Task<string> LoadSettings(SdkData sdkData);

        /// <summary>
        /// Sends History object to the api.
        /// </summary>
        /// <param name="history">Object to send.</param>
        /// <returns></returns>
        Task<ResponseMessage> SendHistory(History history);
    }
}