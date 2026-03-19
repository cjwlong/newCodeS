using ACS.SPiiPlusNET;
using Machine.Enums;
using Machine.Interfaces;
using Machine.Models;
using Newtonsoft.Json;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SharedResource.events.Machine;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Machine.ViewModels
{
    public class AxisViewModel : BindableBase
    {
        private IMachineHardware machine;
        private IDialogService dialogService;
        #region 状态信息
        private AxisStatusModel status = new();
        private double _displayPosition = 0;
        //private double _statusdisplayPosition = 0;
        private double _displayLeftSoftLimit = double.MinValue;
        private double _displayRightSoftLimit = double.MaxValue;
        private bool _canMove = false;
        public double Position { get => status.Position; protected set => SetProperty(ref status.Position, value); }
        public double PositionError { get => status.PositionError; protected set => SetProperty(ref status.PositionError, value); }
        public bool? Enabled { get => status.Enabled; protected set => SetProperty(ref status.Enabled, value); }
        public bool? IsMoving
        {
            get => status.IsMoving;
            protected set
            {
                SetProperty(ref status.IsMoving, value);
                CanMove = IsMoving == false;
            }
        }
        public bool CanMove
        {
            get => _canMove;
            set => SetProperty(ref _canMove, value);
        }
        public LimitStatus LeftLimit { get => status.LeftLimit; protected set => SetProperty(ref status.LeftLimit, value); }
        public LimitStatus RightLimit { get => status.RightLimit; protected set => SetProperty(ref status.RightLimit, value); }
        public double LeftSoftLimit { get => status.LeftSoftLimit; protected set => SetProperty(ref status.LeftSoftLimit, value); }
        public double RightSoftLimit { get => status.RightSoftLimit; protected set => SetProperty(ref status.RightSoftLimit, value); }
        public double DisplayPosition { get => _displayPosition; protected set => SetProperty(ref _displayPosition, value); }
        //public double StatusDisplayPosition { get => _statusdisplayPosition; set => SetProperty(ref _statusdisplayPosition, value); }
        public double DisplayLeftSoftLimit { get => _displayLeftSoftLimit; protected set => SetProperty(ref _displayLeftSoftLimit, value); }
        public double DisplayRightSoftLimit { get => _displayRightSoftLimit; protected set => SetProperty(ref _displayRightSoftLimit, value); }
        #endregion
        public double RadianValue { get => Position / 180 * Math.PI; }  // 用于获取弧度值
        #region 可设置参数
        public AxisDefination AxisDefination { get; private set; }
        private double _axisOffset = 0;
        //private double _workPieceOffset = 0;
        private bool _axisReverseFlag = false;
        public bool AxisReverseFlag
        {
            get => _axisReverseFlag; set
            {
                if (Name == "X") value = true;
                SetProperty(ref _axisReverseFlag, value);
                ConfigStoredFlag = false;
            }
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                SetProperty(ref _isSelected, value);
            }
        }

        public double Speed
        {
            get => AxisDefination.Speed; set
            {
                if (AxisDefination.Speed != value)
                {
                    var oldSpeed = AxisDefination.Speed;
                    AxisDefination.Speed = value;
                    LoggingService.Instance.LogInfo($"{Name} 速度变化: {oldSpeed} ---> {AxisDefination.Speed}");
                    RaisePropertyChanged(nameof(AxisDefination.Speed));
                    ConfigStoredFlag = false;

                }
                //  SetProperty(ref AxisDefination.Speed, value); ConfigStoredFlag = false;
            }
        }   // 速度
        public string Name { get => AxisDefination.Name; set { SetProperty(ref AxisDefination.Name, value); ConfigStoredFlag = false; } }  // 轴名称



        public int NodeNum
        {
            get => AxisDefination.NodeNum;
            set
            {
                if (AxisDefination.NodeNum != value)
                {
                    var oldNodeNum = AxisDefination.NodeNum;
                    AxisDefination.NodeNum = value;
                    LoggingService.Instance.LogInfo($"{Name} 轴号:{oldNodeNum} ---> {AxisDefination.NodeNum}");
                    //OnPropertyChanged(nameof(AxisOffset));
                    RaisePropertyChanged(nameof(NodeNum));
                    ConfigStoredFlag = false;  // 轴号

                }
            }
        }
        public double AxisOffset
        {
            get => _axisOffset;
            set
            {
                if (_axisOffset != value)
                {
                    value = Math.Round(value, 6);
                    var oldaxisOffset = _axisOffset;
                    _axisOffset = value;
                    LoggingService.Instance.LogInfo($"{Name} 轴原点偏移: {oldaxisOffset} ---> {_axisOffset} ,change {_axisOffset - oldaxisOffset}");
                    //OnPropertyChanged(nameof(AxisOffset));
                    RaisePropertyChanged(nameof(AxisOffset));
                    ConfigStoredFlag = false;

                }
            }
        }    // 轴偏移
             //public event PropertyChangedEventHandler PropertyChanged;

        public double AcceleratedSpeed
        {
            get => AxisDefination.acceleratedSpeed;
            set
            {
                if (AxisDefination.acceleratedSpeed != value)
                {
                    var oldacceleratedSpeed = AxisDefination.acceleratedSpeed;
                    AxisDefination.acceleratedSpeed = value;
                    LoggingService.Instance.LogInfo($"{Name} 轴加速度：{oldacceleratedSpeed} ---> {AxisDefination.acceleratedSpeed}");
                    RaisePropertyChanged(nameof(AcceleratedSpeed));
                    ConfigStoredFlag = false;
                }
            }
        }

        public double DecelerationSpeed
        {
            get => AxisDefination.decelerationSpeed;
            set
            {
                if (AxisDefination.decelerationSpeed != value)
                {
                    var olddecelerationSpeed = AxisDefination.decelerationSpeed;
                    AxisDefination.decelerationSpeed = value;
                    LoggingService.Instance.LogInfo($"{Name} 轴减速度：{olddecelerationSpeed} ---> {AxisDefination.decelerationSpeed}");
                    RaisePropertyChanged(nameof(DecelerationSpeed));
                    ConfigStoredFlag = false;
                }
            }
        }

        //private void MyObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //    {

        //        HpcLogger.Info($"111");
        //    }
        //    protected virtual void OnPropertyChanged(string propertyName)
        //    {
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //    }



        public double? LeftRetractThreshold
        {
            get => AxisDefination.LeftRetractThreshold; set
            {
                if (AxisDefination.LeftRetractThreshold != value)
                {
                    var oldLeftRetractThreshold = AxisDefination.LeftRetractThreshold;
                    AxisDefination.LeftRetractThreshold = value;
                    LoggingService.Instance.LogInfo($"{Name} 退刀负阈值变化   from {oldLeftRetractThreshold} to {AxisDefination.LeftRetractThreshold}");
                    RaisePropertyChanged(nameof(AxisDefination.LeftRetractThreshold));
                    ConfigStoredFlag = false;
                }
            }
        }
        public double? RightRetractThreshold
        {
            get => AxisDefination.RightRetractThreshold; set
            {
                if (AxisDefination.RightRetractThreshold != value)
                {
                    var oldRightRetractThreshold = AxisDefination.RightRetractThreshold;
                    AxisDefination.RightRetractThreshold = value;
                    LoggingService.Instance.LogInfo($"{Name} 退刀正阈值变化   from {oldRightRetractThreshold} to {AxisDefination.RightRetractThreshold} ");
                    RaisePropertyChanged(nameof(AxisDefination.RightRetractThreshold));
                    ConfigStoredFlag = false;
                }
            }
        }

        public double? SoftLimitMax
        {
            get => AxisDefination.MaxSoftLimit; set
            {
                if (AxisDefination.MaxSoftLimit != value)
                {
                    string oldMaxSoftLimit = AxisDefination.MaxSoftLimit != null ? AxisDefination.MaxSoftLimit.ToString() : "inf";
                    AxisDefination.MaxSoftLimit = value;
                    string log = value != null ? AxisDefination.MaxSoftLimit.ToString() : "inf";
                    LoggingService.Instance.LogInfo($"{Name} 正限位变化: {oldMaxSoftLimit} ---> {log} ");
                    RaisePropertyChanged(nameof(AxisDefination.MaxSoftLimit));
                    ConfigStoredFlag = false;
                }
            }
        }

        public double? SoftLimitMin
        {
            get => AxisDefination.MinSoftLimit; set
            {
                if (AxisDefination.MinSoftLimit != value)
                {
                    string oldMinSoftLimit = AxisDefination.MinSoftLimit != null ? AxisDefination.MinSoftLimit.ToString() : "inf";
                    AxisDefination.MinSoftLimit = value;
                    string log = value != null ? AxisDefination.MinSoftLimit.ToString() : "inf";
                    LoggingService.Instance.LogInfo($"{Name} 负限位变化: {oldMinSoftLimit} ---> {log} ");
                    RaisePropertyChanged(nameof(AxisDefination.MinSoftLimit));
                    ConfigStoredFlag = false;
                }
            }
        }

        public bool IsHomeed
        {
            get => AxisDefination.isHomeed;
            set
            {
                if (AxisDefination.isHomeed != value)
                {
                    bool oldhome = AxisDefination.isHomeed;
                    AxisDefination.isHomeed = value;
                    RaisePropertyChanged(nameof(AxisDefination.isHomeed));
                    ConfigStoredFlag = false;
                }
            }
        }

        private double tempSpeed;
        public double TempSpeed
        {
            get => tempSpeed;
            set { SetProperty(ref tempSpeed, value); CheckUnsaved(); }
        }

        private double tempAcceleratedSpeed;
        public double TempAcceleratedSpeed
        {
            get => tempAcceleratedSpeed;
            set { SetProperty(ref tempAcceleratedSpeed, value); CheckUnsaved(); }
        }

        private double tempDecelerationSpeed;
        public double TempDecelerationSpeed
        {
            get => tempDecelerationSpeed;
            set { SetProperty(ref tempDecelerationSpeed, value); CheckUnsaved(); }
        }

        private bool hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => hasUnsavedChanges;
            set { SetProperty(ref hasUnsavedChanges, value); }
        }

        public void LoadTemp()
        {
            TempSpeed = Speed;
            TempAcceleratedSpeed = AcceleratedSpeed;
            TempDecelerationSpeed = DecelerationSpeed;
            HasUnsavedChanges = false;
        }

        public void SaveTemp()
        {
            Speed = TempSpeed;
            AcceleratedSpeed = TempAcceleratedSpeed;
            DecelerationSpeed = TempDecelerationSpeed;
            HasUnsavedChanges = false;
        }

        public void RestoreOriginal()
        {
            LoadTemp();
        }

        private void CheckUnsaved()
        {
            HasUnsavedChanges = TempSpeed != Speed ||
                                TempAcceleratedSpeed != AcceleratedSpeed ||
                                TempDecelerationSpeed != DecelerationSpeed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        //public double WorkpieceOffset { get => _workPieceOffset; set { SetProperty(ref _workPieceOffset, value); ConfigStoredFlag = false; } } // 工件偏移
        //public bool CcdReverseFlag { get => _ccdReverseFlag; set { SetProperty(ref _ccdReverseFlag, value); ConfigStoredFlag = false; } }  // CCD轴反向标记
        public double Offset;   // 显示用的offset
        #endregion
        [JsonIgnore]
        public bool ConfigStoredFlag = false;
        private double _ptpTarget = 0;
        private double _relTarget = 1;
        public double PtpTarget { get => _ptpTarget; set => SetProperty(ref _ptpTarget, value); } //绝对坐标
        public double RelTarget 
        { 
            get => _relTarget;
            set
            {
                SetProperty(ref _relTarget, Math.Abs(value));
            }
        } //相对坐标
        #region Command
        public DelegateCommand ToggleEnable { get; private set; }
        public DelegateCommand HomeCommand { get; private set; }
        //public CancellationTokenSource _jogCTS;
        //public DelegateCommand<JogParameters> JogCommand { get; private set; }
        public DelegateCommand ToPointCommand { get; private set; }
        public DelegateCommand PosRelPointCommand { get; private set; }
        public DelegateCommand NegRelPointCommand { get; private set; }
        public DelegateCommand ZeroSettingCommand { get; set; }
        public DelegateCommand<string> GetAxisOffsetPosition { get; private set; }
        //public DelegateCommand<string> GetWorkpieceOffsetPosition { get; private set; }
        private readonly IEventAggregator _eventAggregator;
        #endregion
        internal AxisViewModel(IDialogService service, IMachineHardware machine, AxisDefination axisDefination, IEventAggregator eventAggregator)
        {
           
            dialogService = service;
            this.machine = machine;
            this.AxisDefination = axisDefination;
            //PropertyChanged += MyObject_PropertyChanged;
            // 命令绑定
            ToPointCommand = new DelegateCommand(() => ToDisplayPoint(PtpTarget, isAbsolute: true)).ObservesCanExecute(() => CanMove);
            PosRelPointCommand = new DelegateCommand(() => ToDisplayPoint(RelTarget, isAbsolute: false)).ObservesCanExecute(() => CanMove);
            NegRelPointCommand = new DelegateCommand(() => ToDisplayPoint(-RelTarget, isAbsolute: false)).ObservesCanExecute(() => CanMove);
            //JogCommand = new DelegateCommand<JogParameters>(JogMove).ObservesCanExecute(() => CanMove);
            HomeCommand = new DelegateCommand(() =>
            {
                dialogService.ShowDialog("ConfirmBox", new DialogParameters($"message=请确认是否回零{Name}轴?"), r =>
                {
                    if (r.Result == ButtonResult.OK)
                    {
                        Home();
                        LoggingService.Instance.LogInfo($"{AxisDefination.Name}轴回零");
                    }                        
                });
            }).ObservesCanExecute(() => CanMove);
            GetAxisOffsetPosition = new DelegateCommand<string>((para) => AxisOffset = Position).ObservesCanExecute(() => CanMove);
            //GetWorkpieceOffsetPosition = new DelegateCommand<string>((para) => WorkpieceOffset = DisplayPosition).ObservesCanExecute(() => CanMove);
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            ToggleEnable = new DelegateCommand(() =>
            {
                if (Enabled == true)
                {
                    dialogService.ShowDialog("ConfirmBox", new DialogParameters($"message=请确认是否取消{Name}轴使能?"), r =>
                    {
                        if (r.Result == ButtonResult.OK)
                            Disable();
                    });
                }
                else if (Enabled == false) Enable();
            });
            ZeroSettingCommand = new DelegateCommand(() =>
            {
                //string param = p.ToString();

                //if (!string.IsNullOrEmpty(param))
                //{
                //    if (param == "0")
                //    {
                //        LoggingService.Instance.LogInfo($"轴{Name}取消置零");
                //        AxisOffset = 0;
                //    }
                //    else
                //    {
                //        LoggingService.Instance.LogInfo($"轴{Name}置零");
                //        AxisOffset = Position;
                //    }
                //}

                machine.SetZero(AxisDefination);
            });

            if (Name == "X")
            {
                AxisReverseFlag = true;
            }
        }

        //此处target为文本框内的值
        public bool ToDisplayPoint(double target, bool isAbsolute = true, bool ismoveall = false)
        {  //target绝对运动传出去的dest是运动量，相对运动传出去的dest是框里的值,全部传出相对运动
            string mode = null;
            double? dest = null;
            string GCode = null;
            if (Name == "X")
            {
                AxisReverseFlag = true;
            }
            if (isAbsolute)  //绝对运动
            {
                if (Math.Abs(target - DisplayPosition) < 0.0001) return false;  //最小运动量
                dest = target - DisplayPosition;
            }
            else  //相对运动
            {
                if (Math.Abs(target) < 0.0001) return false;  //最小运动量
                dest = target;
                target = target + DisplayPosition;

            }
            mode = "G91";
            GCode = mode + "G01" + AxisDefination.Name + dest.ToString() + "F" + AxisDefination.Speed;  //标准G代码
            string err_code;
            if (Name=="X")
            {
                err_code = machine.ToPoint(AxisDefination, dest.Value, isAbsolute: false, isAsync: true); //传入的dest是运动量
            }
            else
            {
                err_code = machine.ToPoint(AxisDefination, (AxisReverseFlag ? -1 : 1) * dest.Value, isAbsolute: false, isAsync: true); //传入的dest是运动量
                                                                                                                            //}string err_code
            }
            
            //string err_code = machine.SingleAxisMotion(GCode);

            if (err_code != null)
            {
                dialogService.ShowDialog("MessageBox", new DialogParameters($"message={err_code}"), r => { return; });
                return false;
            }

            if (!ismoveall)
            {
                string axisChangeInfoLog = $"机床{Name}轴运动：{DisplayPosition.ToString("F6")} ---> {target.ToString("F6")}";
                LoggingService.Instance.LogInfo(axisChangeInfoLog);
            }

            //Base.HpcLog.HpcLogger.Info($"轴运动模式: {mode} \nmove {Name}: {DisplayPosition:F4} ---> {target:F4}");

            return true;
        }
        public bool Enable()
        {
            string err_code = machine.Enable(AxisDefination);
            if (err_code != null)
            {
                dialogService.ShowDialog("MessageBox", new DialogParameters($"message={err_code}"), r => { return; });
                return false;
            }
            LoggingService.Instance.LogInfo($"使能 {Name}");
            return true;
        }
        public bool Disable()
        {
            string err_code = machine.Disable(AxisDefination);
            if (err_code != null)
            {
                dialogService.ShowDialog("MessageBox", new DialogParameters($"message={err_code}"), r => { return; });
                return false;
            }
            LoggingService.Instance.LogInfo($"取消使能 {Name}" );
            return true;
        }

        public bool Home()
        {
            try
            {
                var task = new DoWorkEventHandler((s, e) =>
                {
                    e.Result = machine.Home(AxisDefination);

                    if (AxisDefination.Name == "Z")
                    {
                        ToDisplayPoint(75, true);
                    }else if (AxisDefination.Name == "X" || AxisDefination.Name == "Y")
                    {
                        ToDisplayPoint(100, true);
                    }

                    machine.WaitMotionEnd(new[] { AxisDefination });

                    IsHomeed = true;

                    //// 安全起见手动确认回到零位
                    //MessageWindow.ConfirmWindow("是否回到显示零位", (r) =>
                    //{
                    //    if (r != null && r.Result == ButtonResult.OK)
                    //    {
                    //        Thread.Sleep(500);
                    //        ToDisplayPoint(0, isAbsolute: true);
                    //        LoggingService.Instance.LogInfo($"{AxisDefination.Name}轴回到显示零点");
                    //    }
                    //});
                });

                var para = new DialogParameters
            {
                { "task", task },
                { "title", $"回零" },
                { "message", $"{Name}轴回零中，请稍候。" },
                { "cancel", new Func<bool>(() =>
                {
                    if (machine.StopWork()==null) return true;
                    else return false;
                })
                }
            };
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    dialogService.ShowDialog("ProgressBox", para, r => { });
                }));
            }
            catch (Exception ex)
            {
                IsHomeed = false;
                LoggingService.Instance.LogError($"{AxisDefination.Name}回零异常！", ex);
            }
            
            //string err_code = machine.Home(AxisDefination);
            //if (err_code != null)
            //{
            //    dialogService.ShowDialog("MessageBox", new DialogParameters($"message={err_code}"), r => { return; });
            //    return false;
            //}
            
            return true;
        }

        public bool Refresh()
        {
            var data = machine.Refresh(AxisDefination);
            if (data != null)
            {
                var temp = data.Item1;
                if (Name == "X")
                {
                    Position =  temp.Position;
                }
                else
                {
                    Position = (AxisReverseFlag ? -1 : 1) * temp.Position;
                }
     
                Enabled = temp.Enabled;
                IsMoving = temp.IsMoving;
                LeftLimit = temp.LeftLimit;
                RightLimit = temp.RightLimit;
                if (AxisReverseFlag)    // 反向
                {
                    LeftSoftLimit = -temp.RightSoftLimit;
                    RightSoftLimit = -temp.LeftSoftLimit;
                }
                else    // 不反向
                {
                    LeftSoftLimit = temp.LeftSoftLimit;
                    RightSoftLimit = temp.RightSoftLimit;
                }
                //DisplayPosition = Position - Offset;
                //DisplayLeftSoftLimit = LeftSoftLimit - Offset;
                //DisplayRightSoftLimit = RightSoftLimit - Offset;
                DisplayPosition = Position - AxisOffset;

                DisplayLeftSoftLimit = LeftSoftLimit - AxisOffset;
                DisplayRightSoftLimit = RightSoftLimit - AxisOffset;
                return true;
            }
            else return false;
        }
        public override string ToString()
        {
            string tempmin, tempmax;
            if (SoftLimitMin == null)
                tempmin = "inf";
            else
                tempmin = SoftLimitMin.ToString();
            if (SoftLimitMax == null)
                tempmax = "inf";
            else
                tempmax = SoftLimitMax.ToString();

            return $"Name: {Name}, <SoftLimitMin> : {tempmin} , <SoftLimitMax> : {tempmax} ";
        }

    }
}
