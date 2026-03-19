using CCD.tools;
using DiastimeterManager.libs;
using LaserManager.libs;
using Machine.Interfaces;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events.RangeFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeviceMegManager.ViewModels
{
    public class DeviceMegViewModel : BindableBase
    {
        public DeviceMegViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            bLLaser = containerProvider.Resolve<BLLaser>();
            hL_G2_Ranger = containerProvider.Resolve<PanasonicModbus>();
            mitutoyo_EJ_Ranger = containerProvider.Resolve<LGQuick>();
        }

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        public BLLaser bLLaser { get; set; }
        public PanasonicModbus hL_G2_Ranger { get; set; }
        public LGQuick mitutoyo_EJ_Ranger { get; set; }
    }
}
