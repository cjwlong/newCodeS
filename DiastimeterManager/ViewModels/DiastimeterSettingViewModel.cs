using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SharedResource.events.RangeFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiastimeterManager.ViewModels
{
    public class DiastimeterSettingViewModel : BindableBase
    {
        public DiastimeterSettingViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<DiastimeterSettingConnectEvent>().Subscribe((r) =>
            {
                if (r.ToString() == "LaserRange")
                    eventAggregator.GetEvent<LaserRangefinderConnectEvent>().Publish();
                else if (r.ToString() == "ContactRange")
                    eventAggregator.GetEvent<ContactRangefinderConnectEvent>().Publish();
            });
        }

        IContainerProvider containerProvider { get; }
        IEventAggregator eventAggregator { get; }
    }
}
