// Created by Kay Czarnotta on 05.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDKBackground
{
    public class AdvertisementWatcherBackgroundWorker
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

        protected BackgroundEngine BackgroundEngine { get; }
        protected BackgroundTaskDeferral Deferral { get; set; }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("AdvertisementWatcherBackgroundTask.Run()");
            Deferral = taskInstance.GetDeferral();

            await BackgroundEngine.InitializeAsync();

            var triggerDetails = taskInstance.TriggerDetails as BluetoothLEAdvertisementWatcherTriggerDetails;
            if (triggerDetails != null)
            {
                int outOfRangeDb = triggerDetails.SignalStrengthFilter.OutOfRangeThresholdInDBm.HasValue ? triggerDetails.SignalStrengthFilter.OutOfRangeThresholdInDBm.Value : 0;
                await BackgroundEngine.ResolveBeaconActionsAsync(TriggerDetailsToBeacons(triggerDetails), outOfRangeDb);
            }

            //setting any value to Progress will fire Progress event with UI app
            taskInstance.Progress = 1;
        }

        /// <summary>
        /// Constructs Beacon instances from the trigger data and adds recognized beacons to the _beacons list
        /// </summary>
        /// <param name="triggerDetails"></param>
        private List<Beacon> TriggerDetailsToBeacons(BluetoothLEAdvertisementWatcherTriggerDetails triggerDetails)
        {
            List<Beacon> beacons = new List<Beacon>(); 
            if (triggerDetails != null)
            {
                foreach (var bleAdvertisementReceivedEventArgs in triggerDetails.Advertisements)
                {
                    Beacon beacon = BeaconFactory.BeaconFromBluetoothLeAdvertisementReceivedEventArgs(bleAdvertisementReceivedEventArgs);
                    beacons.Add(beacon);
                }
            }
            return beacons;
        }

        private void OnFinished(object sender, BackgroundWorkerType type)
        {
            if (type == BackgroundWorkerType.AdvertisementWorker)
            {
                BackgroundEngine.ProcessDelayedActionsAsync().ConfigureAwait(false);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("AdvertisementWatcherBackgroundTask.OnFinished()");
                Deferral?.Complete();
                BackgroundEngine.Finished -= OnFinished;
                BackgroundEngine?.Dispose();
            }
        }
    }
}