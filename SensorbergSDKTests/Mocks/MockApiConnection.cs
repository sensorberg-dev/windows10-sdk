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

namespace SensorbergSDKTests.Mocks
{
    public class MockApiConnection : IApiConnection
    {
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
            var uri = new System.Uri("ms-appx:///Assets/raw/" + file, UriKind.RelativeOrAbsolute);
            return await Windows.Storage.FileIO.ReadTextAsync(await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri));
        }


        public async Task<ResponseMessage> RetrieveLayoutResponseAsync(SDKData data, string apiId = null)
        {
            return new ResponseMessage()
            {
                StatusCode = HttpStatusCode.Ok, Content = await Load("mock/layout_request.json"), Header = await Load("mock/layout_request_header.txt"), IsSuccess = true
            };
        }

        public async Task<string> LoadSettings(SDKData sdkData)
        {
            return null;
        }

        public Task<ResponseMessage> SendHistory(History history)
        {
            return Task.FromResult(new ResponseMessage() {IsSuccess = true});
        }

        public bool APIInvalid { get; set; }
        public bool FailNetwork { get; set; }
        public bool UnknownError { get; set; }
    }
}