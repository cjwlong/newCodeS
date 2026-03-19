using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events.eventMeg
{
    public class CcdFocusMoveMessage
    {
        public double FocusDistance;
        public double FocusSpeed;
        public List<(DateTime, double)> TimePositions = new();
    }
}
