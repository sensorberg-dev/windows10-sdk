// Created by Kay Czarnotta on 12.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Diagnostics;
using Windows.ApplicationModel.Background;
using SensorbergSDK.Background;

namespace SimpleAppBackgroundTask
{
    public sealed class SimpleAppTimerBackgroundTask : IBackgroundTask
    {
        private TimedBackgroundWorker worker;

        public SimpleAppTimerBackgroundTask()
        {
            worker = new TimedBackgroundWorker();
            worker.BeaconActionResolved += (sender, action) => { Debug.Write("Action resolved: " + action.PayloadString); };
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            worker.Run(taskInstance);
        }
    }
}