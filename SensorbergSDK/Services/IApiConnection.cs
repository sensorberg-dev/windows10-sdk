// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction for every connection to the Sensorberg backends.
    /// </summary>
    public interface IApiConnection
    {
        /// <summary>
        /// Sends a layout request to server and returns the HTTP response, if any.
        /// </summary>
        /// <param name="apiId">optional api id, overrides the given id by SDKData.</param>
        /// <returns>A HttpResponseMessage containing the server response or null in case of an error.</returns>
        Task<ResponseMessage> RetrieveLayoutResponse(string apiId = null);

        /// <summary>
        /// Receive the settings for the app.
        /// </summary>
        Task<string> LoadSettings();

        /// <summary>
        /// Sends History object to the api.
        /// </summary>
        /// <param name="history">Object to send.</param>
        Task<ResponseMessage> SendHistory(History history);
    }
}