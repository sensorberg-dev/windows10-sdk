using System;
using Windows.UI.Xaml.Data;

namespace SensorbergShowcase
{
    public class BoolToScannerStateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            return valueAsBool ? "Scanning..." : "Scanner is stopped";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToAdvertisingButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            return valueAsBool ? "stop advertising" : "start advertising";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBackgroundImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            return valueAsBool ? "Assets/Graphics/BigBackground.jpg" : "Assets/Graphics/Background.jpg";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
