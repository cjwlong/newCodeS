using Machine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Models
{
    // 轴的物理状态
    public class AxisStatusModel
    {
        // 以下为默认值
        public double Position = 0;
        public double PositionError = 0;
        public bool? Enabled = null;
        public bool? IsMoving = null;
        public LimitStatus LeftLimit = LimitStatus.NotEnabled;
        public LimitStatus RightLimit = LimitStatus.NotEnabled;
        public double LeftSoftLimit = double.MinValue;
        public double RightSoftLimit = double.MaxValue;
    }
}
