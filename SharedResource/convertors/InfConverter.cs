using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SharedResource.convertors
{
    public class InfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "inf";

            if (value is double doubleValue && double.IsInfinity(doubleValue))
                return "inf";

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue) || stringValue.Equals("inf", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (double.TryParse(stringValue, out double doubleValue))
                    return doubleValue;
            }

            return null;
        }
    }
}
