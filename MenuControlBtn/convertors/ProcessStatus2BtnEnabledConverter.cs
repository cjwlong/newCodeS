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
    internal class ProcessStatus2BtnEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            if (!Enum.TryParse(value.ToString(), out G_ProcessStatus status))
                return false;

            string operation = parameter.ToString().ToLower();

            switch (status)
            {
                case G_ProcessStatus.UnReset: // 未复位
                case G_ProcessStatus.Finished: // 已完成
                    return false; // 所有按钮都禁用

                case G_ProcessStatus.Reseted: // 已复位
                    return operation == "start"; // 只启用开始按钮

                case G_ProcessStatus.Processing: // 加工中
                    return operation != "start"; // 开始按钮禁用，其他启用

                case G_ProcessStatus.Pause: // 暂停
                    return operation == "start" || operation == "stop"; // 只启用开始和停止按钮

                default:
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
