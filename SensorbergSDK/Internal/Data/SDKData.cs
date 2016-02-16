using System;
using Windows.Storage;

namespace SensorbergSDK.Internal
{
    internal class Constants
    {
        public const string XApiKey = "X-Api-Key"; // Application api key / required
        public const string Xiid = "X-iid"; // Application installation id assigned by SDK / required
        public const string Xpid = "X-pid"; // Request layout with beacon pid
        public const string Xgeo = "X-geo"; // Request layout for given geo location
        public const string Xqos = "X-qos"; // Connection type

        public const string DemoApiKey = "04a709a208c83e2bc0ec66871c46d35af49efde5151032b3e865768bbf878db8";

        public static readonly string LayoutApiUriAsString = "https://resolver.sensorberg.com/layout";
        public static readonly string ApiUrlTemplate = "https://connect.sensorberg.com/api/beacon/resolve/?proximityId={0}&major={1}&minor={2}&event={3}&deviceId={4}";
        public static readonly string FilterUrlTemplate = "https://connect.sensorberg.com/api/application/{0}/uuids";

        public static readonly string SensorbergUuidSpace = "7367672374000000ffff0000ffff00";

        public const int ActionTypeUrlMessage = 1;
        public const int ActionTypeVisitWebsite = 2;
        public const int ActionTypeInApp = 3;

        public const int Id1LengthWithoutDashes = 32;
        public const int MinimumLayoutContentLength = 10; // Arbitrary value to make sure that empty layouts are not validated
        public const int BeaconExitDelayInMilliseconds = 1000;
    }

    /// <summary>
    /// Contains the global SDK data.
    /// </summary>
    public sealed class SDKData
    {
        private const string KeySensorbergSdkApiKey = "sensorberg_sdk_api_key";
        private const string KeySensorbergSdkGuid = "sensorberg_sdk_guid";
        private const string KeyLayoutBeaconId1Hash = "sensorberg_sdk_layout_uuid_hash";
        private const string KeySensorbergSdkReportInterval = "sensorberg_sdk_report_interval";
        private const string KeyDatabaseCleaningTime = "sensorberg_sdk_database_cleaning_time";
        private const string KeyBackgroundTaskEnabled = "sensorberg_sdk_background_task_enabled";
        private const string KeyNewActionsFromBackground = "sensorberg_sdk_new_actions_from_background";
        private const string KeyBackgroundFilterUpdateRequired = "sensorberg_sdk_background_filter_update_required";
        private const string KeyIncrementalId = "sensorberg_sdk_incremental_id";
        private const string KeyAppIsVisible = "sensorberg_sdk_app_visibility";
        private const string KeyVisibilityLastUpdated = "sensorberg_sdk_visibility_last_updated";

        private const int DefaultReportIntervalInSeconds = 900; //15 minutes
        private const int AppVisibilityFallbackDelayInSeconds = 60;

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

        public string ApiKey
        {
            get
            {
                string apiKey = string.Empty;

                if (_localSettings.Values.ContainsKey(KeySensorbergSdkApiKey))
                {
                    apiKey = _localSettings.Values[KeySensorbergSdkApiKey].ToString();
                }

                return apiKey;
            }
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
            get
            {
                if (!_localSettings.Values.ContainsKey(KeySensorbergSdkGuid))
                {
                    _localSettings.Values[KeySensorbergSdkGuid] = Guid.NewGuid().ToString();
                }

                return _localSettings.Values[KeySensorbergSdkGuid].ToString();
            }
        }

        public string LayoutBeaconId1Hash
        {
            get
            {
                string hash = string.Empty;

                if (_localSettings.Values.ContainsKey(KeyLayoutBeaconId1Hash))
                {
                    hash = _localSettings.Values[KeyLayoutBeaconId1Hash].ToString();
                }

                return hash;
            }
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
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyDatabaseCleaningTime))
                {
                    _localSettings.Values[KeyDatabaseCleaningTime] = DateTimeOffset.Now;
                }

                return (DateTimeOffset)_localSettings.Values[KeyDatabaseCleaningTime];
            }
            set
            {
                _localSettings.Values[KeyDatabaseCleaningTime] = value;
            }
        }

        public int ReportIntervalInSeconds
        {
            get
            {
                if (!_localSettings.Values.ContainsKey(KeySensorbergSdkReportInterval))
                {
                    _localSettings.Values[KeySensorbergSdkReportInterval] = DefaultReportIntervalInSeconds;
                }

                return (int)_localSettings.Values[KeySensorbergSdkReportInterval];
            }
            set
            {
                if (value > 0
                    && (!_localSettings.Values.ContainsKey(KeySensorbergSdkReportInterval)
                        || (int)_localSettings.Values[KeySensorbergSdkReportInterval] != value))
                {
                    _localSettings.Values[KeySensorbergSdkReportInterval] = value;
                }
            }
        }

        public bool NewActionsFromBackground
        {
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyNewActionsFromBackground))
                {
                    _localSettings.Values[KeyNewActionsFromBackground] = false;
                }

                return (bool)_localSettings.Values[KeyNewActionsFromBackground];
            }
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
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyBackgroundTaskEnabled))
                {
                    _localSettings.Values[KeyBackgroundTaskEnabled] = false;
                }

                return (bool)_localSettings.Values[KeyBackgroundTaskEnabled];
            }
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
            get
            {
                if (!_localSettings.Values.ContainsKey(KeyBackgroundFilterUpdateRequired))
                {
                    _localSettings.Values[KeyBackgroundFilterUpdateRequired] = false;
                }

                return (bool)_localSettings.Values[KeyBackgroundFilterUpdateRequired];
            }
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
            get
            {
                if (_localSettings.Values.ContainsKey(KeyAppIsVisible))
                {
                    return (bool)_localSettings.Values[KeyAppIsVisible];
                }

                return false;
            }
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
