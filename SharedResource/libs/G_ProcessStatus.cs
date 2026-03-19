using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    public enum G_DeviceStatus
    {
        Empty = -1,     // 不可加工状态
        intact = 0,     // 就位
    }

    public enum G_ProcessStatus
    {
        UnReset = 1,    // 未复位
        Reseted = 2,    // 已复位
        Processing = 3, // 加工中
        Pause = 4,      // 暂停
        Stopped = 5,
        Finished = 6,     // 已完成
    }

    public enum ProcessType
    {
        Stop = 1,    // 停止
        Running = 2,    // 加工运行中
        OK = 3,
    }


    public enum ProcessError
    {
        None = 1,    // 没开始原始状态
        Error = 2, // 错误
        OK = 3,      // 无
        ACSError = 4,  //acs错误
        ExError = 5,     // 程序异常报错
    }

    public  enum ErrorType
    {
        None = 0,    // 无错误
        LaserError=1,
        AxisOff=2,
        HPressure=3,
        HSensor=4,//>8MM

    }

    public enum LaserErrorType
    {
       
        ConError =0,
        LaserStatusError = 1,
        StatusReturnError=2,


    }
}
