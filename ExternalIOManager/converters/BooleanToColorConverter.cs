using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ExternalIOManager.converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colorName)
            {
                if (boolValue)
                {
                    // 状态为true时显示指定颜色
                    return ColorConverter.ConvertFromString(colorName);
                }
                else
                {
                    // 状态为false时显示灰色
                    return "#d2d2d2";
                }
            }

            return "#d2d2d2";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
