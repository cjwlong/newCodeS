using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Machine.Converters
{
    public class AxisName2LimitEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEnable = false;

            if (value.ToString() == "X" ||
                value.ToString() == "Y" ||
                value.ToString() == "Z" ||
                value.ToString() == "A")
            {
                isEnable = true;
            }
            else isEnable = false;

            return isEnable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
