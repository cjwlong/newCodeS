using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SharedResource.convertors
{
    public class DeviceStatusToEnableConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceStatus ds)
            {
                return ds switch
                {
                    DeviceStatus.Connecting => false,
                    _ => true,
                };
            }
            else
            {
                if ((bool)value) return false;
                else return true;
            }            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
