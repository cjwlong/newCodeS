using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangeFinderManager.Interface
{
    public interface IRanger
    {
        DeviceStatus DeviceStatus { get; }
        double? Distance { get; }  //传感器的测量值
        bool? IsConnected { get; } //传感器是否连接
        bool IsRational { get; }
        void Connect();
        void Disconnect();
    }
}
