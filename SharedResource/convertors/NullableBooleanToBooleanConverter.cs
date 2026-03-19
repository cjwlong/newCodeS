using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SharedResource.convertors
{
    public class NullableBooleanToBooleanConverter : IValueConverter
    {
        ///当界面的绑定到DataContext中的属性发生变化时，会调用该方法，将绑定的bool值转换为界面需要的Visibility类型的值
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            return (string)parameter == "reverse" ? !(bool)value : (bool)value;
        }

        ///当界面的Visibility值发生变化时，会调用该方法，将Visibility类型的值转换为bool值返回给绑定到DataContext中的属性
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true;
            return (bool)value;
        }
    }
}
