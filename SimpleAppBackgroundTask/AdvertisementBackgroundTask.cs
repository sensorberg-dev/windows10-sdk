// Created by Kay Czarnotta on 12.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Diagnostics;
using Windows.ApplicationModel.Background;
using SensorbergSDK.SensorbergSDKBackground;

namespace SimpleAppBackgroundTask
{
    public sealed class AdvertisementBackgroundTask:IBackgroundTask
    {
        private AdvertisementWatcherBackgroundWorker worker;

        public AdvertisementBackgroundTask()
        {
            worker = new AdvertisementWatcherBackgroundWorker();
            worker.BeaconActionResolved += (sender, action) => { Debug.Write("Action resolved: " + action.PayloadString); };
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            worker.Run(taskInstance);
        }
    }
}