using Machine.Interfaces;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.events.Machine;
using SharedResource.events;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection.PortableExecutable;
using System.Windows.Input;

namespace MenuControl.ViewModels
{
    public class MenuControlBtnViewModel : BindableBase
    {
        public MenuControlBtnViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            Machine = containerProvider.Resolve<IMachine>();
            globalCraftPara = containerProvider.Resolve<GlobalCraftPara>();
            GlobalProcessStatus = containerProvider.Resolve<GlobalProcessStatus>();

            eventAggregator.GetEvent<GlobalResetEvent>().Subscribe((bo) =>
            {
                if (bo)
                {
                    GlobalProcessStatus.DeviceStatus = G_DeviceStatus.intact;
                    GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Reseted;
                    MessageBox.Show("复位已完成，加工功能已激活", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    GlobalProcessStatus.DeviceStatus = G_DeviceStatus.Empty;
                    GlobalProcessStatus.ProcessStatus = G_ProcessStatus.UnReset;
                    MessageBox.Show("复位失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;

        public IMachine Machine { get; private set; }
        public GlobalCraftPara globalCraftPara { get; }

        private GlobalProcessStatus _globalProcessStatus;
        public GlobalProcessStatus GlobalProcessStatus
        {
            get { return _globalProcessStatus; }
            set
            {
                SetProperty(ref _globalProcessStatus, value);
            }
        }

        //private DelegateCommand<object> _publishCommand;
        //public DelegateCommand<object> PublishCommand => _publishCommand ??
        //    (_publishCommand = new DelegateCommand<object>(ExecuteMenuControlAsync));

        private DelegateCommand<string> _publishCommand;
        public DelegateCommand<string> PublishCommand => _publishCommand ??
            (_publishCommand = new DelegateCommand<string>(async (meg) => await ExecuteMenuControlAsync(meg)));


        private DelegateCommand _globalResetCommand;
        public DelegateCommand GlobalResetCommand => _globalResetCommand ??
            (_globalResetCommand = new DelegateCommand(ExecuteGlobalReset));

        private DelegateCommand _killAllAxisCommand;
        public DelegateCommand KillAllAxisCommand => _killAllAxisCommand ??
            (_killAllAxisCommand = new DelegateCommand(() =>
            {
                //Machine.StopAll();
                StaticEventAggregator.eventAggregator.GetEvent<EmergencyStopEvent>().Publish();
            }));


        private DelegateCommand _measurementCommand;
        public DelegateCommand MeasurementCommand => _measurementCommand ??
            (_measurementCommand = new DelegateCommand(() =>
            {
                //Machine.StopAll();
                StaticEventAggregator.eventAggregator.GetEvent<MeasurementEvent>().Publish();

            }));
        private async Task ExecuteMenuControlAsync(object obj)
        {
           
            if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.UnReset || 
                GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Finished ||
                GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Stopped)
            {
                MessageBox.Show("在加工前请复位", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            GlobalProcessStatus.ProcessType = ProcessType.Stop;
            var meg = obj as string;

            switch (meg)
            {
                case "start":
                    var re = MessageBox.Show("是否进行开始加工？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                    if (re != MessageBoxResult.OK)
                    {
                        return;
                    }
                 
                    if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Pause)
                    {
                        //GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Processing;
                        eventAggregator.GetEvent<Cmd_ContinueProcessEvent>().Publish();
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(globalCraftPara.FileName))
                        {
                            MessageBox.Show("没有可用的工艺参数");
                            return;
                        }
                        //GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Processing;
                        LoggingService.Instance.LogInfo($"开始加工：工艺参数 --> {globalCraftPara.FileName}");
                        //GlobalProcessStatus.ProcessType= ProcessType.Running;
                        List<double> list = new List<double>() { globalCraftPara.XProcessPlace, globalCraftPara.YProcessPlace, globalCraftPara .ZProcessPlace, globalCraftPara .AProcessPlace, globalCraftPara .BProcessPlace};
                        ProcessPrepareRequest request1 = new ProcessPrepareRequest();
                        request1.Values = list;
                        request1.Message = globalCraftPara.TargetFile_path;
                        eventAggregator.GetEvent<Cmd_StartProcessPrepareEvent>().Publish(request1);
                        var result = await request1.Completion.Task;
                        if (result)
                        {
                            //_globalMachineState.LaserOk
                            GlobalProcessStatus.ProcessType = ProcessType.OK;
                            MessageBox.Show("加工完成！");
                        }
                    }

                    //
                    break;
                case "pause":
                   
                    var re1 = MessageBox.Show("是否暂停加工？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Hand);
                    if (re1 != MessageBoxResult.OK)
                    {
                        return;
                    }
                    //GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Pause;
                    GlobalProcessStatus.ProcessType = ProcessType.Stop;
                    eventAggregator.GetEvent<Cmd_PauseProcessEvent>().Publish();
                    break;
                case "stop":
                     re1 = MessageBox.Show("是否停止加工？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                    if (re1 != MessageBoxResult.OK)
                    {
                        return;
                    }
                    //GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Stopped;
                    GlobalProcessStatus.ProcessType = ProcessType.Stop;
                    //eventAggregator.GetEvent<Cmd_StopProcessEvent>().Publish();
                    StopProcessRequest rs = new StopProcessRequest();
                    eventAggregator.GetEvent<Cmd_StopProcessEvent>().Publish(rs);
                    await Task.Delay(100);

                    var resultStop = await rs.Completion.Task;
                    if (resultStop)
                    {
                        MessageBox.Show("停止执行完成！");
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 复位
        /// </summary>
        private void ExecuteGlobalReset()
        {
            var re = MessageBox.Show("是否进行开始复位？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (re != MessageBoxResult.OK)
            {
                return;
            }
            // 加工中
            if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
            {
                MessageBox.Show("程序正在加工中，请先停止加工", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 暂停
            if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Pause)
            {
                MessageBoxResult boxResult = MessageBox.Show("程序已暂停，请先停止加工", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            eventAggregator.GetEvent<Cmd_GlobalResetEvent>().Publish();
        }
    }
}
