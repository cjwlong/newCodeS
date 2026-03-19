using System.Text;
using System.Windows.Threading;
using System.Windows;
using System;
using Prism.Mvvm;
using System.IO.Ports;
using RangeFinderManager.Interface;
using OperationLogManager.libs;
using RJCP.IO.Ports;

namespace RangeFinderManager.libs
{
    internal class LGQuick : IRangerHardware
    {
        private SerialPortStream _serialPort;

        private bool _isRational;
        private string _error;
        private double _distance;
        public bool IsRational => _isRational;

        public string Error => _error;

        public double Distance => _distance;

        private bool _connected = false;


        public LGQuick(SerialPortStream serialPort)
        {
            _serialPort = serialPort;
        }
        private string SendCommand(string command)
        {
            try
            {
                command += "\r\n"; // 添加CRLF
                byte[] buffer = Encoding.ASCII.GetBytes(command);
                _serialPort.Write(buffer, 0, buffer.Length);

                // 读取响应（假设响应以CRLF结尾）
                return _serialPort.ReadLine();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"LGQuick_通信错误", ex);
                return null;
            }
        }

        public bool Connect()
        {
            try
            {
                _serialPort.Open();
                _connected = true;
                LoggingService.Instance.LogInfo("笔触连接成功");
                return true;
            }
            catch
            {
                _connected = false;
                return false;
            }
        }

        public bool Disconnect()
        {
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _connected = false;
                    return true;
                }
                catch (Exception)
                {
                    _connected = true;
                    return false;
                }
            }
            else
                return false;
        }

        public (bool IsRational, string Error, double Distance) RefreshStatus()
        {
            _distance = 0.0;

            string response = SendCommand("GCJ,0011");
            if (response != null)
            {
                string[] parts = response.Split(',');
                if (parts.Length >= 5)
                {
                    string errorCode = parts[2];
                    string value = parts[3];
                    string toleranceResult = parts[4];

                    if (errorCode != "0")
                    {
                        _isRational = false;
                        _error = "错误码：0";
                    }
                    _isRational = true;
                    if (double.TryParse(value,
                System.Globalization.NumberStyles.Float, // 允许小数点/正负号
                System.Globalization.CultureInfo.InvariantCulture, // 固定小数点为 '.'
                out double result))
                    {
                        _distance = result / 100000;
                    }
                    else
                    {
                        _isRational = false;
                        _error = "值转换失败";
                    }
                }
                else { _isRational = false; _error = "错误码：0"; }
            }
            else { _isRational = false; _error = "错误码：0"; }
            return (_isRational, _error, _distance);
        }

    }
}
