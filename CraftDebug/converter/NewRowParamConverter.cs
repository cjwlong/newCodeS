using CraftDebug.libs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CraftDebug.converter
{
    public class NewRowParamConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null)
                return DependencyProperty.UnsetValue;

            var dataItem = values[0]; // 当前行的数据项
            var columnIndexParam = values[1]; // 列索引参数

            // 解析列索引
            if (!int.TryParse(columnIndexParam.ToString(), out int columnIndex))
                return DependencyProperty.UnsetValue;

            // 返回封装后的参数
            return new MeasurementButtonParam
            {
                RowIndex = (int)dataItem,
                ColumnIndex = columnIndex
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
