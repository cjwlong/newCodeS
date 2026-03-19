using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Enums
{
    internal enum JDNcAlmInfo
    {
        NCALM_NO, //无报警
        NCALM_EMG, //急停
        NCALM_ERR, //报警
        NCALM_UPS, //外部电源已掉电
        NCALM_PROMPT, //提示
        NCALM_WNG //警告类： 伺服警告 变频警告 外部设备警告
    };
}
