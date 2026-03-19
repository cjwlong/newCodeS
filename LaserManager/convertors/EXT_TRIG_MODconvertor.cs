using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LaserManager.convertors
{
    public class EXT_TRIG_MODconvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }
            var v = (string)value;
            if (parameter.Equals("0"))
            {
                if (v.Equals("GATED"))
                {
                    return true;
                }
                return false;
            }
            if (parameter.Equals("1"))
            {
                if (v.Equals("TOD"))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }
            var v = (bool)value;
            if (parameter.Equals("0"))
            {
                if (v)
                {
                    return "GATED";
                }
                return "";
            }
            if (parameter.Equals("1"))
            {
                if (v)
                {
                    return "TOD";
                }
                return " ";
            }
            return " ";
        }
    }
}
