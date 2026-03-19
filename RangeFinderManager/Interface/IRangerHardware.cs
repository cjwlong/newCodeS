using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangeFinderManager.Interface
{
    internal interface IRangerHardware
    {
        bool IsRational { get; }
        string Error { get; }
        double Distance { get; }

        bool Connect();
        bool Disconnect();

        (bool IsRational, string Error, double Distance) RefreshStatus();
    }
}
