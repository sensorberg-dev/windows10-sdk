// Created by Kay Czarnotta on 19.08.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using SensorbergSDK;
using SensorbergSDK.Internal.Transport;

namespace TrackAndTraceApp.Converters
{
    public class BeaconActionToColorConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            BeaconAction ra = value as BeaconAction;
            if (ra != null)
            {
                if (ra.Payload != null && ra.Payload.ContainsKey("color"))
                {
                    string colorName = ra.Payload.GetNamedString("color")?.ToLowerInvariant();
                    switch (colorName)
                    {
                        case "green":
                        {
                            return new SolidColorBrush(Colors.LightGreen);
                        }
                        case "red":
                        {
                            return new SolidColorBrush(Colors.Red);
                        }
                        case "yellow":
                        {
                            return new SolidColorBrush(Colors.Yellow);
                        }
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}