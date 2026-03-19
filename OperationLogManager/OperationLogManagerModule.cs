using OperationLogManager.libs;
using OperationLogManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace OperationLogManager
{
    public class OperationLogManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.OperationLog_Region, typeof(OperationLog));
            regionManager.RegisterViewWithRegion(RegionManage.NormalLog_Region, typeof(NormalLog));
            regionManager.RegisterViewWithRegion(RegionManage.ExceptionLog_Region, typeof(ExceptionLog));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}