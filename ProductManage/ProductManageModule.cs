using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using ProductManage.Views;
using SharedResource.libs;

namespace ProductManage
{
    public class ProductManageModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.ProductionMeg_Region, typeof(ProductionMeg));
            regionManager.RegisterViewWithRegion(RegionManage.ProductMeg_Region, typeof(ProductMeg));
            regionManager.RegisterViewWithRegion(RegionManage.YieldChart_Region, typeof(YieldChart));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}