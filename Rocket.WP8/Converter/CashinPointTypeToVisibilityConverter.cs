using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rocket.Converter
{
    public class CashinPointTypeToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
		    if (!(value is string) || !(parameter is string)) return Visibility.Collapsed;

		    return ((value as string).Equals(parameter as string, StringComparison.InvariantCultureIgnoreCase))
		        ? Visibility.Visible
		        : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
