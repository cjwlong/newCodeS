using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalIOManager.libs
{
    public class IODeviceStatus
    {
        public bool RedLamp { get; set; }
        public bool YellowLamp { get; set; }
        public bool GreenLamp { get; set; }
        public bool Alarm { get; set; }
        public bool EmergencyStop { get; set; }
        public bool DoorOpen { get; set; }

        public bool GasPressure { get; set; }
    }

    public enum IOstatus { connected, connecting, disconnect };
}
