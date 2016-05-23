// Created by Kay Czarnotta on 10.05.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System.Runtime.Serialization;

namespace SensorbergSDK.Data
{
    [DataContract]
    public class HistoryEvent
    {

        [DataMember(Name = "pid")]
        public string BeaconId { get; set; }
        [DataMember(Name = "dt")]
        public string EventTime { get; set; }
        [DataMember(Name = "trigger")]
        public int Trigger { get; set; }

        public bool Delivered { get; set; }
    }
}