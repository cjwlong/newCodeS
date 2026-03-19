using Ookii.Dialogs.Wpf;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace CraftDebug.ViewModels
{
    public class CraftDebugRegionViewModel : BindableBase
    {
        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;

        public CraftDebugRegionViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<MeasurementOutputEvent>().Subscribe((info) =>
            {
                var result = MessageBox.Show("是否导出全部，否则导出单步？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.OK)
                {
                    try
                    {
                        string filename = $"{DateTime.Now:yyyyMMddHHmmssfff}_All_CraftDebug.CSV";

                        var dialog = new VistaFolderBrowserDialog
                        {
                            Description = "选择导出保存的路径",
                            UseDescriptionForTitle = true,
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            string outputpath = Path.Combine(dialog.SelectedPath, filename);

                            eventAggregator.GetEvent<MeasurementOutputAllEvent>().Publish(new(outputpath, 1));
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"导出失败；\n\r{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        });
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    try
                    {
                        string filename = $"{DateTime.Now:yyyyMMddHHmmssfff}_Step0{info}_CraftDebug.CSV";

                        var dialog = new VistaFolderBrowserDialog
                        {
                            Description = "选择导出保存的路径",
                            UseDescriptionForTitle = true,
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            string outputpath = Path.Combine(dialog.SelectedPath, filename);

                            eventAggregator.GetEvent<MeasurementOutputSingleEvent>().Publish(new(outputpath, int.Parse(info)));
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"导出失败；\n\r{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        });
                    }
                }
            }, ThreadOption.UIThread);
        }
    }
}
