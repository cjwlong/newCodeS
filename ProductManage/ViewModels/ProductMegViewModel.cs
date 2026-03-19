using OperationLogManager.libs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ProductManage.ViewModels
{
    internal class ProductMegViewModel : BindableBase
    {
        public ProductMegViewModel(IContainerProvider provider)
        {
            try
            {
                containerProvider = provider;
                eventAggregator = containerProvider.Resolve<IEventAggregator>();
                globalCraftPara = containerProvider.Resolve<GlobalCraftPara>();

                // "_Total" 表示所有逻辑处理器的平均
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                // 首次调用 NextValue() 会返回 0，后续才是有效值
                _cpuCounter.NextValue();

                _cts = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        float usage = _cpuCounter.NextValue();
                        Application.Current.Dispatcher?.Invoke(() => CpuUsage = usage);
                        await Task.Delay(2000, _cts.Token); // 2 秒采样一次
                    }
                }, _cts.Token);
            }
            catch (Exception  ex)
            {

                return;
            }
        }

        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;
        public GlobalCraftPara globalCraftPara { get; }

        private CancellationTokenSource _cts;
        private readonly PerformanceCounter _cpuCounter;
        private readonly DispatcherTimer _timer;

        private float _cpuUsage;
        public float CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (Math.Abs(_cpuUsage - value) < 0.1) return;
                SetProperty(ref _cpuUsage, value);
            }
        }
    }
}
