using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Services.Dialogs;
using SharedResource.events;
using SharedResource.libs;

namespace SharedResource
{
    public class SharedResourceModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            StaticEventAggregator.eventAggregator = containerProvider.Resolve<IEventAggregator>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}