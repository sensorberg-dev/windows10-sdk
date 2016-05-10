// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Web.Http;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;
using System.Collections.Generic;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDKTests.Mocks
{
    public class MockApiConnection : IApiConnection
    {
        public List<HistoryAction> HistoryActions { get; } = new List<HistoryAction>();
        public List<HistoryEvent> HistoryEvents { get; }= new List<HistoryEvent>();

        private async Task<string> Load(string file)
        {
            if (APIInvalid)
            {
                return string.Empty;
            }
            if (FailNetwork)
            {
                throw new IOException();
            }
            if (UnknownError)
            {
                throw new Exception("ups");
            }
            var uri = new Uri("ms-appx:///Assets/raw/" + file, UriKind.RelativeOrAbsolute);
            return await Windows.Storage.FileIO.ReadTextAsync(await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri));
        }


        public async Task<ResponseMessage> RetrieveLayoutResponse(SdkData data, string apiId = null)
        {
            return new ResponseMessage()
            {
                StatusCode = HttpStatusCode.Ok, Content = await Load("mock/layout_request.json"), Header = await Load("mock/layout_request_header.txt"), IsSuccess = true
            };
        }

        public async Task<string> LoadSettings(SdkData sdkData)
        {
            return MockSettings;
        }

        public Task<ResponseMessage> SendHistory(History history)
        {
            if (APIInvalid)
            {
                return Task.FromResult(new ResponseMessage() { IsSuccess = false });
            }
            if (FailNetwork)
            {
                throw new IOException();
            }
            if (UnknownError)
            {
                throw new Exception("ups");
            }
            if (history != null)
            {
                HistoryEvents.AddRange(history.Events);
                HistoryActions.AddRange(history.Actions);
            }
            return Task.FromResult(new ResponseMessage() {IsSuccess = true});
        }

        public bool APIInvalid { get; set; }
        public bool FailNetwork { get; set; }
        public bool UnknownError { get; set; }
        public string MockSettings { get; set; }
    }
}