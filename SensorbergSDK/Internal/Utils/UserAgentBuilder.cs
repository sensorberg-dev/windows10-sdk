using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal.Utils
{
    internal static class UserAgentBuilder
    {
        public static string BuildUserAgentJson()
        {
            try
            {
                string osInfo = $"windows/{SystemInfoHelper.SystemVersion}/{SystemInfoHelper.DeviceManufacturer}/{SystemInfoHelper.DeviceModel}";
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
            catch (Exception e)
            {
                return string.Empty;
            }
        }
    }
}
