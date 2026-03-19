using Prism.Ioc;
using Prism.Services.Dialogs;
using System.Windows;
using System.Windows.Input;

namespace LigEngine.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IContainerProvider provider)
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            };
            containerProvider = provider;
            dialogService = containerProvider.Resolve<DialogService>();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 允许拖动窗口
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                return;

            DragMove();
        }

        IContainerProvider containerProvider;
        IDialogService dialogService;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var meg = "是否退出？";
            dialogService.ShowDialog("ConfirmBox", new DialogParameters($"message={meg}"), r =>
            {
                if (ButtonResult.OK == r.Result)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    e.Cancel = true;
                }
            });
        }
    }
}
