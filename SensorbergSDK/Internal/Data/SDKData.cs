// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using Windows.Storage;

namespace SensorbergSDK.Internal.Data
{
    /// <summary>
    /// Persisten storage of the sdk related configuration and settings.
    /// </summary>
    public static class SdkData
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

        public static string UserId
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

        public static string DeviceId
        {
            [DebuggerStepThrough]
            get
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeySensorbergSdkGuid))
                {
                    ApplicationData.Current.LocalSettings.Values[KeySensorbergSdkGuid] = Guid.NewGuid().ToString();
                }

                return ApplicationData.Current.LocalSettings.Values[KeySensorbergSdkGuid] + (string.IsNullOrEmpty(UserId) ? string.Empty : "/" + UserId);
            }
        }

        public static string LayoutBeaconId1Hash
        {
            [DebuggerStepThrough]
            get
            {
                string hash = string.Empty;

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyLayoutBeaconId1Hash))
                {
                    hash = ApplicationData.Current.LocalSettings.Values[KeyLayoutBeaconId1Hash].ToString();
                }

                return hash;
            }
            [DebuggerStepThrough]
            set
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyLayoutBeaconId1Hash)
                    || !ApplicationData.Current.LocalSettings.Values[KeyLayoutBeaconId1Hash].Equals(value))
                {
                    ApplicationData.Current.LocalSettings.Values[KeyLayoutBeaconId1Hash] = value;
                }
            }
        }

        public static DateTimeOffset DatabaseCleaningTime
        {
            [DebuggerStepThrough]
            get
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyDatabaseCleaningTime))
                {
                    ApplicationData.Current.LocalSettings.Values[KeyDatabaseCleaningTime] = DateTimeOffset.Now;
                }

                return (DateTimeOffset)ApplicationData.Current.LocalSettings.Values[KeyDatabaseCleaningTime];
            }
            [DebuggerStepThrough]
            set
            {
                ApplicationData.Current.LocalSettings.Values[KeyDatabaseCleaningTime] = value;
            }
        }

        public static bool BackgroundTaskEnabled
        {
            [DebuggerStepThrough]
            get
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyBackgroundTaskEnabled))
                {
                    ApplicationData.Current.LocalSettings.Values[KeyBackgroundTaskEnabled] = false;
                }

                return (bool)ApplicationData.Current.LocalSettings.Values[KeyBackgroundTaskEnabled];
            }
            [DebuggerStepThrough]
            set
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyBackgroundTaskEnabled)
                    || !ApplicationData.Current.LocalSettings.Values[KeyBackgroundTaskEnabled].Equals(value))
                {
                    ApplicationData.Current.LocalSettings.Values[KeyBackgroundTaskEnabled] = value;
                }
            }
        }

        public static bool BackgroundFilterUpdateRequired
        {
            [DebuggerStepThrough]
            get
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyBackgroundFilterUpdateRequired))
                {
                    ApplicationData.Current.LocalSettings.Values[KeyBackgroundFilterUpdateRequired] = false;
                }

                return (bool)ApplicationData.Current.LocalSettings.Values[KeyBackgroundFilterUpdateRequired];
            }
            [DebuggerStepThrough]
            set
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyBackgroundFilterUpdateRequired)
                    || !ApplicationData.Current.LocalSettings.Values[KeyBackgroundFilterUpdateRequired].Equals(value))
                {
                    ApplicationData.Current.LocalSettings.Values[KeyBackgroundFilterUpdateRequired] = value;
                }
            }
        }

        public static bool AppIsVisible
        {
            [DebuggerStepThrough]
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyAppIsVisible))
                {
                    return (bool)ApplicationData.Current.LocalSettings.Values[KeyAppIsVisible];
                }

                return false;
            }
            [DebuggerStepThrough]
            set
            {
                ApplicationData.Current.LocalSettings.Values[KeyAppIsVisible] = value;
                ApplicationData.Current.LocalSettings.Values[KeyVisibilityLastUpdated] = DateTimeOffset.Now;
            }
        }

        /// <summary>
        /// Returns next incremental ID. The ID is global to the application and the counter is not
        /// reset, when the application is restarted.
        /// </summary>
        /// <returns>The next incremental ID.</returns>
        public static int NextId()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(KeyIncrementalId))
            {
                ApplicationData.Current.LocalSettings.Values[KeyIncrementalId] = 0;
            }

            int id = (int)ApplicationData.Current.LocalSettings.Values[KeyIncrementalId];

            if (id >= int.MaxValue || id < 0)
            {
                // Start over
                id = 0;
            }

            ApplicationData.Current.LocalSettings.Values[KeyIncrementalId] = id + 1;
            return id;
        }
    }
}
