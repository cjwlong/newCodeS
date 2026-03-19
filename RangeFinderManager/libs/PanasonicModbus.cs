using System.Net.Sockets;
using System.Text.RegularExpressions;
using Prism.Mvvm;
using System.Windows;
using System;
using Modbus.Device;
using System.Threading;
using RangeFinderManager.Interface;
using OperationLogManager.libs;

namespace RangeFinderManager.libs
{
    internal class PanasonicModbus : IRangerHardware
    {
        private TcpClient _tcpClient;
        private IModbusMaster _modbusMaster;

        public PanasonicModbus(string ip, int port)
        {
            _ip = ip;
            _port = port;
            //LoggingService.Instance.LogInfo($"初始化IP:{_ip},Port:{_port}");
        }

        private bool _connected = false;

        private string _ip = "192.168.1.6";
        private int _port = 502;
        private int _slaveAddress = 99;
        public int SlaveAddress => _slaveAddress;

        private bool _isRational;
        private string _error;
        private double _distance;

        public bool IsRational => _isRational;

        public string Error => _error;

        public double Distance => _distance;

        ///// <summary>
        ///// 设置从机地址
        ///// </summary>
        //public void SetRegisterValue()
        //{
        //    if (!_connected) return;
        //    try
        //    {
        //        // 转换Modbus地址为寄存器地址
        //        ushort modbusAddress = (ushort)(400169 - 400001);
        //        ushort[] data = new ushort[] { (ushort)SlaveAddress };
        //        _modbusMaster.WriteMultipleRegisters((byte)SlaveAddress, modbusAddress, data);
        //        MessageBox.Show($"成功设置寄存器 {400169} 的值为 {SlaveAddress}");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"设置寄存器 {400169} 的值失败: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// 置零：true-启动，false-取消
        ///// </summary>
        ///// <param name="enable"></param>
        //public void SetZero(bool enable)
        //{
        //    try
        //    {
        //        ushort modbusAddress = 51;
        //        ushort value = enable ? (ushort)1 : (ushort)0;
        //        _modbusMaster.WriteMultipleRegisters((byte)SlaveAddress, modbusAddress, new ushort[] { value });
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //        throw;
        //    }
        //}
        ///// <summary>
        ///// 初始化：Bank 1，Bank 2，Bank 3，Bank 4，ALL Bank 5，ALL Areas 6
        ///// </summary>
        ///// <param name="initializationType"></param>
        //public void InitializeSensor(int initializationType)
        //{
        //    try
        //    {
        //        if (initializationType < 1 || initializationType > 6)
        //        {
        //            MessageBox.Show("初始化类型需在1-6之间");
        //            return;
        //        }

        //        ushort modbusAddress = 61; // 对应400062寄存器
        //        _modbusMaster.WriteSingleRegister((byte)SlaveAddress, modbusAddress, (ushort)initializationType);
        //        MessageBox.Show($"初始化成功，类型：{initializationType}");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"初始化失败：{ex.Message}");
        //    }
        //}

        public bool Connect()
        {
            try
            {
                _tcpClient = new TcpClient(_ip, _port);
                _modbusMaster = ModbusIpMaster.CreateIp(_tcpClient);
                _connected = true;
                TurnOnLaser();
                LoggingService.Instance.LogInfo("连接激光测距");
                return true;
            }
            catch (Exception ex)
            {
                _connected = false;
                LoggingService.Instance.LogError($"激光测距连接失败", ex);
                return false;
            }
        }

        public bool Disconnect()
        {
            try
            {
                TurnOffLaser();
                _modbusMaster?.Dispose();
                _tcpClient?.Close();
                _connected = false;
            }
            catch (Exception)
            {
                _connected = true;
                return false;
            }
            return true;
        }

        public (bool IsRational, string Error, double Distance) RefreshStatus()
        {
            _distance = 0.0;
            try
            {
                Thread.Sleep(30);
                // 400020 对应 Modbus 地址 19（400020 - 400001 = 19），读取 2 个寄存器（共 4 字节）
                ushort startAddress = 19;
                ushort numberOfPoints = 2;
                ushort[] data = _modbusMaster.ReadHoldingRegisters((byte)SlaveAddress, startAddress, numberOfPoints);

                int rawValue = BitConverter.ToInt32(new byte[] {
                (byte)data[0],          // 第一个寄存器低字节
                (byte)(data[0] >> 8),  // 第一个寄存器高字节
                (byte)data[1],           // 第二个寄存器低字节
                (byte)(data[1] >> 8)  // 第二个寄存器高字节
            }, 0);
                _isRational = true;
                _error = null;
                _distance = rawValue * 0.1 / 1000;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"刷新端口异常", ex);
                _isRational = false;
                _error = ex.Message;
                _distance = double.NaN;
            }
            return (_isRational, _error, _distance);
        }

        /// <summary>
        /// 打开或关闭激光
        /// </summary>
        /// <param name="isLaserOn">true=打开激光，false=关闭激光</param>
        public void SetLaser(bool isLaserOn)
        {
            try
            {
                ushort modbusAddress = 49; // 对应400050寄存器
                ushort value = isLaserOn ? (ushort)1 : (ushort)0;
                _modbusMaster.WriteSingleRegister((byte)SlaveAddress, modbusAddress, value);
                LoggingService.Instance.LogInfo($"激光已{(isLaserOn ? "打开" : "关闭")}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光控制失败!", ex);
            }
        }

        /// <summary>
        /// 打开激光
        /// </summary>
        public void TurnOnLaser()
        {
            SetLaser(true);
        }

        /// <summary>
        /// 关闭激光
        /// </summary>
        public void TurnOffLaser()
        {
            SetLaser(false);
        }
    }
}
