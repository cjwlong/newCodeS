using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    public class GlobalProcessStatus : BindableBase
    {
        public GlobalProcessStatus(IContainerProvider provider) 
        { 
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
        }

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;

        private G_DeviceStatus _deviceStatus = G_DeviceStatus.Empty;
        public G_DeviceStatus DeviceStatus
        {
            get { return _deviceStatus; }
            set
            {
                if (value == _deviceStatus) return;
                SetProperty(ref _deviceStatus, value);
            }
        }

        private G_ProcessStatus _processStatus = G_ProcessStatus.UnReset;
        public G_ProcessStatus ProcessStatus
        {
            get { return _processStatus; }
            set
            {
                if (value == _processStatus) return;
                if (value == G_ProcessStatus.Processing ||
                    value == G_ProcessStatus.Pause)
                    DeviceStatus = G_DeviceStatus.intact;
                if (value == G_ProcessStatus.Finished)
                    DeviceStatus = G_DeviceStatus.Empty;
                SetProperty(ref _processStatus, value);
            }
        }


        private ProcessType _processType = ProcessType.Stop;
        public ProcessType ProcessType
        {
            get { return _processType; }
            set
            {
                if (value == _processType) return;
                SetProperty(ref _processType, value);
            }
        }
    }
}
