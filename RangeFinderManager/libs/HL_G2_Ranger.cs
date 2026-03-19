using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Timers;
using Prism.Ioc;
using Prism.Events;
using System.Xml.Linq;
using System.Windows;
using System.Threading;
using Timer = System.Timers.Timer;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using RangeFinderManager.Interface;
using SharedResource.tools;
using SharedResource.enums;
using SharedResource.events.RangeFinder;
using OperationLogManager.libs;
using IronPython.Runtime;
using static IronPython.Modules._ast;

namespace RangeFinderManager.libs
{
    public class HL_G2_Ranger : BindableBase, IRanger
    {
        /// <summary>
        /// 相关字段
        /// </summary>
        Thread RefreshTask;
        private string _filePath = $"{ConfigStore.StoreDir}/SensorSetZeroValue.json";
        private IRangerHardware _rangerHardware;

        private string rangerTytpe = "HL_G2";
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

        private string _ip;
        public string IP
        {
            get { return _ip; }
            set
            {
                if (_ip == value) return;
                {
                    var regex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
                    bool isValidIP;
                    if (isValidIP = regex.IsMatch(value))
                        _ip = value;
                    else
                    {
                        return;
                    }
                }
                SetProperty(ref _ip, value);
                Properties.Settings.Default.HLG2IP = value;
            }
        }

        private int _Port;
        public int Port
        {
            get { return _Port; }
            set
            {
                if (_Port == value) return;
                SetProperty(ref _Port, value);
                Properties.Settings.Default.HLG2Port = value;
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
                //DeviceStatus = IsRational == true ? DeviceStatus.Idle : DeviceStatus.Error;
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
        private double _offset;
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



        /// <summary>
        /// 构造函数
        /// </summary>
        public HL_G2_Ranger(IContainerProvider provider)
        {            
            var aggregator = provider.Resolve<IEventAggregator>();

            aggregator.GetEvent<LaserRangeGetValueEvent>().Subscribe((data) =>
            {
                data ??= new();
                if (!IsRational || IsConnected == false)
                    data.IsPlausible = false;

                if (Distance != null)
                {
                    data.Value = Distance.Value;
                    data.FilteredValue = FilteredDistance;
                }
            });

            LoadSettings();

            if (Properties.Settings.Default.LastConnection)
                Connect();

            CreateRefresh();
        }

        ~HL_G2_Ranger()
        {

        }

        /// <summary>
        /// 初始化硬件类
        /// </summary>
        public bool InitializeHardware(string hardwareType)
        {
            if (hardwareType == "HL_G2")
            {
                LoggingService.Instance.LogInfo("初始化HL_G2");
                _rangerHardware = new PanasonicModbus(IP, Port);
                return true;
            }
            else
                return false;
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
                                MessageWindow.ShowDialog($"激光位移传感器连接失败：\n{isReadCorrect.Error}");
                                _rangerHardware.Disconnect();  //关闭串口
                            }
                            else //读成功，测距警告或示数
                            {
                                DeviceStatus = DeviceStatus.Idle;
                                IsConnected = true; //连接成功

                                if (File.Exists(_filePath))
                                {
                                    var jsonData = File.ReadAllText(_filePath);
                                    var jsonArray = JArray.Parse(jsonData);
                                    var lastEntry = jsonArray.Last;

                                    if (lastEntry != null)
                                    {
                                        _offset = (double)lastEntry["SetZeroValue"];
                                        LoggingService.Instance.LogInfo($"读取测距仪零点{_offset}");
                                    }
                                }
                                else
                                {
                                    _offset = 0.0;
                                    LoggingService.Instance.LogError($"默认测距仪零点{_offset}");
                                }
                            }
                        }
                        else
                        {
                            DeviceStatus = DeviceStatus.Disconnected;
                            IsConnected = false; //连接失败
                            MessageWindow.ShowDialog($"激光位移传感器连接失败");
                            LoggingService.Instance.LogError("激光位移传感器连接失败");
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
                    Distance = null;
                    RangeResult = "未连接";
                    LoggingService.Instance.LogInfo("断开激光测距");
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
                FilteredDistance = Math.Round((double)(Distance - _offset), 4);
                RangeResult = FilteredDistance.ToString("0.0000");
            }
            else
            {
                FilteredDistance = 0.0;
                RangeResult = status.Error == "测距警告" ? "测距警告" : "读取异常";
            }
        }

        public void CancelSetZeroOffset()
        {
            _offset = 0;
            LoggingService.Instance.LogInfo("取消置零成功");
        }

        /// <summary>
        /// 传感器置零
        /// </summary>
        public void SetZeroOffset()
        {
            if (Distance == null)
            {
                MessageWindow.ShowDialog("未连接或读取异常，无法归零");
                return;
            }
            _offset = Distance.Value;

            var newEntry = new JObject
            {
                ["SetZeroTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["SetZeroValue"] = _offset
            };

            JArray jsonArray;

            if (File.Exists(_filePath))
            {
                var jsonData = File.ReadAllText(_filePath);
                jsonArray = JArray.Parse(jsonData);
            }
            else
            {
                jsonArray = new JArray();
            }

            jsonArray.Add(newEntry);

            File.WriteAllText(_filePath, jsonArray.ToString());
        }

        private void LoadSettings()
        {
            _ip = Properties.Settings.Default.HLG2IP;
            _Port = Properties.Settings.Default.HLG2Port;
        }

        public void SaveSettings()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Properties.Settings.Default.Save();
                    return;
                }
                catch
                {
                    if (i == 4) throw;
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
