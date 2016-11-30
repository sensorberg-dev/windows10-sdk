// Created by Kay Czarnotta on 25.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK
{
    /// <summary>
    /// Defines the configuration for the SDK.
    /// </summary>
    public class SdkConfiguration
    {
        private string _resolverUri;

        /// <summary>
        /// ApiKey to connect to the Sensorberg Backend. See https://manage.sensorberg.com/#/applications for the key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Class name to register the background timer.
        /// <see cref="SDKManager.RegisterBackgroundTaskAsync"/>
        /// </summary>
        public string BackgroundTimerClassName { get; set; }

        /// <summary>
        /// class name to register the advertismend task, this task receives the bluetooth events.
        /// <see cref="SDKManager.RegisterBackgroundTaskAsync"/>
        /// </summary>
        public string BackgroundAdvertisementClassName { get; set; }

        /// <summary>
        /// Defines an differend background beacon uuid space.
        /// See https://msdn.microsoft.com/de-de/library/windows/apps/windows.devices.bluetooth.advertisement.bluetoothleadvertisementfilter.bytepatterns.aspx for further information.
        /// Without background usage this property isn't needet.
        /// </summary>
        public string BackgroundBeaconUuidSpace { get; set; }

        /// <summary>
        /// Defines if the beacon scanner should automaticly start.
        /// </summary>
        public bool AutoStartScanner { get; set; }

        /// <summary>
        /// Enable or disable the usage of geolocation for the beacon actions.
        /// </summary>
        public bool UseLocation { get; set; }

        /// <summary>
        /// Defines an user or advertisement id for collection all the events from a specific user or device.
        /// </summary>
        public string UserId
        {
            get { return SdkData.UserId; }
            set { SdkData.UserId = value; }
        }

        /// <summary>
        /// The manufacturer ID to filter beacons that are being watched.
        /// </summary>
        public ushort ManufacturerId { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// The beacon code to filter beacons that are being watched.
        /// </summary>
        public ushort BeaconCode { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// Sets the uri for the resolver. If it is set all related uris will recreated.
        /// </summary>
        public string ResolverUri
        {
            get { return _resolverUri; }
            set
            {
                if (!value.StartsWith("http"))
                {
                    value = "https://" + value;
                }
                if (!value.EndsWith("/"))
                {
                    value = value + "/";
                }
                _resolverUri = value;
                LayoutUri = value + "layout";
                SettingsUri = value + "applications/{0}/settings/windows10/";
            }
        }

        public string LayoutUri { get; set; }
        public string SettingsUri { get; set; }

        /// <summary>
        /// Creates a new object that starts automatic the scanner and collect every beacon contains the sensorberg uuids.
        /// </summary>
        public SdkConfiguration()
        {
            AutoStartScanner = true;
            BackgroundBeaconUuidSpace = string.Empty;
            ResolverUri = Constants.ResolverUri;
        }

        public string GetLayoutUriV2(string apiId = null)
        {
            return string.Format("https://demo.sensorberg.com/api/v1/sdk/gateways/{0}/interactions.json", string.IsNullOrEmpty(apiId) ? ApiKey : apiId);
        }

        public string GetSettingsUriV2()
        {
            return string.Format("https://demo.sensorberg.com/api/v1/sdk/gateways/{0}/settings.json?platform=windows10", ApiKey);
        }

        public string GetPushHistoryV2()
        {
            return string.Format("https://demo.sensorberg.com/api/v1/sdk/events.json", ApiKey);
        }
    }
}