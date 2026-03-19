using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace SharedResource.userControls
{
    public class EnhancedUserControl : UserControl
    {
        /// <summary>
        /// 适用于TextBox和ComboBox的数字检查，正负数都可以
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Double_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox source)
            {
                double temp = 0;
                string now_string = source.Text.Remove(source.SelectionStart, source.SelectionLength).Insert(source.SelectionStart, e.Text);
                if (e.Text == "\n")
                    e.Handled = true;
                if (e.Source is TextBox tb || e.Source is ComboBox cb)
                {
                    e.Handled = false;
                    if (now_string == "-" || now_string == ".")
                        e.Handled = false;
                    else if (!double.TryParse(now_string, out temp))
                        e.Handled = true;
                }
                else
                {
                    e.Handled = false;
                }
            }
        }
        /// <summary>
        /// 适用于TextBox和ComboBox的数字检查，正数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PosDouble_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double temp = 0;
            TextBox source = (TextBox)e.OriginalSource;
            string now_string = source.Text.Remove(source.SelectionStart, source.SelectionLength).Insert(source.SelectionStart, e.Text);
            if (e.Text == "\n")
                e.Handled = true;
            if (e.Source is TextBox tb || e.Source is ComboBox cb)
            {
                e.Handled = false;
                if (now_string.Contains('-'))
                    e.Handled = true;
                if (now_string == ".")
                    e.Handled = false;
                else if (!double.TryParse(now_string, out temp))
                    e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
        /// <summary>
        /// 适用于TextBox和ComboBox的数字检查，正数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PosInteger_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int temp = 0;
            TextBox source = (TextBox)e.OriginalSource;
            string now_string = source.Text.Remove(source.SelectionStart, source.SelectionLength).Insert(source.SelectionStart, e.Text);
            if (e.Text == "\n")
                e.Handled = true;
            if (e.Source is TextBox tb || e.Source is ComboBox cb)
            {
                e.Handled = false;
                if (now_string.Contains('-') || now_string.Contains('.'))
                    e.Handled = true;
                else if (!int.TryParse(now_string, out temp))
                    e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
        /// <summary>
        /// 适用于TextBox和ComboBox的数字检查，正负数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int temp = 0;
            TextBox source = (TextBox)e.OriginalSource;
            string now_string = source.Text.Remove(source.SelectionStart, source.SelectionLength).Insert(source.SelectionStart, e.Text);
            if (e.Text == "\n")
                e.Handled = true;
            if (e.Source is TextBox tb || e.Source is ComboBox cb)
            {
                e.Handled = false;
                if (now_string.Contains('.'))
                    e.Handled = true;
                if (now_string == "-")
                    e.Handled= false;
                else if (!int.TryParse(now_string, out temp))
                    e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        public void IPAddress_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox source)
            {
                // 获取当前文本框的内容，并插入新输入的字符
                string now_string = source.Text.Remove(source.SelectionStart, source.SelectionLength).Insert(source.SelectionStart, e.Text);

                // 如果输入的是换行符，则忽略
                if (e.Text == "\n")
                {
                    e.Handled = true;
                    return;
                }

                // 检查输入的文本是否符合IP地址的格式
                e.Handled = !IsValidIPAddress(now_string);
            }
        }

        private bool IsValidIPAddress(string input)
        {
            // IP地址的正则表达式
            string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){0,3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?){0,1}$";
            //string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){0,3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.?)$";//末尾允许输入点
            Regex regex = new Regex(pattern);

            // 完全匹配
            if (regex.IsMatch(input))
            {
                return true;
            }

            // 部分匹配，检查是否可以形成有效的IP地址
            string[] parts = input.Split('.');
            if (parts.Length > 4)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Length == 0) // 不能有空部分
                {
                    return false;
                }

                // 使用int.TryParse解析部分字符串
                if (!int.TryParse(part, out int number))
                {
                    return false; // 如果无法解析，返回false
                }

                if (number < 0 || number > 255) // 检查是否在有效范围内
                {
                    return false;
                }

                if (part.Length > 1 && part[0] == '0') // 检查前导零
                {
                    return false; // 不允许前导零
                }
            }

            return true; // 所有部分都有效
        }

        public void NumericInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox source)
            {
                // 获取当前文本框的内容
                string currentText = source.Text;
                int selectionStart = source.SelectionStart;
                int selectionLength = source.SelectionLength;

                // 构建新的字符串，模拟用户输入后的内容
                string newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, e.Text);

                // 检查输入是否为数字或小数点
                if (!e.Text.All(c => char.IsDigit(c) || c == '.'))
                {
                    e.Handled = true; // 阻止输入
                }
            }
        }
    }
}
