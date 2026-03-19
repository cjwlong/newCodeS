using ExternalIOManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace ExternalIOManager
{
    public class ExternalIOManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.IODeviceSetting_Region, typeof(IoMonitor));
            regionManager.RegisterViewWithRegion(RegionManage.IODeviceDispaly_Region, typeof(IOMegDisplay));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}