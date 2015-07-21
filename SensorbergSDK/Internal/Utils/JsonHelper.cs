using System;
using Windows.Data.Json;

namespace SensorbergSDK.Internal
{
    class JsonHelper
    {
        public static int Optional(JsonObject jsonObject, string name, int defaultValue = 0)
        {
            int val = defaultValue;

            try
            {
                if (jsonObject.ContainsKey(name))
                    val = (int)jsonObject.GetNamedValue(name).GetNumber();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("JsonHelper.Optional(): " + e.Message);
            }

            return val;
        }

        public static string OptionalString(JsonObject jsonObject, string name, string defaultValue = "")
        {
            string val = defaultValue;

            try
            {
                if(jsonObject.ContainsKey(name))
                    val = (string)jsonObject.GetNamedValue(name).GetString();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("JsonHelper.OptionalString(): " + e.Message);
            }

            return val;
        }
        public static JsonObject OptionalObject(JsonObject jsonObject, string name)
        {
            JsonObject val = null;

            try
            {
                if (jsonObject.ContainsKey(name))
                    if(jsonObject.GetNamedValue(name).ValueType == JsonValueType.Object)
                        val = jsonObject.GetNamedObject(name);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("JsonHelper.OptionalObject(): " + e.Message);
            }

            return val;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool OptionalBoolean(JsonObject jsonObject, string name, bool defaultValue)
        {
            //System.Diagnostics.Debug.WriteLine("JsonHelper.OptionalBoolean(): " + name);
            bool val = defaultValue;

            try
            {
                if (jsonObject.ContainsKey(name))
                {
                    val = jsonObject.GetNamedBoolean(name);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("JsonHelper.OptionalBoolean(): " + e.Message);
            }

            return val;
        }
    }
}
