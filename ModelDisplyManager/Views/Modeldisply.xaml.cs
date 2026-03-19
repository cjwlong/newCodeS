using HelixToolkit.Wpf.SharpDX;
using Prism.Events;
using Prism.Ioc;
using SharedResource.events.ModelDisplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModelDisplyManager.Views
{
    /// <summary>
    /// Modeldisply.xaml 的交互逻辑
    /// </summary>
    public partial class Modeldisply : UserControl
    {
        public Modeldisply(IContainerProvider provider)
        {
            InitializeComponent();

            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<ModelRefreshEvent>().Subscribe(() => {
                view.ZoomExtents(1);
                if (view.Camera is PerspectiveCamera pc)
                {
                    // LookDirection 是一个从相机位置指向目标点的向量
                    var lookDir = pc.LookDirection;
                    double currentDistance = lookDir.Length;

                    // 单位化方向
                    lookDir.Normalize();

                    // 后退 20% 的距离
                    double backoff = currentDistance * 2;

                    // 把 Position 沿着 负方向平移
                    pc.Position = pc.Position - lookDir * backoff;
                }
            }, ThreadOption.UIThread);
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;
    }
}
