using System;
using System.Diagnostics;
using System.Text;
using Windows.Storage;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Contains the global SDK data.
    /// </summary>
    public sealed class SDKData
    {
        private const string KeySensorbergSdkApiKey = "sensorberg_sdk_api_key";
        private const string KeySensorbergSdkGuid = "sensorberg_sdk_guid";
        private const string KeyLayoutBeaconId1Hash = "sensorberg_sdk_layout_uuid_hash";
        private const string KeyDatabaseCleaningTime = "sensorberg_sdk_database_cleaning_time";
        private const string KeyBackgroundTaskEnabled = "sensorberg_sdk_background_task_enabled";
        private const string KeyNewActionsFromBackground = "sensorberg_sdk_new_actions_from_background";
        private const string KeyBackgroundFilterUpdateRequired = "sensorberg_sdk_background_filter_update_required";
        private const string KeyIncrementalId = "sensorberg_sdk_incremental_id";
        private const string KeyAppIsVisible = "sensorberg_sdk_app_visibility";
        private const string KeyVisibilityLastUpdated = "sensorberg_sdk_visibility_last_updated";

        private const int AppVisibilityFallbackDelayInSeconds = 60;
        private const string USERID = "userid";

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private static SDKData _instance;
        public static SDKData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SDKData();
                }

                return _instance;
            }
        }

        public string UserId
        {
            [DebuggerStepThrough]
            get
            {
                object id;
                ApplicationData.Current.LocalSettings.Values.TryGetValue(USERID, out id);
                return id as string;
            }
            [DebuggerStepThrough] set
            {
                string id = value;
                if (!string.IsNullOrEmpty(id))
                {
                    id = Uri.EscapeDataString(id);
                }
                ApplicationData.Current.LocalSettings.Values[USERID] = id;
            }
        }

        public string ApiKey
        {
            [DebuggerStepThrough]
            get
            {
                string apiKey = string.Empty;

                if (_localSettings.Values.ContainsKey(KeySensorbergSdkApiKey))
                {
                    apiKey = _localSettings.Values[KeySensorbergSdkApiKey].ToString();
                }

                return apiKey;
            }
            [DebuggerStepThrough]
            set
            {
                if (!_localSettings.Values.ContainsKey(KeySensorbergSdkApiKey)
                    || !_localSettings.Values[KeySensorbergSdkApiKey].Equals(value))
                {
                    _localSettings.Values[KeySensorbergSdkApiKey] = value;
                }
            }
        }

        public string DeviceId
        {
            [DebuggerStepThrough]
            get
            {
                if (!_localSettings.Values.ContainsKey(KeySensorbergSdkGuid))
                {
                    _localSettings.Values[KeySensorbergSdkGuid] = Guid.NewGuid().ToString();
                }

                return _localSettings.Values[KeySensorbergSdkGuid] + (string.IsNullOrEmpty(UserId) ? string.Empty : "/" + UserId);
            }
        }

        public string LayoutBeaconId1Hash
        {
            [DebuggerStepThrough]
            get
            {
                string hash = string.Empty;

                if (_localSettings.Values.ContainsKey(KeyLayoutBeaconId1Hash))
                {
                    hash = _localSettings.Values[KeyLayoutBeaconId1Hash].ToString();
                }

                return hash;
            }
            [DebuggerStepThrough]
            set
            {
                if (!_localSettings.Values.ContainsKey(KeyLayoutBeaconId1Hash)
                    || !_localSettings.Values[KeyLayoutBeaconId1Hash].Equals(value))
                {
                    _localSettings.Values[KeyLayoutBeaconId1Hash] = value;
                }
            }
        }

        public DateTimeOffset DatabaseCleaningTime
        {
            [DebuggerStepThrough]
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyDatabaseCleaningTime))
                {
                    _localSettings.Values[KeyDatabaseCleaningTime] = DateTimeOffset.Now;
                }

                return (DateTimeOffset)_localSettings.Values[KeyDatabaseCleaningTime];
            }
            [DebuggerStepThrough]
            set
            {
                _localSettings.Values[KeyDatabaseCleaningTime] = value;
            }
        }

        public bool NewActionsFromBackground
        {
            [DebuggerStepThrough]
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyNewActionsFromBackground))
                {
                    _localSettings.Values[KeyNewActionsFromBackground] = false;
                }

                return (bool)_localSettings.Values[KeyNewActionsFromBackground];
            }
            [DebuggerStepThrough]
            set
            {
                if (!_localSettings.Values.ContainsKey(KeyNewActionsFromBackground)
                    || !_localSettings.Values[KeyNewActionsFromBackground].Equals(value))
                {
                    _localSettings.Values[KeyNewActionsFromBackground] = value;
                }
            }
        }

        public bool BackgroundTaskEnabled
        {
            [DebuggerStepThrough]
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyBackgroundTaskEnabled))
                {
                    _localSettings.Values[KeyBackgroundTaskEnabled] = false;
                }

                return (bool)_localSettings.Values[KeyBackgroundTaskEnabled];
            }
            [DebuggerStepThrough]
            set
            {
                if (!_localSettings.Values.ContainsKey(KeyBackgroundTaskEnabled)
                    || !_localSettings.Values[KeyBackgroundTaskEnabled].Equals(value))
                {
                    _localSettings.Values[KeyBackgroundTaskEnabled] = value;
                }
            }
        }

        public bool BackgroundFilterUpdateRequired
        {
            [DebuggerStepThrough]
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyBackgroundFilterUpdateRequired))
                {
                    _localSettings.Values[KeyBackgroundFilterUpdateRequired] = false;
                }

                return (bool)_localSettings.Values[KeyBackgroundFilterUpdateRequired];
            }
            [DebuggerStepThrough]
            set
            {
                if (!_localSettings.Values.ContainsKey(KeyBackgroundFilterUpdateRequired)
                    || !_localSettings.Values[KeyBackgroundFilterUpdateRequired].Equals(value))
                {
                    _localSettings.Values[KeyBackgroundFilterUpdateRequired] = value;
                }
            }
        }

        public bool AppIsVisible
        {
            [DebuggerStepThrough]
            get
            {
                if (_localSettings.Values.ContainsKey(KeyAppIsVisible))
                {
                    return (bool)_localSettings.Values[KeyAppIsVisible];
                }

                return false;
            }
            [DebuggerStepThrough]
            set
            {
                _localSettings.Values[KeyAppIsVisible] = value;
                _localSettings.Values[KeyVisibilityLastUpdated] = DateTimeOffset.Now;
            }
        }

        /// <summary>
        /// Checks, if we should show beacon notifications in the background or not.
        /// </summary>
        /// <returns>True, if we should handle the beacons in the background.</returns>
        public bool ShowNotificationsOnBackground()
        {
            bool showNotificationsOnBackground = false;

            if (_localSettings.Values.ContainsKey(KeyAppIsVisible)
                && _localSettings.Values.ContainsKey(KeyVisibilityLastUpdated))
            {
                // We show beacon notifications on background, when KeyVisibilityUpdateTime value
                // is older than 1 minute or KeyVisibility value is false
                var lastUpdated = (DateTimeOffset)_localSettings.Values[KeyVisibilityLastUpdated];
                var appIsVisible = (bool)_localSettings.Values[KeyAppIsVisible];

                if (!appIsVisible
                    || lastUpdated.AddSeconds(AppVisibilityFallbackDelayInSeconds) < DateTimeOffset.Now)
                {
                    showNotificationsOnBackground = true;
                }
            }
            else
            {
                showNotificationsOnBackground = true;
            }

            return showNotificationsOnBackground;
        }

        /// <summary>
        /// Returns next incremental ID. The ID is global to the application and the counter is not
        /// reset, when the application is restarted.
        /// </summary>
        /// <returns>The next incremental ID.</returns>
        public int NextId()
        {
            if (!_localSettings.Values.ContainsKey(KeyIncrementalId))
            {
                _localSettings.Values[KeyIncrementalId] = 0;
            }

            int id = (int)_localSettings.Values[KeyIncrementalId];

            if (id >= int.MaxValue || id < 0)
            {
                // Start over
                id = 0;
            }

            _localSettings.Values[KeyIncrementalId] = id + 1;
            return id;
        }
    }
}
