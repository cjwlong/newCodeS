using LaserManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace LaserManager
{
    public class LaserManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.LaserSetting_Region, typeof(LaserSetting));
            regionManager.RegisterViewWithRegion(RegionManage.LaserDebug_Region, typeof(LaserDebug));
     
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}