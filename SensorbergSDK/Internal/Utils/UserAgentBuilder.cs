// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.IO;
using System.Runtime.Serialization.Json;
using SensorbergSDK.Data;

namespace SensorbergSDK.Utils
{
    internal static class UserAgentBuilder
    {
        public static string BuildUserAgentJson()
        {
            try
            {
                string osInfo = $"Windows10/{SystemInfoHelper.SystemVersion}/{SystemInfoHelper.DeviceManufacturer}/{SystemInfoHelper.DeviceModel}";
                string sdkInfo = $"{SystemInfoHelper.SdkVersion}";
                string appInfo = $"{SystemInfoHelper.PackageName}/{SystemInfoHelper.ApplicationName}/{SystemInfoHelper.ApplicationVersion}";
                UserAgentModel userAgent = new UserAgentModel(osInfo, sdkInfo, appInfo);
                MemoryStream stream1 = new MemoryStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(UserAgentModel));
                ser.WriteObject(stream1, userAgent);
                stream1.Position = 0;
                StreamReader sr = new StreamReader(stream1);
                var content = sr.ReadToEnd();
                var filtered= content.Replace("\\/","/");
                return filtered;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
