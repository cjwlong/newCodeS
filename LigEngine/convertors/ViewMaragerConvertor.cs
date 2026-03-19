using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LigEngine.convertors
{
    internal class ViewMaragerConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PPage currentPage && parameter is string gridName)
            {
                switch (gridName)
                {
                    case "HomePage":
                        return currentPage == PPage.HomePage ? Visibility.Visible : Visibility.Collapsed;
                    case "CameraPage":
                        return currentPage == PPage.CameraPage ? Visibility.Visible : Visibility.Collapsed;
                    case "CraftConfigPage":
                        return currentPage == PPage.CraftConfigPage ? Visibility.Visible : Visibility.Collapsed;
                    case "SettingPage":
                        return currentPage == PPage.SettingPage ? Visibility.Visible : Visibility.Collapsed;
                    case "DebugPage":
                        return currentPage == PPage.DebugPage ? Visibility.Visible : Visibility.Collapsed;
                    default:
                        return Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
