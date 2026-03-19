using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public class GlobalResetEvent : PubSubEvent<bool>
    {
    }

    public class Cmd_GlobalResetEvent : PubSubEvent
    { }
}
