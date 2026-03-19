using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SharedResource.convertors
{
    public class Laserstatus2BtnStatusConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                if (parameter.ToString() == "connect")
                    return "已连接";
                else if (parameter.ToString() == "connecting")
                    return false;
                else return Visibility.Visible;
            }
            else
            {
                if (parameter.ToString() == "connect")
                    return "连接";
                else if (parameter.ToString() == "connecting")
                    return true;
                else return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
