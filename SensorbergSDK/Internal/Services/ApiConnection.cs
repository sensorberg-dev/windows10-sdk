// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http.Filters;
using SensorbergSDK.Internal.Utils;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;

namespace SensorbergSDK.Internal.Services
{
    public class ApiConnection : IApiConnection
    {
        /// <summary>
        /// Sends a layout request to server and returns the HTTP response, if any.
        /// </summary>
        /// <param name="data">api key and device id for the request</param>
        /// <param name="apiId">optional api id, overrides the given id by SDKData</param>
        /// <returns>A HttpResponseMessage containing the server response or null in case of an error.</returns>
        public async Task<HttpResponseMessage> RetrieveLayoutResponseAsync(SDKData data, string apiId = null)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            HttpBaseProtocolFilter baseProtocolFilter = new HttpBaseProtocolFilter();

            baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            requestMessage.Method = HttpMethod.Get;
            requestMessage.RequestUri = new Uri(Constants.LayoutApiUriAsString);

            HttpClient apiConnection = new HttpClient(baseProtocolFilter);
            apiConnection.DefaultRequestHeaders.Add(Constants.XApiKey, string.IsNullOrEmpty(apiId) ? data.ApiKey : apiId);
            apiConnection.DefaultRequestHeaders.Add(Constants.Xiid, data.DeviceId);
            HttpResponseMessage responseMessage = null;

            try
            {
                responseMessage = await apiConnection.SendRequestAsync(requestMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LayoutManager.RetrieveLayoutResponseAsync(): Failed to send HTTP request: " + ex.Message);
            }

            return responseMessage;
        }


        public async Task<string> LoadSettings(SDKData sdkData)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            HttpBaseProtocolFilter baseProtocolFilter = new HttpBaseProtocolFilter();

            baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            requestMessage.Method = HttpMethod.Get;
            requestMessage.RequestUri = new Uri(string.Format(Constants.SettingsUri, sdkData.ApiKey));

            HttpClient httpClient = new HttpClient(baseProtocolFilter);


            var responseMessage = await httpClient.SendRequestAsync(requestMessage);


            if (responseMessage == null || responseMessage.IsSuccessStatusCode == false)
            {
                return null;
            }

            return responseMessage.Content.ToString();
        }

        public async Task<System.Net.Http.HttpResponseMessage> SendHistory(History history)
        {
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(History));
            ser.WriteObject(stream1, history);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);

            System.Net.Http.HttpClient apiConnection = new System.Net.Http.HttpClient();
            apiConnection.DefaultRequestHeaders.Add(Constants.XApiKey, SDKData.Instance.ApiKey);
            apiConnection.DefaultRequestHeaders.Add(Constants.Xiid, SDKData.Instance.DeviceId);
            bool result = apiConnection.DefaultRequestHeaders.TryAddWithoutValidation(Constants.XUserAgent, UserAgentBuilder.BuildUserAgentJson());
            var content = new StringContent(sr.ReadToEnd(), Encoding.UTF8, "application/json");

            System.Net.Http.HttpResponseMessage responseMessage = await apiConnection.PostAsync(new Uri(Constants.LayoutApiUriAsString), content);
            return responseMessage;
        }
    }
}