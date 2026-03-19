using ExternalIOManager.Interfaces;
using Modbus.Device;
using Modbus.IO;
using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExternalIOManager.libs
{
    public class IODeviceService : BindableBase, IIODeviceService
    {
        private IModbusMaster _master;
        private TcpClient _tcpClient;
        private bool _disposed = false;
        private string IOFile_path = Path.Combine(ConfigStore.StoreDir, "IO.json");

        // 配置参数\
        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                SetProperty(ref _ipAddress, value);
            }
        }

        private int _port;
        public int Port
        {
            get => _port;
            set
            {
                SetProperty(ref _port, value);
            }
        }

        private DeviceStatus _deviceStatus = DeviceStatus.Disconnected;
        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set
            {
                SetProperty(ref _deviceStatus, value);
            }
        }

        private const int Timeout = 1000; // 通信超时时间(毫秒)

        public IODeviceService()
        {
            LoadConfigForFile();
        }

        public async Task Connect()
        {
            try
            {

                _tcpClient = new TcpClient();
                await Task.Run(() =>
                {
                    try
                    {
                        _tcpClient.Connect(IPAddress.Parse(IpAddress), Port);
                        // 使用NModbus4创建Modbus TCP主站
                        _master = ModbusIpMaster.CreateIp(_tcpClient);
                        DeviceStatus = DeviceStatus.Connecting;

                    }
                    catch (Exception ex)
                    {
                        DeviceStatus = DeviceStatus.Disconnected;
                    }
                });
                // 设置超时
                _tcpClient.ReceiveTimeout = Timeout;
                _tcpClient.SendTimeout = Timeout;

                if (_master == null)
                {
                    DeviceStatus = DeviceStatus.Disconnected;
                    throw new Exception("IO模块连接失败");
                }
                DeviceStatus = DeviceStatus.Idle;
            }
            catch (Exception ex)
            {
                DeviceStatus = DeviceStatus.Disconnected;
                throw;
            }
        }

        public bool[] ReadOutputCoils(ushort start, ushort length, byte slaveAddress = 1)
        {
            try
            {
                if (_tcpClient != null)
                {

                    if (!_tcpClient.Connected)
                        Reconnect();

                    return _master.ReadCoils(slaveAddress, start, length);
                }
                else
                {
                    return new bool[length];
                }
            }
            catch (Exception ex)
            {
                // 发生异常时尝试重新连接
                Reconnect();
                throw new Exception("【读取输出线圈】失败：" + ex.Message);
            }
        }

        public string ReadInput(int device, out bool value)
        {
            try
            {
                if (_tcpClient == null)
                {
                    value = false;
                    return "null";
                }
                if (!_tcpClient.Connected)
                    Reconnect();
                if (_master == null)
                {
                    value = false;
                    return "null";
                }
                var inputs = _master.ReadInputs(1, 0, 8); // 读取8个离散输入
                value = inputs[device];
                return null;
            }
            catch (Exception ex)
            {
                // 发生异常时尝试重新连接
                Reconnect();
                value = false;
                return ex.Message;
            }
        }

        public void WriteSingleCoil(ushort coilAddress, bool value, byte slaveAddress = 1)
        {
            try
            {
                if(_tcpClient==null)
                {
                    return;
                }
                if (!_tcpClient.Connected)
                    Reconnect();
                if(_master==null)
                {
                    return;
                }
                _master.WriteSingleCoil(slaveAddress, coilAddress, value);
            }
            catch (Exception ex)
            {
                // 发生异常时尝试重新连接
                Reconnect();
                LoggingService.Instance.LogError("【写入输出线圈】失败", ex);
            }
        }

        private void Reconnect()
        {
            try
            {
                if (_tcpClient.Connected)
                    _tcpClient.Close();

                _tcpClient.Connect(IPAddress.Parse(IpAddress), Port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Modbus重新连接失败: {ex.Message}");
            }
        }

        // 实现IDisposable接口
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _master?.Dispose();
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                }

                _disposed = true;
                DeviceStatus = DeviceStatus.Disconnected;
            }
        }

        ~IODeviceService()
        {
            Dispose(false);
        }

        public void SaveConfig2File()
        {
            try
            {
                JObject json = new JObject()
                {
                    { nameof(IpAddress), IpAddress},
                    { nameof(Port), Port},
                };

                ConfigStore.CheckStoreFloder();
                File.WriteAllText(IOFile_path, json.ToString());
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("IO设备保存配置失败！", ex);
            }
        }

        public void LoadConfigForFile()
        {
            try
            {
                if (File.Exists(IOFile_path))
                {
                    string info = File.ReadAllText(IOFile_path);
                    JObject json = JObject.Parse(info);

                    string ip = json[nameof(IpAddress)]?.ToString();
                    int? port = json[nameof(Port)]?.ToObject<int>();

                    if (!string.IsNullOrWhiteSpace(ip) && port.HasValue)
                    {
                        IpAddress = ip;
                        Port = port.Value;
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("配置文件中的IO设备信息不存在或格式无效");
                    }
                }
                else
                {
                    LoggingService.Instance.LogWarning("未找到IO设备配置文件，初始化IO连接信息失败");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"IO设备加载配置文件时发生错误!", ex);
            }
        }
    }
}
