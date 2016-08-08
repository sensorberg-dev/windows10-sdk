// Created by Kay Czarnotta on 29.07.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.
namespace SensorbergSDK.Internal.Data
{
    public class SensorbergApplication
    {
        public string AppKey { get; set; }
        public string AppName { get; set; }
        public SensorbergPlatform Platform { get; set; }
    }

    public enum SensorbergPlatform
    {
        Unknown, Android, Windows, Ios
    }
}