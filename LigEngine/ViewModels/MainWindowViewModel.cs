using HelixToolkit.Wpf.SharpDX;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace LigEngine.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<ChangeLogAndDebugPageEvent>().Subscribe((b) =>
            {
                if (!(bool)b)
                {
                    IsLogDisplay = Visibility.Visible;
                    IsDebugDisplay = Visibility.Collapsed;
                }
                else
                {
                    IsLogDisplay = Visibility.Collapsed;
                    IsDebugDisplay = Visibility.Visible;
                }
            });

            InitView();
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;
        public string Title { get; } = "光擎" + System.Windows.Application.ResourceAssembly.GetName().Version.ToString();

        private PPage _currentPage = PPage.CameraPage;
        public PPage CurrentPage
        {
            get => _currentPage;
            set
            {
                SetProperty(ref _currentPage, value);
            }
        }

        private Visibility _isLogDisplay = Visibility.Visible;
        public Visibility IsLogDisplay
        {
            get => _isLogDisplay;
            set => SetProperty(ref _isLogDisplay, value);
        }

        private Visibility _isDebugDisplay = Visibility.Collapsed;
        public Visibility IsDebugDisplay
        {
            get => _isDebugDisplay;
            set => SetProperty(ref _isDebugDisplay, value);
        }

        private DelegateCommand<string> _switch2PageCommand;
        public DelegateCommand<string> Switch2PageCommand =>
            _switch2PageCommand ?? (_switch2PageCommand = new DelegateCommand<string>(ExecuteSwitch2Page));

        private void ExecuteSwitch2Page(string page)
        {
            switch (page)
            {
                case "HomePage":
                    CurrentPage = PPage.HomePage;
                    break;
                case "CameraPage":
                    CurrentPage = PPage.CameraPage;
                    break;
                case "CraftConfigPage":
                    CurrentPage = PPage.CraftConfigPage;
                    break;
                case "SettingPage":
                    CurrentPage = PPage.SettingPage;
                    break;
                case "DebugPage":
                    CurrentPage = PPage.DebugPage;
                    break; 
                default:
                    MessageBox.Show("页面跳转异常，未知错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private async void InitView()
        {
            await Task.Delay(10);
            CurrentPage = PPage.HomePage;
        }
    }
}
