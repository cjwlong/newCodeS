using System.Reflection;
using System;
using System.Threading;
using System.Windows;
using CCD;
using DeviceMegManager;
using LaserManager;
using LigEngine.Views;
using MenuControl;
using MenuControl.Views;
using OperationLogManager;
using OperationLogManager.libs;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using ProductManage;
using System.Reflection.PortableExecutable;
using SharedResource.Views;
using SharedResource.ViewModels;
using Machine;
using Machine.Interfaces;
using Machine.ViewModels;
using PublishTools;
using SharedResource;
using CCD.ViewModels;
using LaserManager.libs;
using ParamConfigManager;
using ParamConfigManager.interfaces;
using ParamConfigManager.tools;
using ModelDisplyManager;
using System.IO;
using SharedResource.tools;
using SharedResource.libs;
using ProductManage.libs;
using ExternalIOManager;
using ExternalIOManager.Interfaces;
using ExternalIOManager.libs;
using ExternalIOManager.ViewModels;
using DiastimeterManager;
using DiastimeterManager.libs;
using SharedResource.enums;
using CraftDebug;
using Machine.Models;

namespace LigEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        Mutex mutex;

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<ProgressBox, ProgressBoxViewModel>();  // 任务窗口
            containerRegistry.RegisterDialog<ConfirmBox, ConfirmBoxViewModel>();    // 注册确认窗口
            containerRegistry.RegisterDialog<SharedResource.Views.MessageBox, MessageBoxViewModel>("MessageBox");    // 注册消息窗口

            containerRegistry.RegisterSingleton<LoggingService>();

            containerRegistry.RegisterSingleton<IMachine, MachineViewModel>();
            containerRegistry.RegisterSingleton<OffsetSettingsViewModel>();
            containerRegistry.RegisterSingleton<CCDControlViewModel>();

            containerRegistry.RegisterSingleton<BLLaser>();

            containerRegistry.RegisterSingleton<LGQuick>();
            
            containerRegistry.RegisterSingleton<PanasonicModbus>();
            containerRegistry.RegisterSingleton<IConfigService, ConfigService>();

            containerRegistry.RegisterSingleton<GlobalCraftPara>();
            containerRegistry.RegisterSingleton<ProductStatistics>();
            containerRegistry.RegisterSingleton<GlobalMachineState>();

            containerRegistry.RegisterSingleton<IIODeviceService, IODeviceService>();
            containerRegistry.RegisterSingleton<IoMonitorViewModel>();
            containerRegistry.RegisterSingleton<GlobalProcessStatus>();
            containerRegistry.RegisterSingleton<MachineConfigManager>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<MenuControlModule>();
            moduleCatalog.AddModule<ProductManageModule>();
            moduleCatalog.AddModule<DeviceMegManagerModule>();
            moduleCatalog.AddModule<OperationLogManagerModule>();
            moduleCatalog.AddModule<MachineModule>();
            moduleCatalog.AddModule<CCDModule>();
            moduleCatalog.AddModule<LaserManagerModule>();
            moduleCatalog.AddModule<DiastimeterManagerModule>();
            moduleCatalog.AddModule<PublishToolsModule>();
            moduleCatalog.AddModule<SharedResourceModule>();
            moduleCatalog.AddModule<ParamConfigManagerModule>();
            moduleCatalog.AddModule<ModelDisplyManagerModule>();
            moduleCatalog.AddModule<ExternalIOManagerModule>();
            moduleCatalog.AddModule<CraftDebugModule>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigStore.CheckStoreFloder();

            bool ret;
            mutex = new Mutex(true, "LigEngine", out ret);

            if (!ret)
            {
                System.Windows.MessageBox.Show("程序已在运行，不可重复打开！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            
            base.OnStartup(e);
        }
    }
}
