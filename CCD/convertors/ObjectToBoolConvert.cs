using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CCD.convertors
{
    public class ObjectToBoolConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                if (parameter == null)
                    return intValue > 0;
                else if (int.TryParse((string)parameter, out int num))
                {
                    if (num < 0)
                        return intValue > 1;
                    return num == intValue;
                }
            }
            // 其他类型的值或值为 null 时返回 false
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
