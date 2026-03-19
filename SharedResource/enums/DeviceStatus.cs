using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.enums
{
    public enum DeviceStatus
    {
        Disconnected = 0x01,  // 断开
        Error = 0x02,         // 错误/报警
        Connecting = 0x04,     // 连接中
        Idle = 0x08,           // 空闲
        Busy = 0x10,           // 正忙
    }
}
