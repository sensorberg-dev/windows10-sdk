using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SensorbergShowcase
{
	public class RangeToImageUriConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			string uri = null;

			if (value is int)
			{
				int range = (int)value;
				uri = "/Assets/Graphics/range" + range.ToString() + ".png";
			}

			return uri;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
