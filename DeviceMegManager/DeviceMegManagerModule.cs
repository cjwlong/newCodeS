using DeviceMegManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace DeviceMegManager
{
    public class DeviceMegManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.DeviceMeg_Region, typeof(DeviceMeg));
            regionManager.RegisterViewWithRegion(RegionManage.ProcessProgress_Region, typeof(ProcessProgress));
            regionManager.RegisterViewWithRegion(RegionManage.DeviceControl_Region, typeof(DeviceStatusControl));
            regionManager.RegisterViewWithRegion(RegionManage.DeviceSetting_Region, typeof(DeviceSettingPage));
            regionManager.RegisterViewWithRegion(RegionManage.DevicDebug_Region, typeof(DeviceDebugPage));
            //regionManager.RegisterViewWithRegion(RegionManage.CoordinateSetting_Region, typeof(CoordinateSetting));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}