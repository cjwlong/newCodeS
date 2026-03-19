using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DeviceMegManager.convertors
{
    internal class DeviceStatus2colorConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceStatus result)
            {
                return result switch
                {
                    DeviceStatus.Disconnected => "Transparent",
                    DeviceStatus.Connecting => "#f9c11f",
                    _ => "#289947",
                };
            }
            else if (value is CameraState ds)
            {
                return ds switch
                {
                    CameraState.Disconnected => "Transparent",
                    _ => "#289947",
                };
            }
            else
            {
                string re = "";
                switch (value)
                {
                    case true:
                        re = "#289947";
                        break;
                    case false:
                        re = "Transparent";
                        break;
                }
                return re;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
