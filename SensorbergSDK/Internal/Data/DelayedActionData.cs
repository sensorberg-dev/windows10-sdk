// Created by Kay Czarnotta on 10.05.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System;
using SensorbergSDK.Transport;

namespace SensorbergSDK.Data
{
    public class DelayedActionData
    {
        public string Id { get; set; }
        public ResolvedAction ResolvedAction { get; set; }
        public DateTimeOffset DueTime { get; set; }
        public string BeaconPid { get; set; }
        public BeaconEventType EventTypeDetectedByDevice { get; set; }
    }
}