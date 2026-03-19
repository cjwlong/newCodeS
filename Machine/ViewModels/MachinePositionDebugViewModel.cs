using ACS.SPiiPlusNET;
using Ionic.BZip2;
using Machine.Interfaces;
using MaterialDesignThemes.Wpf.Behaviors;
using Microsoft.Data.OData.Query.SemanticAst;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.tools;
using SharedResource.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Community.CsharpSqlite.Sqlite3;
using static IronPython.Modules._ast;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using static System.Net.Mime.MediaTypeNames;
using MessageBox = System.Windows.MessageBox;

namespace Machine.ViewModels
{
  public  class MachinePositionDebugViewModel : BindableBase
    {
      //  private const string DataFile = "data.json";

        private string DataFile = Path.Combine(ConfigStore.StoreDir, "LaserData.json");

        // 全数据集合（持久化）
        public ObservableCollection<MasterItem> Masters { get; set; } = new();
        public ObservableCollection<DetailItem> Details { get; set; } = new();

        public ObservableCollection<DisItem> DisItems { get; set; } = new(); 
        // 供 UI 输入（上方）
        private string _focusStart;
        public string FocusStart { get => _focusStart; set { _focusStart = value; RaisePropertyChanged(); } }

        private string _focusEnd;
        public string FocusEnd { get => _focusEnd; set { _focusEnd = value; RaisePropertyChanged(); } }

        private string _focusInterval;
        public string FocusInterval { get => _focusInterval; set { _focusInterval = value; RaisePropertyChanged(); } }

        private string _focusLen;
        public string FocusLen { get => _focusLen; set { _focusLen = value; RaisePropertyChanged(); } }
        
        private string _focusCount;
        public string FocusCount { get => _focusCount; set { _focusCount = value; RaisePropertyChanged(); } }


        private string _focusAxis;
        public string FocusAxis { get => _focusAxis; set { _focusAxis = value; RaisePropertyChanged(); } }


        private string _markStart;
        public string MarkStart { get => _markStart; set { _markStart = value; RaisePropertyChanged(); } }

        private string _markEnd;
        public string MarkEnd { get => _markEnd; set { _markEnd = value; RaisePropertyChanged(); } }

        private string _markInterval;
        public string MarkInterval { get => _markInterval; set { _markInterval = value; RaisePropertyChanged(); } }

        private string _markAxis;
        public string MarkAxis { get => _markAxis; set { _markAxis = value; RaisePropertyChanged(); } }

        private string _lineAxis;
        public string LineAxis { get => _lineAxis; set { _lineAxis = value; RaisePropertyChanged(); } }


        private string _LaserYDis;
        public string  LaserYDis { get => _LaserYDis; set { _LaserYDis = value; RaisePropertyChanged(); } }

        private string _laserZDis;
        public string LaserZDis { get => _laserZDis; set { _laserZDis = value; RaisePropertyChanged(); } }

        private string _laserXDis;
        public string LaserXDis { get => _laserXDis; set { _laserXDis = value; RaisePropertyChanged(); } }



        private string _yAxisPosition;
        public string YAxisPosition { get => _yAxisPosition; set { _yAxisPosition = value; RaisePropertyChanged(); } }

        private string _xAxisPosition;
        public string XAxisPosition { get => _xAxisPosition; set { _xAxisPosition = value; RaisePropertyChanged(); } }

        private string _zAxisPosition;
        public string ZAxisPosition { get => _zAxisPosition; set { _zAxisPosition = value; RaisePropertyChanged(); } }


        private string _xRlativeDis;
        public string XRlativeDis { get => _xRlativeDis; set { _xRlativeDis = value; RaisePropertyChanged(); } }

        private string _yRlativeDis;
        public string YRlativeDis { get => _yRlativeDis; set { _yRlativeDis = value; RaisePropertyChanged(); } }

        private string _zRlativeDis;
        public string ZRlativeDis { get => _zRlativeDis; set { _zRlativeDis = value; RaisePropertyChanged(); } }

        /// <summary>
        /// 划线z轴开始位置
        /// </summary>
        private string _markz;
        public string MarkZ { get => _markz; set { _markz = value; RaisePropertyChanged(); } }
        
        public ObservableCollection<string> AxisList { get; } = new() { "X", "Y", "Z", "A", "C" };

        // 选中项（主表/子表）
        private MasterItem _selectedMaster;
        public MasterItem SelectedMaster
        {
            get => _selectedMaster;
            set
            {
                _selectedMaster = value;
                RaisePropertyChanged();
                RefreshFilteredDetails(); // 切换主表时刷新子表视图
            }
        }

