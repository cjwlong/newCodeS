using CCD.ViewModels;
using DiastimeterManager.libs;
using LaserManager.libs;
using Machine.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceMegManager.ViewModels
{
    internal class DeviceSettingPageViewModel : BindableBase
    {
        public DeviceSettingPageViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();           
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;

        private DelegateCommand<string> _saveSettingsCommand;
        public DelegateCommand<string> SaveSettingsCommand => _saveSettingsCommand ??
            (_saveSettingsCommand = new DelegateCommand<string>((r) =>
            {
                string info = "";
                switch (r.ToString())
                {
                    case "运动机床":
                        info = "Machine";
                        break;
                    case "激光器":
                        info = "Laser";
                        break;
                    case "相机":
                        info = "Camer";
                        break;
                    case "测距仪":
                        info = "LaserRange";
                        break;
                    case "IO":
                        info = "IO";
                        break;
                    default:
                        break;
                }

                if (info != "") eventAggregator.GetEvent<SaveSettingEvent>().Publish(info);
            }));
    }
}
