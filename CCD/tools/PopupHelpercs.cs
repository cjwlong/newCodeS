using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CCD.tools
{
    public class PopupHelpercs
    {
        public static void ShowPopup(string message)
        {

            // 弹出弹窗（这里假设使用MessageBox作为弹窗）
            MessageBox.Show(message);

        }

        public static bool ShowConfirmationDialog(string message, string title = "提示")
        {
            MessageBoxResult result = MessageBox.Show(message, title, MessageBoxButton.YesNo);

            // 根据用户的选择返回相应的结果
            return result == MessageBoxResult.Yes;   // 如果用户点击了确定按钮，则返回true，否则返回false

        }
    }
}
