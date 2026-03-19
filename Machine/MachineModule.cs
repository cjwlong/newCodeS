using Machine.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;

namespace Machine
{
    public class MachineModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.AcsAxisControl_Region, typeof(MachineStatus));
            regionManager.RegisterViewWithRegion(RegionManage.HardwareMonitor_Region, typeof(AxisPanelUserControl));
            regionManager.RegisterViewWithRegion(RegionManage.ACSSetting_Region, typeof(MachineAdvanceSettings));
            regionManager.RegisterViewWithRegion(RegionManage.ACSDebug_Region, typeof(MachineDebug));
            regionManager.RegisterViewWithRegion(RegionManage.PositionDebug_Region, typeof(MachinePositionDebug));
            regionManager.RegisterViewWithRegion(RegionManage.AbPositionDebug_Region, typeof(AbPositionDebug));

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}