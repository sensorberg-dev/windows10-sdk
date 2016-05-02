using System;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using Windows.Data.Json;
using SensorbergSDK.Internal;

namespace SensorbergSDK
{
    [DataContract]
    public sealed class AppSettings
    {
        private const string BEACON_EXIT_TIMEOUT_KEY = "scanner.exitTimeoutMillis";
        private const string HISTORY_UPLOAD_INTERVAL_KEY = "network.historyUploadInterval";
        private const string RSSI_ENTER_THRESHOLD_KEY = "scanner.enterRssiThreshold";
        private const string ENTER_DISTANCE_THRESHOLD_KEY = "scanner.enterDistanceThreshold";
        private const string LAYOUT_UPDATE_INTERVAL_KEY = "network.beaconLayoutUpdateInterval";
        private const string SETTINGS_UPDATE_INTERVAL_KEY = "settings.updateTime";

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

        public static AppSettings FromJson(JsonObject json)
        {
            AppSettings settings = new AppSettings();

            Debug.WriteLine("Settings json = " + json);

            if (json.ContainsKey(BEACON_EXIT_TIMEOUT_KEY))
            {
                var exitTimeout = json[BEACON_EXIT_TIMEOUT_KEY];
                settings.BeaconExitTimeout = exitTimeout.ValueType == JsonValueType.Null ? Constants.DefaultBeaconExitTimeout : Convert.ToUInt64(GetValueAsString(exitTimeout));
            }
            else
            {
                settings.BeaconExitTimeout = Constants.DefaultBeaconExitTimeout;
            }

            if (json.ContainsKey(HISTORY_UPLOAD_INTERVAL_KEY))
            {
                var historyInterval = json[HISTORY_UPLOAD_INTERVAL_KEY];
                settings.HistoryUploadInterval = historyInterval.ValueType == JsonValueType.Null ? Constants.DefaultHistoryUploadInterval : Convert.ToUInt64(GetValueAsString(historyInterval));
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
                    settings.RssiEnterThreshold = Convert.ToInt16(GetValueAsString(rssiEnterThreshold));
                }
            }

            if (json.ContainsKey(ENTER_DISTANCE_THRESHOLD_KEY))
            {
                var enterDistanceThreshold = json[ENTER_DISTANCE_THRESHOLD_KEY];
                if (enterDistanceThreshold.ValueType != JsonValueType.Null)
                {
                    settings.EnterDistanceThreshold =
                        Convert.ToUInt64(GetValueAsString(enterDistanceThreshold));
                }
            }

            if (json.ContainsKey(LAYOUT_UPDATE_INTERVAL_KEY))
            {
                var layoutUpdateInterval = json[LAYOUT_UPDATE_INTERVAL_KEY];
                settings.LayoutUpdateInterval = layoutUpdateInterval.ValueType == JsonValueType.Null ? Constants.DefaultLayoutUpdateInterval : Convert.ToUInt64(GetValueAsString(layoutUpdateInterval));
            }
            else
            {
                settings.LayoutUpdateInterval = Constants.DefaultLayoutUpdateInterval;
            }

            if (json.ContainsKey(SETTINGS_UPDATE_INTERVAL_KEY))
            {
                var settingsUpdateInterval = json[SETTINGS_UPDATE_INTERVAL_KEY];
                settings.SettingsUpdateInterval = settingsUpdateInterval.ValueType == JsonValueType.Null ? Constants.DefaultSettingsUpdateInterval : Convert.ToUInt64(GetValueAsString(settingsUpdateInterval));
            }
            else
            {
                settings.SettingsUpdateInterval = Constants.DefaultSettingsUpdateInterval;
            }

            return settings;
        }

        private static string GetValueAsString(IJsonValue enterDistanceThreshold)
        {
            return enterDistanceThreshold.ValueType == JsonValueType.String
                ? enterDistanceThreshold.GetString()
                : enterDistanceThreshold.GetNumber().ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return $"BeaconExitTimeout = {BeaconExitTimeout}, HistoryUploadInterval = {HistoryUploadInterval}, RssiEnterThreshold = {RssiEnterThreshold}, EnterDistanceThreshold = {EnterDistanceThreshold}, LayoutUpdateInterval = {LayoutUpdateInterval}, SettingsUpdateInterval = {SettingsUpdateInterval}";
        }
    }
}
