using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public static class StaticEventAggregator
    {
        public static IEventAggregator eventAggregator;
        static StaticEventAggregator()
        {

        }
    }
}
