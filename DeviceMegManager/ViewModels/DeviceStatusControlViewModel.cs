using CCD.ViewModels;
using DiastimeterManager.libs;
using ExternalIOManager.Interfaces;
using ExternalIOManager.libs;
using LaserManager.libs;
using Machine.Interfaces;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.events.MVS_CCD;
using SharedResource.events.RangeFinder;
using SharedResource.libs;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeviceMegManager.ViewModels
{
    public class DeviceStatusControlViewModel : BindableBase
    {
        public DeviceStatusControlViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            Machine = containerProvider.Resolve<IMachine>();
            bLLaser = containerProvider.Resolve<BLLaser>();
            hL_G2_Ranger = containerProvider.Resolve<PanasonicModbus>();
            mitutoyo_EJ_Ranger = containerProvider.Resolve<LGQuick>();
            ioDeviceService = containerProvider.Resolve<IIODeviceService>();
            //cCDControlViewModel = containerProvider.Resolve<CCDControlViewModel>();
            eventAggregator.GetEvent<CamerConnectStatus>().Subscribe((meg) =>
            {
                CameraState = meg;
            });
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;

        public IMachine Machine { get; private set; }
        //public CCDControlViewModel cCDControlViewModel { get; private set; }
        public BLLaser bLLaser { get; set; }
        public PanasonicModbus hL_G2_Ranger { get; set; }
        public LGQuick mitutoyo_EJ_Ranger { get; set; }
        public IIODeviceService ioDeviceService { get; set; }

        private CameraState _cameraState = CameraState.Disconnected;
        public CameraState CameraState
        {
            get { return _cameraState; }
            set
            {
                SetProperty(ref _cameraState, value);
            }
        }

        private DelegateCommand<string> _deviceConnectCommand;
        public DelegateCommand<string> DeviceConnectCommand => _deviceConnectCommand ??
            (_deviceConnectCommand = new DelegateCommand<string>(async (r) =>
            {
                switch (r.ToString())
                {
                    case "运动机床":
                        if (Machine.DeviceStatus == DeviceStatus.Disconnected)
                            Machine.Connect();
                        else if (Machine.DeviceStatus == DeviceStatus.Idle)
                            Machine.Disconnect();
                        else
                            MessageWindow.ShowDialog("机床正忙！");
                        break;
                    case "激光器":
                        eventAggregator.GetEvent<BLLaserConnectEvent>().Publish();
                        //await Task.Run(async () =>
                        //{
                        //    string msg = "";
                        //    if (!bLLaser.IsConnected)
                        //    {
                        //        bLLaser.DisplayText = "激光器连接中...";
                        //        try
                        //        {
                        //            await Task.Run(() => bLLaser.Connect());
                        //        }
                        //        catch (Exception ex)
                        //        {
                        //            bLLaser.DisplayText = "激光器连接失败";
                        //            MessageWindow.ShowDialog($"激光器连接失败{ex.Message}");
                        //            return;
                        //        }
                        //    }
                        //    else if (bLLaser.IsConnected)
                        //    {
                        //        bLLaser.DisplayText = "激光器断开中...";
                        //        try
                        //        {
                        //            await Task.Run(() => bLLaser.DisConnect());
                        //            bLLaser.DisplayText = "激光器断开连接";
                        //        }
                        //        catch (Exception ex)
                        //        {
                        //            MessageWindow.ShowDialog($"激光器断开失败{ex.Message}");
                        //        }
                        //    }
                        //});
                        break;
                    case "相机":
                        eventAggregator.GetEvent<CcdToggleConnectionEvent>().Publish();
                        break;
                    case "激光测距仪":
                        eventAggregator.GetEvent<DiastimeterSettingConnectEvent>().Publish("LaserRange");
                        break;
                    case "接触式测距仪":
                        eventAggregator.GetEvent<DiastimeterSettingConnectEvent>().Publish("ContactRange");
                        break;
                    case "IO设备":
                        eventAggregator.GetEvent<ioDeviceConnectEvent>().Publish();
                        break;
                    default:
                        break;
                }
            }));
    }
}
