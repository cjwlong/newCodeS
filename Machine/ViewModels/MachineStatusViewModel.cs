using Machine.Enums;
using Machine.Interfaces;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.events.Machine;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    internal class MachineStatusViewModel : BindableBase
    {
        private IContainerProvider containerProvider;
        private bool? _controlMode = null;
        //private bool _moveMode = false;
        public string _controlModeButtonText;
        private string _moveModeButtonText = "绝对";
        private string _gCodeCommand = null;
        private string _gMotionMode = "G90";
        private string _gMotionType = "G00";
        private string _gMotionSpeed = "10";
        public string GMotionMode { get => _gMotionMode; set => SetProperty(ref _gMotionMode, value); }
        public string GMotionType { get => _gMotionType; set => SetProperty(ref _gMotionType, value); }
        public string GMotionSpeed { get => _gMotionSpeed; set => SetProperty(ref _gMotionSpeed, value); }

        private double _speedRate = 0;
        public double SpeedRate { get => _speedRate; set => SetProperty(ref _speedRate, value); }

        public bool ControlMode
        {
            get => _controlMode == null ? false : _controlMode.Value;
            set
            {
                ControlModeButtonText = value ? "刀尖运动" : "轴运动";
                SetProperty(ref _controlMode, value);
                MachineVM.FocusMotionMode = value;
                Properties.Settings.Default.MotionMode = value;
                SaveSettingsWithRetry();
            }
        }
        private void SaveSettingsWithRetry(int retryCount = 5, int delayMilliseconds = 100)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    Properties.Settings.Default.Save();
                    return;
                }
                catch
                {
                    if (i == retryCount - 1) throw;
                    System.Threading.Thread.Sleep(delayMilliseconds);
                }
            }
        }

        private MachineMoveMode _currentMoveMode;
        public MachineMoveMode CurrentMoveMode { get => _currentMoveMode; set => SetProperty(ref _currentMoveMode, value); }

        //public bool MoveMode
        //{
        //    get => _moveMode;
        //    set
        //    {
        //        SetProperty(ref _moveMode, value);
        //        if (value) MoveModeButtonText = "相对";
        //        else MoveModeButtonText = "绝对";
        //    }
        //}
        public string ControlModeButtonText { get => _controlModeButtonText; set => SetProperty(ref _controlModeButtonText, value); }
        public string MoveModeButtonText { get => _moveModeButtonText; set => SetProperty(ref _moveModeButtonText, value); }
        public string GCodeCommand { get => _gCodeCommand; set => SetProperty(ref _gCodeCommand, value); }
        public MachineViewModel MachineVM { get; set; }
        //public AxesCompensation Axes_Compensation { get; set; }
        public DelegateCommand StopAllCommand { get; set; }
        public DelegateCommand FocusCommand { get; set; }
        public DelegateCommand<string> PositionShiftCommand { get; set; }
        
        public MachineStatusViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();
            //Axes_Compensation = AxesCompensation.Instance;
            ControlMode = Properties.Settings.Default.MotionMode;

            FocusCommand = new(() =>
            {
                var err = MachineVM.Focus();
                MessageWindow.ShowDialog(err);
            });
            

            StopAllCommand = new DelegateCommand(() =>
            {
                StaticEventAggregator.eventAggregator.GetEvent<EmergencyStopEvent>().Publish();    // 触发急停事件
                MachineVM.StopAll();
            });

            PositionShiftCommand = new DelegateCommand<string>((p) =>
            {
                string target_name = p.ToString();
                SensorOffsetViewModel source = null, target = null;
                foreach (var node in MachineVM.OffsetSettings.SensorOffset)
                {
                    if (node.Name == target_name)
                        target = node;
                    if (node.IsChecked)
                        source = node;
                }
                if (source == null || source == target)   // 不需要移动
                    return;

                double[] position = MachineVM.Get6AxesPosition();
                var now_position = MachineVM.GetToolHead(position, source.GetArray);
                target.IsChecked = true;
                var tool_num = MachineVM.OffsetSettings.SensorOffset.IndexOf(MachineVM.OffsetSettings.GetSelectedSensor());
                double[] tar_pos = MachineVM.FromVector(now_position.Item1, now_position.Item2, now_position.Item3, tool_num, out var t); // 求坐标差

                MachineVM.ToPoint(tar_pos, inProgress: false, isAbsolute: true, isAsync: true);

            }).ObservesCanExecute(() => MachineVM.IsIdle); ;
        }
    }
}
