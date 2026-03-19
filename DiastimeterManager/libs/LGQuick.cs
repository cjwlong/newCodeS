using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Mvvm;
using RJCP.IO.Ports;
using ServiceManager;
using SharedResource.enums;
using SharedResource.libs;
using ServiceManager;
using SharedResource.tools;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;

namespace DiastimeterManager.libs
{
    public class LGQuick : BindableBase
    {
        private SerialPortStream _serialPort;
        //private readonly Dispatcher _dispatcher;
        private System.Timers.Timer _readTimer;

        private double _zeroSetting = 0;
        public string _portName = string.Empty;
        public int _baudRate = 0;
        private string LGQuickFile_path = Path.Combine(ConfigStore.StoreDir, "LGQuick.json");
        public event EventHandler<string> DataReceived;

        public LGQuick()
        {
            //_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            LoadConfigForFile();
            InitTimer();
        }

        private void InitTimer()
        {
            _readTimer = new System.Timers.Timer(100);
            _readTimer.Elapsed += ReadTimerCallback;
            _readTimer.AutoReset = true;
        }

        private DeviceStatus _deviceStatus = DeviceStatus.Disconnected;
        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        private double _lGQuickValue;
        public double LGQuickValue
        {
            get => _lGQuickValue;
            set => SetProperty(ref _lGQuickValue, value);
        }

        private double _LQData;
        public double LQData
        {
            get
            {
                _LQData = ReadCurrentValue();
                return _LQData;
            }
        }


        private bool _channelMode;
        public bool ChannelMode
        {
            get => _channelMode;
            set => SetProperty(ref _channelMode, value);
        }

        private string GetCommandId() => $"001{(ChannelMode ? "1" : "2")}";

        public bool Connect(string portName, int baudRate)
        {
            Disconnect(); // 清理之前的连接

            try
            {                
                DeviceStatus = DeviceStatus.Connecting;

                _serialPort = new SerialPortStream(portName, baudRate)
                {
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    NewLine = "\r\n",
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                //_serialPort.DataReceived -= SerialPort_DataReceived;
                //_serialPort.DataReceived += SerialPort_DataReceived;

                _serialPort.Open();
                DeviceStatus = DeviceStatus.Idle;

                _portName = portName;
                _baudRate = baudRate;
                Task.Run(()=> SaveConfig2File());

                StartReading();
                LoggingService.Instance.LogInfo("接触式传感器连接成功");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("接触式传感器连接失败", ex);
                _serialPort?.Dispose();
                _serialPort = null;
                DeviceStatus = DeviceStatus.Disconnected;
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                StopReading();

                if (_serialPort != null)
                {
                    //_serialPort.DataReceived -= SerialPort_DataReceived;

                    if (_serialPort.IsOpen)
                        _serialPort.Close();

                    _serialPort.Dispose();
                    _serialPort = null;
                }

                DeviceStatus = DeviceStatus.Disconnected;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("断开设备失败", ex);
            }
        }

        private string SendCommand(string command)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return null;

                command += "\r\n";
                byte[] buffer = Encoding.ASCII.GetBytes(command);
                _serialPort.Write(buffer, 0, buffer.Length);

                return _serialPort.ReadLine();
            }
            catch (Exception ex)
            {
                //LoggingService.Instance.LogError("接触式传感器通信错误", ex);
                return null;
            }
        }

        public double ReadCurrentValue()
        {
            if (DeviceStatus != DeviceStatus.Idle) return 0;

            string id = GetCommandId();
            string response = SendCommand($"GCJ,{id}");
            //LoggingService.Instance.LogInfo($"{response}");
            if (response == null)
            {
                //LoggingService.Instance.LogError("未收到设备响应");
                return 0;
            }

            string[] parts = response.Split(',');
            if (parts.Length < 6)
            {
                LoggingService.Instance.LogError($"响应格式错误: {response}");
                return 0;
            }

            string errorCode = parts[2];
            string valueStr = parts[3];
            string errorFlags = parts[5].Trim();

            if (errorCode != "0")
            {
                LoggingService.Instance.LogError($"错误码: {errorCode}");

                return 0;
            }

            if (errorFlags == "30")
            {
                LoggingService.Instance.LogWarning("检测到错误标志 30，尝试清除 standby 和错误状态");

                SendCommand($"SSU,{id}");
                SendCommand($"PEC,{id}");
                Thread.Sleep(200); // 等待设备恢复
            }

            if (!long.TryParse(valueStr, out long rawValue))
            {
                LoggingService.Instance.LogError($"测量值解析失败: {valueStr}");
                return 0;
            }

            return rawValue / 100000.0 - _zeroSetting;
        }

        private void StartReading()
        {
            if (_readTimer == null)
            {
                InitTimer();
            }

            if (!_readTimer.Enabled)
                _readTimer.Start();
        }

        private void StopReading()
        {
            _readTimer?.Stop();
            _readTimer?.Dispose();
            _readTimer = null;
        }

       

        private  static  object _Lock = new object();  
        private void ReadTimerCallback(object sender, ElapsedEventArgs e)
        {
            try
            {
                double  readValue;
                lock (_Lock)
                {
                    readValue = ReadCurrentValue();
                    if (readValue > 8)
                    {
                        GlobalCollectionService<ErrorType>.Instance.Insert((int)ErrorType.HSensor, ErrorType.HSensor);
                    }
                    else
                    {
                        GlobalCollectionService<ErrorType>.Instance.Remove((int)ErrorType.HSensor, ErrorType.HSensor);
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LGQuickValue = readValue;
                    });
                }
               
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("定时读取失败", ex);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string receivedData = _serialPort.ReadExisting();
                //_dispatcher.Invoke(() => DataReceived?.Invoke(this, receivedData));
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("串口数据接收失败", ex);
            }
        }

        public bool ListenerValue(double range)
        {
            if (DeviceStatus != DeviceStatus.Idle) return false;

            try
            {
                return LGQuickValue > range;
            }
            catch
            {
                return false;
            }
        }

        public async Task SetZeroSetting()
        {
            //_zeroSetting = value;

            try
            {
                string id = GetCommandId();
                string response = SendCommand($"PZS,{id}");
                await Task.Delay(200);
                LoggingService.Instance.LogInfo("接触式传感器置零");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("接触式传感器置零异常", ex);
            }
        }

        public void SaveConfig2File()
        {
            try
            {
                JObject json = new JObject()
                {
                    { nameof(_portName), _portName},
                    { nameof(_baudRate), _baudRate},
                    { nameof(ChannelMode), ChannelMode},
                };

                ConfigStore.CheckStoreFloder();
                File.WriteAllText(LGQuickFile_path, json.ToString());
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("接触式传感器保存配置失败！", ex);
            }
        }

        private void LoadConfigForFile()
        {
            try
            {
                if (File.Exists(LGQuickFile_path))
                {
                    string info = File.ReadAllText(LGQuickFile_path);
                    JObject json = JObject.Parse(info);

                    string ip = json[nameof(_portName)]?.ToString();
                    int? port = json[nameof(_baudRate)]?.ToObject<int>();
                    bool? Channel = json[nameof(ChannelMode)]?.ToObject<bool>();

                    if (!string.IsNullOrWhiteSpace(ip) && port.HasValue)
                    {
                        _portName = ip;
                        _baudRate = port.Value;
                        ChannelMode = Channel ?? false;
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("配置文件中的接触式传感器信息不存在或格式无效");
                    }
                }
                else
                    LoggingService.Instance.LogWarning("接触式传感器配置文件不存在，初始化端口失败");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"接触式传感器加载配置文件时发生错误!", ex);
            }
        }
    }
}
