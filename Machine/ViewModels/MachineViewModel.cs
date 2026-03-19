using ACS.SPiiPlusNET;
using IronPython.Hosting;
using Machine.Harware;
using Machine.Interfaces;
using Machine.Models;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PublishTools.Helpers;
using ServiceManager;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.events.Machine;
using SharedResource.libs;
using SharedResource.Parameters;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using static IronPython.Modules._ast;

namespace Machine.ViewModels
{
    public partial class MachineViewModel : BindableBase, IMachine
    {
        public readonly Dictionary<string, int> axes_num = new Dictionary<string, int>() {
            { "X", 0 }, {"Y", 1 }, { "Z", 2}, {"A", 3 },{ "B", 4}, { "C", 5}, { "α", 3}, { "β", 4},{ "γ", 5} };
        private readonly IDialogService dialogService;
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly string CSV_Filepath = Path.Combine(ConfigStore.StoreDir, "global_reset.csv");
        private readonly string HOME_FilePath = Path.Combine(Directory.GetCurrentDirectory(), "home", "homeAll.txt");

        private DeviceStatus _deviceStatus = DeviceStatus.Disconnected;
        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus; set
            {
                if (value != DeviceStatus)
                {
                    if (value == DeviceStatus.Disconnected)
                        OnDisconnected?.Invoke();
                    else if (value == DeviceStatus.Idle)
                        OnConnected?.Invoke();
                    SetProperty(ref _deviceStatus, value);
                }
            }
        }

        private IMachineHardware machine;  //定义接口实例，可引用所有显隐式接口方法
        private MachineConnectionInfo machineInfo;  //机床的IP和Port信息
        private RetractInfo retractInfo;
        private GlobalMachineState globalMachineState;
        private System.Timers.Timer timer = new System.Timers.Timer()
        {
            Interval = 20,
            AutoReset = true,
            Enabled = true
        };
        private bool _rtcpOn = false;
        private bool? isMoving = false;
        private bool isIdle = false;
        private string _selectedSensor = "";
        private double _feed;
        private double _focusFeed;
        private bool _headCanMove = false;
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
        public bool HeadCanMove { get => _headCanMove; set => SetProperty(ref _headCanMove, value); }

        public double Feed
        {
            get => _feed; set
            {
                SetProperty(ref _feed, value);
                Properties.Settings.Default.Feed = value;
                SaveSettingsWithRetry();
            }
        }

        public double FocusFeed
        {
            get => _focusFeed; set
            {
                SetProperty(ref _focusFeed, value);
                Properties.Settings.Default.FocusFeed = value;
                SaveSettingsWithRetry();
            }
        }

        public bool RtcpOn { get => _rtcpOn; private set => SetProperty(ref _rtcpOn, value); }
        public bool FocusMotionMode = false;
        public AxesCompensation Axes_Compensation { get; set; }
        public OffsetSettingsViewModel OffsetSettings { get; private set; }
        public ObservableCollection<AxisViewModel> Axes { get; private set; }
        public ObservableCollection<ToolHeadAxisViewModel> ToolHeadAxes { get; private set; }
        public ObservableCollection<IOViewModel> IOs { get; private set; }
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public bool? IsMoving
        {
            get => isMoving;
            private set
            {
                if (value == IsMoving) return;
                SetProperty(ref isMoving, value);
                IsIdle = IsMoving == false;
                //UpdateHeadCanMove(); // 更新刀尖运动是否可点
            }
        }

        private string _globalResetInfo = $"加工前进行复位";
        public string GlobalResetInfo
        {
            get => _globalResetInfo;
            set
            {
                SetProperty(ref _globalResetInfo, value);
            }
        }

        private float _xGlobalReset = 0;
        public float XGlobalReset
        {
            get => _xGlobalReset;
            set
            {
                SetProperty(ref _xGlobalReset, value);
                GlobalResetInfo = $"加工前进行复位，当前复位点：X-{XGlobalReset}，Y-{YGlobalReset}，Z-{ZGlobalReset}，A-{AGlobalReset}，C-{CGlobalReset}";
            }
        }
        private float _yGlobalReset = 0;
        public float YGlobalReset
        {
            get => _yGlobalReset;
            set
            {
                SetProperty(ref _yGlobalReset, value);
                GlobalResetInfo = $"加工前进行复位，当前复位点：X-{XGlobalReset}，Y-{YGlobalReset}，Z-{ZGlobalReset}，A-{AGlobalReset}，C-{CGlobalReset}";
            }
        }
        private float _zGlobalReset = 0;
        public float ZGlobalReset
        {
            get => _zGlobalReset;
            set
            {
                SetProperty(ref _zGlobalReset, value);
                GlobalResetInfo = $"加工前进行复位，当前复位点：X-{XGlobalReset}，Y-{YGlobalReset}，Z-{ZGlobalReset}，A-{AGlobalReset}，C-{CGlobalReset}";
            }
        }
        private float _aGlobalReset = 0;
        public float AGlobalReset
        {
            get => _aGlobalReset;
            set
            {
                SetProperty(ref _aGlobalReset, value);
                GlobalResetInfo = $"加工前进行复位，当前复位点：X-{XGlobalReset}，Y-{YGlobalReset}，Z-{ZGlobalReset}，A-{AGlobalReset}，C-{CGlobalReset}";
            }
        }
        private float _cGlobalReset = 0;
        public float CGlobalReset
        {
            get => _cGlobalReset;
            set
            {
                SetProperty(ref _cGlobalReset, value);
                GlobalResetInfo = $"加工前进行复位，当前复位点：X-{XGlobalReset}，Y-{YGlobalReset}，Z-{ZGlobalReset}，A-{AGlobalReset}，C-{CGlobalReset}";
            }
        }
        public bool IsIdle { get => isIdle; private set => SetProperty(ref isIdle, value); }
        public string SelectedSensor { get => _selectedSensor; set => SetProperty(ref _selectedSensor, value); }
        public string Ip { get => machineInfo.Ip; set => SetProperty(ref machineInfo.Ip, value); }
        public int Port { get => machineInfo.Port; set => SetProperty(ref machineInfo.Port, value); }
        public bool IsRetract { get => retractInfo.IsRetract; set => SetProperty(ref retractInfo.IsRetract, value); }
        public double RetractValue { get => retractInfo.RetractValue; set => SetProperty(ref retractInfo.RetractValue, value); }

        public Action OnConnected;
        public Action OnDisconnected;
        private delegate void MachineRefreshed(System.DateTime now);
        private event MachineRefreshed OnMachineRefreshed;

        // 机床选择
        public ObservableCollection<Tuple<string, Type>> Machines { get; private set; }
        private Tuple<string, Type> _selectedMachine;
        public Tuple<string, Type> SelectedMachine
        {
            get => _selectedMachine; set    // 机床切换逻辑
            {
                if (_selectedMachine != value)
                {
                    // 先处理掉之前的机床
                    if (machine != null)
                    {
                        if (DeviceStatus == DeviceStatus.Idle)
                            Disconnect();
                        else if (DeviceStatus != DeviceStatus.Disconnected)
                        {
                            MessageWindow.ShowDialog("机床正忙");
                            return;
                        }
                        Axes.Clear();
                        machine = null;
                    }
                    var temp_machine = containerProvider.Resolve(value.Item2);
                    if (temp_machine != null)
                    {
                        machine = (IMachineHardware)temp_machine;
                        string json_path = ConfigStore.StoreDir + "/" + value.Item1 + "MachineConfig.json";
                        if (LoadConfig(json_path) != null)  // 加载配置
                        {
                            MessageWindow.Show("配置文件读取失败，加载默认配置");

                            if (File.Exists(json_path))
                                File.Copy(json_path, $"{json_path}-{System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.bak");
                            machineInfo ??= new();        // 加载失败使用默认配置
                            retractInfo ??= new();

                            Ip = "10.0.0.100";
                            Port = 701;
                            IsRetract = false;
                            RetractValue = 0;

                            Axes ??= new();
                            Axes.Clear();
                            Axes.AddRange(new List<AxisViewModel>()
                            {
                                new (dialogService, machine, new() { Name = "X", NodeNum = 0 },eventAggregator),
                                new (dialogService, machine, new() { Name = "Y", NodeNum = 1 },eventAggregator),
                                new (dialogService, machine, new() { Name = "Z", NodeNum = 2 },eventAggregator),
                                new (dialogService, machine, new() { Name = "A", NodeNum = 3 },eventAggregator),
                                new (dialogService, machine, new() { Name = "B", NodeNum = 4 },eventAggregator),
                                new (dialogService, machine, new() { Name = "C", NodeNum = 5 },eventAggregator)
                            });
                            OffsetSettings = containerProvider.Resolve<OffsetSettingsViewModel>();
                            OffsetSettings.SensorOffset ??= new();
                            OffsetSettings.SensorOffset.Clear();
                            OffsetSettings.SensorOffset.AddRange(new List<SensorOffsetViewModel>()
                            {
                                new () { Name="振镜", IsChecked=true },
                                new () { Name="相机", IsChecked=false },
                            });
                            OffsetSettings.XTX ??= new();
                            OffsetSettings.XTX.Clear();
                            OffsetSettings.XTX.AddRange(new List<MechanicalStructureParameter>()
                            {
                                new ("XTB",0),
                                new ("XTC",0),
                                new ("YTA",0),
                                new ("YTC",0),

                                new ("ZTA",0),
                                new ("ZTB",0),
                                new ("ATB",0),
                                new ("ATC",0),

                                new ("BTC",0),
                                new ("ATW",0),
                                new ("BTW",0),
                                new ("CTW",0),
                            });
                            SaveConfig(json_path);
                            LoadConfig(json_path);
                        }
                    }
                }
                SetProperty(ref _selectedMachine, value);
                // 保存设置
                _selectedMachineInfo.Value = SelectedMachine.Item1;
            }
        }
        private ParameterViewModel<string> _selectedMachineInfo;
        public ParameterViewModel<bool> MachineInterpolationIsEnabled { get; private set; }
        public ParameterViewModel<double> MachineInterpolationIsStep { get; private set; }
        #region Command

        //球轴承
        public DelegateCommand ToggleConnectionCommand { get; private set; }
        public DelegateCommand<string> FocusControlCommand { get; private set; }
        public DelegateCommand HomeAllCommand { get; private set; }
        public DelegateCommand WorkpieceOffsetCheckCommand { get; private set; }
        public DelegateCommand<string> BlowVacuumCommand { get; private set; }

        private System.DateTime _lastRefreshTime = System.DateTime.MinValue;
        private readonly object _refreshLock = new(); // 防止并发执行

        #endregion

        #region Python公式

        private void UpdateHeadCanMove()
        {
            // 如果 IsMoving 为 false 且所有 Axes 的 Enabled 都为 true，则 HeadCanMove 为 true
            if (IsMoving == false && Axes.All(a => a.Enabled == true))
            {
                HeadCanMove = true;
            }
            else
            {
                HeadCanMove = false;
            }
        }
        //刀尖运动时，判断移动方向是否一致
        public bool CheckIsMove(Point3D target, Vector3D normal, Vector3D direction_x, int tool, out double theta, double change, int node)
        {
            try
            {
                double[] point = FromVector(target, normal, direction_x, tool, out theta);
                bool flag = true;
                if (Math.Abs(point[node] - change) > 1e-4)
                {
                    flag = false;
                    MessageWindow.ConfirmWindow($"目标值超出限定范围，可能反向运动，是否继续？", r =>
                    {
                        if (r.Result == ButtonResult.OK)
                        {
                            flag = true;
                        }
                    });
                }
                return flag;

            }
            catch (Exception ex)
            {
                MessageWindow.ShowDialog($"公式计算错误\n请检查相应公式并重新加载\n{ex.Message}");
                theta = 0;
                return false;

            }
        }

