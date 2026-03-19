using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RangeFinderManager.Views;
using SharedResource.libs;

namespace RangeFinderManager
{
    public class RangeFinderManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.RangeFinderSetting_Region, typeof(RangeFinderSetting));
            regionManager.RegisterViewWithRegion(RegionManage.LaserRangeSetting_Region, typeof(LaserRangeSetting));
            regionManager.RegisterViewWithRegion(RegionManage.ContactRangeSetting_Region, typeof(ContactRangeSetting));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}