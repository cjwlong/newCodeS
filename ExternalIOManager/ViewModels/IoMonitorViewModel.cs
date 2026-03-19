using ExternalIOManager.Interfaces;
using ExternalIOManager.libs;
using Microsoft.Xaml.Behaviors.Core;
using Mono.Unix.Native;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.events.Machine;
using SharedResource.libs;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using ServiceManager;
using System.Reflection.PortableExecutable;

namespace ExternalIOManager.ViewModels
{
#if false
    public class IoMonitorViewModel : BindableBase
    {
        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;
        private IODeviceStatus _deviceStatus;
        private GlobalProcessStatus _globalProcessStatus;
        private readonly GlobalMachineState _globalMachineState;
        private Timer _updateTimer;
        private bool _isMonitoring;

        private static bool emergencyStopPopupShown = false;
        private static bool doorOpenPopupShown = false;

        public IoMonitorViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            _deviceService = containerProvider.Resolve<IIODeviceService>();
            _globalProcessStatus = containerProvider.Resolve<GlobalProcessStatus>();
            _globalMachineState = containerProvider.Resolve<GlobalMachineState>();
            //_globalMachineState.PropertyChanged += (s, e) => EvaluateLamps();  io 反馈

            DeviceStatus = new IODeviceStatus();

            ConnectCommand = new DelegateCommand(ExecuteConnect);
            StartMonitoringCommand = new DelegateCommand(StartMonitoring);
            StopMonitoringCommand = new DelegateCommand(StopMonitoring);
            //ChangedLigStatusCommand = new DelegateCommand<string>(ExecuteChangedLigStatus);
           // LoadLamps();
            eventAggregator.GetEvent<SaveSettingEvent>().Subscribe((r) =>
            {
                if (r.ToString() == "IO")
                {
                    _deviceService.SaveConfig2File();
                }
            });

            eventAggregator.GetEvent<ioDeviceConnectEvent>().Subscribe(ExecuteConnect);

            ExecuteConnect();

         
            Task.Run(() => GetDevStatus()); 
        }

        private IIODeviceService _deviceService;
        public IIODeviceService DeviceService
        {
            get { return _deviceService; }
            set
            {
                SetProperty(ref _deviceService, value);
            }
        }

        public string IP
        {
            get => _deviceService.IpAddress;
            set
            {
                if (_deviceService.IpAddress != value)
                {
                    _deviceService.IpAddress = value;
                    RaisePropertyChanged(nameof(IP));
                }
            }
        }

        public int Port
        {
            get => _deviceService.Port;
            set
            {
                if (value != _deviceService.Port)
                {
                    _deviceService.Port = value;
                    RaisePropertyChanged(nameof(Port));
                }
            }
        }

