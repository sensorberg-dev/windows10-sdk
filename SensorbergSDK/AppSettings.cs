using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace SensorbergSDK.Internal.Data
{
    [DataContract]
    internal sealed class AppSettings
    {
        private const string BEACON_EXIT_TIMEOUT_KEY = "scanner.exitTimeoutMillis";
        private const string HISTORY_UPLOAD_INTERVAL_KEY = "network.historyUploadInterval";
        private const string RSSI_ENTER_THRESHOLD_KEY = "scanner.enterRssiThreshold";
        private const string ENTER_DISTANCE_THRESHOLD_KEY = "scanner.enterDistanceThreshold";
        private const string LAYOUT_UPDATE_INTERVAL_KEY = "network.beaconLayoutUpdateInterval";
        private const string SETTINGS_UPDATE_INTERVAL_KEY = "settings.updateTime";

        [DataMember(Name = "scanner.exitTimeoutMillis")]
        public UInt64 BeaconExitTimeout { get; set; }

        [DataMember(Name = "network.historyUploadInterval")]
        public UInt64 HistoryUploadInterval { get; set; }

        [DataMember(Name = "scanner.enterRssiThreshold")]
        public Int16? RssiEnterThreshold { get; set; }

        [DataMember(Name = "scanner.enterDistanceThreshold")]
        public UInt64? EnterDistanceThreshold { get; set; }

        [DataMember(Name = "network.beaconLayoutUpdateInterval")]
        public UInt64 LayoutUpdateInterval { get; set; }

        [DataMember(Name = "settings.updateTime")]
        public UInt64 SettingsUpdateInterval { get; set; }

        public static AppSettings FromJson(JsonObject json)
        {
            AppSettings settings = new AppSettings();

            Debug.WriteLine("Settings json = " + json);

            if (json.ContainsKey(BEACON_EXIT_TIMEOUT_KEY))
            {
                var exitTimeout = json[BEACON_EXIT_TIMEOUT_KEY];
                settings.BeaconExitTimeout = exitTimeout.ValueType == JsonValueType.Null ? Constants.DefaultBeaconExitTimeout : Convert.ToUInt64(exitTimeout.GetNumber());
            }
            else
            {
                settings.BeaconExitTimeout = Constants.DefaultBeaconExitTimeout;
            }

            if (json.ContainsKey(HISTORY_UPLOAD_INTERVAL_KEY))
            {
                var historyInterval = json[HISTORY_UPLOAD_INTERVAL_KEY];
                settings.HistoryUploadInterval = historyInterval.ValueType == JsonValueType.Null ? Constants.DefaultHistoryUploadInterval : Convert.ToUInt64(historyInterval.GetNumber());
            }
            else
            {
                settings.HistoryUploadInterval = Constants.DefaultHistoryUploadInterval;
            }

            if (json.ContainsKey(RSSI_ENTER_THRESHOLD_KEY))
            {
                var rssiEnterThreshold = json[RSSI_ENTER_THRESHOLD_KEY];
                if (rssiEnterThreshold.ValueType != JsonValueType.Null)
                {
                    settings.RssiEnterThreshold = Convert.ToInt16(rssiEnterThreshold.GetNumber());
                }
            }

            if (json.ContainsKey(ENTER_DISTANCE_THRESHOLD_KEY))
            {
                var enterDistanceThreshold = json[ENTER_DISTANCE_THRESHOLD_KEY];
                if (enterDistanceThreshold.ValueType != JsonValueType.Null)
                {
                    settings.EnterDistanceThreshold = Convert.ToUInt64(enterDistanceThreshold.GetNumber());
                }
            }

            if (json.ContainsKey(LAYOUT_UPDATE_INTERVAL_KEY))
            {
                var layoutUpdateInterval = json[LAYOUT_UPDATE_INTERVAL_KEY];
                settings.LayoutUpdateInterval = layoutUpdateInterval.ValueType == JsonValueType.Null ? Constants.DefaultLayoutUpdateInterval : Convert.ToUInt64(layoutUpdateInterval.GetNumber());
            }
            else
            {
                settings.LayoutUpdateInterval = Constants.DefaultLayoutUpdateInterval;
            }

            if (json.ContainsKey(SETTINGS_UPDATE_INTERVAL_KEY))
            {
                var settingsUpdateInterval = json[SETTINGS_UPDATE_INTERVAL_KEY];
                settings.SettingsUpdateInterval = settingsUpdateInterval.ValueType == JsonValueType.Null ? Constants.DefaultSettingsUpdateInterval : Convert.ToUInt64(settingsUpdateInterval.GetNumber());
            }
            else
            {
                settings.SettingsUpdateInterval = Constants.DefaultSettingsUpdateInterval;
            }

            return settings;
        }

        public override string ToString()
        {
            return $"BeaconExitTimeout = {BeaconExitTimeout}, HistoryUploadInterval = {HistoryUploadInterval}, RssiEnterThreshold = {RssiEnterThreshold}, EnterDistanceThreshold = {EnterDistanceThreshold}, LayoutUpdateInterval = {LayoutUpdateInterval}, SettingsUpdateInterval = {SettingsUpdateInterval}";
        }
    }
}