        #endregion


    
        public MachineViewModel(IContainerProvider provider)
        {
           

            containerProvider = provider;
            FormulaFile = ParameterManager.LoadPara<string>(nameof(FormulaFile));
            MachineInterpolationIsEnabled = ParameterManager.LoadPara<bool>(nameof(MachineInterpolationIsEnabled));
            MachineInterpolationIsStep = ParameterManager.LoadPara<double>(nameof(MachineInterpolationIsStep));
            globalMachineState = containerProvider.Resolve<GlobalMachineState>();

            FormulaFile.Value = "XYZ_20250304.fml";
            FormulaFile.PreDataChange += (old_value, new_value) =>
            {
                return !LoadFormula(new_value); // 加载成功才会换公式
            };
            dialogService = containerProvider.Resolve<IDialogService>();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            OffsetSettings = containerProvider.Resolve<OffsetSettingsViewModel>();
         
            Axes_Compensation = AxesCompensation.Instance;

           

            Feed = Properties.Settings.Default.Feed;
            FocusFeed = 500;
            LoadGlobalReset();
            // 机床类型注册
            Machines = new()
            {
                new("ACS", typeof(MachineHardwareAcs)),
                //new("Beckhoff", typeof(MachineHardWareBeckhoff)),
                //new("Fidia", typeof(MachineHardwareFidia)),
                //new("Siemens-Test", typeof(MachineHardwareSiemens)),
               //new("Simu", typeof(MachineHardwareSimulator)),
                //new("JD-Test", typeof(MachineHardwareJD)),
            };

            Axes = new();

            //ToolHeadAxes = new();// { new() { Name = "X" }, new() { Name = "Y" }, new() { Name = "Z" }, new() { Name = "A" }, new() { Name = "B" }, new() { Name = "C" } };
            _selectedMachineInfo = ParameterManager.LoadPara<string>("SelectedMahine");
            var candidates = Machines.Where(x => x.Item1 == _selectedMachineInfo?.Value);
            if (candidates.Count() == 1)
                SelectedMachine = candidates.First();
            else
                SelectedMachine = Machines.FirstOrDefault();    // 机床Model类实例化在这里面

            // 刷新时钟
            //timer.Elapsed += (_, _) => Refresh();
            timer.Elapsed += (s, e) => _ = RefreshAsync();

            timer.Stop();

            OnConnected += () => timer.Start();    // 连上开始刷新
            OnDisconnected += () =>
            {
                timer.Stop();
                eventAggregator.GetEvent<CoprocessingExceptionEvent>().Publish();
            };   // 断开停止刷新

            // 命令绑定
            HomeAllCommand = new(() =>
            {
                var task = new DoWorkEventHandler((s, e) =>
                {
                    foreach (var ax in Axes)
                    {
                        e.Result = machine.Home(ax.AxisDefination);
                    }
                });
                var para = new DialogParameters
                {
                    { "task", task },
                    { "title", $"回零" },
                };
                System.Windows.Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    dialogService.ShowDialog("ProgressBox", para, r => { });
                }));
            });
            ToggleConnectionCommand = new(() =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                    Connect();
                else if (DeviceStatus == DeviceStatus.Idle)
                    Disconnect();
                else
                    MessageWindow.ShowDialog("机床正忙！");

            });

            WorkpieceOffsetCheckCommand = new(() =>
            {
                ToVector(new Point3D(0, 0, 0), new Vector3D(0, 0, 1), new(1, 0, 0), 0, out _, inProgress: false);
            });  ///TODO
            // 焦点运动控制
            FocusControlCommand = new DelegateCommand<string>((p) =>
            {
                var distance = new double[6];
                string axis_name = p[..1];

                // 获取当前坐标和刀尖坐标系
                (var now_point, var z_vector, var x_vector) =
                    GetToolHead(Get6AxesPosition());
                var y_vector = z_vector.Cross(x_vector);

                // 定义目标点
                Point3D new_point;
                Vector3D new_x_vector, new_z_vector;
                var tool_num = OffsetSettings.SensorOffset.IndexOf(OffsetSettings.GetSelectedSensor());

                //所有dxdydz一起动，只能绝对，不能相对
                if (p[..1] == "d")
                {
                    axes_num.TryGetValue("dx", out var nodedx);
                    axes_num.TryGetValue("dy", out var nodedy);
                    axes_num.TryGetValue("dz", out var nodedz);

                    distance[nodedx] = ToolHeadAxes.Where(x => x.Name == "dx").First().PtpTarget;
                    distance[nodedy] = ToolHeadAxes.Where(x => x.Name == "dy").First().PtpTarget;
                    distance[nodedz] = ToolHeadAxes.Where(x => x.Name == "dz").First().PtpTarget;

                    //MessageBox.Show(distance.ToString());

                    double sum = distance[nodedx] * distance[nodedx] + distance[nodedy] * distance[nodedy] + distance[nodedz] * distance[nodedz];
                    if (sum > 0)
                    {
                        sum = Math.Sqrt(sum);
                        distance[nodedx] /= sum;
                        distance[nodedy] /= sum;
                        distance[nodedz] /= sum;
                    }
                    new_point = now_point;
                    new_z_vector = new Vector3D(distance[nodedx], distance[nodedy], distance[nodedz]);
                    new_x_vector = new Vector3D(x_vector.X, x_vector.Y, x_vector.Z);

                    new_z_vector /= new_z_vector.Length;
                    // To Point
                    ToVector(new_point, new_z_vector, new_x_vector, tool_num, out _, inProgress: false, isAsync: true);

                    return;
                }
                else if (axes_num.TryGetValue(axis_name, out var node) && node >= 3)  // ABC
                {
                    var toolhead = ToolHeadAxes.Where(x => x.Name == axis_name).First();
                    //MessageBox.Show(toolhead);
                    double[] position = Get6AxesPosition();
                    (var now_point1, var z_vector1, var x_vector1) = GetToolHead(position);

                    var new_position = Get6ToolHeadAxesPosition();
                    var old_position_XYZ = new Point3D(new_position[0], new_position[1], new_position[2]);
                    var old_position_ABC = new Point3D(new_position[3], new_position[4], new_position[5]);
                    double[] Axis_Offset = GetAxisOffset();
                    if (p.Length >= 2)// 相对运动
                        new_position[node] += (p[1] == '+' ? 1 : -1) * toolhead.RelTarget;
                    else if (p.Length == 1) // 绝对位置
                        new_position[node] = toolhead.PtpTarget;
                    //MessageBox.Show(toolhead.RelTarget.ToString());
                    for (int i = 0; i < new_position.Count(); i++)
                        new_position[i] += Axis_Offset[i];
                    (var now_point2, var z_vector2, var x_vector2) = GetToolHead(new_position);
                    Point3D target = new Point3D(new_position[0], new_position[1], new_position[2]);
                    Point3D target_ABC = new Point3D(new_position[3], new_position[4], new_position[5]);

                    if (!CheckIsMove(now_point1, z_vector2, x_vector2, tool_num, out _, new_position[node], node))
                    {
                        return;
                    }
                    LoggingService.Instance.LogInfo($"刀尖运动 {SelectedSensor}   mode:Absolutely ABC\n" +
                        $"X:{old_position_XYZ.X.ToString("F4")}  --->  {target.X:F4}\n" +
                            $"Y:{old_position_XYZ.Y.ToString("F4")}  --->  {target.Y:F4}\n" +
                            $"Z:{old_position_XYZ.Z.ToString("F4")}  --->  {target.Z:F4}\n" +
                            $"A:{old_position_ABC.X.ToString("F4")}  --->  {target_ABC.X:F4}\n" +
                            $"B:{old_position_ABC.Y.ToString("F4")}  --->  {target_ABC.Y:F4}\n" +
                            $"C:{old_position_ABC.Z.ToString("F4")}  --->  {target_ABC.Z:F4}\n");
                    ToVector(now_point1, z_vector2, x_vector2, tool_num, out _, inProgress: false, isAsync: true);
                }
                else    // XYZ 
                {
                    var axis = GetAxisByName(axis_name.ToUpper());
                    var tool = ToolHeadAxes.Where(x => x.Name == axis_name).First();
                    if (p.Length >= 2)
                    {
                        double[] position = Get6AxesPosition();
                        var new_position = Get6ToolHeadAxesPosition();
                        var old_position_XYZ = new Point3D(new_position[0], new_position[1], new_position[2]);
                        var old_position_ABC = new Point3D(new_position[3], new_position[4], new_position[5]);

                        (var now_point1, var z_vector1, var x_vector1) = GetToolHead(position);
                        new_position[node] += (p[1] == '+' ? 1 : -1) * tool.RelTarget;
                        Point3D target = new Point3D(new_position[0], new_position[1], new_position[2]);
                        Point3D target_ABC = new Point3D(new_position[3], new_position[4], new_position[5]);

                        LoggingService.Instance.LogInfo($"刀尖运动 {SelectedSensor}   mode:Relatively XYZ\n" +
                            $"X:{old_position_XYZ.X.ToString("F4")}  --->  {target.X:F4}\n" +
                            $"Y:{old_position_XYZ.Y.ToString("F4")}  --->  {target.Y:F4}\n" +
                            $"Z:{old_position_XYZ.Z.ToString("F4")}  --->  {target.Z:F4}\n" +
                            $"A:{old_position_ABC.X.ToString("F4")}  --->  {target_ABC.X:F4}\n" +
                            $"B:{old_position_ABC.Y.ToString("F4")}  --->  {target_ABC.Y:F4}\n" +
                            $"C:{old_position_ABC.Z.ToString("F4")}  --->  {target_ABC.Z:F4}\n");

                        ToVector(target, z_vector1, x_vector1, tool_num, out _, inProgress: false, isAsync: true);

                    }
                    else
                    {
                        double[] position = Get6AxesPosition();
                        var new_position = Get6ToolHeadAxesPosition();
                        var old_position_XYZ = new Point3D(new_position[0], new_position[1], new_position[2]);
                        var old_position_ABC = new Point3D(new_position[3], new_position[4], new_position[5]);
                        (var now_point1, var z_vector1, var x_vector1) = GetToolHead(position);
                        new_position[node] = tool.PtpTarget;
                        Point3D target = new Point3D(new_position[0], new_position[1], new_position[2]);
                        Point3D target_ABC = new Point3D(new_position[3], new_position[4], new_position[5]);

                        LoggingService.Instance.LogInfo($"刀尖运动 {SelectedSensor}   mode:Absolutely XYZ\n" +
                          $"X:{old_position_XYZ.X.ToString("F4")}  --->  {target.X:F4}\n" +
                            $"Y:{old_position_XYZ.Y.ToString("F4")}  --->  {target.Y:F4}\n" +
                            $"Z:{old_position_XYZ.Z.ToString("F4")}  --->  {target.Z:F4}\n" +
                            $"A:{old_position_ABC.X.ToString("F4")}  --->  {target_ABC.X:F4}\n" +
                            $"B:{old_position_ABC.Y.ToString("F4")}  --->  {target_ABC.Y:F4}\n" +
                            $"C:{old_position_ABC.Z.ToString("F4")}  --->  {target_ABC.Z:F4}\n");
                        ToVector(target, z_vector1, x_vector1, tool_num, out _, inProgress: false, isAsync: true);
                    }
                }
            }).ObservesCanExecute(() => IsIdle);

            eventAggregator.GetEvent<SaveSettingEvent>().Subscribe((msg) =>
            {
                if (msg.ToString() == "Machine") SaveConfig();
            });

            eventAggregator.GetEvent<Cmd_GlobalResetEvent>().Subscribe(async () =>
            {
                try
                {
                    if (DeviceStatus == DeviceStatus.Disconnected)
                    {
                        MessageBox.Show("机床未连接");
                        return;
                    }

                    // x轴
                    Axes[0].ToDisplayPoint(XGlobalReset, true);
                    await Task.Delay(CalculateDelay(XGlobalReset - Axes[0].DisplayPosition, Axes[0].Speed));

                    // Z轴
                    Axes[2].ToDisplayPoint(ZGlobalReset, true);
                    await Task.Delay(CalculateDelay(ZGlobalReset - Axes[2].DisplayPosition, Axes[2].Speed));                    

                    // y轴
                    Axes[1].ToDisplayPoint(YGlobalReset, true);
                    await Task.Delay(CalculateDelay(YGlobalReset - Axes[1].DisplayPosition, Axes[1].Speed));

                    Axes[3].ToDisplayPoint(AGlobalReset, true);

                    Axes[4].ToDisplayPoint(CGlobalReset, true);

                    LoggingService.Instance.LogInfo("全局复位完成");
                    eventAggregator.GetEvent<GlobalResetEvent>().Publish(true);
                    //MessageBox.Show("复位完成！");

                }
                catch (Exception ex) 
                {
                    LoggingService.Instance.LogError("全局复位异常", ex);
                    eventAggregator.GetEvent<GlobalResetEvent>().Publish(false);
                }
            });

            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Subscribe((r) =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                {
                    MessageBox.Show("机床未连接");
                    return;
                }
                switch (r.Item1.ToString())
                {
                    case "X":
                        Axes[0].ToDisplayPoint(r.Item2, r.Item3);
                        break;
                    case "Y":
                        Axes[1].ToDisplayPoint(r.Item2, r.Item3);
                        break;
                    case "Z":
                        Axes[2].ToDisplayPoint(r.Item2, r.Item3);
                        break;
                    case "A":
                        Axes[3].ToDisplayPoint(r.Item2, r.Item3);
                        break;
                    case "B":
                        Axes[4].ToDisplayPoint(r.Item2, r.Item3);
                        break;
                    default:
                        break;
                }
            });

            eventAggregator.GetEvent<Cmd_StartProcessPrepareEvent>().Subscribe(async (meg) =>
            {
#if true
                try
                {
                    if (DeviceStatus == DeviceStatus.Disconnected)
                    {
                        MessageBox.Show("机床未连接");
                        return;
                    }
                    //Axes[2].ToDisplayPoint(meg.Item1[2], true);

                    //await Task.Delay((int)(CalculateDelay(meg.Item1[2], Axes[2].Speed)));
                    var ax = Axes.Where(d => d.Name == "C").ToList();
                    if (ax.Any())
                    {
                        AxisViewModel amx = ax.FirstOrDefault();
                        int i = Axes.IndexOf(amx);
                        Axes[i].ToDisplayPoint(meg.Values[i], true);
                        await Task.Delay(CalculateDelay(meg.Values[i], Axes[i].Speed));

                        while (!IsWithinTolerance(Axes[i].DisplayPosition, meg.Values[i]))
                        {

                        }
                    }


                    ax = Axes.Where(d => d.Name == "A").ToList();
                    if (ax.Any())
                    {
                        AxisViewModel amx = ax.FirstOrDefault();
                        int i = Axes.IndexOf(amx);
                        Axes[i].ToDisplayPoint(meg.Values[i], true);
                        await Task.Delay(CalculateDelay(meg.Values[i], Axes[i].Speed));
                        while (!IsWithinTolerance(Axes[i].DisplayPosition, meg.Values[i]))
                        {

                        }
                    }
                    ax = Axes.Where(d => d.Name == "Z").ToList();
                    if (ax.Any())
                    {
                        AxisViewModel amx = ax.FirstOrDefault();
                        int i = Axes.IndexOf(amx);
                        Axes[i].ToDisplayPoint(meg.Values[i], true);
                        await Task.Delay(CalculateDelay(meg.Values[i], Axes[i].Speed));
                        while (!IsWithinTolerance(Axes[i].DisplayPosition, meg.Values[i]))
                        {

                        }
                    }
                    ax = Axes.Where(d => d.Name == "Y").ToList();
                    if (ax.Any())
                    {
                        AxisViewModel amx = ax.FirstOrDefault();
                        int i = Axes.IndexOf(amx);
                        Axes[i].ToDisplayPoint(meg.Values[i], true);
                        await Task.Delay(CalculateDelay(meg.Values[i], Axes[i].Speed));
                        while (!IsWithinTolerance(Axes[i].DisplayPosition, meg.Values[i]))
                        {

                        }
                    }
                    ax = Axes.Where(d => d.Name == "X").ToList();
                    if (ax.Any())
                    {
                        AxisViewModel amx = ax.FirstOrDefault();
                        int i = Axes.IndexOf(amx);
                        Axes[i].ToDisplayPoint(meg.Values[i], true);
                        await Task.Delay(CalculateDelay(meg.Values[i] - Axes[i].DisplayPosition, Axes[i].Speed));
                        while (!IsWithinTolerance(Axes[i].DisplayPosition, meg.Values[i]))
                        {

                        }
                    }


                    if(await machine.StartRunningFile(meg.Message)==ProcessError.OK.ToString())
                    {
                        meg.Completion.SetResult(true);
                    }
                    else
                    {
                        meg.Completion.SetResult(false);
                    }
                }

                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("移动到加工位置过程出现异常", ex);
                    MessageBox.Show("移动到加工位置过程出现异常，加工失败");
                    return;
                }                

                //eventAggregator.GetEvent<Cmd_StartProcessEvent>().Publish(meg.Item2);
#endif

#if false
                if (DeviceStatus == DeviceStatus.Disconnected)
                {
                    MessageBox.Show("机床未连接");
                    return;
                }

                try
                {//0x 1y 2z 3a 4c
                    if (Axes[1].Name == "Y")
                    {
                        Axes[1].ToDisplayPoint(meg.Item1[1], true);
                        await Task.Delay((CalculateDelay(meg.Item1[1] - Axes[1].DisplayPosition, Axes[1].Speed)));
                    }

                    if (Axes[2].Name == "Z")
                    {
                        Axes[2].ToDisplayPoint(meg.Item1[2], true);
                        await Task.Delay((CalculateDelay(meg.Item1[2] - Axes[2].DisplayPosition, Axes[2].Speed)));
                    }

                    if (Axes[0].Name == "X")
                    {
                        Axes[0].ToDisplayPoint(meg.Item1[0], true);
                        await Task.Delay((CalculateDelay(meg.Item1[0] - Axes[0].DisplayPosition, Axes[0].Speed)));
                    }

                    if (Axes[3].Name == "A")
                    {
                        Axes[3].ToDisplayPoint(meg.Item1[3], true);
                        await Task.Delay((CalculateDelay(meg.Item1[3] - Axes[3].DisplayPosition, Axes[3].Speed)));
                    }

                    if (Axes[4].Name == "C")
                    {
                        Axes[4].ToDisplayPoint(meg.Item1[4], true);
                        await Task.Delay((CalculateDelay(meg.Item1[4] - Axes[4].DisplayPosition, Axes[4].Speed)));
                    }

                    machine.StartRunningFile(meg.Item2);
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("移动到加工位置过程出现异常", ex);
                    MessageBox.Show("移动到加工位置过程出现异常，加工失败");
                    return;
                }
#endif
            });


            eventAggregator.GetEvent<RunRequestEvent>().Subscribe(async (meg) =>
            {

                try
                {
                    if (DeviceStatus == DeviceStatus.Disconnected)
                    {
                        MessageBox.Show("机床未连接");
                        return;
                    }

                    await machine.StartRunSript(meg.Data);
                    await Task.Delay(100); // 模拟耗时操作

                    // 处理完成，通知发布者
                    meg.CompletionSource.SetResult(true);

                }

                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("执行脚本过程出现异常", ex);
                    MessageBox.Show("执行脚本过程出现异常，加工失败");
                    meg.CompletionSource.SetResult(false);
                    return;
                }

            });

            eventAggregator.GetEvent<SetAxesParamEvent>().Subscribe((meg) =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                {
                    MessageBox.Show("机床未连接");
                    return;
                }
                machine.MotionConfigure((Axis)Axes[0].NodeNum, meg.XSpeed, meg.XAccelerate, meg.XDecelerate);
                machine.MotionConfigure((Axis)Axes[1].NodeNum, meg.YSpeed, meg.YAccelerate, meg.YDecelerate);
                machine.MotionConfigure((Axis)Axes[2].NodeNum, meg.ZSpeed, meg.ZAccelerate, meg.ZDecelerate);
                machine.MotionConfigure((Axis)Axes[3].NodeNum, meg.ASpeed, meg.AAccelerate, meg.ADecelerate);
                machine.MotionConfigure((Axis)Axes[4].NodeNum, meg.BSpeed, meg.BAccelerate, meg.BDecelerate);

                Axes[0].Speed = meg.XSpeed;
                Axes[0].AcceleratedSpeed = meg.XAccelerate;
                Axes[0].DecelerationSpeed = meg.XAccelerate;
                Axes[0].LoadTemp();

                Axes[1].Speed = meg.YSpeed;
                Axes[1].AcceleratedSpeed = meg.YAccelerate;
                Axes[1].DecelerationSpeed = meg.YAccelerate;
                Axes[1].LoadTemp();

                Axes[2].Speed = meg.ZSpeed;
                Axes[2].AcceleratedSpeed = meg.ZAccelerate;
                Axes[2].DecelerationSpeed = meg.ZAccelerate;
                Axes[2].LoadTemp();

                Axes[3].Speed = meg.ASpeed;
                Axes[3].AcceleratedSpeed = meg.AAccelerate;
                Axes[3].DecelerationSpeed = meg.AAccelerate;
                Axes[3].LoadTemp();

                Axes[4].Speed = meg.BSpeed;
                Axes[4].AcceleratedSpeed = meg.BAccelerate;
                Axes[4].DecelerationSpeed = meg.BAccelerate;
                Axes[4].LoadTemp();

            });

            eventAggregator.GetEvent<MachineSetWorkpieceOffsetEvent>().Subscribe((doubles) =>
            {
                OffsetSettings.WorkpieceOffset = new()
                {
                    X = doubles[0],
                    Y = doubles[1],
                    Z = doubles[2],
                    Alpha = doubles[3],
                    Beta = doubles[4],
                    Gamma = doubles[5]
                };
            });
            //ToVector(t.Item1, t.Item2, out double theta, isAsync: true));

            //eventAggregator.GetEvent<EmergencyStopEvent>().Subscribe(() =>
            //{
            //    // StopAll(); //25/11/27  卡顿问题
            //    Task.Run(() =>
            //    {
            //        try
            //        {
            //            StopAll();   //  
            //        }
            //        catch (Exception ex)
            //        {
            //            LoggingService.Instance.LogError("EmergencyStop StopAll 出错: " + ex.Message);
            //        }
            //    });

            //});

            eventAggregator.GetEvent<EmergencyStopEvent>().Subscribe(async () =>
            {
                try
                {
                    StopAll(); // 不会卡 UI
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("StopAllAsync error: " + ex.Message);
                }
            }, ThreadOption.BackgroundThread);
            eventAggregator.GetEvent<KillAllAxisEvent>().Subscribe(() =>
            {
                if (DeviceStatus != DeviceStatus.Disconnected)
                {
                  
                    if (machine.StopAll() == null)
                    {
                        MessageBox.Show("急停结束");
                    }
                    //LoggingService.Instance.LogWarning("急停");
                }
            });

            // ccd获取坐标
            eventAggregator.GetEvent<CcdGetPointEvent>().Subscribe((Point) =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected || DeviceStatus == DeviceStatus.Connecting) // 刷新状态失败
                    return;

                var target = GetToolHead(Get6AxesPosition(), OffsetSettings.GetSelectedSensor().GetArray);
                List<double> position = new()
                {
                    target.Item1.X, // 中心点坐标
                    target.Item1.Y,
                    target.Item1.Z,
                    target.Item2.X, // 相机平面的法向量: dir_z
                    target.Item2.Y,
                    target.Item2.Z,
                    target.Item3.X, // 相机平面的x轴向量: dir_x
                    target.Item3.Y,
                    target.Item3.Z
                };
                Point.Axes = position.ToArray();
                Point.ToolHead = OffsetSettings.GetSelectedSensor().Name;
            });
            // ccd移动机床
            eventAggregator.GetEvent<CcdToolMoveEvent>().Subscribe((t) =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected != true && t.Count() == 6)
                    return;

                var now_position = GetToolHead(Get6AxesPosition(), OffsetSettings.SensorOffset[1].GetArray);
                Point3D target = new(t[0], t[1], t[2]);
                Vector3D dir_z = new(t[3], t[4], t[5]);
                Vector3D dir_x = new(t[6], t[7], t[8]);

                if (dir_z != now_position.Item2)
                {
                    MessageWindow.ShowDialog("不允许在相机视角下变换观察角度");
                    return;
                }
                if (OffsetSettings.GetSelectedSensorIndex() != 1)
                {
                    MessageWindow.ShowDialog("请确认当前刀尖处于相机状态下");
                    return;
                }
                //PixelToAxisMove();
                ToVector(target, dir_z, dir_x, 1, out _, true);
            }, ThreadOption.BackgroundThread);
            // ccd设定相机中心
            eventAggregator.GetEvent<CcdSetCameraCenterEvent>().Subscribe((center) =>
            {
                var sensor = OffsetSettings.SensorOffset.Where(x => x.Name == "相机").FirstOrDefault();
                if (sensor != null)
                {
                    double[] galvo = FromVector(new Point3D(center[0], center[1], 0), new Vector3D(0, 0, 1), new Vector3D(1, 0, 0), 1, out var t);
                    double[] camera = FromVector(new Point3D(center[2], center[3], 0), new Vector3D(0, 0, 1), new Vector3D(1, 0, 0), 1, out var a);
                    bool flag = true;
                    if (Math.Pow(camera[0] - galvo[0], 2) + Math.Pow(camera[1] - galvo[1], 2) > Math.Pow(0.02, 2))
                        flag = MessageWindow.ConfirmWindow("振镜位置偏差较大，请重新确认结构参数。仍要修正振镜位置？") == true;
                    if (flag)
                    {
                        sensor.OffsetX -= camera[0] - galvo[0];
                        sensor.OffsetY -= camera[1] - galvo[1];
                    }
                }
            });

