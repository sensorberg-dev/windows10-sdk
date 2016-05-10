// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Runtime.Serialization;

namespace SensorbergSDK.Internal
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember(Name = "scanner.exitTimeoutMillis")]
        public ulong BeaconExitTimeout { get; set; }

        [DataMember(Name = "network.historyUploadInterval")]
        public ulong HistoryUploadInterval { get; set; }

        [DataMember(Name = "scanner.enterRssiThreshold")]
        public short? RssiEnterThreshold { get; set; }

        [DataMember(Name = "scanner.enterDistanceThreshold")]
        public ulong? EnterDistanceThreshold { get; set; }

        [DataMember(Name = "network.beaconLayoutUpdateInterval")]
        public ulong LayoutUpdateInterval { get; set; }

        [DataMember(Name = "settings.updateTime")]
        public ulong SettingsUpdateInterval { get; set; }

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
