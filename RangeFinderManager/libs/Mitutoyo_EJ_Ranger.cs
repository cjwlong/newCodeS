using Newtonsoft.Json.Linq;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Threading;
using RangeFinderManager.Interface;
using SharedResource.tools;
using SharedResource.enums;
using OperationLogManager.libs;
using System.IO.Ports;
using RJCP.IO.Ports;

namespace RangeFinderManager.libs
{
    public class Mitutoyo_EJ_Ranger : BindableBase, IRanger
    {
        Thread RefreshTask;
        //private SerialPortStream _serialPort;
        //private string _filePath = $"{ConfigStore.StoreDir}/SensorSetZeroValue.json";
        private IRangerHardware _rangerHardware;

        private int baudRate = 9600;
        public int BaudRate { get => baudRate; set => SetProperty(ref baudRate, value); }

        /// <summary>
        /// 串口参数
        /// </summary>
        private SerialPortStream serialPort;
        public ObservableCollection<string> COMs { get; private set; } = new ObservableCollection<string>();
        private string _nowCom = Properties.Settings.Default.LastCom;
        public string NowCom
        {
            get => _nowCom; set
            {
                SetProperty(ref _nowCom, value);
                if (value != null)
                {
                    Properties.Settings.Default.LastCom = NowCom;   // 保存设置
                    SaveSettingsWithRetry();
                }
            }
        }

        private string rangerTytpe = "Mitutoyo_EJ";
        public string RangerType
        {
            get => rangerTytpe; set
            {
                SetProperty(ref rangerTytpe, value); if (value != null)
                {
                    SaveSettingsWithRetry();
                }
            }
        }

        /// <summary>
        /// 状态参数
        /// </summary>
        bool SuspendFlag = true;
        private bool _isRational = false;
        public bool IsRational
        {
            get => _isRational; private set
            {
                SetProperty(ref _isRational, value);
                DeviceStatus = IsRational == true ? DeviceStatus.Idle : DeviceStatus.Error;
            }
        }

        private bool? _isConnected = false;
        public bool? IsConnected
        {
            get => _isConnected;
            private set
            {
                if (IsConnected != value && value != null)
                {
                    Properties.Settings.Default.LastConnection = value.Value;
                    SaveSettingsWithRetry();
                }

                SetProperty(ref _isConnected, value);
                if (value == true)
                    SuspendFlag = false;
                else
                    SuspendFlag = true;
            }
        }
        private string _rangeResult = "未连接";
        public string RangeResult { get => _rangeResult; private set => SetProperty(ref _rangeResult, value); }

        /// <summary>
        /// 数据参数
        /// </summary>
        //private double _offset;
        private double? _distance = null;
        public double? Distance { get => _distance; private set => SetProperty(ref _distance, value); }
        private double _filteredDistace = 0.0;
        public double FilteredDistance { get => _filteredDistace; set => SetProperty(ref _filteredDistace, value); }

        private DeviceStatus _deviceStatus = DeviceStatus.Disconnected;
        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus; set
            {
                SetProperty(ref _deviceStatus, value);
            }
        }

