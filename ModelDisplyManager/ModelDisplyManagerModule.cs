using ModelDisplyManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace ModelDisplyManager
{
    public class ModelDisplyManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            //regionManager.RegisterViewWithRegion(RegionManage.ProcessAnime_Region, typeof(Modeldisply));
            regionManager.RegisterViewWithRegion(RegionManage.ProcessAnime_Region, typeof(ModelData)); 
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}