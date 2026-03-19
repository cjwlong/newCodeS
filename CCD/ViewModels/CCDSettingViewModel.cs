using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using CCD.Controls;
using Prism.Ioc;
using Prism.Events;
using Prism.Commands;
using SharedResource.events.MVS_CCD;
using SharedResource.enums;
using SharedResource.events;

namespace CCD.ViewModels
{
    public class CCDSettingViewModel : BindableBase
    {
        public CCDSettingViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<CamerConnectStatus>().Subscribe((meg) =>
            {
                CameraState = meg;
            });
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;

        private CameraState _cameraState = CameraState.Disconnected;
        public CameraState CameraState
        {
            get { return _cameraState; }
            set
            {
                SetProperty(ref _cameraState, value);
            }
        }

        private DelegateCommand _inputCalibrationCommand;
        public DelegateCommand InputCalibrationCommand => _inputCalibrationCommand ??
            (_inputCalibrationCommand = new DelegateCommand(() => eventAggregator.GetEvent<InputCalibrationEvent>().Publish()));

        private DelegateCommand _cameraConnectCommand;
        public DelegateCommand CameraConnectedCommand => _cameraConnectCommand ??
            (_cameraConnectCommand = new DelegateCommand(() => eventAggregator.GetEvent<CcdToggleConnectionEvent>().Publish()));
    }
}
