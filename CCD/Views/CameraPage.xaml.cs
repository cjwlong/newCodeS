using Prism.Events;
using Prism.Ioc;
using SharedResource.events.Machine;
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

namespace CCD.Views
{
    /// <summary>
    /// CameraPage.xaml 的交互逻辑
    /// </summary>
    public partial class CameraPage : UserControl
    {
        CCDControl CameraViewer;
        public CameraPage(IContainerProvider provider)
        {
            InitializeComponent();

            CameraViewer = provider.Resolve<CCDControl>();
            CcdPageRoot.Children.Add(CameraViewer);

            var eventAggregator = provider.Resolve<IEventAggregator>();

            CameraViewer.SetToolMove((double[] point) =>
            {
                //eventAggregator.GetEvent<CcdToolMoveEvent>().Publish(point); 不需要移动
            });
        }
    }
}
