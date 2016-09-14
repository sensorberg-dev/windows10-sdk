// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Web.Http;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;
using System.Collections.Generic;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDKTests.Mocks
{
    public class MockApiConnection : IApiConnection
    {
        public string LayoutFile { get; set; } = "mock/layout_request.json";
        public List<HistoryAction> HistoryActions { get; } = new List<HistoryAction>();
        public List<HistoryEvent> HistoryEvents { get; }= new List<HistoryEvent>();
        public string ValidApiKey { get; set; }
        private async Task<string> Load(string file)
        {
            if (APIInvalid)
            {
                LastCallResult = NetworkResult.AuthenticationFailed;
                return string.Empty;
            }
            if (FailNetwork)
            {
                LastCallResult = NetworkResult.NetworkError;
                throw new IOException();
            }
            if (UnknownError)
            {
                LastCallResult = NetworkResult.UnknownError;
                throw new Exception("ups");
            }
            LastCallResult = NetworkResult.Success;
            var uri = new Uri("ms-appx:///Assets/raw/" + file, UriKind.RelativeOrAbsolute);
            return await Windows.Storage.FileIO.ReadTextAsync(await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri));
        }


        public SdkConfiguration Configuration { get; set; }
        public NetworkResult LastCallResult { get; set; }

        public async Task<ResponseMessage> RetrieveLayoutResponse(string apiId = null)
        {
            if (FailNetwork)
            {
                LastCallResult = NetworkResult.NetworkError;
            }
            LastCallResult = NetworkResult.Success;
            if (!string.IsNullOrEmpty(ValidApiKey) && ValidApiKey != apiId && Configuration?.ApiKey != ValidApiKey)
            {
                return new ResponseMessage() {IsSuccess = true, Content = "", NetworResult = NetworkResult.Success, StatusCode = HttpStatusCode.Ok};
            }
            return new ResponseMessage()
            {
                StatusCode = HttpStatusCode.Ok,
                Content = await Load(LayoutFile),
                Header = await Load("mock/layout_request_header.txt"),
                IsSuccess = true
            };
        }

        public async Task<string> LoadSettings()
        {
            if (APIInvalid)
            {
                LastCallResult = NetworkResult.AuthenticationFailed;
                return string.Empty;
            }
            if (FailNetwork)
            {
                LastCallResult = NetworkResult.NetworkError;
                throw new IOException();
            }
            if (UnknownError)
            {
                LastCallResult = NetworkResult.UnknownError;
                throw new Exception("ups");
            }
            LastCallResult = NetworkResult.Success;
            return MockSettings;
        }

        public Task<ResponseMessage> SendHistory(History history)
        {
            if (APIInvalid)
            {
                LastCallResult = NetworkResult.AuthenticationFailed;
                return Task.FromResult(new ResponseMessage() { IsSuccess = false });
            }
            if (FailNetwork)
            {
                LastCallResult = NetworkResult.NetworkError;
                throw new IOException();
            }
            if (UnknownError)
            {
                LastCallResult = NetworkResult.UnknownError;
                throw new Exception("ups");
            }
            if (history != null)
            {
                HistoryEvents.AddRange(history.Events);
                HistoryActions.AddRange(history.Actions);
            }
            LastCallResult = NetworkResult.Success;
            return Task.FromResult(new ResponseMessage() {IsSuccess = true});
        }

        public bool APIInvalid { get; set; }
        public bool FailNetwork { get; set; }
        public bool UnknownError { get; set; }
        public string MockSettings { get; set; }
    }
}