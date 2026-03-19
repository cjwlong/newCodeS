using ParamConfigManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace ParamConfigManager
{
    public class ParamConfigManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var ragionManager = containerProvider.Resolve<IRegionManager>();
            ragionManager.RegisterViewWithRegion(RegionManage.CraftConfig_Region, typeof(ParamConfig));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}