// Created by Kay Czarnotta on 20.06.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;

namespace SensorbergSDK.Internal.Data
{
    public class DelayedActionHelper
    {
        public string Id { get; set; }
        public DateTimeOffset Offset { get; set; }
        public string Content { get; set; }

        public bool Executed { get; set; }
        public string Location { get; set; }
    }
}