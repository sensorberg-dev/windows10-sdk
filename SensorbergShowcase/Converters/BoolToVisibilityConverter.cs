using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace SensorbergShowcase
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            Visibility visibility = valueAsBool ? Visibility.Visible : Visibility.Collapsed;

            if (parameter != null && parameter is string && (parameter as string).Equals("Inverse"))
            {
                visibility = valueAsBool ? Visibility.Collapsed : Visibility.Visible;
            }

            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
