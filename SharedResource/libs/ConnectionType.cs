using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    public enum ConnectionType
    {
        Sdk = 0x0001,   // 使用sdk内部的通讯
        SdkIp = 0x0002,     // 需要提供IP的SDK通讯
        SdkIpPort = 0x0003, // 需要提供IP&Port的SDK通讯

        Serial = 0x0010,    // 串口
        Socket = 0x0020,    // Socket
        RS232 = 0x0030,    // 232
        RS485 = 0x0040,    // 485

        Io = 0x0100,   // IO
        Pipe = 0x1000,    // 此项备用
    }
}
