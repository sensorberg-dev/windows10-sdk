// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Runtime.Serialization;

namespace SensorbergSDK.Internal.Data
{
    [DataContract]
    internal class UserAgentModel
    {
        public UserAgentModel(string operatingSystemInfo, string sdkInfo, string applicationInfo)
        {
            OperatingSystemInfo = operatingSystemInfo;
            SdkInfo = sdkInfo;
            ApplicationInfo = applicationInfo;
        }

        [DataMember(Name = "os")]
        public string OperatingSystemInfo { get; private set; }

        [DataMember(Name = "sdk")]
        public string SdkInfo { get; private set; }

        [DataMember(Name = "app")]
        public string ApplicationInfo { get; private set; }

    }
}
