// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using Windows.Storage;

namespace SensorbergSDK.Data
{
    /// <summary>
    /// Contains the global SDK data.
    /// </summary>
    public sealed class SdkData
    {
        private const string KeySensorbergSdkApiKey = "sensorberg_sdk_api_key";
        private const string KeySensorbergSdkGuid = "sensorberg_sdk_guid";
        private const string KeyLayoutBeaconId1Hash = "sensorberg_sdk_layout_uuid_hash";
        private const string KeyDatabaseCleaningTime = "sensorberg_sdk_database_cleaning_time";
        private const string KeyBackgroundTaskEnabled = "sensorberg_sdk_background_task_enabled";
        private const string KeyBackgroundFilterUpdateRequired = "sensorberg_sdk_background_filter_update_required";
        public const string KeyIncrementalId = "sensorberg_sdk_incremental_id";
        private const string KeyAppIsVisible = "sensorberg_sdk_app_visibility";
        private const string KeyVisibilityLastUpdated = "sensorberg_sdk_visibility_last_updated";

        private const string Userid = "userid";

        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private static SdkData _instance;
        public static SdkData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SdkData();
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
                ApplicationData.Current.LocalSettings.Values.TryGetValue(Userid, out id);
                string s = id as string;
                if (s == null)
                {
                    s = string.Empty;
                }
                return s;
            }
            [DebuggerStepThrough] set
            {
                string id = value;
                id = !string.IsNullOrEmpty(id) ? Uri.EscapeDataString(id) : string.Empty;
                ApplicationData.Current.LocalSettings.Values[Userid] = id;
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
