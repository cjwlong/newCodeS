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
    internal class LimitStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (LimitStatus)value;
            switch (v)
            {
                case LimitStatus.Limited: return new SolidColorBrush(new Color() { R =223, G= 72, B=83, A = 255 });
                case LimitStatus.SoftLimited: return new SolidColorBrush(new Color() { R =235, G= 183, B=55, A = 255 });
                case LimitStatus.Normal: return new SolidColorBrush(new Color() { R = 66, G = 191, B = 95, A = 255 });
                default: return new SolidColorBrush(new Color() { R = 167, G = 169, B = 171, A = 255 });
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = ((SolidColorBrush)value).Color; // 获取颜色
            if (v == Colors.Red)
                return LimitStatus.Limited;
            else if (v == Colors.Yellow)
                return LimitStatus.SoftLimited;
            else if (v == Colors.Green)
                return LimitStatus.Normal;
            else return null;
        }
    }
}
