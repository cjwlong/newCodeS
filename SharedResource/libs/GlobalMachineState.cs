using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    public class GlobalMachineState : BindableBase
    {
        private bool _isMachineRunning = false;
        public bool IsMachineRunning
        {
            get => _isMachineRunning;
            set { if (value == _isMachineRunning) return; SetProperty(ref _isMachineRunning, value); }
        }

        private bool _axisEnabled;
        public bool AxisEnabled
        {
            get => _axisEnabled;
            set { if (value == _axisEnabled) return; SetProperty(ref _axisEnabled, value); }
        }

        private bool _laserOk = true;
        public bool LaserOk
        {
            get => _laserOk;
            set { if (value == _laserOk) return; SetProperty(ref _laserOk, value); }
        }

        private bool _limitSafe = true;
        public bool LimitSafe
        {
            get => _limitSafe;
            set { if (value == _limitSafe) return; SetProperty(ref _limitSafe, value); }
        }
    }
}
