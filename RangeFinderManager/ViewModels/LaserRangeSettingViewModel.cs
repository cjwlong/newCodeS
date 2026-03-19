using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using RangeFinderManager.libs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangeFinderManager.ViewModels
{
    public class LaserRangeSettingViewModel : BindableBase
    {
        IContainerProvider containerProvider;
        public HL_G2_Ranger HLG2Ranger { get; set; }

        public DelegateCommand SetZeroOffsetCommand { get; private set; }
        public DelegateCommand CancelSetZeroOffsetCommand { get; private set; }
        public DelegateCommand<string> RangerConnectCommand { get; private set; }

        public LaserRangeSettingViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            HLG2Ranger = containerProvider.Resolve<HL_G2_Ranger>();

            SetZeroOffsetCommand = new(() => HLG2Ranger.SetZeroOffset());

            CancelSetZeroOffsetCommand = new(() => HLG2Ranger.CancelSetZeroOffset());

            RangerConnectCommand = new((r) =>
            {
                if (r == "HL_G2")
                {
                    HLG2Ranger.RangerType = "HL_G2";
                    if (HLG2Ranger.IsConnected == true)
                        HLG2Ranger.Disconnect();
                    else if (HLG2Ranger.IsConnected == false)
                        HLG2Ranger.Connect();
                }
            });
        }
    }
}
