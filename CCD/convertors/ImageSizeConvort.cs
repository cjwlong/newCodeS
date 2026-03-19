using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CCD.convertors
{
    public class ImageSizeConvort : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return Binding.DoNothing;

            if (!double.TryParse(values[0].ToString(), out double operand1) || !double.TryParse(values[1].ToString(), out double operand2))
                return Binding.DoNothing;

            return operand1 * operand2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
