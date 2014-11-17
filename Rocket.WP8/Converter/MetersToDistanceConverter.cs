using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Rocket.Converter
{
	public class MetersToDistanceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
		    if (!(value is double)) return null;

		    var dist = (double)value;

            return (dist < 1000) ? string.Format("{0:0}м", dist) : string.Format("{0:0.#}км", dist / 1000.0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
