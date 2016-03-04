// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System.Threading.Tasks;
using Windows.Web.Http;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockApiConnection : IApiConnection
    {
        public Task<HttpResponseMessage> RetrieveLayoutResponseAsync(SDKData data, string apiId = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> LoadSettings(SDKData sdkData)
        {
            throw new System.NotImplementedException();
        }

        public Task<System.Net.Http.HttpResponseMessage> SendHistory(History history)
        {
            throw new System.NotImplementedException();
        }
    }
}