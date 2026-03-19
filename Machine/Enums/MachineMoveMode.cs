using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Enums
{
    public enum MachineMoveMode
    {
        [Description("绝对")]
        Absolute,

        [Description("相对")]
        Relative,

        //[Description("点动")]
        //Jog,

        //[Description("往复")]
        //RoundTrip
    }
}
