// Created by Kay Czarnotta on 15.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Text;
using System.Text.RegularExpressions;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace SensorbergSDK.Internal.Utils
{
    public static class Helper
    {
        public static async void BeginInvoke(Action action)
        {
            CoreDispatcher dispatcher = CoreWindow.GetForCurrentThread() != null ? CoreWindow.GetForCurrentThread().Dispatcher : null;
            if (dispatcher == null)
            {
                if (Window.Current != null)
                {
                    dispatcher = Window.Current.Dispatcher;
                }
            }
            if (dispatcher == null)
            {
                dynamic v = Windows.ApplicationModel.Core.CoreApplication.MainView;
                if (v != null)
                {
                    if (v.CoreWindow != null)
                    {
                        dispatcher = v.CoreWindow.Dispatcher;
                    }
                    if (dispatcher == null)
                    {
                        dispatcher = v.Dispatcher;
                    }
                }
            }

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { action(); });
        }
        public static string EnsureEncodingIsUtf8(string str)
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