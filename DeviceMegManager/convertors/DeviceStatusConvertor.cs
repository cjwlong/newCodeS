using DeviceMegManager.Views;
using SharedResource.enums;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DeviceMegManager.convertors
{
    internal class DeviceStatusConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceStatus result)
            {
                return result switch
                {
                    DeviceStatus.Disconnected => "连接",
                    _ => "断开",
                };
            }
            else if (value is CameraState ds)
            {
                return ds switch
                {
                    CameraState.Disconnected => "连接",
                    _ => "断开",
                };
            }
            else
            {
                string re = "";
                switch (value)
                {
                    case true:
                        re = "断开";
                        break;
                    case false:
                        re = "连接";
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
