// Created by Kay Czarnotta on 12.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.ApplicationModel.Background;
using SensorbergSDK.SensorbergSDKBackground;

namespace SimpleAppBackgroundTask
{
    public sealed class SimpleAppTimerBackgroundTask : IBackgroundTask
    {
        private TimedBackgroundWorker worker;

        public SimpleAppTimerBackgroundTask()
        {
            worker = new TimedBackgroundWorker();
        }
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            worker.Run(taskInstance);
        }
    }
}