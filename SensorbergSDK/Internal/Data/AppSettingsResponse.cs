// Created by Kay Czarnotta on 03.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Runtime.Serialization;

namespace SensorbergSDK.Internal.Data
{
    [DataContract]
    public class AppSettingsResponse
    {
        [DataMember(Name = "settings")]
        public AppSettings Settings { get; set; }
    }
}