        public IODeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                SetProperty(ref _isMonitoring, value);
            }
        }

        private bool _isDebug = false;
        public bool IsDebug
        {
            get => _isDebug;
            set => SetProperty(ref _isDebug, value);
        }

        public DelegateCommand ConnectCommand {  get; }
        public DelegateCommand StartMonitoringCommand { get; }
        public DelegateCommand StopMonitoringCommand { get; }
        //public DelegateCommand<string> ChangedLigStatusCommand { get; }

        private async void ExecuteConnect()
        {
            try
            {
                if (_deviceService.DeviceStatus == SharedResource.enums.DeviceStatus.Idle)
                {
                    _deviceService.Dispose();
                }
                else
                {
                    try
                    {
                        await Task.Run(()=> _deviceService.Connect());
                        //StartMonitoring();
                        LoggingService.Instance.LogInfo("IO设备已连接");
                    }
                    catch (Exception ex)
                    {
                        _deviceService.DeviceStatus= SharedResource.enums.DeviceStatus.Disconnected;
                        LoggingService.Instance.LogError("IO模块连接失败", ex);
                        MessageWindow.ShowDialog($"IO模块连接失败");
                    }
                }
            }
            catch (Exception ex)
            {
                
            }            
        }


        private async Task LoadLamps()
        {
            if(_deviceService==null)
            {
                return;
            }
            _deviceService.WriteSingleCoil(1, false);
            await Task.Delay(150);
            _deviceService.WriteSingleCoil(2, false);
            await Task.Delay(150);
            _deviceService.WriteSingleCoil(3, false);
            await Task.Delay(150);
            _deviceService.WriteSingleCoil(4, false);
            await Task.Delay(150);
        }
        private async Task EvaluateLamps()
        {
            //if (DeviceService.DeviceStatus != SharedResource.enums.DeviceStatus.Idle) return;
            
            if (DeviceStatus.DoorOpen && _globalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
            {
                // 报警
                _deviceService.WriteSingleCoil(4, true);
            }
            else if (!_globalMachineState.AxisEnabled || !_globalMachineState.LaserOk || !_globalMachineState.LimitSafe)
            {
                // 红灯
                _deviceService.WriteSingleCoil(1, true);
            }
            else if (_globalMachineState.IsMachineRunning && DeviceStatus.DoorOpen)
            {
                // 黄灯
                _deviceService.WriteSingleCoil(2, true);
            }
            else
            {
                // 绿灯
                _deviceService.WriteSingleCoil(3, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexLamp">1,红灯,2:黄灯，3:绿灯，4:报警灯</param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private async Task  SetLampsStauts(ushort indexLamp,bool  flag)
        {

            _deviceService.WriteSingleCoil(indexLamp, flag);

        }

        /// <summary>
        /// 调试过程中开门要黄灯？刻蚀过程中开门 红灯？
        /// </summary>
        private void  GetDevStatus()
        {
            while (true)
            {
                if(_deviceService.DeviceStatus== SharedResource.enums.DeviceStatus.Disconnected)
                {
                    continue;
                }
                var status = new IODeviceStatus();
                if (GlobalCollectionService<ErrorType>.Instance.GetCollectionCount() == 0)
                {
                    SetLampsStauts(1, false);
                    SetLampsStauts(2, false);
                    SetLampsStauts(3, true);
                    status.RedLamp = false;
                    status.YellowLamp = false;
                    status.GreenLamp = true;
                    Application.Current.Dispatcher.Invoke(() => DeviceStatus = status);
                    continue;

                }
                if (GlobalCollectionService<ErrorType>.Instance.Contains((int)LaserErrorType.ConError, ErrorType.LaserError)
                    || GlobalCollectionService<ErrorType>.Instance.Contains((int)LaserErrorType.LaserStatusError, ErrorType.LaserError)
                    || GlobalCollectionService<ErrorType>.Instance.Contains((int)LaserErrorType.StatusReturnError, ErrorType.LaserError)
                    || GlobalCollectionService<ErrorType>.Instance.Contains((int)ErrorType.AxisOff, ErrorType.AxisOff)
                    || GlobalCollectionService<ErrorType>.Instance.Contains((int)ErrorType.HSensor, ErrorType.HSensor))
                {
                    SetLampsStauts(1, true);
                    SetLampsStauts(2, false);
                    SetLampsStauts(3, false);
                    status.RedLamp = true;
                    status.YellowLamp = false;
                    status.GreenLamp = false;
                }
                if (GlobalCollectionService<ErrorType>.Instance.Contains((int)ErrorType.HPressure, ErrorType.HPressure))
                {
                    SetLampsStauts(2, true);
                    SetLampsStauts(1, false);
                    SetLampsStauts(3, false);
                    status.RedLamp = false;
                    status.YellowLamp = true;
                    status.GreenLamp = false;
                }
                Application.Current.Dispatcher.Invoke(() => DeviceStatus = status);
                //Thread.Sleep(intervalMs);
            }
        }

        private void StartMonitoring()
        {
            _updateTimer = new Timer(UpdateDeviceStatus, null, 0, 1000);
            IsMonitoring = true;
        }

        private void StopMonitoring()
        {
            _updateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            IsMonitoring = false;
        }

        private void UpdateDeviceStatus(object state)
        {
            try
            {
                var status = new IODeviceStatus();

                var coils = _deviceService.ReadOutputCoils(0, 5);  //io模块输出
                status.RedLamp = coils[1];
                status.YellowLamp = coils[2];
                status.GreenLamp = coils[3];
                status.Alarm = coils[4];

                bool emergencyStop, doorOpen;
                _deviceService.ReadInput(1, out emergencyStop); ///外界给io模块输入 ，软件读取后做其他动作
                _deviceService.ReadInput(0, out doorOpen);
                bool HPressure = false;
                //_deviceService.ReadInput(2, out HPressure); 

                status.EmergencyStop = emergencyStop;
                status.DoorOpen = doorOpen;

                DisposeDoorAndEmergencyStop(doorOpen, emergencyStop);
                if(doorOpen)
                {
                    if (_globalProcessStatus.ProcessType == ProcessType.Running)
                    {
                        status.RedLamp =true;
                        status.YellowLamp = false;
                        SetLampsStauts(1,true);
                        SetLampsStauts(4, true);
                    }
                    else
                    {
                        status.YellowLamp = true;
                        status.RedLamp = false;
                        SetLampsStauts(2, true);
                    }
                }
                //if(HPressure)
                //{
                //    status.YellowLamp = true;
                //    SetLampsStauts(2,true);
                //}
                //else
                //{
                //    status.YellowLamp = false;
                //    SetLampsStauts(2, false);
                //}
              

                // 主线程更新UI
                Application.Current.Dispatcher.Invoke(() => DeviceStatus = status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设备通信异常：{ex.Message}");
            }
        }

        private void DisposeDoorAndEmergencyStop(bool doorStatus, bool emergencyStopStatus)
        {
            try
            {
                if (emergencyStopStatus && !emergencyStopPopupShown)
                {
                    emergencyStopPopupShown = true; 
                    StaticEventAggregator.eventAggregator.GetEvent<EmergencyStopEvent>().Publish();
                    MessageBox.Show("急停已按下");
                }
                else if (!emergencyStopStatus)
                {
                    emergencyStopPopupShown = false;
                }

                if (doorStatus && !doorOpenPopupShown)
                {
                    doorOpenPopupShown = true;
                    //if (_globalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
                    //{
                    //    eventAggregator.GetEvent<Cmd_PauseProcessEvent>().Publish();
                    //    MessageBox.Show("门已打开，加工程序已暂停");
                    //}
                    if(_globalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
                    {
               
                        // 创建一个DispatcherTimer
                        //DispatcherTimer timer = new DispatcherTimer();
                        //timer.Interval = TimeSpan.FromSeconds(200);
                        //timer.Tick += (sender, e) =>
                        //{
                        //    // 停止计时器
                        //    timer.Stop();
                        //    // 关闭当前窗口（如果是MessageBox，则需要访问父窗口或其他方式）
                        //    Application.Current.MainWindow.Close(); // 或者 Application.Current.Shutdown();
                        //};
                        //timer.Start();
                       

                        if (_globalProcessStatus.ProcessType == ProcessType.Running)
                        {
                           
                            LoggingService.Instance.LogInfo($"门已打开，所有轴即将急停");
                            MessageBox.Show("门已打开，所有轴即将停止");
                            eventAggregator.GetEvent<KillAllAxisEvent>().Publish();
                        }
                       
                       
                    }
                }
                else if (!doorStatus)
                {
                    doorOpenPopupShown = false;
                }
            }
            catch (Exception)
            {

            }
        }

        private void ExecuteChangedLigStatus(string Param)
        {
            if (!IsDebug) return;

            switch (Param.ToString())
            {
                case "红":
                    _deviceService.WriteSingleCoil(1, !DeviceStatus.RedLamp);
                    break;
                case "黄":
                    _deviceService.WriteSingleCoil(2, !DeviceStatus.YellowLamp);
                    break;
                case "绿":
                    _deviceService.WriteSingleCoil(3, !DeviceStatus.GreenLamp);
                    break;
                case "报警":
                    _deviceService.WriteSingleCoil(4, !DeviceStatus.Alarm);
                    break;
                default:
                    break;
            }
        }


        #region  io light
        //private void DeviceStatusMonitoring()
        //{ 
        //    while()
        //    {
        //        if()
        //        {
        //            bool emergencyStop, doorOpen;
        //            _deviceService.ReadInput(1, out emergencyStop); //急停
        //            _deviceService.ReadInput(0, out doorOpen);//开门

        //        }


        //    }
        
        //}



        #endregion
    }
#endif

#if true
    public class IoMonitorViewModel : BindableBase
    {
        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;
        private IODeviceStatus _deviceStatus;
        private GlobalProcessStatus _globalProcessStatus;
        private readonly GlobalMachineState _globalMachineState;
        private Timer _updateTimer;
        private bool _isMonitoring;

        private static bool emergencyStopPopupShown = false;
        private static bool doorOpenPopupShown = false;

        public IoMonitorViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            _deviceService = containerProvider.Resolve<IIODeviceService>();
            _globalProcessStatus = containerProvider.Resolve<GlobalProcessStatus>();
            _globalMachineState = containerProvider.Resolve<GlobalMachineState>();
            _globalMachineState.PropertyChanged += (s, e) => EvaluateLamps();

            DeviceStatus = new IODeviceStatus();

            ConnectCommand = new DelegateCommand(ExecuteConnect);
            StartMonitoringCommand = new DelegateCommand(StartMonitoring);
            StopMonitoringCommand = new DelegateCommand(StopMonitoring);
            //ChangedLigStatusCommand = new DelegateCommand<string>(ExecuteChangedLigStatus);

            eventAggregator.GetEvent<SaveSettingEvent>().Subscribe((r) =>
            {
                if (r.ToString() == "IO")
                {
                    _deviceService.SaveConfig2File();
                }
            });

            eventAggregator.GetEvent<ioDeviceConnectEvent>().Subscribe(ExecuteConnect);

            ExecuteConnect();
        }

        private IIODeviceService _deviceService;
        public IIODeviceService DeviceService
        {
            get { return _deviceService; }
            set
            {
                SetProperty(ref _deviceService, value);
            }
        }

        public string IP
        {
            get => _deviceService.IpAddress;
            set
            {
                if (_deviceService.IpAddress != value)
                {
                    _deviceService.IpAddress = value;
                    RaisePropertyChanged(nameof(IP));
                }
            }
        }

        public int Port
        {
            get => _deviceService.Port;
            set
            {
                if (value != _deviceService.Port)
                {
                    _deviceService.Port = value;
                    RaisePropertyChanged(nameof(Port));
                }
            }
        }

        public IODeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set
            {
                SetProperty(ref _deviceStatus, value);
            }
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                SetProperty(ref _isMonitoring, value);
            }
        }

        private bool _isDebug = false;
        public bool IsDebug
        {
            get => _isDebug;
            set => SetProperty(ref _isDebug, value);
        }

        public DelegateCommand ConnectCommand { get; }
        public DelegateCommand StartMonitoringCommand { get; }
        public DelegateCommand StopMonitoringCommand { get; }
        //public DelegateCommand<string> ChangedLigStatusCommand { get; }

        private async void ExecuteConnect()
        {
            try
            {
                if (_deviceService.DeviceStatus == SharedResource.enums.DeviceStatus.Idle)
                {
                    _deviceService.Dispose();
                }
                else
                {
                    try
                    {
                        await Task.Run(() => _deviceService.Connect());
                        StartMonitoring();
                        LoggingService.Instance.LogInfo("IO设备已连接");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("IO模块连接失败", ex);
                        MessageWindow.ShowDialog($"IO模块连接失败");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async Task EvaluateLamps()
        {
            if (DeviceService.DeviceStatus != SharedResource.enums.DeviceStatus.Idle) return;
            //_deviceService.WriteSingleCoil(1, false); //先删除
            //await Task.Delay(150);
            //_deviceService.WriteSingleCoil(2, false);
            //await Task.Delay(150);
            //_deviceService.WriteSingleCoil(3, false);
            //await Task.Delay(150);
            //_deviceService.WriteSingleCoil(4, false);
            //await Task.Delay(150);

            int value = 0;
            if (DeviceStatus.DoorOpen && _globalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
            {
                // 报警
                _deviceService.WriteSingleCoil(4, true);
                _deviceService.WriteSingleCoil(1, true);//红灯
                value++;
            }
            if (!_globalMachineState.AxisEnabled || !_globalMachineState.LaserOk || !_globalMachineState.LimitSafe)
            {
                // 红灯
                _deviceService.WriteSingleCoil(1, true);
                value++;
            }
            if (_globalMachineState.IsMachineRunning && DeviceStatus.DoorOpen)
            {
                // 黄灯
                _deviceService.WriteSingleCoil(2, true);
                value++;
            }
            if(_globalProcessStatus.ProcessType == ProcessType.OK) //加工完成
            {
                _deviceService.WriteSingleCoil(2, true);
                value++;
            }
            if(value == 0)
            {
                // 绿灯
                _deviceService.WriteSingleCoil(3, true);

                _deviceService.WriteSingleCoil(1, false);
                await Task.Delay(150);
                _deviceService.WriteSingleCoil(2, false);
                await Task.Delay(150);
                _deviceService.WriteSingleCoil(4, false);
                await Task.Delay(150);
            }
        }

        private void StartMonitoring()
        {
            _updateTimer = new Timer(UpdateDeviceStatus, null, 0, 1000);
            IsMonitoring = true;
        }

        private void StopMonitoring()
        {
            _updateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            IsMonitoring = false;
        }
        /// <summary>
        /// X输入 ：
        //        X0 -门显示未关闭
                 //X1-急停
                //X2-气压异常报警（目前未在软件得到验证）

        //Y输出 ：
                //Y0-出光模式切换
                //Y1 -红灯
                //Y2-黄灯
                //Y3-绿灯
                //Y4-蜂鸣
        /// </summary>
        /// <param name="state"></param>
        private void UpdateDeviceStatus(object state)
        {
            try
            {
                var status = new IODeviceStatus();

                var coils = _deviceService.ReadOutputCoils(0, 5);
                status.RedLamp = coils[1];
                status.YellowLamp = coils[2];
                status.GreenLamp = coils[3];
                status.Alarm = coils[4];

                bool emergencyStop, doorOpen,GasIsOK;
                _deviceService.ReadInput(1, out emergencyStop);
                _deviceService.ReadInput(0, out doorOpen);
                _deviceService.ReadInput(2, out GasIsOK);//低电平输出

                status.EmergencyStop = emergencyStop;
                status.DoorOpen = doorOpen;
                status.GasPressure = GasIsOK?false:true;  //气压模块电平反输出

                _deviceService.WriteSingleCoil(1, status.GasPressure); //红灯
                _deviceService.WriteSingleCoil(4, status.GasPressure);//蜂鸣

                if (DeviceStatus.DoorOpen != status.DoorOpen) EvaluateLamps();

                DisposeDoorAndEmergencyStop(doorOpen, emergencyStop);

                // 主线程更新UI
                Application.Current.Dispatcher.Invoke(() => DeviceStatus = status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设备通信异常：{ex.Message}");
            }
        }

        private void DisposeDoorAndEmergencyStop(bool doorStatus, bool emergencyStopStatus)
        {
            try
            {
                if (emergencyStopStatus && !emergencyStopPopupShown)
                {
                    emergencyStopPopupShown = true;
                    StaticEventAggregator.eventAggregator.GetEvent<EmergencyStopEvent>().Publish();
                    MessageBox.Show("急停已按下");
                }
                else if (!emergencyStopStatus)
                {
                    emergencyStopPopupShown = false;
                }

                if (doorStatus && !doorOpenPopupShown)
                {
                    doorOpenPopupShown = true;
                    //if (_globalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
                    //{
                    //    eventAggregator.GetEvent<Cmd_PauseProcessEvent>().Publish();
                    //    MessageBox.Show("门已打开，加工程序已暂停");
                    //}
                    //else
                    //{
                    //    eventAggregator.GetEvent<KillAllAxisEvent>().Publish();
                    //    MessageBox.Show("门已打开，所有轴已急停");
                    //}
                }
                else if (!doorStatus)
                {
                    doorOpenPopupShown = false;
                }
            }
            catch (Exception)
            {

            }
        }

        private void ExecuteChangedLigStatus(string Param)
        {
            if (!IsDebug) return;

            switch (Param.ToString())
            {
                case "红":
                    _deviceService.WriteSingleCoil(1, !DeviceStatus.RedLamp);
                    break;
                case "黄":
                    _deviceService.WriteSingleCoil(2, !DeviceStatus.YellowLamp);
                    break;
                case "绿":
                    _deviceService.WriteSingleCoil(3, !DeviceStatus.GreenLamp);
                    break;
                case "报警":
                    _deviceService.WriteSingleCoil(4, !DeviceStatus.Alarm);
                    break;
                default:
                    break;
            }
        }
    }

#endif
}
