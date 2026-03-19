using DiastimeterManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.events.RangeFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;

namespace DiastimeterManager.ViewModels
{
    internal class LaserRangeSettingViewModel : BindableBase
    {
        public LaserRangeSettingViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            PanasonicModbus = containerProvider.Resolve<PanasonicModbus>();

            eventAggregator.GetEvent<LaserRangefinderConnectEvent>().Subscribe(() =>
            {
                    ExecuteConnect();
            });

            eventAggregator.GetEvent<SaveSettingEvent>().Subscribe((r) =>
            {
                if (r.ToString() == "LaserRange")
                {
                    PanasonicModbus.SaveConfig2File();
                }
            });
        }

        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;

        private PanasonicModbus _panasonicModbus;
        public PanasonicModbus PanasonicModbus
        {
            get { return _panasonicModbus; }
            set
            {
                SetProperty(ref _panasonicModbus, value);
            }
        }

        private DelegateCommand _connectCommand;
        public DelegateCommand ConnectCommand =>
            _connectCommand = (_connectCommand = new DelegateCommand(ExecuteConnect));

        private DelegateCommand<string> _setOffsetCommand;
        public DelegateCommand<string> SetOffsetCommand =>
            _setOffsetCommand ?? (_setOffsetCommand = new DelegateCommand<string>((r) => {
                if (r.ToString() == "1")
                {
                    PanasonicModbus.SetPresetValues(PanasonicModbus.ReadMeasurement());
                }                    
                if (r.ToString() == "0")
                    PanasonicModbus.SetZero(false);
            }));

        public void ExecuteConnect()
        {
            Task.Run(() =>
            {
                try
                {
                    if (PanasonicModbus.DeviceStatus == SharedResource.enums.DeviceStatus.Disconnected)
                    {
                        PanasonicModbus.Connect();
                        //eventAggregator.GetEvent<DiastimeterConnectionStatusChangedEvent>().Publish(new("LaserRange", true));
                    }
                    else
                    {
                        PanasonicModbus.Disconnect();
                        //eventAggregator.GetEvent<DiastimeterConnectionStatusChangedEvent>().Publish(new("LaserRange", false));
                    }
                }
                catch
                {
                }
            });
        }
    }
}
