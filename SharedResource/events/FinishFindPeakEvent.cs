using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public class FinishFindPeakEvent : PubSubEvent<Tuple<bool, double>>
    {

    }
}
