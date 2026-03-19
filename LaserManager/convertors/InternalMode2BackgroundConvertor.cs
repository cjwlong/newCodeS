using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LaserManager.convertors
{
    public class InternalMode2BackgroundConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                if (parameter.ToString() == "int")
                    return "#57a64a";
                else
                    return "#57a64a";
            }

            else if (!(bool)value)
            {
                if (parameter.ToString() == "int")
                    return "#a7a9ab";
                else
                    return "#a7a9ab";
            }
            else 
                return "#ffffff";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
