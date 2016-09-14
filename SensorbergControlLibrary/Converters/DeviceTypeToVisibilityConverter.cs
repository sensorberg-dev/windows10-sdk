// Created by Kay Czarnotta on 22.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using TrackAndTraceApp.Utils;

namespace TrackAndTraceApp.Converters
{
    public class DeviceTypeToVisibilityConverter: IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool retValue = false;
            switch (parameter as string)
            {
                case "IoT":
                    retValue = Util.IsIoT;
                    break;
                case "Mobile":
                    retValue = Util.IsMobile;
                    break;
                case "Desktop":
                    retValue = Util.IsDesktop;
                    break;
            }

            return (Invert ? !retValue : retValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}