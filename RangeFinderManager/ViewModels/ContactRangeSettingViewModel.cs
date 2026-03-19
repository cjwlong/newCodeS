using Prism.Commands;
using Prism.Ioc;
using RangeFinderManager.libs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangeFinderManager.ViewModels
{
    internal class ContactRangeSettingViewModel
    {
        public ContactRangeSettingViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            MitutoyoEJRanger = containerProvider.Resolve<Mitutoyo_EJ_Ranger>();


            RangerConnectCommand = new(() =>
            {
                MitutoyoEJRanger.RangerType = "Mitutoyo_EJ";
                if (MitutoyoEJRanger.IsConnected == true)
                    MitutoyoEJRanger.Disconnect();
                else if (MitutoyoEJRanger.IsConnected == false)
                    MitutoyoEJRanger.Connect();
            });

            RefreshPortConnectCommand = new(() => {
                MitutoyoEJRanger.InitCom();
            });
        }

        IContainerProvider containerProvider;

        public ObservableCollection<int> BaudRateLists { get; } = new ObservableCollection<int>() {
            9600, 19200, 31250, 38400,
            57600, 74880, 115200, 230400,
            250000, 460800, 500000, 921600 };

        public Mitutoyo_EJ_Ranger MitutoyoEJRanger { get; set; }
        public DelegateCommand RangerConnectCommand { get; private set; }
        public DelegateCommand RefreshPortConnectCommand { get; private set; }
    }
}
