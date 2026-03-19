using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Models
{
    public class AxisDefination
    {
        public string Name = "P";
        public int NodeNum = 0;
        public double Speed = 10;
        public double? MinSoftLimit = null;
        public double? MaxSoftLimit = null;
        public double? LeftRetractThreshold = null;
        public double? RightRetractThreshold = null;

        public double acceleratedSpeed = 100;
        public double decelerationSpeed = 100;

        public bool isHomeed = false;
    }
}