        private DetailItem _selectedDetail;
        public DetailItem SelectedDetail
        {
            get => _selectedDetail;
            set { _selectedDetail = value; RaisePropertyChanged(); }
        }

        // 子表：只显示与 SelectedMaster 相关的项
        private ObservableCollection<DetailItem> _filteredDetails = new();
        public ObservableCollection<DetailItem> FilteredDetails
        {
            get => _filteredDetails;
            set { _filteredDetails = value; RaisePropertyChanged(); }
        }

        // Commands
        public ICommand AddMasterCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand DeleteSelectedMasterCommand { get; }
        public ICommand SaveMainCommand { get; }
        public ICommand RunCommand { get; }

        public ICommand DetailCommand { get; }

        public RelayCommand<MasterItem> MasterMoveCommand { get; }
        public RelayCommand<MasterItem> MasterDeleteCommand { get; }

        public RelayCommand<DetailItem> DetailMoveCommand { get; }

        public RelayCommand<DetailItem> SelectedScanCommand { get; }
        
        public RelayCommand<DetailItem> DetailDeleteCommand { get; }
        public ICommand SaveSubCommand { get; }
        public ICommand DeleteSelectedDetailCommand { get; }

        
        public RelayCommand<object> DummyCommand { get; } // placeholder if needed

        private readonly IContainerProvider containerProvider;

        private readonly IEventAggregator eventAggregator;

        public ICommand GetPosCommand { get; }
        public MachineViewModel MachineVM { get; set; }
        public MachinePositionDebugViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            // 初始化命令
            AddMasterCommand = new RelayCommand(AddMaster);
            RestoreCommand = new RelayCommand(RestoreCmd);
            DeleteSelectedMasterCommand = new RelayCommand(DeleteSelectedMaster);
            SaveMainCommand = new RelayCommand(SaveAllData);
            RunCommand = new RelayCommand(RunForSelectedMaster);

            DetailCommand = new RelayCommand(CreateDetail);

            MasterMoveCommand = new RelayCommand<MasterItem>(MasterMove);
            MasterDeleteCommand = new RelayCommand<MasterItem>(MasterDelete);

           // DetailMoveCommand = new RelayCommand<DetailItem>(DetailMoveAsync);
            DetailMoveCommand = new RelayCommand<DetailItem>(
                async item => await DetailMoveBtnAsync(item)
            );
          
            SelectedScanCommand = new RelayCommand<DetailItem>(
              async item => await DetailScanBtnAsync()
          );
            DetailDeleteCommand = new RelayCommand<DetailItem>(DetailDelete);

            SaveSubCommand = new RelayCommand(SaveAllData);
            DeleteSelectedDetailCommand = new RelayCommand(DeleteSelectedDetail);
            GetPosCommand = new RelayCommand(GetPosition);
            // Load persisted data on startup
            LoadAllData();
            RefreshFilteredDetails();
            //MarkEnd = "123";
        }

        // ---------- 操作实现 ----------

        private void GetPosition()
        {
            var ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals("X", StringComparison.OrdinalIgnoreCase));
            if (ax == null) return;

