using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Rocket.Converter
{
    public class CashinPointTypeToObjectConverter : IValueConverter
    {
        public Dictionary<string, object> Objects { get; set; }
        public object DefaultObject { get; set; }

        public CashinPointTypeToObjectConverter()
        {
            Objects = new Dictionary<string, object>();
        }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
		    if (!(value is string)) return DefaultObject;

            return (Objects.ContainsKey(value as string)) 
                ? Objects[value as string]
                : DefaultObject;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
