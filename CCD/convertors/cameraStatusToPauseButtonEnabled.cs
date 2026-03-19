using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CCD.convertors
{
    public class CameraStatusToPauseButtonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查相机状态是否未连接
            if (value is CameraState status && status == CameraState.Disconnected)
            {
                return false; // 返回 false 表示按钮不可用
            }

            return true; // 默认情况下按钮可用
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
