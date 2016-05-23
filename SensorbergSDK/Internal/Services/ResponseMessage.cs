// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.Web.Http;

namespace SensorbergSDK.Services
{
    public class ResponseMessage
    {
        public string Header { get; set; }
        public string Content { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }

        public NetworkResult NetworResult { get; set; }
    }
}