#if false
            // ccd对焦
            eventAggregator.GetEvent<CcdFocusMoveEvent>().Subscribe(message =>
            {
                void RefreshFocus(DateTime now)
                {
                    message.TimePositions.Add((now, Axes[0].DisplayPosition));
                }
                List<double> old_speed = Axes.Select(x => x.Speed).ToList();
                foreach (var axis in Axes)
                    axis.Speed = message.FocusSpeed;
                try
                {
                    OnMachineRefreshed += RefreshFocus;
                    Axes[0].ToDisplayPoint(message.FocusDistance, false, false);
                }
                finally
                {
                    Thread.Sleep((int)(message.FocusDistance / message.FocusSpeed * 1000 + 1000));
                    OnMachineRefreshed -= RefreshFocus;
                    old_speed.Zip(Axes).ToList().ForEach(x => x.Second.Speed = x.First);
                }
            });

            eventAggregator.GetEvent<FinishFocuseEvent>().Subscribe(message => {
                Axes[0].ToDisplayPoint(message, true, false);
            });

            // 寻找顶点过程移动
            eventAggregator.GetEvent<CcdFindPeakEvent>().Subscribe(message =>
            {
                if (message.isOnDown)
                {
                    void RefreshFocus(DateTime now)
                    {
                        message.OnDown.Add((now, Axes[1].DisplayPosition));
                    }
                    Axes[1].ToDisplayPoint(-1, false, false);
                    Thread.Sleep((int)(1 / Axes[1].Speed * 1000 + 1000));

                    List<double> old_speed = Axes.Select(x => x.Speed).ToList();
                    foreach (var axis in Axes)
                        axis.Speed = message.MoveSpeed;

                    try
                    {
                        OnMachineRefreshed += RefreshFocus;
                        Axes[1].ToDisplayPoint(2, false, false);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Thread.Sleep((int)(2 / message.MoveSpeed * 1000 + 1000));
                        OnMachineRefreshed -= RefreshFocus;
                        old_speed.Zip(Axes).ToList().ForEach(x => x.Second.Speed = x.First);
                    }
                }
                else
                {
                    void RefreshFocus(DateTime now)
                    {
                        message.LeftRight.Add((now, Axes[2].DisplayPosition));
                    }
                    Axes[2].ToDisplayPoint(-1, false, false);
                    Thread.Sleep((int)(1 / Axes[2].Speed * 1000 + 1000));

                    List<double> old_speed = Axes.Select(x => x.Speed).ToList();
                    foreach (var axis in Axes)
                        axis.Speed = message.MoveSpeed;

                    try
                    {
                        OnMachineRefreshed += RefreshFocus;
                        Axes[2].ToDisplayPoint(2, false, false);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Thread.Sleep((int)(2 / message.MoveSpeed * 1000 + 1000));
                        OnMachineRefreshed -= RefreshFocus;
                        old_speed.Zip(Axes).ToList().ForEach(x => x.Second.Speed = x.First);
                    }
                }
            });

            // 寻找顶点完毕移动
            eventAggregator.GetEvent<FinishFindPeakEvent>().Subscribe(result =>
            {
                if (result.Item1)
                {
                    Axes[1].ToDisplayPoint(result.Item2, true, false);
                    Thread.Sleep((int)(2 / Axes[1].Speed * 1000 + 1000));
                }
                else
                {
                    Axes[2].ToDisplayPoint(result.Item2, true, false);
                    Thread.Sleep((int)(2 / Axes[2].Speed * 1000 + 1000));
                }
            });
