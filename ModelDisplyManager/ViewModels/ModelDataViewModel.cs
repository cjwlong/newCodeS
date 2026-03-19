using DiastimeterManager.libs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelDisplyManager.ViewModels
{
  public  class ModelDataViewModel : BindableBase
    {
        public ModelDataViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            hL_G2_Ranger = containerProvider.Resolve<PanasonicModbus>();
            mitutoyo_EJ_Ranger = containerProvider.Resolve<LGQuick>();
            //hL_G2_Ranger.PanasonicModbusValue = 30.1;

        }

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;

        public PanasonicModbus hL_G2_Ranger { get; set; }
        public LGQuick mitutoyo_EJ_Ranger { get; set; }
    }
}
