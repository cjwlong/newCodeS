using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using ProductManage.libs;
using SharedResource.events;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProductManage.ViewModels
{
    public class ProductionMegViewModel : BindableBase
    {
        public ProductionMegViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            productStatistics = containerProvider.Resolve<ProductStatistics>();
            _globalProcessStatus = containerProvider.Resolve<GlobalProcessStatus>();

            eventAggregator.GetEvent<StartProcessEvent>().Subscribe(StartProcess);
            eventAggregator.GetEvent<ContinueProcessEvent>().Subscribe(ContinueProcess);
            eventAggregator.GetEvent<PauseProcessEvent>().Subscribe(PauseProcess);
            eventAggregator.GetEvent<StopProcessEvent>().Subscribe(StopProcess);
            eventAggregator.GetEvent<FinishedProcessEvent>().Subscribe(FinishProcess);

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ProductStatistics productStatistics;
        private readonly DispatcherTimer timer;

        private DateTime? _lastTickTime;
        //private enum ProcessState { Stopped, Running, Paused }
        //private ProcessState _currentState = ProcessState.Stopped;

        private GlobalProcessStatus _globalProcessStatus;
        public GlobalProcessStatus GlobalProcessStatus
        {
            get { return _globalProcessStatus; }
            set
            {
                SetProperty(ref _globalProcessStatus, value);
            }
        }

        private double _currentProcessingTime = 0.0;
        public double CurrentProcessingTime
        {
            get { return _currentProcessingTime; }
            set
            {
                SetProperty(ref _currentProcessingTime, value);
            }
        }

        private double _currentIdleTime = 0.0;
        public double CurrentIdleTime
        {
            get { return _currentIdleTime; }
            set
            {
                SetProperty(ref _currentIdleTime, value);
            }
        }

        public double TotalProductCount => productStatistics.GetTotalProductCount();

        public double TotalProcessingTime => productStatistics.GetTotalProcessingTime();

        public double TotalIdleTime => productStatistics.GetTotalIdleTime();

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_lastTickTime == null)
            {
                _lastTickTime = DateTime.Now;
                return;
            }

            var now = DateTime.Now;
            var elapsed = now - _lastTickTime.Value;
            _lastTickTime = now;

            switch (GlobalProcessStatus.ProcessStatus)
            {
                case G_ProcessStatus.Processing:
                    CurrentProcessingTime += Math.Floor(elapsed.TotalSeconds);
                    //productStatistics.AddProcessingTime(elapsed.TotalSeconds);
                    break;
                case G_ProcessStatus.Pause:
                    CurrentIdleTime += Math.Floor(elapsed.TotalSeconds);
                    //productStatistics.AddIdleTime(elapsed.TotalSeconds);
                    break;
            }

            RaisePropertyChanged(nameof(TotalProcessingTime));
            RaisePropertyChanged(nameof(TotalIdleTime));
            RaisePropertyChanged(nameof(TotalProductCount));
        }

        private void StartProcess()
        {
            _lastTickTime = DateTime.Now;
            timer.Start();
        }

        private void ContinueProcess()
        {
            _lastTickTime = DateTime.Now;
            timer.Start();
        }

        private void PauseProcess()
        {
            _lastTickTime = DateTime.Now;
        }

        private void StopProcess()
        {
            timer.Stop();        

            // 重置当前计时
            CurrentProcessingTime = 0;
            CurrentIdleTime = 0;
            _lastTickTime = null;
        }

        private void FinishProcess(bool isSucceed)
        {
            timer.Stop();

            if (isSucceed)
            {
                // 记录本次数据到当天记录中
                productStatistics.RecordDailyProduction(
                    DateTime.Today,
                    productCount: 1,
                    processingTime: CurrentProcessingTime,
                    idleTime: CurrentIdleTime
                );                
                LoggingService.Instance.LogInfo($"{DateTime.Now} 加工完成!");
                productStatistics.ExportProductionToCsv();
            }
            else LoggingService.Instance.LogError($"{DateTime.Now} 加工失败!");

            // 重置当前计时
            CurrentProcessingTime = 0;
            CurrentIdleTime = 0;
            _lastTickTime = null;

            RaisePropertyChanged(nameof(TotalProcessingTime));
            RaisePropertyChanged(nameof(TotalIdleTime));
            RaisePropertyChanged(nameof(TotalProductCount));
        }
    }
}
