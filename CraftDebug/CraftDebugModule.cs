using CraftDebug.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace CraftDebug
{
    public class CraftDebugModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            
            regionManager.RegisterViewWithRegion(RegionManage.CraftDebugStep1_Region, typeof(Measurement1View));
            regionManager.RegisterViewWithRegion(RegionManage.CraftDebugStep2_Region, typeof(MeasurementView));
            regionManager.RegisterViewWithRegion(RegionManage.CraftDebugStep3_Region, typeof(Measurement3View));
            regionManager.RegisterViewWithRegion(RegionManage.CraftDebugStep4_Region, typeof(Measurement4View));

            regionManager.RegisterViewWithRegion(RegionManage.CraftDebug_Region, typeof(CraftDebugRegion));

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}