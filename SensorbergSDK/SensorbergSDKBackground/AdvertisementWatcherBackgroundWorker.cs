// Created by Kay Czarnotta on 05.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.ApplicationModel.Background;

namespace SensorbergSDKBackground
{
    public class AdvertisementWatcherBackgroundWorker
    {
        protected BackgroundEngine BackgroundEngine { get; }

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