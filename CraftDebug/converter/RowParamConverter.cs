using CraftDebug.libs;
using CraftDebug.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace CraftDebug.converter
{
    public class RowParamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DataGridRow row && parameter is string colStr && int.TryParse(colStr, out int col))
            {
                return new MeasurementButtonParam
                {
                    RowIndex = row.GetIndex(),
                    ColumnIndex = col
                };
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

}