#elif true
            // ccd对焦
            eventAggregator.GetEvent<CcdFocusMoveEvent>().Subscribe(message =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                {
                    throw new Exception("机床未连接！");
                }
                void RefreshFocus(DateTime now)
                {
                    message.TimePositions.Add((now, Axes[0].DisplayPosition));
                }
                List<double> old_speed = Axes.Select(x => x.Speed).ToList();
                foreach (var axis in Axes)
                {
                    axis.Speed = message.FocusSpeed;
                 
                    //axis.AcceleratedSpeed; //加速度
                    //axis.TempAcceleratedSpeed
                }
               
                try
                {
                    OnMachineRefreshed += RefreshFocus;
                    Axes[0].ToDisplayPoint(message.FocusDistance, false, false);  //-message.FocusDistance改成message.FocusDistance  远离x轴方向 
                    Thread.Sleep(CalculateDelay(message.FocusDistance, message.FocusSpeed));
                    OnMachineRefreshed -= RefreshFocus;
                    old_speed.Zip(Axes).ToList().ForEach(x => x.Second.Speed = x.First);
                }
                catch
                {
                    return;
                }
            });

            eventAggregator.GetEvent<FinishFocuseEvent>().Subscribe(message =>
            {
                Axes[0].ToDisplayPoint(message, true, false);
            });

            // 寻找顶点过程移动
            eventAggregator.GetEvent<CcdFindPeakEvent>().Subscribe(message =>
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                {
                    throw new Exception("机床未连接！");
                }
                if (message.isOnDown)
                {
                    void RefreshFocus(DateTime now)
                    {
                        message.OnDown.Add((now, Axes[1].DisplayPosition));
                    }
                    Axes[1].ToDisplayPoint(-1, false, false);
                    Thread.Sleep(CalculateDelay(1, Axes[1].Speed));

                    List<double> old_speed = Axes.Select(x => x.Speed).ToList();
                    foreach (var axis in Axes)
                        axis.Speed = message.MoveSpeed;

                    try
                    {
                        OnMachineRefreshed += RefreshFocus;
                        Axes[1].ToDisplayPoint(2, false, false);
                        Thread.Sleep(CalculateDelay(2, message.MoveSpeed));
                        OnMachineRefreshed -= RefreshFocus;
                        old_speed.Zip(Axes).ToList().ForEach(x => x.Second.Speed = x.First);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                       
                    }
                }
                else
                {
                    void RefreshFocus(DateTime now)
                    {
                        message.LeftRight.Add((now, Axes[2].DisplayPosition));
                    }
                    Axes[2].ToDisplayPoint(-1, false, false);
                    Thread.Sleep(CalculateDelay(1, Axes[2].Speed));

                    List<double> old_speed = Axes.Select(x => x.Speed).ToList();
                    foreach (var axis in Axes)
                        axis.Speed = message.MoveSpeed;

                    try
                    {
                        OnMachineRefreshed += RefreshFocus;
                        Axes[2].ToDisplayPoint(2, false, false);
                        Thread.Sleep(CalculateDelay(2, message.MoveSpeed));
                        OnMachineRefreshed -= RefreshFocus;
                        old_speed.Zip(Axes).ToList().ForEach(x => x.Second.Speed = x.First);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        
                    }
                }
            });

            // 寻找顶点完毕移动
            eventAggregator.GetEvent<FinishFindPeakEvent>().Subscribe(result =>
            {
                if (result.Item1)
                {
                    Axes[1].ToDisplayPoint(result.Item2, true, false);
                    Thread.Sleep(CalculateDelay(2, Axes[1].Speed));
                }
                else
                {
                    Axes[2].ToDisplayPoint(result.Item2, true, false);
                    Thread.Sleep(CalculateDelay(2, Axes[2].Speed));
                }
            });

#endif

            // ccd设定机床偏移
            eventAggregator.GetEvent<CcdSetMachineCenterEvent>().Subscribe((center) =>
            {
                double x = center[0], y = center[1];

                //Axes[0].AxisOffset -= x;
                //Axes[1].AxisOffset -= y;
                OffsetSettings.XTX.Where(x => x.Name.Equals("XTC")).First().Value -= x;
                OffsetSettings.XTX.Where(x => x.Name.Equals("YTC")).First().Value -= y;
            });

            Connect();
        }


        private System.DateTime _lastEmergencyStopTime = System.DateTime.MinValue;
        private object _emergencyStopLock = new object();
        private bool _isEmergency = false;
        ~MachineViewModel()
        {
            timer.Stop();
        }
       
        public static bool IsWithinTolerance(double a, double b, double tolerance = 0.1)
        {
            double delta = a - b;
            return delta >= -tolerance && delta <= tolerance;
        }
        public bool Connect()
        {
            if (DeviceStatus != DeviceStatus.Disconnected)
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    dialogService.ShowDialog("MessageBox", new DialogParameters($"message=连接中，请勿频繁点击"), (r) => { });
                }));
                return false;
            }
            Task.Run(() =>
            {
                DeviceStatus = DeviceStatus.Connecting;
                string err_code = "公式文件错误";
                // 加载Python公式
                if (LoadFormula())
                    err_code = machine.Connect(machineInfo);    // 正常才连接
                else
                {

                    err_code = machine.Connect(machineInfo);
                }
                if (err_code != null)
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(new Action(() =>
                    {
                        dialogService.ShowDialog("MessageBox", new DialogParameters($"message=连接失败\n{err_code}"), (r) => { });
                    }));
                    DeviceStatus = DeviceStatus.Disconnected;
                    LoggingService.Instance.LogInfo($"对象:{SelectedMachine.Item1}连接失败");

                }
                else
                {
                    DeviceStatus = DeviceStatus.Idle;
                    Properties.Settings.Default.LastConnection = true;
                    SaveSettingsWithRetry();
                    StringBuilder structureparameters = new StringBuilder();
                    StringBuilder sensorOffsetparameters = new StringBuilder();
                    StringBuilder axesinfo = new StringBuilder();
                    //foreach (var parameter in OffsetSettings.XTX)
                    //{
                    //    structureparameters.AppendLine(parameter.ToString());
                    //}
                    foreach (var parameter in Axes)
                    {
                        axesinfo.AppendLine(parameter.ToString());
                    }
                    //foreach (var parameter in OffsetSettings.SensorOffset)
                    //{
                    //    sensorOffsetparameters.AppendLine(parameter.ToString());
                    //}
                    LoggingService.Instance.LogInfo($"对象:{SelectedMachine.Item1}连接成功\n轴信息:\n{axesinfo}\n");
                }
            });
            return true;
        }
        public bool Disconnect()
        {
            if (DeviceStatus != DeviceStatus.Idle)
            {
                return false;
            }
            DeviceStatus = DeviceStatus.Connecting;
            timer.Stop();
            string err_code = machine.Disconnect(machineInfo);
            if (err_code != null)
            {
                DeviceStatus = DeviceStatus.Idle;
                System.Windows.Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    dialogService.ShowDialog("MessageBox", new DialogParameters($"message=断开失败\n{err_code}"), (r) => { });
                }));
                //IsConnected = true;
                LoggingService.Instance.LogInfo($"对象:{SelectedMachine.Item1}\r\n操作:断开连接\r\n结果:失败");
            }
            else
            {
                DeviceStatus = DeviceStatus.Disconnected;
                Properties.Settings.Default.LastConnection = false;
                SaveSettingsWithRetry();
                LoggingService.Instance.LogInfo($"对象:{SelectedMachine.Item1}\r\n操作:断开连接\r\n结果:成功");
            }
            return true;
        }

