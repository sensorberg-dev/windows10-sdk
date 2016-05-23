// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Collections.Generic;
using System.Runtime.Serialization;
using SensorbergSDK.Internal;

namespace SensorbergSDK
{
    /// <summary>
    /// Class to hold all configuration for the app. These settings are changeable by the Sensorberg backend.
    /// https://github.com/sensorberg-dev/android-sdk-samples/blob/master/technical-features.md#advanced-features
    /// Some settings are preset for the SDK but also custom data is possible.
    /// </summary>
    [DataContract]
    public class AppSettings
    {
        private const string ScannerExittimeoutmillis = "scanner.exitTimeoutMillis";
        private const string NetworkHistoryuploadinterval = "network.historyUploadInterval";
        private const string ScannerEnterrssithreshold = "scanner.enterRssiThreshold";
        private const string ScannerEnterdistancethreshold = "scanner.enterDistanceThreshold";
        private const string NetworkBeaconlayoutupdateinterval = "network.beaconLayoutUpdateInterval";
        private const string SettingsUpdatetime = "settings.updateTime";

        [DataMember(Name="settings")]
        protected Dictionary<string,object> Settings { get; private set; } = new Dictionary<string, object>();

        public ulong BeaconExitTimeout
        {
            get { return Settings.ContainsKey(ScannerExittimeoutmillis) ? ulong.Parse(Settings[ScannerExittimeoutmillis].ToString()) : Constants.DefaultBeaconExitTimeout; }
            set { Settings[ScannerExittimeoutmillis] = value; }
        }

        /// <summary>
        /// Time between each upload of history entries for the statistics.
        /// </summary>
        public ulong HistoryUploadInterval
        {
            get { return Settings.ContainsKey(NetworkHistoryuploadinterval) ? ulong.Parse(Settings[NetworkHistoryuploadinterval].ToString()) : Constants.DefaultHistoryUploadInterval; }
            set { Settings[NetworkHistoryuploadinterval] = value; }
        }

        /// <summary>
        /// Value for handling the Enter/Exit of beacons. The value has to be between -128 < x < 127
        /// </summary>
        public short? RssiEnterThreshold
        {
            get { return Settings.ContainsKey(ScannerEnterrssithreshold) ? short.Parse(Settings[ScannerEnterrssithreshold].ToString()) : (short?) null; }
            set { Settings[ScannerEnterrssithreshold] = value; }
        }

        /// <summary>
        /// Value for handling Enter and Exit based on distance. The value is in meter.
        /// </summary>
        public ulong? EnterDistanceThreshold
        {
            get { return Settings.ContainsKey(ScannerEnterdistancethreshold) ? ulong.Parse(Settings[ScannerEnterdistancethreshold].ToString()) : Constants.DefaultBeaconExitTimeout; }
            set { Settings[ScannerEnterdistancethreshold] = value; }
        }

        /// <summary>
        /// Time to update the layout.
        /// </summary>
        public ulong LayoutUpdateInterval
        {
            get { return Settings.ContainsKey(NetworkBeaconlayoutupdateinterval) ? ulong.Parse(Settings[NetworkBeaconlayoutupdateinterval].ToString()) : Constants.DefaultLayoutUpdateInterval; }
            set { Settings[NetworkBeaconlayoutupdateinterval] = value; }
        }

        /// <summary>
        /// Time to update the app settings.
        /// </summary>
        public ulong SettingsUpdateInterval
        {
            get { return Settings.ContainsKey(SettingsUpdatetime) ? ulong.Parse(Settings[SettingsUpdatetime].ToString()) : Constants.DefaultSettingsUpdateInterval; }
            set { Settings[SettingsUpdatetime] = value; }
        }

        public object this[string key]
        {
            get { return Settings[key]; }
        }

        public bool ContainsKey(string key)
        {
            return Settings.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return Settings.Remove(key);
        }
        public Dictionary<string, object>.KeyCollection Keys
        {
            get { return Settings.Keys; }
        }
    }
}
