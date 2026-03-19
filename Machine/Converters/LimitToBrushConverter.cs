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
    public class LimitToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LimitStatus status)
            {
                return status switch
                {
                    LimitStatus.NotEnabled => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDBDBD")), // 灰色
                    LimitStatus.Normal => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#239443")),    // 绿色
                    LimitStatus.Limited => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E63F32")),    // 红色
                    LimitStatus.SoftLimited => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCC31F")), // 黄色
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDBDBD")) // 默认灰色
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
