// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
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

        public Task<string> LoadSettings(SDKData sdkData)
        {
            throw new System.NotImplementedException();
        }

        public Task<ResponseMessage> SendHistory(History history)
        {
            return Task.FromResult(new ResponseMessage() {IsSuccess = true});
        }
    }
}