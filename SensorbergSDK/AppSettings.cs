using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using SensorbergSDK.Internal;

namespace SensorbergSDK
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember(Name = "scanner.exitTimeoutMillis")]
        public UInt64 BeaconExitTimeout { get;}

        [DataMember(Name = "network.historyUploadInterval")]
        public UInt64 HistoryUploadInterval { get;}

        [DataMember(Name = "scanner.enterRssiThreshold")]
        public Int16? RssiEnterThreshold { get; }

        [DataMember(Name = "scanner.enterDistanceThreshold")]
        public UInt64? EnterDistanceThreshold { get; }

        [DataMember(Name = "network.beaconLayoutUpdateInterval")]
        public UInt64 LayoutUpdateInterval { get;  }

        [DataMember(Name = "settings.updateTime")]
        public UInt64 SettingsUpdateInterval { get;}

        /// <summary>
        /// Creates an appsettings object, including the basic setup.
        /// </summary>
        public AppSettings()
        {
            BeaconExitTimeout = Constants.DefaultBeaconExitTimeout;
            SettingsUpdateInterval = Constants.DefaultSettingsUpdateInterval;
            HistoryUploadInterval = Constants.DefaultHistoryUploadInterval;
            LayoutUpdateInterval = Constants.DefaultLayoutUpdateInterval;
        }

        public override string ToString()
        {
            return
                $"BeaconExitTimeout = {BeaconExitTimeout}, HistoryUploadInterval = {HistoryUploadInterval}, RssiEnterThreshold = {RssiEnterThreshold}, EnterDistanceThreshold = {EnterDistanceThreshold}, LayoutUpdateInterval = {LayoutUpdateInterval}, SettingsUpdateInterval = {SettingsUpdateInterval}";
        }
    }
}
