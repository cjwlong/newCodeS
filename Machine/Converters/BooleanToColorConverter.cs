using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Machine.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool?)value;
            switch (v)
            {
                case true: return new SolidColorBrush(new Color() { R = 66, G = 191, B = 95, A = 255 });
                case false: return new SolidColorBrush(new Color() { R = 167, G = 169, B = 171, A = 255 });
                default: return new SolidColorBrush(new Color() { R = 235, G = 183, B = 55, A = 255 });
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = ((SolidColorBrush)value).Color; // 获取颜色
            if (v == Colors.Red)
                return false;
            else if (v == Colors.Green)
                return true;
            else return null;
        }
    }
}
