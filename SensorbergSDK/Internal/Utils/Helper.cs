// Created by Kay Czarnotta on 15.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Text;
using System.Text.RegularExpressions;

namespace SensorbergSDK.Internal.Utils
{
    public static class Helper
    {
        public static string EnsureEncodingIsUTF8(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static string StripLineBreaksAndExcessWhitespaces(string str)
        {
            string stripped = str.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
            stripped = Regex.Replace(stripped, @" +", " ");
            stripped = stripped.Trim();
            return stripped;
        }
    }
}