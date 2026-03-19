using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MenuControl.convertors
{
    internal class Status2ResetEnableConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            switch ((G_ProcessStatus)value)
            {
                case G_ProcessStatus.UnReset:
                    result = true;
                    break;
                case G_ProcessStatus.Reseted:
                    result = true;
                    break;
                case G_ProcessStatus.Processing:
                    result = false;
                    break;
                case G_ProcessStatus.Pause:
                    result = false;
                    break;
                case G_ProcessStatus.Finished:
                    result = true;
                    break;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
