// Created by Kay Czarnotta on 10.05.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SensorbergSDK.Data
{
    /// <summary>
    /// History, HistoryEvent,HistoryAction are internal classes that used to construct json structure for an HTTP post.
    /// </summary>
    [DataContract]
    public class History
    {
        public const string Timeformat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

        [DataMember(Name = "deviceTimestamp")]
        public string DeviceTimestamp { get; set; } = DateTime.UtcNow.ToString(Timeformat);
        [DataMember(Name= "events")]
        public IList<HistoryEvent> Events { get; set; } = new List<HistoryEvent>();
        [DataMember(Name= "actions")]
        public IList<HistoryAction> Actions { get; set; } = new List<HistoryAction>();
    }
}