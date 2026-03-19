using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Services.Dialogs;
using SharedResource.tools;

namespace PublishTools
{
    public class PublishToolsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            MessageWindow.dialogService = containerProvider.Resolve<IDialogService>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}