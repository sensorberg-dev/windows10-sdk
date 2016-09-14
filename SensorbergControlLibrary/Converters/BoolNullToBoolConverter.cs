// Created by Kay Czarnotta on 18.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using Windows.UI.Xaml.Data;

namespace SensorbergControlLibrary.Converters
{
    public class BoolNullToBoolConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var b = value as bool?;
            if (b != null)
            {
                return b.Value;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}