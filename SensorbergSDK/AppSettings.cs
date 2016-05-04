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
        public ulong BeaconExitTimeout { get; }

        [DataMember(Name = "network.historyUploadInterval")]
        public ulong HistoryUploadInterval { get; }

        [DataMember(Name = "scanner.enterRssiThreshold")]
        public short? RssiEnterThreshold { get; }

        [DataMember(Name = "scanner.enterDistanceThreshold")]
        public ulong? EnterDistanceThreshold { get; }

        [DataMember(Name = "network.beaconLayoutUpdateInterval")]
        public ulong LayoutUpdateInterval { get; }

        [DataMember(Name = "settings.updateTime")]
        public ulong SettingsUpdateInterval { get; }

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
