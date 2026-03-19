using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Reflection.Metadata.Ecma335;

namespace Machine.Converters
{
    internal class InfiniteToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is double doublevalue && doublevalue >= 99999999999) { return "+inf"; }
            if(value is double doublevalue1 && doublevalue1 <= -99999999999) { return "-inf"; }
            if(value is double doublevalue2 && Math.Abs(doublevalue2) >= 10000)
            {
                return doublevalue2.ToString("0.####e+0");
            }
            string result = string.Format("{0:N4}", value);
            return result;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}