            XAxisPosition = ax.Position.ToString();
            ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals("Y", StringComparison.OrdinalIgnoreCase));
            if (ax == null) return;

            YAxisPosition = ax.Position.ToString();
            ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals("Z", StringComparison.OrdinalIgnoreCase));
            if (ax == null) return;

            ZAxisPosition = ax.Position.ToString();
        }

        private void RestoreCmd()
        {
              string File_path = Path.Combine(ConfigStore.StoreDir, "ZYDis.txt");
              LoadFromFile(File_path);
        }
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                switch (parts[0])
                {
                    case "YInit":
                        LaserYDis = parts[1];
                        break;

                    case "ZInit":
                        LaserZDis = parts[1];
                        break;

                    case "XInit":
                        LaserXDis = parts[1];
                        break;
                }
            }
        }
        // 新增主表（从输入框）
        private void AddMaster()
        {
            if (string.IsNullOrEmpty(FocusStart))
            {
                MessageBox.Show("焦点起点未输入！");
                return;
            }
            if (string.IsNullOrEmpty(FocusEnd))
            {
                MessageBox.Show("焦点终点未输入！");
                return;
            }

            if (string.IsNullOrEmpty(FocusInterval))
            {
                MessageBox.Show("焦点间隔未输入！");
                return;
            }
            if (string.IsNullOrEmpty(MarkStart))
            {
                MessageBox.Show("打标开始未输入！");
                return;
            }
            if (string.IsNullOrEmpty(MarkInterval))
            {
                MessageBox.Show("打标间隔未输入！");
                return;
            }
            if (string.IsNullOrEmpty(FocusLen))
            {
                MessageBox.Show("Z轴内容未输入！");
                return;
            }
            if (!double.TryParse(FocusStart, out double s) ||
                !double.TryParse(FocusEnd, out double e) ||
                !double.TryParse(FocusInterval, out double itv))
            {
                MessageBox.Show("请在起点/终点/间隔输入有效数字。");
                return;
            }

            if (!double.TryParse(MarkStart, out double ms) ||
               !double.TryParse(MarkEnd, out double me) ||
               !double.TryParse(MarkInterval, out double mki))
            {
                MessageBox.Show("请在起点/终点/间隔输入有效数字。");
                return;
            }
            if (!double.TryParse(FocusLen, out double msz))
            {
                MessageBox.Show("请在划线轴开始步距地方输入有效数字。");
                return;
            }
            if (e > s)
            {

                MessageBox.Show("焦距方向终点数据要小于起点，请重新输入。");
                return;

            }
            if(itv>0)
            {
                MessageBox.Show("焦距间距数据要小于0，请重新输入！");
                return;
            }
            if ((e - s) / itv != (me - ms) / mki)
            {
                //MessageBox.Show("焦点间隔数必须等于打标间隔数，请重新填写！");
                //return;
            }
            bool equal = FocusAxis == MarkAxis && MarkAxis == LineAxis;
            if (equal)
            {
                MessageBox.Show("所选轴名不能有相同，请重新选择！");
                return;
            }
            if (!double.TryParse(LaserYDis, out double disY) ||
             !double.TryParse(LaserZDis, out double disZ)
             ||
             !double.TryParse(LaserXDis, out double disX))
            {
                disY = 0;
                disZ = 0;
                disX = 0;
                //MessageBox.Show("请在XYZ输入有效数字。");
                //return;
            }
            var newId = (Masters.Any() ? Masters.Max(m => m.Id) : 0) + 1;
            var m = new MasterItem
            {
                Id = newId,
                FocusStart = s,
                FocusEnd = e,
                FocusInterval = itv,
                FocusAxis = FocusAxis ?? string.Empty,


                MarkStart = ms,
                MarkEnd = me,
                MarkInterval = mki,
                MarkAxis = MarkAxis ?? string.Empty,
                LaserYDis = disY,
                LaserZDis = disZ,
                LaserXDis = disX,
                StartZ= msz,
                IsFocusRange = false
            };
            Masters.Add(m);

            // 自动选中新添加的行
            SelectedMaster = m;
        }



        // 运行：为选中主表生成子点（并将这些点加入 Details 数据集）
        private async void RunForSelectedMaster()
        {
            if (SelectedMaster == null)
            {
                MessageBox.Show("请先选中一行主表，然后点击运行。");
                return;
            }

            // 先移除该 Master 之前生成的 Detail（避免重复）
            for (int i = Details.Count - 1; i >= 0; i--)
            {
                if (Details[i].MasterId == SelectedMaster.Id)
                    Details.RemoveAt(i);
            }

            // 生成新的 detail（起点→终点 按间隔）
            double pos = SelectedMaster.FocusStart; //焦点x
            double markPos = SelectedMaster.MarkStart;//Y轴上
            int nextId = (Details.Any() ? Details.Max(d => d.Id) + 1 : 1);



            int k = 1;//第一条就划线 xy停留在起始位置
            var axnz = MachineVM.Axes.Where(d => d.Name.ToUpper() == "Z");
            double zPosStart = 0;
            if (axnz.Any())
            {
                zPosStart = axnz.FirstOrDefault().Position;
              
                //激光出光脚本运行
            }

            // 生成（包含起点和终点在内的点，按 <= 判断）
            //while (pos>= SelectedMaster.FocusEnd + 1e-9)
            //{
            //    Details.Add(new DetailItem
            //    {
            //        Id = nextId++,
            //        MasterId = SelectedMaster.Id,
            //        AxisPosition = pos,
            //        FocusAxis = SelectedMaster.FocusAxis,
            //        FocusInterval = SelectedMaster.FocusInterval,

            //        MarkInterval = SelectedMaster.MarkInterval,

            //        MarkAxis = SelectedMaster.MarkAxis,


            //        MarkPosition = markPos,
            //        IsFocus = false,
            //        LaserZDis= SelectedMaster.LaserZDis,
            //        LaserYDis=SelectedMaster.LaserYDis,
            //        LaserXDis = SelectedMaster.LaserXDis,
            //        //划线并z轴移动

            //    });

            //    #region  移动出光
            //    ////if(k>0)
            //    ////{
                    
           

            //    /// }

            //    eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(MarkAxis.ToString(), markPos, true)); //y
            //    await WaitAxisPositionAsync(MarkAxis.ToString().ToUpper(), markPos, 0.0001);//y

            //    eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(FocusAxis.ToString(), pos, true));//x
            //    await WaitAxisPositionAsync(FocusAxis.ToString().ToUpper(), pos, 0.0001);


            //    double z1 = Convert.ToDouble(FocusLen.Trim()); // 
            //    double z2 = 10;
            //    double z = 0;
            //    if ((k & 1) == 1)
            //    {
            //        z = z1; //下划线
            //                //奇数Console.WriteLine("{0} 是奇数", x);
            //    }
            //    else
            //    {
            //        z = z1 * (-1); //上划线
            //    }
            //    eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new("Z", z, false)); //相对的地方z1
            //    double zPosend = zPosStart + z;
            //    await WaitAxisPositionAsync("Z", zPosend, 0.001);
            //    zPosStart = axnz.FirstOrDefault().Position;


            //    #endregion
            //    pos += SelectedMaster.FocusInterval;  //x
            //    markPos += SelectedMaster.MarkInterval; //y
            //    k++;
            //}

          
           int kk = 1;
            double zmarkpos = ConvertStringToDouble(MarkZ);
            zPosStart = zmarkpos;
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new("Z", zmarkpos, true)); //y
            await WaitAxisPositionAsync("Z", zmarkpos, 0.001); //先移动到z的打标位置

            for (int j=0;j< FilteredDetails.Count;j++)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedDetail = FilteredDetails[j];
                });

                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(FilteredDetails[j].MarkAxis.ToString(), FilteredDetails[j].MarkPosition, true)); //y
                await WaitAxisPositionAsync(FilteredDetails[j].MarkAxis.ToString().ToUpper(), FilteredDetails[j].MarkPosition, 0.001);//y

                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(FilteredDetails[j].FocusAxis.ToString(), FilteredDetails[j].AxisPosition, true));//x
                await WaitAxisPositionAsync(FilteredDetails[j].FocusAxis.ToString().ToUpper(), FilteredDetails[j].AxisPosition, 0.001);


                double z1 = FilteredDetails[j].StartZ; // 
                double z = 0;
                if ((k & 1) == 1)
                {
                    z = z1; //下划线  如果是复数下划线
                            //奇数Console.WriteLine("{0} 是奇数", x);
                }
                else
                {
                    z = z1 * (-1); //上划线
                }
                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new("Z", z, false)); //相对的地方z1
                double zPosend = zPosStart + z;
                await WaitAxisPositionAsync("Z", zPosend, 0.001);
                zPosStart = axnz.FirstOrDefault().Position;

                kk++;
            }
            if (kk > 1)
            {
                MessageBox.Show("运行完成！");
            }
            // 刷新视图：显示当前 SelectedMaster 的 points
            //RefreshFilteredDetails();
            //移动划线

        }


        private  void CreateDetail()
        {
            if (SelectedMaster == null)
            {
                MessageBox.Show("请先选中一行主表，然后点击运行。");
                return;
            }

            // 先移除该 Master 之前生成的 Detail（避免重复）
            for (int i = Details.Count - 1; i >= 0; i--)
            {
                if (Details[i].MasterId == SelectedMaster.Id)
                    Details.RemoveAt(i);
            }

            // 生成新的 detail（起点→终点 按间隔）
            double pos = SelectedMaster.FocusStart; //焦点
            double markPos = SelectedMaster.MarkStart;//Y轴上
            int nextId = (Details.Any() ? Details.Max(d => d.Id) + 1 : 1);
            int indexD = 0;

            int k = 0;
            var axnz = MachineVM.Axes.Where(d => d.Name.ToUpper() == "Z");
            double zPosStart = 0;
            if (axnz.Any())
            {
                zPosStart = axnz.FirstOrDefault().Position;

                //激光出光脚本运行
            }

            // 生成（包含起点和终点在内的点，按 <= 判断）
            while (pos >= SelectedMaster.FocusEnd + 1e-9)
            {
                Details.Add(new DetailItem
                {
                    Id = nextId++,
                    DetailId = SelectedMaster.Id + "." + (++indexD).ToString(),
                    MasterId = SelectedMaster.Id,
                    AxisPosition = pos,
                    FocusAxis = SelectedMaster.FocusAxis,
                    FocusInterval = SelectedMaster.FocusInterval,

                    MarkInterval = SelectedMaster.MarkInterval,

                    MarkAxis = SelectedMaster.MarkAxis,


                    MarkPosition = markPos,
                    IsFocus = false,
                    LaserZDis = SelectedMaster.LaserZDis,
                    LaserYDis = SelectedMaster.LaserYDis,
                    LaserXDis = SelectedMaster.LaserXDis,
                    StartZ = SelectedMaster.StartZ,

                    //划线并z轴移动

                });
                pos += SelectedMaster.FocusInterval;
                markPos += SelectedMaster.MarkInterval;
                k++;
            }

            // 刷新视图：显示当前 SelectedMaster 的 points
            RefreshFilteredDetails();
            //移动划线

        }
        // 刷新 FilteredDetails，使其显示与 SelectedMaster.Id 对应的 Detail 行
        private void RefreshFilteredDetails()
        {
            if (SelectedMaster == null)
            {
                FilteredDetails = new ObservableCollection<DetailItem>();
                return;
            }

            var list = Details.Where(d => d.MasterId == SelectedMaster.Id)
                              .OrderBy(d => d.Id)
                              .ToList();
            FilteredDetails = new ObservableCollection<DetailItem>(list);
        }

        // 点击主表行的“移动查看”按钮（带参数）
        private void MasterMove(MasterItem item)
        {
            if (item == null) return;
            // 这里是接口位置：在这里调用你设备的控制函数
            //MessageBox.Show($"主表移动查看：Id={item.Id}, Axis={item.AxisName}, Start={item.Start}");
            // TODO: MotionController.MoveTo(item.AxisName, item.Start) 等
        }

        // 点击主表行的“删除”按钮（带参数）
        private void MasterDelete(MasterItem item)
        {
            if (item == null) return;

            // 级联删除该 Master 对应的 Details
            for (int i = Details.Count - 1; i >= 0; i--)
                if (Details[i].MasterId == item.Id)
                    Details.RemoveAt(i);

            Masters.Remove(item);

            // 若删掉的是当前SelectedMaster，清空选择
            if (SelectedMaster == item) SelectedMaster = null;

            RefreshFilteredDetails();
        }

        // 点击子表行的“移动查看” 先移动z y x
        private async Task DetailMoveBtnAsync(DetailItem item)
        {
            if (item == null) return;


            // eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(item.MarkAxis.ToUpper(), item.MarkPosition, true));   //Y 找到打标位置
            //var  axn=MachineVM.Axes.Where(d => d.Name.ToUpper() == item.MarkAxis.ToUpper());
            // if (axn.Any())
            // {
            //   var  axPosition=  axn.FirstOrDefault().Position;
            //     while(Math.Abs(axPosition- item.AxisPosition)>0.002)
            //     {

            //     }
            // }
            // eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new("Z", item.LaserZDis, false));
            //  axn = MachineVM.Axes.Where(d => d.Name.ToUpper() == "Z");
            // if (axn.Any())
            // {
            //     var axPosition = axn.FirstOrDefault().Position;
            //     while (Math.Abs(axPosition - item.LaserZDis) > 0.002)
            //     {

            //     }
            // }
            // eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(item.MarkAxis.ToUpper(), item.MarkPosition + item.LaserYDis, true));
            // if (axn.Any())
            // {
            //     var axPosition = axn.FirstOrDefault().Position;
            //     while (Math.Abs(axPosition - item.AxisPosition + item.LaserYDis) > 0.002)
            //     {

            //     }
            // }

            await DetailMoveAsync(item);
            MessageBox.Show("移动结束！");

        }

        
         private async Task DetailScanBtnAsync()
        {
            if (SelectedDetail == null) return;

            
            await DetailMoveAsync(SelectedDetail, true);
            MessageBox.Show("移动结束！");

        }
        public async Task WaitAxisPositionAsync(string axis, double target ,double Tolerance=0.002)
        {
         
            while (true)
            {
                var ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals(axis, StringComparison.OrdinalIgnoreCase));
                if (ax == null) break;

                double pos = ax.Position;

                if (Math.Abs(pos - target) < Tolerance)
                    break;

                await Task.Delay(10); // 让线程休眠，不影响 UI
            }
        }
        /// <summary>
        /// 点击子表的移动查看按钮
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task DetailMoveAsync(DetailItem item,bool isScan=false)
        {
            if (isScan)
            {
                if (MessageBox.Show($"确定要移动到选中行的激光位置？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }
            else
            {
                if (MessageBox.Show($"确定要移动到选中行的相机位置查看？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }

            if (item == null) return;

            var axisName = item.MarkAxis.ToUpper();
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
             .Publish(new("X", -140, true));

            await WaitAxisPositionAsync("X", -140); //item.AxisPosition

            if (isScan)  //重新移动到打标位置开始重来
            {
                double zmarkpos = ConvertStringToDouble(MarkZ);

                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
                .Publish(new("Z", zmarkpos, true));
                await WaitAxisPositionAsync("Z", zmarkpos);

                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
               .Publish(new("Y", item.MarkPosition, true));

                await WaitAxisPositionAsync("Y", item.MarkPosition);

                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
               .Publish(new("X", item.AxisPosition, true));

                await WaitAxisPositionAsync("X", item.AxisPosition);

                return;
            }
            

            // 第一步：移动到打标位置  x先移动到-140
         


            //移动到零点位置
            double zpos = ConvertStringToDouble(ZAxisPosition);
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
              .Publish(new("Z", zpos, true));

            await WaitAxisPositionAsync("Z", zpos);

            double ypos = ConvertStringToDouble(YAxisPosition);
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
              .Publish(new("Y", ypos, true));

            await WaitAxisPositionAsync("Y", ypos);


            double  xpos = ConvertStringToDouble(XAxisPosition);
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
              .Publish(new("X", xpos, true));

            await WaitAxisPositionAsync("X", xpos);

            //接触位移到激光
            // double  totalXpos= ConvertStringToDouble(XRlativeDis)+ ConvertStringToDouble(XAxisPosition);







            // 第二步：Z 轴移动  AxisPosition 焦点轴位置  x  y 间隔轴名MarkPosition
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>() 
                .Publish(new("Z", item.LaserZDis, false));

            await WaitAxisPositionAsync("Z", item.LaserZDis);

            // 第三步：Y 再次移动
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
                .Publish(new(axisName, item.MarkPosition + item.LaserYDis, true));

            await WaitAxisPositionAsync(axisName, item.MarkPosition + item.LaserYDis);


            // 第二步：X 轴移动
            eventAggregator.GetEvent<PreinstallForAxisMoveEvent>()
                .Publish(new("X", item.AxisPosition+item.LaserXDis, true));

            await WaitAxisPositionAsync("X", item.AxisPosition + item.LaserXDis);

            MessageBox.Show("移动结束！");
        }
        static double ConvertStringToDouble(string str)
        {
            if (double.TryParse(str, out double number))
            {
                return number;
            }
            else
            {
                throw new FormatException("输入字符串不是有效的浮点数格式");
            }
        }

        // 点击子表行的“删除”
        private void DetailDelete(DetailItem item)
        {
            if (item == null) return;
            Details.Remove(item);
            RefreshFilteredDetails();
        }

        // 删除选中主表按钮（在表格下方）
        private void DeleteSelectedMaster()
        {
            if (SelectedMaster == null) return;
            MasterDelete(SelectedMaster);
        }

        // 删除选中子表按钮（在表格下方）
        private void DeleteSelectedDetail()
        {
            if (SelectedDetail == null) return;
            DetailDelete(SelectedDetail);
        }

        // ---------- 保存/加载（整个数据一次性存/读，同一文件） ----------
        private void SaveAllData()
        {
            try
            {
                var result = MessageBox.Show("是否保存？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    return;

                }
                GetDisItem();
                var container = new SaveDataContainer
                {
                    Masters = Masters.ToList(),
                    Details = Details.ToList(),
                    DisItems = DisItems.ToList(),
                };
                var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFile, json);
                MessageBox.Show("数据已保存到 LaserData.json 成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message);
            }
        }


        private  void   GetDisItem()
        {
            if (!double.TryParse(ZRlativeDis, out double s) ||
              !double.TryParse(YRlativeDis, out double e) ||
              !double.TryParse(XRlativeDis, out double itv))
            {
                MessageBox.Show("请在X间距/Y间距/Z间距输入有效数字。");
                return;
            }

            if (!double.TryParse(ZAxisPosition, out double ms) ||
               !double.TryParse(YAxisPosition, out double me) ||
               !double.TryParse(XAxisPosition, out double mki))
            {
                MessageBox.Show("请在当前位置输入有效数字。");
                return;
            }
            if (!double.TryParse(MarkZ, out double mz))
              
            {
                MessageBox.Show("请在开始打标z的位置输入有效数字。");
                return;
            }
            DisItems = new ObservableCollection<DisItem>();
             var m = new DisItem
            {

                ZRlativeDis = s,
                YRlativeDis = e,
                XRlativeDis = itv,

                ZAxisPosition = ms,
                YAxisPosition = me,
                XAxisPosition = mki,
                MarkZ = mz,
             
            };
            DisItems.Add(m);
        }
        private void LoadAllData()
        {
            try
            {
                if (!File.Exists(DataFile)) return;
                var json = File.ReadAllText(DataFile);
                var container = JsonSerializer.Deserialize<SaveDataContainer>(json);
                if (container == null) return;

                Masters.Clear();
                Details.Clear();
                DisItems.Clear();
                if (container.Masters != null)
                    foreach (var m in container.Masters) Masters.Add(m);

                if (container.Details != null)
                    foreach (var d in container.Details) Details.Add(d);

                if (container.DisItems != null)
                {
                    foreach (var d in container.DisItems) DisItems.Add(d);

                    if (DisItems.Count > 0)
                    {
                        ZRlativeDis = DisItems[0].ZRlativeDis.ToString();
                        YRlativeDis = DisItems[0].YRlativeDis.ToString();
                        XRlativeDis = DisItems[0].XRlativeDis.ToString();

                        ZAxisPosition = DisItems[0].ZAxisPosition.ToString();
                        YAxisPosition = DisItems[0].YAxisPosition.ToString();
                        XAxisPosition = DisItems[0].XAxisPosition.ToString();
                        MarkZ = DisItems[0].MarkZ.ToString();
                    }

                }
            }
            catch (Exception ex)
            {
                // 读取失败时仅输出调试信息，不阻止程序
                System.Diagnostics.Debug.WriteLine("数据加载 failed: " + ex.Message);
            }
        }

        internal void CalculateMarkEnd()
        {
            try
            {
                double focusStart = 0.0;
                double focusEnd = 0.0;
                double focusInterval = 0.0;
                double focusCount = 0.0;
                double markStart = 0.0;
                double markInterval = 0.0;
                bool isNotNull = false;
                if (!string.IsNullOrEmpty(FocusStart))
                {
                    isNotNull = true;
                    if (double.TryParse(FocusStart, out double result))
                    {
                        // result
                        focusStart = result;
                    }
                    else
                    {
                        MessageBox.Show("FocusStart 格式错误");
                    }
                }
                if (!string.IsNullOrEmpty(FocusEnd))
                {
                    isNotNull = true;
                    if (double.TryParse(FocusEnd, out double result1))
                    {
                        // result
                        focusEnd = result1;
                    }
                    else
                    {
                        MessageBox.Show("FocusEnd 格式错误");
                    }
                }
                if (!string.IsNullOrEmpty(FocusInterval))
                {
                    isNotNull = true;
                    if (double.TryParse(FocusInterval, out double result2))
                    {
                        // result
                        focusInterval = result2;
                    }
                    else
                    {
                        MessageBox.Show("焦点间隔 格式错误");
                    }
                }
                if (!string.IsNullOrEmpty(FocusCount))
                {
                    isNotNull = true;
                    if (double.TryParse(FocusCount, out double result2))
                    {
                        // result
                        focusCount = result2;
                    }
                    else
                    {
                        MessageBox.Show("焦点次数 格式错误");
                    }
                }
                if (!string.IsNullOrEmpty(MarkStart))
                {
                    isNotNull = true;
                    if (double.TryParse(MarkStart, out double result3))
                    {
                        // result
                        markStart = result3;
                    }
                    else
                    {
                        MessageBox.Show("打标开始 格式错误");
                    }
                }
                if (!string.IsNullOrEmpty(MarkInterval))
                {
                    isNotNull = true;
                    if (double.TryParse(MarkInterval, out double result4))
                    {
                        // result
                        markInterval = result4;
                    }
                    else
                    {
                        MessageBox.Show("打标间隔 格式错误");
                    }
                }
                if(!isNotNull)
                {
                    return;
                }

                if (focusInterval == 0)
                    return;

                double span = focusEnd - focusStart;

                // 支持倒序：如果间隔方向和跨度方向相反，取负
                if (span * focusInterval < 0)
                    focusInterval = -focusInterval;


                // 计算条数
                //int count = (int)Math.Floor((focusEnd - focusStart) / focusInterval) + 1;
                int count = (int)Math.Floor(Math.Abs(span) / Math.Abs(focusInterval)) + 1;
                if (count < 1)
                    return;

                // 计算打标终点
                double markEnd = markStart + (count - 1) * markInterval;

                MarkEnd = markEnd.ToString();
            }
            catch
            {
                // 可选：忽略错误
            }
        }

        /// <summary>
        /// 失去焦点
        /// </summary>
        internal void CalculateMarkCount()
        {
            try
            {
                double focusStart = 0.0;
                double focusEnd = 0.0;
                double focusInterval = 0.0;
                double focusCount = 0;

                if (!string.IsNullOrEmpty(FocusStart))
                {
                    if (double.TryParse(FocusStart, out double result))
                    {
                        // result
                        focusStart = result;
                    }
                    else
                    {
                        MessageBox.Show("FocusStart 格式错误");
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(FocusEnd))
                {
                    if (double.TryParse(FocusEnd, out double result1))
                    {
                        // result
                        focusEnd = result1;
                    }
                    else
                    {
                        MessageBox.Show("FocusEnd 格式错误");
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(FocusInterval))
                {
                    if (double.TryParse(FocusInterval, out double result2))
                    {
                        // result
                        focusInterval = result2;
                        if(string.IsNullOrEmpty(FocusCount))
                        {
                           FocusCount= GetMarkCount(focusStart, focusEnd,focusInterval).ToString();
                        }
                        else
                        {
                            int  markCount= GetMarkCount(focusStart, focusEnd, focusInterval);
                            if(markCount!= int.Parse(FocusCount))
                            {
                                MessageBox.Show("焦点间隔下次数填写错误！");
                                return;
                            }
                           
                        }
                    }
                    else
                    {
                        MessageBox.Show("焦点间隔 格式错误");
                    }
                }
                

                if (!string.IsNullOrEmpty(FocusCount))
                {
                    if (double.TryParse(FocusCount, out double result2))
                    {
                        // result
                        focusCount = result2;
                        if (string.IsNullOrEmpty(FocusInterval))
                        {
                            FocusInterval = GetMarkInterval(focusStart, focusEnd, (int)focusCount).ToString();
                        }
                        else
                        {
                            double markInterval1 = GetMarkInterval(focusStart, focusEnd, (int)focusCount);
                            if (markInterval1 != focusInterval)
                            {
                                MessageBox.Show("焦点次数固定下间隔数填写错误！");
                                return;
                            }

                        }
                    }
                    else
                    {
                        MessageBox.Show("焦点间隔 格式错误！");
                        return;
                    }
                }

               
            }
            catch(Exception EX)
            {
                MessageBox.Show("转换发生错误！"+EX.Message);
                return;
                // 可选：忽略错误
            }
        }

        /// <summary>
        /// 起点终点和间隔求次数
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        int GetMarkCount(double start, double end, double interval)
        {
            if (interval == 0)
                throw new ArgumentException("interval 不能为 0");

            double length = Math.Abs(end - start);
            double step = Math.Abs(interval);

            return (int)Math.Floor(length / step) + 1;
        }
        /// <summary>
        /// 已知起点终点和次数求间隔
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        double GetMarkInterval(double start, double end, int count)
        {
            if (count < 2)
                MessageBox.Show("打标次数必须 >= 2");

            return (end - start) / (count - 1);
           
        }
        // container for json
        private class SaveDataContainer
        {
            public System.Collections.Generic.List<MasterItem> Masters { get; set; }
            public System.Collections.Generic.List<DetailItem> Details { get; set; }

            public System.Collections.Generic.List<DisItem> DisItems { get; set; }
            
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null)
            {
                if (typeof(T).IsValueType) return _canExecute(default!);
                return _canExecute((T)parameter!);
            }
            if (parameter is T t) return _canExecute(t);
            try
            {
                var converted = (T)Convert.ChangeType(parameter, typeof(T));
                return _canExecute(converted);
            }
            catch { return false; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
            {
                if (typeof(T).IsValueType) _execute(default!);
                else _execute((T)parameter!);
                return;
            }
            if (parameter is T t) _execute(t);
            else
            {
                var converted = (T)Convert.ChangeType(parameter, typeof(T));
                _execute(converted);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class MasterItem
    {
        public int Id { get; set; }

        // --- 焦点 ---
        public double FocusStart { get; set; }
        public double FocusEnd { get; set; }
        public double FocusInterval { get; set; }
        public string FocusAxis { get; set; }

        // --- 打标 ---
        public double MarkStart { get; set; }
        public double MarkEnd { get; set; }
        public double MarkInterval { get; set; }
        public string MarkAxis { get; set; }

        public bool IsFocusRange { get; set; }

        public double LaserZDis { get; set; }

        public double LaserYDis { get; set; }


        public double LaserXDis { get; set; }

        public double StartZ { get; set; }

    }

    public class DetailItem
    {
        public string DetailId { get; set; }
        public int Id { get; set; }
        public int MasterId { get; set; }

        // 焦点
        public double AxisPosition { get; set; }
        public string FocusAxis { get; set; }
        public double FocusInterval { get; set; }

        // 打标
        public string MarkAxis { get; set; }
        public double MarkInterval { get; set; }
        public double MarkPosition { get; set; }

        public bool IsFocus { get; set; }

        public double LaserZDis { get; set; }

        public double LaserYDis { get; set; }

        public double LaserXDis { get; set; }


        public double StartZ { get; set; }
    }



    public class DisItem
    {
        public double XAxisPosition { get; set; }

        public double YAxisPosition { get; set; }

        public double ZAxisPosition { get; set; }


        public double XRlativeDis { get; set; }

        public double YRlativeDis { get; set; }

        public double ZRlativeDis { get; set; }

        public double MarkZ { get; set; }
        
    }
}
