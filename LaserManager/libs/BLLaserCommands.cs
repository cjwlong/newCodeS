using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserManager.libs
{
    internal static class BLLaserCommands
    {
        public static readonly string GetLaserStatus = "UserStatus";//获取激光器状态
        public static readonly string StartLaser = "LaserStart";//打开激光器
        public static readonly string StopLaser = "LaserStop";//关闭激光器

        public static readonly string LaserPowerConfig = "OutputPower=";//修改激光器功率
        public static readonly string LaserFrequencyConfig = "LaserFrequency=";//修改激光器频率

        public static readonly string EmissionOn = "EmissionOn";//当前激光器内部控制出光
        public static readonly string EmissionOff = "EmissionOff";//当前激光器内部控制出光

        public static readonly string ExtTrigOn = "ExtTrigOn";//当前激光器外部控制使能
        public static readonly string ExtTrigOff = "ExtTrigOff";//当前激光器外部控制使能

        public static readonly string GATE = "EXT_TRIG_MOD=GATED";//外部触发模式：GATED:GATE 控制；TOD：自由触发
        public static readonly string TOD = "EXT_TRIG_MOD=TOD";//外部触发模式：GATED:GATE 控制；TOD：自由触发

        public static readonly string BurstNumberConfig = "BurstNumber=";//设定脉冲个数(设定值：1-15 )
        public static readonly string OutputDividerConfig = "OutputDivider=";//设定输出频率分频因子(设定值：1-1000000)
    }
}
