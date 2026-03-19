using Machine.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Machine.Views
{
    /// <summary>
    /// MachineStatus.xaml 的交互逻辑
    /// </summary>
    public partial class MachineStatus : UserControl
    {
        public MachineStatus()
        {
            InitializeComponent();
        }

        private void TextBlock_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (DataContext is MachineStatusViewModel MachineStatusVM && !MachineStatusVM.MachineVM.IsBusy)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    e.Handled = true;
                    var a = System.Windows.Media.VisualTreeHelper.GetParent(System.Windows.Media.VisualTreeHelper.GetParent(sender as UIElement));
                    if (a is Grid grid)
                    {
                        foreach (var ch in grid.Children)
                        {
                            if (ch is Button btn && btn.Name == "EnableButton")
                            {
                                var axis_name = btn.Content as string;
                                var axis = MachineStatusVM.MachineVM.GetAxisByName(axis_name);
                                if (axis.IsMoving != false || System.Math.Abs(axis.RelTarget) > 2) return;
                                axis.ToDisplayPoint(axis.RelTarget * e.Delta / 120, isAbsolute: false);
                                return;
                            }
                            if (ch is Button Btn && Btn.Name == "FocusAxisName")
                            {
                                var axis_name = Btn.Content as string;
                                var axis = MachineStatusVM.MachineVM.GetAxisByName(axis_name);
                                MachineStatusVM.MachineVM.FocusControlCommand.Execute($"{axis_name}{(e.Delta > 0 ? '+' : '-')}");
                                return;
                            }
                        }
                    }
                    MachineStatusVM.MachineVM.StopAll();
                }
            }
        }

        private void ComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNonNegativeNumberInput(e.Text);
        }

        private bool IsNonNegativeNumberInput(string input)
        {
            return Regex.IsMatch(input, @"^\d*\.?\d*$");  // 允许整数和小数，不允许负号
        }
    }
}
