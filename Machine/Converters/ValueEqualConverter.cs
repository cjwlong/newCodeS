using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Converters
{
    public class ValueEqualConverter : IValueConverter
    {
        ///当界面的绑定到DataContext中的属性发生变化时，会调用该方法，将绑定的bool值转换为界面需要的Visibility类型的值
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null && parameter != null)
                {
                    // 尝试将参数转换为与绑定值相同的类型
                    var convertedParameter = System.Convert.ChangeType(parameter, value.GetType());
                    return EqualityComparer<object>.Default.Equals(value, convertedParameter);
                }

            }
            catch { }

            return false;
        }

        ///当界面的Visibility值发生变化时，会调用该方法，将Visibility类型的值转换为bool值返回给绑定到DataContext中的属性
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
