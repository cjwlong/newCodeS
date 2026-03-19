using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.events.ModelDisplay;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using MeshGeometry3D = HelixToolkit.Wpf.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace ModelDisplyManager.ViewModels
{
    public class ModeldisplyViewModel : BindableBase
    {
        public ModeldisplyViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            EffectsManager = new DefaultEffectsManager();
            Camera = new PerspectiveCamera { };

            //TranslateTransform3D translateTransform = new TranslateTransform3D(0, 0, 0);

            //transformGroup.Children.Add(translateTransform);
            //transformGroup.Children.Add(new RotateTransform3D(
            //    new AxisAngleRotation3D(
            //        new Vector3D(0, 0, 1), -30)));

            LoadFile_Model(model_path);

            //eventAggregator.GetEvent<StartProcessEvent>().Subscribe(()=> StartRotation()); 动画代码删除
            //eventAggregator.GetEvent<PauseProcessEvent>().Subscribe(()=>StopRotation(false));
            //eventAggregator.GetEvent<StopProcessEvent>().Subscribe(()=>StopRotation(true));
            //eventAggregator.GetEvent<ContinueProcessEvent>().Subscribe(()=> StartRotation());
            //eventAggregator.GetEvent<FinishedProcessEvent>().Subscribe((r)=> StopRotation(true));
        }
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;

        public EffectsManager EffectsManager { get; }
        public PerspectiveCamera Camera { get; set; }
        private readonly PBRMaterial stl_material = new PBRMaterial() { AlbedoColor = new SharpDX.Color4(0.95f, 0.95f, 0.95f, 1f), MetallicFactor = 1f, RoughnessFactor=0.5f};
        //private readonly PBRMaterial stl_material = new PBRMaterial() { AlbedoColor = new SharpDX.Color4(0f, 0f, 1f, 1f), MetallicFactor = 1f };

        public ObservableElement3DCollection QZC_STLmodel { get; } = new ObservableElement3DCollection();

        private readonly string model_path = Path.Combine(Directory.GetCurrentDirectory().ToString(), "3d_models", "demo1.STL");

        private List<Transform3DGroup> scaleTranslateGroups = new();
        private List<AxisAngleRotation3D> rotationList = new List<AxisAngleRotation3D>();
        private bool isRotating = false;

        public DelegateCommand StarttestCommand {  get; set; }
        public DelegateCommand StoptestCommand {  get; set; }

        private void LoadFile_Model(string model_path)
        {
            string fileExtension = Path.GetExtension(model_path);

            if (fileExtension == ".stl" || fileExtension == ".STL")
            {
                var reader = new StLReader();
                var stlCol = reader.Read(model_path);
                AttachSTLModelList(stlCol);
            }

            Task.Run(() =>
            {
             Thread.Sleep(2500);
             eventAggregator.GetEvent<ModelRefreshEvent>().Publish();
            });            
        }

        private void AttachSTLModelList(List<Object3D> objs)
        {
            foreach (var obj in objs)
            {
                obj.Geometry.UpdateOctree();
                obj.Geometry.UpdateBounds();

                var geometry = obj.Geometry as MeshGeometry3D;
                var center = GetGeometryCenter(geometry);

                // 创建可变的变换组（后期用于放大+平移）
                var dynamicTransformGroup = new Transform3DGroup();
                scaleTranslateGroups.Add(dynamicTransformGroup);

                var staticTransform = CreateInitialRotation(geometry, -30);

                var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
                var rotateTransform = new RotateTransform3D(rotation, center);

                // 创建变换组（加上你已有的变换）
                var modelTransformGroup = new Transform3DGroup();
                modelTransformGroup.Children.Add(rotateTransform);
                modelTransformGroup.Children.Add(dynamicTransformGroup);                
                modelTransformGroup.Children.Add(staticTransform);

                // 应用到模型
                var meshModel = new MeshGeometryModel3D
                {
                    Transform = modelTransformGroup,
                    Geometry = obj.Geometry,
                    IsThrowingShadow = false,
                    Material = stl_material,
                };

                QZC_STLmodel.Add(meshModel);
                rotationList.Add(rotation); // 加入可控列表

                if (isRotating)
                {
                    StartRotationAnimation(rotation);
                }
            }
        }

        private void StartRotationAnimation(AxisAngleRotation3D rotation)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 先清理旧动画，避免重复绑定
                    rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);

                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 360,
                        Duration = new Duration(TimeSpan.FromSeconds(10)),
                        RepeatBehavior = RepeatBehavior.Forever
                    };

                    rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, animation);
                }));
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"StartRotationAnimation:", ex);
            }
        }

        private void StopRotation(bool reset)
        {
            isRotating = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var rotation in rotationList)
                {
                    // 必须在 UI 线程停止动画
                    rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
                }

                if (reset)
                {
                    ApplyScaleAndTranslation(1, new Vector3D(0, 0, 0));
                }
            });
        }

        private void StartRotation()
        {
            isRotating = true;

            foreach (var rotation in rotationList)
            {
                StartRotationAnimation(rotation);
            }
            Application.Current.Dispatcher.Invoke(() => {
                ApplyScaleAndTranslation(3, new Vector3D(1, 42, 0));
            });

           
        }

        private RotateTransform3D CreateInitialRotation(MeshGeometry3D geometry, double angleDegrees)
        {
            var center = GetGeometryCenter(geometry);
            var rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), angleDegrees);
            return new RotateTransform3D(rotation, center);
        }

        private void ApplyScaleAndTranslation(double scaleFactor, Vector3D translation)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < scaleTranslateGroups.Count; i++)
                {
                    var model = QZC_STLmodel[i] as MeshGeometryModel3D;
                    if (model == null) continue;

                    var geometry = model.Geometry as MeshGeometry3D;
                    if (geometry == null) continue;

                    var center = GetGeometryCenter(geometry);

                    var scaleTransform = new ScaleTransform3D(
                        scaleFactor, scaleFactor, scaleFactor,
                        center.X, center.Y, center.Z);

                    var translateTransform = new TranslateTransform3D(
                        translation.X, translation.Y, translation.Z);

                    // 清除旧变换，重新添加新的放大+位移
                    scaleTranslateGroups[i].Children.Clear();
                    scaleTranslateGroups[i].Children.Add(scaleTransform);
                    scaleTranslateGroups[i].Children.Add(translateTransform);
                }
            });            
        }

        private Point3D GetGeometryCenter(MeshGeometry3D geometry)
        {
            if (geometry == null || geometry.Positions.Count == 0)
                return new Point3D(0, 0, 0);

            double minX = geometry.Positions.Min(p => p.X);
            double maxX = geometry.Positions.Max(p => p.X);
            double minY = geometry.Positions.Min(p => p.Y);
            double maxY = geometry.Positions.Max(p => p.Y);
            double minZ = geometry.Positions.Min(p => p.Z);
            double maxZ = geometry.Positions.Max(p => p.Z);

            return new Point3D(
                (minX + maxX) / 2,
                (minY + maxY) / 2,
                (minZ + maxZ) / 2
            );
        }
    }
}
