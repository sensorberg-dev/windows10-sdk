using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorbergSDK.Internal
{
    internal static class Constants
    {
        public const string XApiKey = "X-Api-Key"; // Application api key / required
        public const string Xiid = "X-iid"; // Application installation id assigned by SDK / required
        public const string Xpid = "X-pid"; // Request layout with beacon pid
        public const string Xgeo = "X-geo"; // Request layout for given geo location
        public const string Xqos = "X-qos"; // Connection type
        public const string XUserAgent = "User-Agent"; // user agent
        public const string AuthorizationHeader = "Authorization"; // authorization token

        public const string DemoApiKey = "04a709a208c83e2bc0ec66871c46d35af49efde5151032b3e865768bbf878db8";

        public static readonly string LayoutApiUriAsString = "https://resolver.sensorberg.com/layout";
        public static readonly string ApiUrlTemplate = "https://connect.sensorberg.com/api/beacon/resolve/?proximityId={0}&major={1}&minor={2}&event={3}&deviceId={4}";
        public static readonly string FilterUrlTemplate = "https://connect.sensorberg.com/api/application/{0}/uuids";
        public static readonly string SettingsUri = "https://resolver.sensorberg.com/applications/{0}/settings/windows/";

        public static readonly string SensorbergUuidSpace = "7367672374000000ffff0000ffff00";

        public const int ActionTypeUrlMessage = 1;
        public const int ActionTypeVisitWebsite = 2;
        public const int ActionTypeInApp = 3;

        public const int Id1LengthWithoutDashes = 32;
        public const int MinimumLayoutContentLength = 10; // Arbitrary value to make sure that empty layouts are not validated

        public const int BeaconsListRefreshIntervalInMilliseconds = 1000;

        /// <summary>
        /// Default values for api settings. All time values are in miliseconds.
        /// </summary>
        public const UInt64 DefaultSettingsUpdateInterval = 86400000;
        public const UInt64 DefaultBeaconExitTimeout = 10000;
        public const UInt64 DefaultHistoryUploadInterval = 900000;
        public const UInt64 DefaultLayoutUpdateInterval = 3600000;
        public const int DefaultBackgroundScannerEnterThreshold = -120;

    }
}
