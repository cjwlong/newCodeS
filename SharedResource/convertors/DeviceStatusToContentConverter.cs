using Prism.Events;
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
    public class DeviceStatusToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceStatus ds)
            {
                return ds switch
                {
                    DeviceStatus.Busy => "正忙",
                    DeviceStatus.Connecting => "连接中",
                    DeviceStatus.Idle => "已连接",
                    DeviceStatus.Disconnected => "连接",
                    DeviceStatus.Error => "错误",
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