        /// <summary>
        /// 重复保存读取操作
        /// </summary>
        private void SaveSettingsWithRetry(int retryCount = 10, int delayMilliseconds = 100)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    Properties.Settings.Default.Save();
                    return;
                }
                catch
                {
                    if (i == retryCount - 1) throw;
                    Thread.Sleep(delayMilliseconds);
                }
            }
        }

        public Mitutoyo_EJ_Ranger(IContainerProvider provider)
        {
            var aggregator = provider.Resolve<IEventAggregator>();

            //aggregator.GetEvent<LaserRangeGetValueEvent>().Subscribe((data) =>
            //{
            //    data ??= new();
            //    if (!IsRational || IsConnected == false)
            //        data.IsPlausible = false;

            //    if (Distance != null)
            //    {
            //        data.Value = Distance.Value;
            //        data.FilteredValue = FilteredDistance;
            //    }
            //});

            if (Properties.Settings.Default.LastConnection)
                Connect();

            CreateRefresh();

            InitCom();
        }

        /// <summary>
        /// 初始化端口
        /// </summary>
        public void InitCom()
        {
            var temp = SerialPortStream.GetPortNames();

            Application.Current.Dispatcher.Invoke(() =>
            {
                COMs ??= new();
                COMs.Clear();
                foreach (var device in temp)
                    COMs.Add(device);
            });

            string last_com = Properties.Settings.Default.LastCom;

            if (COMs.Contains(last_com)) NowCom = last_com;

            else NowCom = COMs.FirstOrDefault();
        }

        /// <summary>
        /// 初始化硬件类
        /// </summary>
        public bool InitializeHardware(string hardwareType)
        {
            if (NowCom == null)
            {
                MessageWindow.ShowDialog("串口号为空");
                return false;
            }
            else
            {
                serialPort = new SerialPortStream()
                {
                    PortName = NowCom,
                    BaudRate = BaudRate,
                    Parity = RJCP.IO.Ports.Parity.None,   // 无奇偶校验
                    DataBits = 8,           // 每个字节8位数据
                    StopBits = RJCP.IO.Ports.StopBits.One,// 1停止位
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 1000,     // 超时
                    WriteTimeout = 1000,    // 超时
                };

                if (hardwareType == "Mitutoyo_EJ")
                    _rangerHardware = new LGQuick(serialPort);
                return true;
            }
        }

        /// <summary>
        /// 传感器连接
        /// </summary>
        public void Connect()
        {
            if (IsConnected == false)
            {
                Task.Run(() =>
                {
                    if (InitializeHardware(RangerType))
                    {
                        DeviceStatus = DeviceStatus.Connecting;

                        IsConnected = null; //连接中

                        _rangerHardware.Disconnect();

                        bool connectStatus = _rangerHardware.Connect(); //串口打开

                        if (connectStatus)
                        {
                            //连接时先判断能不能读数（或是否对应上了传感器），如果不能取消连接

                            var isReadCorrect = _rangerHardware.RefreshStatus();

                            if (!isReadCorrect.IsRational && isReadCorrect.Error != "测距警告")  //读失败
                            {
                                DeviceStatus = DeviceStatus.Disconnected;
                                IsConnected = false; //连接失败
                                MessageWindow.ShowDialog($"接触式传感器连接失败：\n{isReadCorrect.Error}");
                                _rangerHardware.Disconnect();  //关闭串口
                            }
                            else //读成功，测距警告或示数
                            {
                                DeviceStatus = DeviceStatus.Idle;
                                IsConnected = true; //连接成功
                                LoggingService.Instance.LogInfo($"接触式传感器连接成功！");

                                //if (File.Exists(_filePath))
                                //{
                                //    var jsonData = File.ReadAllText(_filePath);
                                //    var jsonArray = JArray.Parse(jsonData);
                                //    var lastEntry = jsonArray.Last;

                                //    if (lastEntry != null)
                                //    {
                                //        _offset = (double)lastEntry["SetZeroValue"];
                                //        LoggingService.Instance.LogInfo($"读取测距仪零点{_offset}");
                                //    }
                                //}
                                //else
                                //{
                                //    _offset = 0.0;
                                //    LoggingService.Instance.LogInfo($"默认测距仪零点{_offset}");
                                //}
                            }
                        }
                        else
                        {
                            DeviceStatus = DeviceStatus.Disconnected;
                            IsConnected = false; //连接失败
                            MessageWindow.ShowDialog($"接触式传感器连接失败");
                            LoggingService.Instance.LogError("接触式传感器连接失败");
                        }
                    }
                    else DeviceStatus = DeviceStatus.Disconnected;
                });
            }
        }

        /// <summary>
        /// 传感器断联
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected == true)
            {
                bool disconnectStatus = _rangerHardware.Disconnect();

                if (disconnectStatus)
                {
                    DeviceStatus = DeviceStatus.Disconnected;
                    IsConnected = false;
                    serialPort = null;
                    Distance = null;
                    RangeResult = "未连接";
                    LoggingService.Instance.LogInfo("断开接触式传感器");
                }
            }
        }

        /// <summary>
        /// 传感器状态刷新
        /// </summary>
        private void CreateRefresh()
        {
            RefreshTask = null;
            RefreshTask = new(async () =>
            {
                while (true)
                {
                    if (SuspendFlag) RangeResult = "未连接";

                    while (SuspendFlag)
                        await Task.Delay(500);

                    DataProcess();

                    await Task.Delay(10);
                }
            });
            RefreshTask.IsBackground = true;
            RefreshTask.Start();
        }

        /// <summary>
        /// 数据处理显示
        /// </summary>
        private void DataProcess()
        {
            var status = _rangerHardware.RefreshStatus();

            IsRational = status.IsRational;

            if (IsRational)
            {
                Distance = status.Distance;
                FilteredDistance = Math.Round((double)Distance, 4);
                RangeResult = FilteredDistance.ToString("0.0000");
            }
            else
            {
                FilteredDistance = 0.0;
                RangeResult = status.Error == "测距警告" ? "测距警告" : "读取异常";
            }
        }

    }
}
