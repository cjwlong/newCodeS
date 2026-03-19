using Prism.Events;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public class SetAxesParamEvent : PubSubEvent<AxesParameters>
    {
    }
}
