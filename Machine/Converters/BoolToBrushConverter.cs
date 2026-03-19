using Machine.Enums;
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
    internal class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool status)
            {
                return status switch
                {
                    true => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#239443")),    // 绿色
                    false => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCC31F")), // 黄色
                };
            }

            // 非预期类型返回灰色
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDBDBD"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
