// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Reflection;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;

namespace SensorbergSDK.Utils
{
    internal static class SystemInfoHelper
    {
        public static string SystemFamily { get; }
        public static string SystemVersion { get; }
        public static string SystemArchitecture { get; }
        public static string ApplicationName { get; }
        public static string ApplicationVersion { get; }
        public static string DeviceManufacturer { get; }
        public static string DeviceModel { get; }
        public static string SystemName { get; }
        public static string SdkVersion { get; }
        public static string PackageName { get; }

        static SystemInfoHelper()
        {
            AnalyticsVersionInfo ai = AnalyticsInfo.VersionInfo;
            SystemFamily = ai.DeviceFamily;

            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = v & 0x000000000000FFFFL;
            SystemVersion = $"{v1}.{v2}.{v3}.{v4}";

            Package package = Package.Current;
            SystemArchitecture = package.Id.Architecture.ToString();
            Assembly sdkAssembly = Assembly.Load(new AssemblyName("SensorbergSDK"));
            var version  = sdkAssembly.GetName().Version;
            SdkVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            ApplicationName = package.DisplayName;
            PackageName = package.Id.Name;
            PackageVersion pv = package.Id.Version;
            ApplicationVersion = $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            EasClientDeviceInformation eas = new EasClientDeviceInformation();
            DeviceManufacturer = eas.SystemManufacturer;
            DeviceModel = eas.SystemProductName;
            SystemName = eas.OperatingSystem;
        }
    }
}