#if false
        // 刷新状态
        public bool Refresh()
        {
            
            try
            {
                if (Axes.Any(x => x.ConfigStoredFlag == false) || OffsetSettings.ConfigChanged == true || OffsetSettings.SensorOffset.Any(x => x.ConfigChanged == true) || OffsetSettings.XTX.Any(x => x.ConfigChanged == true))
                {
                    SaveConfig();

                    OffsetSettings.ConfigChanged = false;
                    foreach (var axis in Axes)
                        axis.ConfigStoredFlag = true;
                    foreach (var sensor in OffsetSettings.SensorOffset)
                        sensor.ConfigChanged = false;
                    foreach (var xtx in OffsetSettings.XTX)
                        xtx.ConfigChanged = false;
                }

                var IsConnected = machine.IsConnected(machineInfo);
                if (!IsConnected)
                {
                    timer.Stop();
                    Disconnect();
                    MessageWindow.ShowDialog($"机床连接断开!");
                    return false;
                }
                //var selected = OffsetSettings.GetSelectedSensor();
                // 同步轴状态
                foreach (var ax in Axes)
                {
                    ax.ConfigStoredFlag = true;
                    ax.Refresh();
                }
                OnMachineRefreshed?.Invoke(DateTime.Now);

                if (Axes.Any(x => x.IsMoving == true))
                {
                    IsMoving = true;
                    DeviceStatus = DeviceStatus.Busy;
                }
                else if (IsConnected == true)
                {
                    DeviceStatus = DeviceStatus.Idle;
                    IsMoving = false;
                }
                else
                {
                    DeviceStatus = DeviceStatus.Error;
                    IsMoving = null;
                }


                // 刷新焦点位置
                //var tool_head = GetToolHead(Get6AxesPosition(), selected.GetArray);
                //var tool_head = Workbench2Model(target.Item1, target.Item2, target.Item3);
                //ToolHeadAxes[0].Position = tool_head.Item1.X;
                //ToolHeadAxes[1].Position = tool_head.Item1.Y;
                //ToolHeadAxes[2].Position = tool_head.Item1.Z;
                for (int i = 3; i < ToolHeadAxes.Count; i++)
                {
                    ToolHeadAxes[i].Position = Axes[i].DisplayPosition;
                }

                // 刷新显示位置
                var laser_data = LaserVectorFromAxes(Get6AxesPosition());
                var laser_target = new Tuple<Point3D, Vector3D, Vector3D, int>(laser_data.Item1, laser_data.Item2 / laser_data.Item2.Length * 215, laser_data.Item3, OffsetSettings.GetSelectedSensor().Name == "振镜" ? 0 : 1);
                eventAggregator.GetEvent<ModelPresenterSetToolTipPositionEvent>().Publish(laser_target);
                var base_target = ObjectVectorFromAxes(Get6AxesPosition());
                eventAggregator.GetEvent<ModelPresenterSetMachineBaseEvent>().Publish(base_target);
            }
            catch (Exception ex)
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                    return true;
                timer.Stop();
                Disconnect();
                MessageWindow.ShowDialog($"机床信息同步失败，连接断开\n{ex.Message}");
                return false;
            }
            return true;
        }

