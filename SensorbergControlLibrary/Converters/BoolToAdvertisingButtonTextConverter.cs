// Created by Kay Czarnotta on 05.09.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using Windows.UI.Xaml.Data;

namespace SensorbergControlLibrary.Converters
{
    public class BoolToAdvertisingButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = value is bool && (bool) value;
            return valueAsBool ? "Stop" : "Start";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}