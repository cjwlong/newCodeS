using DiastimeterManager.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SharedResource.libs;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows;

namespace DiastimeterManager
{
    public class DiastimeterManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionManage.LaserRangeSetting_Region, typeof(LaserRangeSetting));
            regionManager.RegisterViewWithRegion(RegionManage.ContactRangeSetting_Region, typeof(ContactRangeSetting));
            regionManager.RegisterViewWithRegion(RegionManage.RangeFinderSetting_Region, typeof(DiastimeterSetting));

            // 强制实例化 RangeFinderSetting_Region 区域的视图
            ForceInstantiateViewInOldPrism(regionManager, containerProvider, RegionManage.RangeFinderSetting_Region, typeof(DiastimeterSetting));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }

        /// <summary>
        /// 强制实例化区域中的视图
        /// </summary>
        /// <param name="regionManager">区域管理器</param>
        /// <param name="containerProvider">容器（解析视图实例）</param>
        /// <param name="regionName">目标区域名称</param>
        /// <param name="viewType">要实例化的视图类型</param>
        private void ForceInstantiateViewInOldPrism(IRegionManager regionManager, IContainerProvider containerProvider, string regionName, Type viewType)
        {
            IRegion targetRegion = null;
            try
            {
                targetRegion = regionManager.Regions[regionName];
            }
            catch (KeyNotFoundException ex)
            {
                // 区域未找到
                System.Diagnostics.Debug.WriteLine($"警告：区域 {regionName} 不存在，错误信息：{ex.Message}");
                return;
            }

            // 检查视图是否已在区域中实例化（避免重复创建）
            var existingView = targetRegion.Views.FirstOrDefault(view => view.GetType() == viewType);
            if (existingView != null)
            {
                System.Diagnostics.Debug.WriteLine($"视图 {viewType.Name} 已实例化，无需重复操作");
                return;
            }

            // 从容器解析视图实例
            var viewInstance = containerProvider.Resolve(viewType) as FrameworkElement;
            if (viewInstance == null)
            {
                System.Diagnostics.Debug.WriteLine($"错误：无法解析视图 {viewType.Name}，请检查视图是否已注册到容器");
                return;
            }

            // 将视图添加到区域，触发实例化
            targetRegion.Add(viewInstance);

            System.Diagnostics.Debug.WriteLine($"成功在区域 {regionName} 实例化视图 {viewType.Name}");
        }
    }
}