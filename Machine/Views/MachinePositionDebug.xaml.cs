using Machine.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// MachinePositionDebug.xaml 的交互逻辑
    /// </summary>
    public partial class MachinePositionDebug : UserControl
    {
        public MachinePositionDebug()
        {
            InitializeComponent();
        }
        private void MarkInterval_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is MachinePositionDebugViewModel vm)
            {
                vm.CalculateMarkEnd();
            }
        }


        private void Mark_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is MachinePositionDebugViewModel vm)
            {
                vm.CalculateMarkCount();
            }
        }
    }
}
