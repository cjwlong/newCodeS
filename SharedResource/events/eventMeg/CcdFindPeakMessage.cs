using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events.eventMeg
{
    public class CcdFindPeakMessage
    {
        public bool isOnDown = true;
        public double MoveSpeed;
        public List<(DateTime, double)> OnDown = new();
        public List<(DateTime, double)> LeftRight = new();
    }
}
