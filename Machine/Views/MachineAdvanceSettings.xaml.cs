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
    /// MachineAdvanceSettings.xaml 的交互逻辑
    /// </summary>
    public partial class MachineAdvanceSettings : UserControl
    {
        public MachineAdvanceSettings()
        {
            InitializeComponent();
        }
        

        private void TextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    (this.DataContext as MachineAdvanceSettingsViewModel).MachineVM.SaveConfig();
                });
            });
        }

    }
}