#endif
        private void LoadGlobalReset()
        {
            if (!File.Exists(CSV_Filepath))
            {
                XGlobalReset = 0;
                YGlobalReset = 0;
                ZGlobalReset = 0;
                AGlobalReset = 0;
                CGlobalReset = 0;
                LoggingService.Instance.LogError("读取复位文件失败", new Exception("文件 'global_reset' 不存在"));
                return;
            }
            try
            {
                using var reader = new StreamReader(CSV_Filepath);
                reader.ReadLine();

                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    XGlobalReset = 0;
                    YGlobalReset = 0;
                    ZGlobalReset = 0;
                    AGlobalReset = 0;
                    CGlobalReset = 0;
                    LoggingService.Instance.LogError("读取复位文件失败", new Exception("文件 'global_reset' 为空"));
                    return;
                }

                var values = line.Split(',');

                if (values.Length >= 5 &&
                double.TryParse(values[0], out double xPos) &&
                double.TryParse(values[1], out double yPos) &&
                double.TryParse(values[2], out double zPos) &&
                double.TryParse(values[3], out double aPos) &&
                double.TryParse(values[4], out double bPos))
                {
                    XGlobalReset = (float)xPos;
                    YGlobalReset = (float)yPos;
                    ZGlobalReset = (float)zPos;
                    AGlobalReset = (float)aPos;
                    CGlobalReset = (float)bPos;
                    LoggingService.Instance.LogInfo("全局复位初始化完成");
                }
            }
            catch (Exception ex)
            {
                XGlobalReset = 0;
                YGlobalReset = 0;
                ZGlobalReset = 0;
                AGlobalReset = 0;
                CGlobalReset = 0;
                LoggingService.Instance.LogError("全局复位初始化失败", ex);
                return;
            }
        }

        public async Task<bool> RefreshAsync()
        {
            lock (_refreshLock)
            {
                // 简单节流，每100ms最多执行一次
                if ((DateTime.Now - _lastRefreshTime).TotalMilliseconds < 50)
                    return true;

                _lastRefreshTime = DateTime.Now;
            }

            try
            {
                // 处理配置保存等逻辑（不涉及 UI，可异步）
                await Task.Run(() =>
                {
                    if (Axes.Any(x => x.ConfigStoredFlag == false) ||
                        OffsetSettings.ConfigChanged ||
                        OffsetSettings.SensorOffset.Any(x => x.ConfigChanged) ||
                        OffsetSettings.XTX.Any(x => x.ConfigChanged))
                    {
                        OffsetSettings.ConfigChanged = false;
                        foreach (var axis in Axes)
                            axis.ConfigStoredFlag = true;
                        foreach (var sensor in OffsetSettings.SensorOffset)
                            sensor.ConfigChanged = false;
                        foreach (var xtx in OffsetSettings.XTX)
                            xtx.ConfigChanged = false;
                    }
                });

                // 判断连接状态（可能涉及网络/串口，可异步）
                bool isConnected = await Task.Run(() => machine.IsConnected(machineInfo));
                if (!isConnected)
                {
                    timer.Stop();
                    Disconnect();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageWindow.ShowDialog($"机床连接断开!");
                    });
                    return false;
                }

                // 刷新轴状态（耗时，异步执行）
                await Task.Run(() =>
                {
                    int num = 0;
                    foreach (var ax in Axes)
                    {
                        if (ax.Enabled == false) { num++; }
                        ax.ConfigStoredFlag = true;
                        ax.Refresh(); // 这里可以做滤波或差值平滑
                    }
                    if (num > 0)
                    {
                        globalMachineState.AxisEnabled = false;
                        GlobalCollectionService<ErrorType>.Instance.Insert((int)ErrorType.AxisOff, ErrorType.AxisOff);
                       
                    }
                    else
                    {
                        globalMachineState.AxisEnabled = true;
                        GlobalCollectionService<ErrorType>.Instance.Remove((int)ErrorType.AxisOff, ErrorType.AxisOff);

                    }
                });

                // UI 更新必须在主线程
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    OnMachineRefreshed?.Invoke(DateTime.Now);

                    if (Axes.Any(x => x.IsMoving == true))
                    {
                        await Task.Delay(200);

                        if (Axes.Any(x => x.IsMoving == true))
                        {
                            IsMoving = true;
                            DeviceStatus = DeviceStatus.Busy;
                        }
                    }
                    else
                    {
                        DeviceStatus = DeviceStatus.Idle;
                        IsMoving = false;
                    }
                    globalMachineState.IsMachineRunning = IsMoving ?? false; // null 时当作 false
                    for (int i = 3; i < ToolHeadAxes.Count; i++)
                    {
                        ToolHeadAxes[i].Position = Axes[i].DisplayPosition;
                    }

                    var pos = Get6AxesPosition();
                    var laser_data = LaserVectorFromAxes(pos);
                    var sensor = OffsetSettings.GetSelectedSensor();
                    var laser_target = new Tuple<Point3D, Vector3D, Vector3D, int>(
                        laser_data.Item1,
                        laser_data.Item2 / laser_data.Item2.Length * 215,
                        laser_data.Item3,
                        sensor.Name == "振镜" ? 0 : 1
                    );
                    eventAggregator.GetEvent<ModelPresenterSetToolTipPositionEvent>().Publish(laser_target);

                    var base_target = ObjectVectorFromAxes(pos);
                    eventAggregator.GetEvent<ModelPresenterSetMachineBaseEvent>().Publish(base_target);
                });

                
            }
            catch (Exception ex)
            {
                if (DeviceStatus == DeviceStatus.Disconnected)
                    return true;

                timer.Stop();
                Disconnect();
                //await Application.Current.Dispatcher.InvokeAsync(() =>
                //{
                //    MessageWindow.ShowDialog($"机床信息同步失败，连接断开\n{ex.Message}");
                //});
                return false;
            }

            return true;
        }
        //public string GetGcode()
        //{
        //    if (machine is not MachineHardwareFidia && machine is not MachineHardwareJD)
        //    {
        //        return null;
        //    }
        //    MachineWorkPointMessage m = new();
        //    eventAggregator.GetEvent<MachineGetWorkPointEvent>().Publish(m);
        //    if (m.WorkPoints.Any())
        //    {
        //        string g_code = null;
        //        var NeedFocus = m.NeedFocus;
        //        var targets = m.WorkPoints;
        //        List<(double[], bool)> work_points = new();
        //        foreach (var x in targets)
        //        {
        //            (var work_point, var nz, var nx) = Model2Workbench(x.Item1, x.Item2, x.Item3); // 先转换到机床坐标系
        //            work_points.Add((FromVector(work_point, nz, nx, 0, out _), x.Item4));   // 反解坐标
        //        }

        //        return g_code;
        //    }
        //    return null;
        //}
        public bool AddAxis()
        {
            try
            {
                if (Axes.Count >= 5)
                {
                    dialogService.ShowDialog("MessageBox", new DialogParameters($"message=当前限制不允许超过五 个轴"), r => { });
                    return false;
                }
                // 按默认配置添加一个轴
                Axes.Add(new AxisViewModel(dialogService, machine, new AxisDefination(), eventAggregator));
                return true;
            }
            catch (Exception ex)
            {
                dialogService.ShowDialog("MessageBox", new DialogParameters($"message=添加轴失败\n{ex.Message}"), r => { });
                return false;
            }
        }
        public bool RemoveAxis()
        {
            try
            {
                if (Axes.Count <= 3)
                {
                    dialogService.ShowDialog("MessageBox", new DialogParameters($"message=当前限制不允许少于3个轴"), r => { });
                    return false;
                }
                Axes.RemoveAt(Axes.Count - 1);
                return true;
            }
            catch (Exception ex)
            {
                dialogService.ShowDialog("MessageBox", new DialogParameters($"message=删除轴失败{ex.Message}"), r => { });
                return false;
            }
        }

        public void HomeAll()
        {
            if (DeviceStatus == DeviceStatus.Disconnected)
            {
                MessageBox.Show("机床未连接！");
                return;
            }
            Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(HOME_FilePath))
                    {
                        LoggingService.Instance.LogError("回零异常", new Exception("目标路径不存在回零文件"));
                        return;
                    }
                    LoggingService.Instance.LogInfo("回零开始！");
                    string  msg= await machine.HomeAll();
                    if (msg != null)
                    {
                       // MessageWindow.ShowDialog($"回零报错：{msg}");
                        LoggingService.Instance.LogError("回零报错", new Exception(msg));

                    }
                    LoggingService.Instance.LogInfo("所有轴已回零");

                    foreach (var axis in Axes)
                    {
                        axis.IsHomeed = true;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("回零失败", ex);
                }
            });
        }

        /// <summary>
        /// 机床点对点运动函数
        /// </summary>
        /// <param name="target">目标点</param>
        /// <param name="isAbsolute">是否为绝对坐标</param>
        /// <param name="isAsync">是否异步</param>
        /// <param name="autoZero">未操作轴是否回零</param>
        /// <returns>是否成功</returns>
        public bool ToPoint(double[] target, bool inProgress, bool isAbsolute = true, bool isAsync = true)
        {
            List<AxisDefination> axes = new();
            List<double> targets_for_check = new();
            List<double> targets = new();

            foreach (var axis in Axes)
            {
                if (axes_num.TryGetValue(axis.Name, out var node))
                {
                    if (node >= target.Length)
                        break;
                    //axis.Position = position[node];
                    axes.Add(axis.AxisDefination);

                    targets_for_check.Add(isAbsolute ? target[node] : (axis.Position + target[node]));
                    targets.Add((axis.AxisReverseFlag ? -1 : 1) * target[node]);
                }
            }
            if (!CheckLimit(targets_for_check.ToArray()))
            {
                MessageWindow.ShowDialog("超出软限位");
                return false;
            }
            //var time_use = Axes
            //    .Select(x => (isAbsolute ? x.RelTarget : (x.PtpTarget-x.DisplayPosition))/ x.Speed) // 计算每个轴的用时
            //    .Max(); // 找到用时最长的

            //string info = "";
            //for (int i = 0; i < Axes.LongCount(); i++)
            //{
            //    string str = (targets[i] - Axes[i].Offset).ToString("F4");
            //    info += $"{Axes[i].Name}:  {str}\n";
            //}

            //Base.HpcLog.LoggingService.Instance.LogInfo($"刀尖运动导致机床位置变化至 \n{info} ");


            var err_code = machine.ToPoint(axes.ToArray(), targets.ToArray(), inProgress, isAbsolute: isAbsolute, isAsync: isAsync);
            if (err_code != null)
            {
                //MessageWindow.ShowDialog($"机床移动失败\n{err_code}");
                if (err_code.Contains("err"))
                {
                    MessageWindow.Show($"G代码错误：{err_code}");
                    eventAggregator.GetEvent<EmergencyStopEvent>().Publish();
                }
                return false;
            }
            return true;
        }
        public string Focus()
        {
            return machine.Focus();
        }

        public async Task StopAll()  //之前无异步 由于要异步等待所以才改为异步
        {
            if (DeviceStatus != DeviceStatus.Disconnected)
            {
                machine.StopAll();
                LoggingService.Instance.LogWarning("急停");
            }
            StopProcessRequest rs = new StopProcessRequest();
            eventAggregator.GetEvent<Cmd_StopProcessEvent>().Publish(rs);
            await Task.Delay(100);

            var result = await rs.Completion.Task;
            if (result)
            {
                MessageBox.Show("停止执行完成！");
            }
        }
   


        public AxisViewModel GetAxisByName(string name)
        {
            return Axes.Where(x => x.Name == name).FirstOrDefault();
        }
        public double[] GetPosition()
        {
            List<double> result = new();
            for (int i = 0; i < 3; i++)
                //if (i < 3)
                result.Add(Axes[i].Position);
            //else
            //    result.Add(Axes[i].Position / 180 * Math.PI);

            return result.ToArray();
        }

        double[] IMachine.GetToolHeadPosition()
            => ToolHeadAxes.Select(x => x.Position).ToArray();

        public bool CheckLimit(double[] target, bool isAbsolute = true)
        {
            bool flag = true;
            for (int i = 0; i < target.Length && i < Axes.Count; i++)
            {
                if (Axes[i].LeftSoftLimit > target[i] || Axes[i].RightSoftLimit < target[i])
                    flag = false;
            }
            return flag;
        }
        public bool? CheckLimit(Point3D target, Vector3D normal, Vector3D direction_x)
        {
            try
            {
                (var p, var z, var x) = Model2Workbench(target, normal, direction_x);
                var point = FromVector(p, z, x, 0, out _);
                List<double> targets = new();
                foreach (var axis in Axes)
                {
                    if (axes_num.TryGetValue(axis.Name, out var node))
                    {
                        if (node >= point.Length)
                            break;
                        targets.Add(point[node]);
                    }
                }
                return CheckLimit(targets.ToArray());
            }
            catch (Exception ex)
            {
                MessageWindow.ShowDialog("Python公式计算错误\n请检查相应公式并重启软件\n");
                LoggingService.Instance.LogError("Python公式计算错误", ex);
                return null;
            }
        }

        #region 获取偏移
        public double[] GetAxisOffset()
        {
            //List<double> result = new();
            //for (int i = 0; i < 5; i++)
            //    if (i < 3)
            //        result.Add(Axes[i].AxisOffset);
            //    else
            //        result.Add(Axes[i].AxisOffset / 180 * Math.PI);
            //return result.ToArray();
            try
            {
                var doubles = new double[6];
                foreach (var axis in Axes)
                {
                    if (axes_num.TryGetValue(axis.Name, out var node))
                    {
                        doubles[node] = axis.AxisOffset;
                        //if (node >= 3) doubles[node] /= (180 / Math.PI);
                    }
                }
                return doubles;
            }
            catch { return null; }
        }
        #endregion

        #region 参数加载/保存
        public string SaveConfig(string json_path = null)
        {

            json_path ??= ConfigStore.StoreDir + "/" + SelectedMachine.Item1 + "MachineConfig.json";

            try
            {
                // 写Json
                JObject json = new JObject
                {
                    {nameof(Ip), Ip },
                    {nameof(Port), Port },
                    {nameof(IsRetract), IsRetract },
                    {nameof(RetractValue), RetractValue },
                };
                // 轴信息
                JArray axes_data = new JArray();
                foreach (var axis in Axes)
                {
                    axes_data.Add(new JObject()
                    {
                        {nameof(axis.Name), axis.Name },
                        {nameof(axis.NodeNum), axis.NodeNum },
                        {nameof(axis.Speed), axis.Speed },
                        {nameof(axis.AxisOffset), axis.AxisOffset },
                        {nameof(axis.SoftLimitMin), axis.SoftLimitMin },
                        {nameof(axis.SoftLimitMax), axis.SoftLimitMax },
                        {nameof(axis.LeftRetractThreshold), axis.LeftRetractThreshold },
                        {nameof(axis.RightRetractThreshold), axis.RightRetractThreshold },
                        {nameof(axis.AxisReverseFlag), axis.AxisReverseFlag },
                    });
                }
                json.Add(nameof(Axes), axes_data);

                // IO信息
                JArray io_data = new();
                if (IOs != null && IOs.Count > 0)
                    foreach (var io in IOs)
                    {
                        io_data.Add(new JObject()
                    {
                        { nameof(io.Name), io.Name },
                        { nameof(io.Port), io.Port },
                        { nameof(io.Bit), io.Bit },
                    });
                    }
                json.Add(nameof(IOs), io_data);

                // 传感器偏移
                JArray sensor_offset = new();
                foreach (var sensor in OffsetSettings.SensorOffset)
                {
                    sensor_offset.Add(new JObject()
                    {
                        {nameof(sensor.Name), sensor.Name },
                        {nameof(sensor.OffsetX), sensor.OffsetX },
                        {nameof(sensor.OffsetY), sensor.OffsetY },
                        {nameof(sensor.OffsetZ), sensor.OffsetZ },
                    });
                }
                json.Add(nameof(OffsetSettings.SensorOffset), sensor_offset);

                // 机床参数
                JObject xtx = new();
                foreach (var x in OffsetSettings.XTX)
                {
                    xtx.Add(x.Name, x.Value);
                }
                json.Add(nameof(OffsetSettings.XTX), xtx);
                // 保存
                ConfigStore.CheckStoreFloder();
                File.WriteAllText(json_path, json.ToString());

                return null;
            }
            catch (Exception ex) { return $"配置写入失败\n{ex.Message}"; }
        }

        public string LoadConfig(string json_path = null)
        {
            json_path ??= ConfigStore.StoreDir + "AcsConfig.json";
            try
            {
                string str = File.ReadAllText(json_path);
                JObject json = JObject.Parse(str);

                machineInfo ??= new();
                retractInfo ??= new();
                Ip = (string)json[nameof(Ip)];
                Port = (int)json[nameof(Port)];
                if (json.ContainsKey(nameof(IsRetract)))
                    IsRetract = (bool)json[nameof(IsRetract)];
                if (json.ContainsKey(nameof(RetractValue)))
                    RetractValue = (double)json[nameof(RetractValue)];
                Axes ??= new();
                Axes.Clear();

                ToolHeadAxes ??= new();
                ToolHeadAxes.Clear();
                foreach (JObject axis in json[nameof(Axes)])
                {
                    Axes.Add(new AxisViewModel(dialogService, machine, new()
                    {
                        Name = (string)axis["Name"],
                        NodeNum = (int)axis["NodeNum"],
                        Speed = (double)axis["Speed"],
                        MinSoftLimit = (double?)axis["SoftLimitMin"],
                        MaxSoftLimit = (double?)axis["SoftLimitMax"],
                        LeftRetractThreshold = (double?)axis["LeftRetractThreshold"],
                        RightRetractThreshold = (double?)axis["RightRetractThreshold"],
                    }, eventAggregator)
                    {
                        AxisOffset = (double)axis["AxisOffset"],
                    });
                    if (axis.ContainsKey("AxisReverseFlag"))
                        Axes.Last().AxisReverseFlag = (bool)axis["AxisReverseFlag"];

                    ToolHeadAxes.Add(new()
                    {
                        Name = (string)axis["Name"],
                    });

                }
                IOs ??= new();

                if (json[nameof(IOs)] != null && json[nameof(IOs)].Count() > 0)
                    foreach (var io in json[nameof(IOs)])
                    {
                        IOs.Add(new IOViewModel()
                        {
                            Name = (string)io["Name"],
                            Port = (int)io["Port"],
                            Bit = (int)io["Bit"],
                        });
                    }
                OffsetSettings = containerProvider.Resolve<OffsetSettingsViewModel>();
                OffsetSettings.SensorOffset ??= new();
                OffsetSettings.SensorOffset.Clear();
                if (json[nameof(OffsetSettings.SensorOffset)] != null && json[nameof(OffsetSettings.SensorOffset)].Any())
                {
                    foreach (var sensor in json[nameof(OffsetSettings.SensorOffset)])
                    {
                        OffsetSettings.SensorOffset.Add(new()
                        {
                            Name = (string)sensor["Name"],
                            OffsetX = (double)sensor["OffsetX"],
                            OffsetY = (double)sensor["OffsetY"],
                            OffsetZ = (double)sensor["OffsetZ"],
                            IsChecked = false,
                        });
                    }
                    OffsetSettings.SensorOffset.First().IsChecked = true;
                }
                OffsetSettings.XTX ??= new();
                OffsetSettings.XTX.Clear();
                if (json[nameof(OffsetSettings.XTX)] != null && json[nameof(OffsetSettings.XTX)].Any())
                {
                    foreach (var x in json[nameof(OffsetSettings.XTX)])
                    {
                        if (x is JProperty xProperty)
                            OffsetSettings.XTX.Add(new(xProperty.Name, (double)xProperty.Value));
                    }
                }
                else
                {
                    OffsetSettings.XTX.Add(new("XTB", 0));
                    OffsetSettings.XTX.Add(new("XTC", 0));
                    OffsetSettings.XTX.Add(new("YTA", 0));
                    OffsetSettings.XTX.Add(new("YTC", 0));

                    OffsetSettings.XTX.Add(new("ZTA", 0));
                    OffsetSettings.XTX.Add(new("ZTB", 0));
                    OffsetSettings.XTX.Add(new("ATB", 0));
                    OffsetSettings.XTX.Add(new("ATC", 0));

                    OffsetSettings.XTX.Add(new("BTC", 0));
                    OffsetSettings.XTX.Add(new("ATW", 0));
                    OffsetSettings.XTX.Add(new("BTW", 0));
                    OffsetSettings.XTX.Add(new("CTW", 0));
                }
                return null;
            }
            catch (Exception ex) { return $"配置读取失败\n{ex.Message}"; }

        }
        #endregion

        string IMachine.PrepareForWork(List<(Point3D, Vector3D, Vector3D, bool)> work_points, bool need_focus)
        {
            OffsetSettings.SensorOffset.First().IsChecked = true;   // 切换到振镜
            List<(double[], bool)> targets = new();
            foreach (var x in work_points)
            {
                (var target, var nz, var nx) = Model2Workbench(x.Item1, x.Item2, x.Item3); // 先转换到机床坐标系
                targets.Add((FromVector(target, nz, nx, 0, out _), x.Item4));   // 反解坐标
            }
            string err = machine.PrepareForWork(targets, IsRetract ? RetractValue : null,
                need_focus, Feed, FocusFeed);
            IsBusy = true;
            return err;
        }

        //public string MutilAxisMotion(string GCodeCommand)
        //{
        //    string err = machine.MutilAxisMotion(GCodeCommand);
        //    return err;
        //}

        string IMachine.StopWork()
        {
            string err = machine.StopWork();
            IsBusy = false;
            return err;
        }

        string IMachine.Pause()
        {
            string err = null;

            return err;
        }
        string IMachine.ReStart()
        {
            string err = null;

            return err;
        }

        #region python公式解释器
        // python公式解释器
        private ScriptEngine formula_engine = null;
        private dynamic formula = null;
        public ParameterViewModel<string> FormulaFile { get; set; }
        public bool LoadFormula(string new_path = null)
        {
            new_path = "XYZ_20250304.fml";
            var py_file = $"./temp/Formula.py";
            try
            {
                if (File.Exists(py_file))
                    File.Delete(py_file);

                var zip_file = ConfigStore.StoreDir + "/" + new_path;

                if (new_path == null)
                    zip_file += FormulaFile.Value;

                FileOperations.ExtractFile(zip_file, "./temp", "F0rMu1@");
                formula_engine = Python.CreateEngine();// 创建python解释器
                formula = formula_engine.ExecuteFile(py_file);// 加载脚本文件
            }
            catch (Microsoft.Scripting.SyntaxErrorException ex)
            {
                MessageWindow.ShowDialog($"公式文件不正确\n{FormulaFile.Value}");
                formula_engine = null;
                formula = null;
                return false;
            }
            catch (Exception ex)
            {
                MessageWindow.ShowDialog($"公式文件加载失败\n请检查python文件正确性并重启软件\n{ex.Message}");
                formula_engine = null;
                formula = null;
                return false;
            }
            finally
            {
                if (File.Exists(py_file))
                    File.Delete(py_file);   // 读取完成 删除公式
            }
            return true;
        }

        public (double?[], double?[]) GetRetractThreshold()
        {
            double?[] leftRetract = new double?[6];
            double?[] rightRetract = new double?[6];
            foreach (var axis in Axes)
            {
                if (axes_num.TryGetValue(axis.Name, out var axisNum))
                {
                    leftRetract[axisNum] = axis.LeftRetractThreshold;
                    rightRetract[axisNum] = axis.RightRetractThreshold;
                }
            }
            return (leftRetract, rightRetract);
        }
        /// <summary>
        /// 获取六轴的坐标数组(弧度)
        /// </summary>
        public double[] Get6AxesPosition()
        {
            try
            {
                double[] position = new double[6];
                foreach (var axis in Axes)
                {
                    if (axes_num.TryGetValue(axis.Name, out var node))
                    {
                        position[node] = axis.Position;
                        //if (node >= 3)
                        //    position[node] /= (180 / Math.PI);
                    }
                }
                return position;
            }
            catch { return null; }
        }
        /// <summary>
        /// 获取六轴的坐标数组(弧度)
        /// </summary>
        public double[] Get6ToolHeadAxesPosition()
        {
            try
            {
                double[] position = new double[6];
                foreach (var axis in ToolHeadAxes)
                {
                    if (axes_num.TryGetValue(axis.Name, out var node))
                    {
                        position[node] = axis.Position;
                    }
                }
                return position;
            }
            catch { return null; }
        }
        /// <summary>
        /// 逆运动学
        /// </summary>
        /// <param name="target"></param>
        /// <param name="z_vector"></param>
        /// <param name="x_vector"></param>
        /// <param name="tool">工具头（0:振镜,1:相机）</param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public double[] FromVector(Point3D target, Vector3D z_vector, Vector3D x_vector, int tool, out double theta)
        {
            double[] doubles = new double[9] { target.X, target.Y, target.Z, z_vector.X, z_vector.Y, z_vector.Z, x_vector.X, x_vector.Y, x_vector.Z };
            var data = formula.InverseKinematics(doubles, GetAxisOffset(), OffsetSettings.GetXTX(), OffsetSettings.SensorOffset[tool].GetArray);
            //double[] point = new double[] { (double)data[0], (double)data[1], (double)data[2], (double)data[3], (double)data[4], (double)data[5] };
            double[] point = new double[6];
            for (int i = 0; i < 6; i++)
            {
                point[i] = Math.Round((double)data[i], 6);
            }
            //point[0] += 0.6 * Math.Tan(Math.PI / 180 * (point[4] - Axes[3].AxisOffset));
            //point[1] += 0.15 * Math.Tan(Math.PI / 180 * (point[4] - Axes[3].AxisOffset));

            theta = formula.Angularoffset(doubles, point, GetAxisOffset());
            return point;
        }
        /// <summary>
        /// 正运动学
        /// </summary>
        /// <param name="position">轴机械位置</param>
        /// <param name="tool_offset"></param>
        /// <param name="axis_offset"></param>
        /// <returns>刀尖位置，刀尖法向量（后向）刀尖坐标系X轴方向</returns>
        public Tuple<Point3D, Vector3D, Vector3D> GetToolHead(double[] position, double[] tool_offset = null, double[] axis_offset = null)
        {
            tool_offset ??= OffsetSettings.GetSelectedSensor().GetArray;    // 默认使用选中的工具头
            axis_offset ??= GetAxisOffset();
            var data = formula.ForwardKinematics(position, axis_offset, OffsetSettings.GetXTX(), tool_offset);
            //double[] t = new double[] { (double)data[0], (double)data[1], (double)data[2], (double)data[3], (double)data[4], (double)data[5], (double)data[6], (double)data[7], (double)data[8] };
            double[] t = new double[9];
            for (int i = 0; i < 9; i++)
            {
                t[i] = Math.Round((double)data[i], 6);
            }
            return new(new(t[0], t[1], t[2]), new(t[3], t[4], t[5]), new(t[6], t[7], t[8]));
        }
        // 刀尖点计算
        public Tuple<Point3D, Vector3D, Vector3D> LaserVectorFromAxes(double[] axes)
        {
            var data = formula.Display_Laserhead(axes, GetAxisOffset(), OffsetSettings.GetXTX(), OffsetSettings.GetSelectedSensor().GetArray);
            return new(new Point3D(data[0], data[1], data[2]), new Vector3D(data[3], data[4], data[5]), new(data[6], data[7], data[8]));
        }
        // 机床平台位置计算
        public Tuple<Point3D, Vector3D, Vector3D> ObjectVectorFromAxes(double[] axes)
        {
            var data = formula.Display_Workbench(axes, GetAxisOffset(), OffsetSettings.GetXTX(), OffsetSettings.GetSelectedSensor().GetArray);
            return new(new Point3D(data[0], data[1], data[2]), new Vector3D(data[3], data[4], data[5]), new(data[6], data[7], data[8]));
        }
        // 模型坐标转到机床为表
        public Tuple<Point3D, Vector3D, Vector3D> Model2Workbench(Point3D target, Vector3D t_z, Vector3D t_x)
        {
            double x = target.X;
            double y = target.Y;
            double z = target.Z;
            t_x /= t_x.Length;  // 归一化
            t_z /= t_z.Length;

            var object_offset = OffsetSettings.WorkpieceOffset.GetOffset();
            double xt = object_offset[0];
            double yt = object_offset[1];
            double zt = object_offset[2];
            double at = object_offset[3] / 180 * Math.PI;
            double bt = object_offset[4] / 180 * Math.PI;
            double rt = object_offset[5] / 180 * Math.PI;

            Matrix3D trans = new Matrix3D(
                Math.Cos(bt) * Math.Cos(rt), Math.Cos(at) * Math.Sin(rt) + Math.Sin(at) * Math.Sin(bt) * Math.Cos(rt), Math.Sin(at) * Math.Sin(rt) - Math.Cos(at) * Math.Sin(bt) * Math.Cos(rt), 0,
                -Math.Cos(bt) * Math.Sin(rt), Math.Cos(at) * Math.Cos(rt) - Math.Sin(at) * Math.Sin(bt) * Math.Sin(rt), Math.Sin(at) * Math.Cos(rt) + Math.Cos(at) * Math.Sin(bt) * Math.Sin(rt), 0,
                Math.Sin(bt), -Math.Sin(at) * Math.Cos(bt), Math.Cos(at) * Math.Cos(bt), 0,
                xt, yt, zt, 1
                );
            var a = trans.Transform(target);// new Vector3D(target.X, target.Y, target.Z);
            return new(a, t_z * trans, t_x * trans);
        }
        // 机床坐标转到工件坐标
        public Tuple<Point3D, Vector3D, Vector3D> Workbench2Model(Point3D target, Vector3D t_z, Vector3D t_x)
        {
            double x = target.X;
            double y = target.Y;
            double z = target.Z;
            t_x /= t_x.Length;  // 归一化
            t_z /= t_z.Length;

            var object_offset = OffsetSettings.WorkpieceOffset.GetOffset();
            double xt = object_offset[0];
            double yt = object_offset[1];
            double zt = object_offset[2];
            double at = object_offset[3];
            double bt = object_offset[4];
            double rt = object_offset[5];

            Matrix3D trans = new Matrix3D(
                Math.Cos(bt) * Math.Cos(rt), Math.Cos(at) * Math.Sin(rt) + Math.Sin(at) * Math.Sin(bt) * Math.Cos(rt), Math.Sin(at) * Math.Sin(rt) - Math.Cos(at) * Math.Sin(bt) * Math.Cos(rt), 0,
                -Math.Cos(bt) * Math.Sin(rt), Math.Cos(at) * Math.Cos(rt) - Math.Sin(at) * Math.Sin(bt) * Math.Sin(rt), Math.Sin(at) * Math.Cos(rt) + Math.Cos(at) * Math.Sin(bt) * Math.Sin(rt), 0,
                Math.Sin(bt), -Math.Sin(at) * Math.Cos(bt), Math.Cos(at) * Math.Cos(bt), 0,
                xt * (-Math.Cos(bt) * Math.Cos(rt)) + yt * (-Math.Cos(at) * Math.Sin(rt) - Math.Sin(at) * Math.Sin(bt) * Math.Cos(rt)) + zt * (Math.Cos(at) * Math.Sin(bt) * Math.Cos(rt)),
                xt * (Math.Cos(bt) * Math.Sin(rt)) + yt * (-Math.Cos(at) * Math.Cos(rt) + Math.Sin(at) * Math.Sin(bt) * Math.Sin(rt)) + zt * (Math.Sin(at) * Math.Cos(rt) - Math.Cos(at) * Math.Sin(bt) * Math.Sin(rt)),
                xt * (-Math.Sin(bt)) + yt * (Math.Sin(at) * Math.Cos(bt)) + zt * (-Math.Cos(at) * Math.Cos(bt)), 1
                );
            var a = new Vector3D(target.X, target.Y, target.Z) * trans;
            return new(new Point3D(a.X, a.Y, a.Z), t_z * trans, t_x * trans);
        }
        public bool ToVector(Point3D target, Vector3D normal, Vector3D direction_x, int tool, out double theta, bool inProgress, bool isAsync = false)
        {
            try
            {
                if (MachineInterpolationIsEnabled.Value) // 启用插补
                {
                    // 获取当前刀尖点
                    (var now_point, var now_z, var now_x) = GetToolHead(Get6AxesPosition());

                    var machine_point_list = GetAdaptiveInterpolationPoints(now_point, now_z, now_x, target, normal, direction_x, MachineInterpolationIsStep.Value);

                    //double[] point = FromVector(target, normal, direction_x, tool, out theta);
                    //var machine_point_list = point_list.Select(x => FromVector(x.Item1, x.Item2, x.Item3, tool, out _));
                    _ = FromVector(target, normal, direction_x, tool, out theta);

                    List<Models.AxisDefination> axes = new();
                    List<int> ReverseFlag = new();
                    List<double[]> targets_for_check = new();

                    foreach (var axis in Axes)
                    {
                        if (axes_num.TryGetValue(axis.Name, out var node))
                        {
                            axes.Add(axis.AxisDefination);
                            ReverseFlag.Add((axis.AxisReverseFlag ? -1 : 1));
                        }
                    }
                    // 反向操作
                    // targets_for_check=  targets_for_check.Select(x => x.Zip(ReverseFlag, (x, flag) => x*flag).ToArray()).ToList();
                    foreach (var point in machine_point_list)
                    {
                        List<double> current_machine_point = new();
                        foreach (var axis in axes)
                        {
                            if (axes_num.TryGetValue(axis.Name, out var node))
                            {
                                current_machine_point.Add(point[node]);
                            }
                        }
                        if (!CheckLimit(current_machine_point.ToArray()))
                        {
                            MessageWindow.ShowDialog("超出软限位");
                            return false;
                        }
                        current_machine_point = current_machine_point.Zip(ReverseFlag, (x, flag) => x * flag).ToList();
                        targets_for_check.Add(current_machine_point.ToArray());
                    }
                    var err = machine.MoveContinuous(axes.ToArray(), targets_for_check, inProgress, isAsync: isAsync);
                    if (err != null)
                    {
                        MessageWindow.ShowDialog($"插补运动失败\n{err}");
                        return false;
                    }
                }
                else    // 禁用插补
                {
                    double[] point = FromVector(target, normal, direction_x, tool, out theta);
                    //for (int i = 3; i < point.Length; i++) { point[i] /= (Math.PI / 180); }
                    return ToPoint(point, inProgress, isAsync: isAsync);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageWindow.ShowDialog($"公式计算错误\n请检查相应公式并重新加载\n{ex.Message}");
                theta = 0;
                return false;
            }
        }
//        public static AxisMove PixelToAxisMove(
//            double pixelX,       // 鼠标点击的像素 X
//            double pixelY,       // 鼠标点击的像素 Y
//            int imageWidth,      // 图像宽
//            int imageHeight,     // 图像高
//            double mmPerPixel    // 每像素多少 mm
//)
//        {
//            // 1️⃣ 图像中心（像素坐标）
//            double centerX = imageWidth / 2.0;
//            double centerY = imageHeight / 2.0;

//            // 2️⃣ 像素偏移量（以中心为原点）
//            double dxPixel = pixelX - centerX;
//            double dyPixel = pixelY - centerY;

//            // 3️⃣ 像素 → 物理距离（mm）
//            double dxMm = dxPixel * mmPerPixel;
//            double dyMm = dyPixel * mmPerPixel;

//            // 4️⃣ 坐标映射（按你给的关系）
//            AxisMove move = new AxisMove
//            {
//                // X像素 → 机械Y轴，方向相反
//                Y = -dxMm,

//                // Y像素 → 机械Z轴，方向相反
//                Z = -dyMm
//            };
//            return ToPoint(point, inProgress, isAsync: isAsync);
//            return move;
//        }

        // 获取自适应插值点
        //public List<(Point3D, Vector3D, Vector3D)> GetAdaptiveInterpolationPoints(
        //    Point3D O1_Origin, Vector3D O1_ZAxis, Vector3D O1_XAxis,
        //    Point3D O2_Origin, Vector3D O2_ZAxis, Vector3D O2_XAxis,
        //    double stepSize)
        //{
        //    List<(Point3D, Vector3D, Vector3D)> points = new()
        //    {
        //        (O1_Origin, O1_ZAxis, O1_XAxis) // 起点
        //    };
        //    double[] start_point = FromVector(O1_Origin, O1_ZAxis, O1_XAxis, OffsetSettings.GetSelectedSensorIndex(), out _);
        //    double[] end_point = FromVector(O2_Origin, O2_ZAxis, O2_XAxis, OffsetSettings.GetSelectedSensorIndex(), out _);
        //    int numSteps = (int)(start_point.Zip(end_point, (x, y) => Math.Abs(x-y)).Max() / stepSize);// 计算根据步长计算的插值点数量
        //    if (numSteps == 0) numSteps = 1;  // 确保至少有一个插值点
        //    double DeltaA = end_point[3] - start_point[3], DeltaB = end_point[4] - start_point[4], DeltaC = end_point[5] - start_point[5];
        //    // 计算O1到O2的插值点
        //    for (double t = 0.0 + 1d / numSteps; t <= 1.0; t += 1d / numSteps)  // 最大最多 maxPoints 个插值点
        //    {
        //        Point3D currentOrigin = GeometricHelper.Lerp(O1_Origin, O2_Origin, t); // 刀尖点坐标
        //        Vector3D currentZAxis = GeometricHelper.Slerp(O1_ZAxis, O2_ZAxis, t); // Z轴方向向量插值
        //        Vector3D currentXAxis = GeometricHelper.Slerp(O1_XAxis, O2_XAxis, t); // X轴方向向量插值
        //        //double nowA = start_point[3] + t*DeltaA, nowB = start_point[4] + t*DeltaB, nowC = start_point[5] + t*DeltaC;
        //        //(_, Vector3D currentZAxis, Vector3D currentXAxis) = GetToolHead(new double[] { 0, 0, 0, nowA, nowB, nowC });
        //        points.Add((currentOrigin, currentZAxis, currentXAxis));
        //    }
        //    points.Add((O2_Origin, O2_ZAxis, O2_XAxis));  // 终点
        //    return points;
        //}
        public List<double[]> GetAdaptiveInterpolationPoints(
            Point3D O1_Origin, Vector3D O1_ZAxis, Vector3D O1_XAxis,
            Point3D O2_Origin, Vector3D O2_ZAxis, Vector3D O2_XAxis,
            double stepSize, int? tool = null)
        {
            tool ??= OffsetSettings.GetSelectedSensorIndex();
            //List<(Point3D, Vector3D, Vector3D)> points = new()
            //{
            //    (O1_Origin, O1_ZAxis, O1_XAxis) // 起点
            //};
            double[] start_point = FromVector(O1_Origin, O1_ZAxis, O1_XAxis, tool.Value, out _);
            double[] end_point = FromVector(O2_Origin, O2_ZAxis, O2_XAxis, tool.Value, out _);
            List<double[]> points_list = new() { start_point };

            int numSteps = (int)(start_point.Zip(end_point, (x, y) => Math.Abs(x - y)).Max() / stepSize);// 计算根据步长计算的插值点数量
            if (numSteps == 0) numSteps = 1;  // 确保至少有一个插值点
            double DeltaA = end_point[3] - start_point[3], DeltaB = end_point[4] - start_point[4], DeltaC = end_point[5] - start_point[5];


            // 计算O1到O2的插值点
            for (double t = 0.0 + 1d / numSteps; t <= 1.0; t += 1d / numSteps)  // 最大最多 maxPoints 个插值点
            {
                Point3D currentOrigin = GeometricHelper.Lerp(O1_Origin, O2_Origin, t); // 刀尖点坐标

                Vector3D currentZAxis = GeometricHelper.Slerp(O1_ZAxis, O2_ZAxis, t); // Z轴方向向量插值
                Vector3D currentXAxis = GeometricHelper.Slerp(O1_XAxis, O2_XAxis, t); // X轴方向向量插值

                double[] current_point = FromVector(currentOrigin, currentZAxis, currentXAxis, tool.Value, out _);

                /// TODO: 插值点距离大于步长则插入插值点,优化：可以自适应步长？
                if (Math.Sqrt(current_point.Zip(points_list.Last(), (a, b) => Math.Pow(a - b, 2)).Sum()) > stepSize)
                {
                    double insert_t = t - 0.5d / numSteps;  // 回退一半步长
                    Point3D insertOrigin = GeometricHelper.Lerp(O1_Origin, O2_Origin, insert_t); // 刀尖点坐标
                    Vector3D insertZAxis = GeometricHelper.Slerp(O1_ZAxis, O2_ZAxis, insert_t); // Z轴方向向量插值
                    Vector3D insertXAxis = GeometricHelper.Slerp(O1_XAxis, O2_XAxis, insert_t); // X轴方向向量插值

                    double[] insert_point = FromVector(insertOrigin, insertZAxis, insertXAxis, tool.Value, out _);
                    points_list.Add(insert_point);
                }
                points_list.Add(current_point);
            }
            points_list.Add(end_point);  // 终点

            return points_list;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ToModelVector(Point3D target, Vector3D t_z, Vector3D t_x, out double theta, bool inProgress, double retract_distance = 0)
        {
            try
            {
                var tool_num = OffsetSettings.SensorOffset.IndexOf(OffsetSettings.GetSelectedSensor());
                (var final_point, var final_t_z, var final_t_x) = Model2Workbench(target, t_z, t_x); // 先转换到机床坐标系
                double[] new_point = FromVector(final_point, t_z, t_x, tool_num, out theta);   // 反解坐标
                if (true)
                {
                    if (retract_distance == 0)
                        retract_distance = RetractValue;
                    if (IsRetract && retract_distance >= 0)
                    {
                        // 获取当前坐标和刀尖坐标系
                        bool isNeedRetract = false;
                        (var now_point, var z_vector, var x_vector) = GetToolHead(Get6AxesPosition());
                        double[] offset = GetAxisOffset();
                        double[] nowPoint = Get6AxesPosition();  //当前点的坐标组
                        double[] displayPoint = new double[6];
                        for (int i = 0; i < displayPoint.Length; i++)
                        {
                            displayPoint[i] = nowPoint[i] - offset[i];
                        }
                        // 退刀
                        (var leftRetract, var rightRetract) = GetRetractThreshold();

                        for (int i = 0; i < displayPoint.Length; i++) //判断需要退刀的条件
                        {
                            if (leftRetract[i].HasValue && displayPoint[i] <= leftRetract[i].Value) isNeedRetract = true;
                            if (rightRetract[i].HasValue && displayPoint[i] >= rightRetract[i].Value) isNeedRetract = true;
                            if (isNeedRetract) break;
                        }

                        if (isNeedRetract)
                        {
                            var retract_1 = now_point + retract_distance * z_vector;
                            (var retract_1_point, var retract_1_z, var retract_1_x) = Model2Workbench(retract_1, z_vector, x_vector); // 先转换到机床坐标系
                            ToVector(retract_1, retract_1_z, retract_1_x, tool_num, out _, inProgress);

                            // 到达进刀点
                            var retract_2 = target + retract_distance * t_z;
                            (var retract_2_point, var retract_2_z, var retract_2_x) = Model2Workbench(retract_2, t_z, t_x); // 先转换到机床坐标系
                            ToVector(retract_2, retract_2_z, retract_2_x, tool_num, out _, inProgress);
                        }
                    }
                }
                // 进刀
                return ToVector(final_point, final_t_z, final_t_x, tool_num, out _, inProgress);    // 运动

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher?.Invoke(() =>
                {
                    dialogService.ShowDialog("MessageBox",
                        new DialogParameters($"message=Python公式计算错误\n请检查相应公式并重启软件\n{ex.Message}"),
                        r => { });
                });
                theta = 0;
                return false;
            }
        }

        string IMachine.MutilAxisMotion(string GCodeCommand)
        {
            throw new NotImplementedException();
        }
        private int CalculateDelay(double position, double speed)
        {
            if (speed <= 0) return 1000; // 默认延迟
            return (int)(Math.Abs(position) / speed * 1000) + 1000;
        }

        void IMachine.StopAll()
        {
            throw new NotImplementedException();
        }

        #endregion

       
    }

    public struct AxisMove
    {
        public double Y; // 机械轴 Y
        public double Z; // 机械轴 Z
    }
}
