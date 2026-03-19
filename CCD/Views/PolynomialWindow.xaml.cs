using CCD.ViewModels;
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
using System.Windows.Shapes;

namespace CCD.Views
{
    /// <summary>
    /// PolynomialWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PolynomialWindow : Window
    {
        public PolynomialWindow()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CreateReal();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 关闭窗体
            // 如果你是在 MainWindow 窗体下，可以使用 this.Close() 来关闭窗体
            // 如果你是在其他窗体中，可以通过其他方式来关闭窗体
            Close();
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Validate();
        }


        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NextStep())
            {
                FirstGrid.Visibility = Visibility.Collapsed;
                SecondGrid.Visibility = Visibility.Visible;
            }
        }

        private void ReturnBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReturnStep();
            SecondGrid.Visibility = Visibility.Collapsed;
            FirstGrid.Visibility = Visibility.Visible;
        }

        public void InitWindow(Point point, PolynomialWindowViewModel.PointListDelegate listDelegate, PolynomialWindow window)
        {
            ViewModel.MirrorPoint = new();
            ViewModel.CameraPoint = point;
            ViewModel.WidPoly = window;
            ViewModel.listDelegate = listDelegate;
        }
    }
}
