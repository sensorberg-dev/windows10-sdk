// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.IO;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace SensorbergSDK.Internal.Services
{
    public interface IApiConnection
    {
        /// <summary>
        /// Sends a layout request to server and returns the HTTP response, if any.
        /// </summary>
        /// <param name="data">api key and device id for the request</param>
        /// <param name="apiId">optional api id, overrides the given id by SDKData</param>
        /// <returns>A HttpResponseMessage containing the server response or null in case of an error.</returns>
        Task<HttpResponseMessage> RetrieveLayoutResponseAsync(SDKData data, string apiId = null);

        Task<string> LoadSettings(SDKData sdkData);

        /// <summary>
        /// Sends History object to the api.
        /// </summary>
        /// <param name="history">Object to send</param>
        /// <returns></returns>
        Task<System.Net.Http.HttpResponseMessage> SendHistory(History history);
    }
}