// Created by Kay Czarnotta on 20.04.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using Windows.ApplicationModel.Background;

namespace SensorbergSDKTests.Mocks
{
    public class MockBackgroundTaskInstance: IBackgroundTaskInstance
    {
        public BackgroundTaskDeferral GetDeferral()
        {
            return null;
        }

        public Guid InstanceId { get; }
        public uint Progress { get; set; }
        public uint SuspendedCount { get; }
        public BackgroundTaskRegistration Task { get; }
        public object TriggerDetails { get; }
        public event BackgroundTaskCanceledEventHandler Canceled;
    }
}