using MenuControl.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace MenuControl
{
    public class MenuControlModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.MenuControlBtn_Region, typeof(MenuControl.Views.MenuControlBtn));
            regionManager.RegisterViewWithRegion(RegionManage.MenuLogin_Region, typeof(MenuControl.Views.MenuLogin));
            regionManager.RegisterViewWithRegion(RegionManage.SoftwareMsg_Region, typeof(MenuMegDisplay));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}