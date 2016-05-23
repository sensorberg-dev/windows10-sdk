// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using Windows.ApplicationModel.Background;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Background
{
    /// <summary>
    /// Timer triggered background task for processing pending delayed actions.
    /// This is not part of the public API. Making modifications into background tasks is not required in order to use the SDK.
    /// </summary>
    public class TimedBackgroundWorker
    {
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

        protected BackgroundEngine BackgroundEngine { get; }
        protected BackgroundTaskDeferral Deferral { get; set; }

        public TimedBackgroundWorker()
        {
            BackgroundEngine = new BackgroundEngine();
            BackgroundEngine.Finished += OnFinished;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("TimedBackgroundWorker.Run()");
            Deferral = taskInstance.GetDeferral();

            await BackgroundEngine.InitializeAsync();
            await BackgroundEngine.ProcessDelayedActionsAsync();
        }

        private void OnFinished(object sender, BackgroundWorkerType e)
        {
            System.Diagnostics.Debug.WriteLine("TimedBackgroundWorker.OnFinished()");
            Deferral?.Complete();
            BackgroundEngine.Finished -= OnFinished;
            BackgroundEngine.Dispose();
        }
    }
}
