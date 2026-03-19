using Prism.Events;
using SharedResource.events.eventMeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public class CcdFindPeakEvent : PubSubEvent<CcdFindPeakMessage>
    {
    }
}
