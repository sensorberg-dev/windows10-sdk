// Created by Kay Czarnotta on 05.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using Windows.ApplicationModel.Background;
using SensorbergSDK;

namespace SensorbergSDKBackground
{
    public class AdvertisementWatcherBackgroundWorker
    {
        protected BackgroundEngine BackgroundEngine { get; }
        public event EventHandler<BeaconAction> BeaconActionResolved
        {
            add { BackgroundEngine.BeaconActionResolved += value; }
            remove { BackgroundEngine.BeaconActionResolved -= value; }
        }

        public event EventHandler<string> FailedToResolveBeaconAction
        {
            add { BackgroundEngine.FailedToResolveBeaconAction += value; }
            remove { BackgroundEngine.FailedToResolveBeaconAction -= value; }
        }

        public event EventHandler<bool> LayoutValidityChanged
        {
            add { BackgroundEngine.LayoutValidityChanged += value; }
            remove { BackgroundEngine.LayoutValidityChanged -= value; }
        }

        public AdvertisementWatcherBackgroundWorker()
        {
            BackgroundEngine = new BackgroundEngine();
            BackgroundEngine.Finished += OnFinished;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("AdvertisementWatcherBackgroundTask.Run()");
            await BackgroundEngine.InitializeAsync(taskInstance);
            await BackgroundEngine.ResolveBeaconActionsAsync();
        }

        private void OnFinished(object sender, int e)
        {
            System.Diagnostics.Debug.WriteLine("AdvertisementWatcherBackgroundTask.OnFinished()");
            BackgroundEngine.Finished -= OnFinished;
        }
    }
}