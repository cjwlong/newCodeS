using OperationLogManager.libs;
using Prism.Mvvm;
using SharedResource.enums;
using System;
using System.Text.RegularExpressions;
using EasyModbus;
using System.IO;
using SharedResource.tools;
using Newtonsoft.Json.Linq; // 使用 EasyModbus

namespace DiastimeterManager.libs
{
    public class PanasonicModbus : BindableBase
    {
        public PanasonicModbus()
        {
            InitializeTimer();
            LoadConfigForFile();
        }

        private ModbusClient _modbusClient;
        private string PanasonicFile_path = Path.Combine(ConfigStore.StoreDir, "Panasonic.json");
        private System.Timers.Timer _updateTimer;

        private DeviceStatus _deviceStatus = DeviceStatus.Disconnected;
        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        private double _panasonicModbusValue;
        public double PanasonicModbusValue
        {
            get => _panasonicModbusValue;
            private set => SetProperty(ref _panasonicModbusValue, value);
        }

        private int _Port = 502;
        public int Port
        {
            get => _Port;
            set
            {
                if (_Port == value) return;
                SetProperty(ref _Port, value);
            }
        }

        private int _slaveAddress = 99;
        public int SlaveAddress
        {
            get => _slaveAddress;
            set
            {
                if (_slaveAddress == value) return;
                SetProperty(ref _slaveAddress, value);
            }
        }

        private string _ip = "192.168.1.6";
        public string IP
        {
            get => _ip;
            set
            {
                if (_ip == value) return;
                var regex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
                if (!regex.IsMatch(value)) return;
                SetProperty(ref _ip, value);
            }
        }

        public double ReadMeasurement()
        {
            if (DeviceStatus != DeviceStatus.Idle) return 0;
            try
            {
                int[] data = _modbusClient.ReadHoldingRegisters(19, 2); // 地址19对应400020
                byte[] bytes = new byte[]
                {
                    (byte)(data[0] >> 8), (byte)data[0],
                    (byte)(data[1] >> 8), (byte)data[1]
                };
                int rawValue = BitConverter.ToInt32(bytes, 0);
                return rawValue * 0.1 / 1000; // 转毫米
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光位移传感器读取测量值失败", ex);
                return double.NaN;
            }
        }

        public void SetRegisterValue()
        {
            if (DeviceStatus != DeviceStatus.Idle) return;
            try
            {
                int address = 400169 - 400001;
                _modbusClient.WriteMultipleRegisters(address, new int[] { SlaveAddress });
                LoggingService.Instance.LogInfo($"成功设置寄存器 400169 的值为 {SlaveAddress}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("设置寄存器 400169 的值失败", ex);
            }
        }

        public void SetPresetValues(double offset)
        {
            try
            {
                int offsetRaw = (int)(offset * 10000);
                int high = (offsetRaw >> 16) & 0xFFFF;
                int low = offsetRaw & 0xFFFF;
                _modbusClient.WriteMultipleRegisters(117, new int[] { high, low });
                LoggingService.Instance.LogInfo("激光位移传感器设置偏置成功");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光位移传感器预设值设定失败", ex);
            }
        }

        public void SetZero(bool enable)
        {
            try
            {
                ushort modbusAddress = 51; // 400052 - 400001
                int value = enable ? 1 : 0;
                _modbusClient.WriteMultipleRegisters(modbusAddress, new int[] { value });
                LoggingService.Instance.LogInfo("激光位移传感器恢复偏置成功");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光位移传感器恢复偏置异常", ex);
            }
        }

        public void TurnOrOff(bool mode)
        {
            string temp = mode ? "关闭" : "打开";
            try
            {
                int address = 400150 - 400001;
                int value = mode ? 1 : 0;
                _modbusClient.WriteMultipleRegisters(address, new int[] { value });
                LoggingService.Instance.LogInfo($"激光位移传感器{temp} 光");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"激光位移传感器{temp} 光异常", ex);
            }
        }

        public bool Connect()
        {
            try
            {
                if (_modbusClient != null)
                {
                    if (_modbusClient.Connected)
                        _modbusClient.Disconnect();

                    _modbusClient = null;
                }

                DeviceStatus = DeviceStatus.Connecting;
                _modbusClient = new ModbusClient(IP, Port);
                _modbusClient.UnitIdentifier = (byte)SlaveAddress;
                _modbusClient.Connect();

                LoggingService.Instance.LogInfo("激光位移传感器连接成功");
                DeviceStatus = DeviceStatus.Idle;

                if (_updateTimer == null)
                {
                    InitializeTimer();
                }

                _updateTimer.Start();
                TurnOrOff(false);
                return true;
            }
            catch (Exception ex)
            {
                DeviceStatus = DeviceStatus.Disconnected;
                LoggingService.Instance.LogError("激光位移传感器连接失败", ex);
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_modbusClient == null || !_modbusClient.Connected)
                    return;

                DeviceStatus = DeviceStatus.Connecting;
                TurnOrOff(true);
                _modbusClient?.Disconnect();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光位移传感器断开失败", ex);
            }
            finally
            {
                DeviceStatus = DeviceStatus.Disconnected;
                _modbusClient = null;
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Timers.Timer(500);
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;
        }


        private void OnUpdateTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(DeviceStatus == DeviceStatus.Idle)
            {
                try
                {
                    double newMeasurement = ReadMeasurement();

                    if (!double.IsNaN(newMeasurement))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            PanasonicModbusValue = newMeasurement;
                        });
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("定时器自动更新测量值失败", ex);
                }
            }
        }

        public void SaveConfig2File()
        {
            try
            {
                JObject json = new JObject()
                {
                    { nameof(IP), IP},
                    { nameof(Port), Port},
                };

                ConfigStore.CheckStoreFloder();
                File.WriteAllText(PanasonicFile_path, json.ToString());
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光测距仪保存配置失败！", ex);
            }
        }

        private void LoadConfigForFile()
        {
            try
            {
                if (File.Exists(PanasonicFile_path))
                {
                    string info = File.ReadAllText(PanasonicFile_path);
                    JObject json = JObject.Parse(info);

                    string ip = json[nameof(IP)]?.ToString();
                    int? port = json[nameof(Port)]?.ToObject<int>();

                    if (!string.IsNullOrWhiteSpace(ip) && port.HasValue)
                    {
                        IP = ip;
                        Port = port.Value;
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("配置文件中的激光测距仪信息不存在或格式无效");
                    }
                }
                else
                    LoggingService.Instance.LogWarning("激光测距仪配置文件不存在，初始化IP失败");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"激光测距仪加载配置文件时发生错误!", ex);
            }
        }
    }
}
