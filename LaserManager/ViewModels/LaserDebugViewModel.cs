using Machine.Interfaces;
using Machine.ViewModels;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query.SemanticAst;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace LaserManager.ViewModels
{
    public class LaserDebugViewModel : BindableBase
    {
        //string File_path = Path.Combine(ConfigStore.StoreDir, "MoveItems.json");
        private  string SaveFileName = Path.Combine(ConfigStore.StoreDir, "MoveItems.json");//"MoveItems.json";
        private string DataFile = Path.Combine(ConfigStore.StoreDir, "LaserData.json");

        // private string LGQuickFile_path = Path.Combine(ConfigStore.StoreDir, "LGQuick.json");

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public ObservableCollection<string> Positions { get; set; } = new() { "接触式位移传感器", "聚焦透镜", "相机镜头", "激光位移传感器" };

        public ObservableCollection<string> Gpis { get; set; } = new() { "是", "否"}; 
        public ObservableCollection<string> Axes { get; set; } = new() { "X", "Y", "Z", "A", "C" };

        public ObservableCollection<string> FocusPoint { get; set; } = new () { "点1", "点2", "点3", "点4", "点5", "点6", "点7", "点8", "点9", "点10" };


        public ObservableCollection<MasterItem> Masters { get; set; } = new();
        public ObservableCollection<DetailItem> Details { get; set; } = new();

        public ObservableCollection<DisItem> DisItems { get; set; } = new();

        private string _startPosition;
        public string StartPosition { get => _startPosition; set { _startPosition = value; OnPropertyChanged(); } }

        private string _endPosition;
        public string EndPosition { get => _endPosition; set { _endPosition = value; OnPropertyChanged(); } }

        private string _selectedAxis;
        public string SelectedAxis { get => _selectedAxis; set { _selectedAxis = value; OnPropertyChanged(); } }

        private string _focusPosition;
        public string FocusPosition { get => _focusPosition; set { _focusPosition = value; OnPropertyChanged(); } }
        

        private string _distance;
        public string Distance { get => _distance; set { _distance = value; OnPropertyChanged(); } }

        private string _safeX="0";
        public string SafeX { get => _safeX; set { _safeX = value; OnPropertyChanged(); } }
       

        private string _moveNo;
        public string MoveNo { get => _moveNo; set { _moveNo = value; OnPropertyChanged(); } }


        private string _groupNo;
        public string GroupNo { get => _groupNo; set { _groupNo = value; OnPropertyChanged(); } }



        private string _selectedGroup;
        public string SelectedGroup { get => _selectedGroup; set { _selectedGroup = value; OnPropertyChanged(); } }

        private readonly IEventAggregator eventAggregator;
        public ObservableCollection<MoveItem> MoveItems { get; set; } = new();

        private MoveItem _selectedItem;
        public MoveItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
        public ICommand AddCommand { get; }
        public ICommand MoveCommand { get; }
        public ICommand SaveCommand { get; }

        public ICommand PointFocusCommand { get; }
        public ICommand GetPosCommand { get; }

        public ICommand PointStartCommand { get; }
        public ICommand PointEndCommand { get; }
        public ICommand DisCommand { get; }



        private string _yAxisPosition;
        public string YAxisPosition { get => _yAxisPosition; set { _yAxisPosition = value; RaisePropertyChanged(); } }

        private string _xAxisPosition;
        public string XAxisPosition { get => _xAxisPosition; set { _xAxisPosition = value; RaisePropertyChanged(); } }

        private string _zAxisPosition;
        public string ZAxisPosition { get => _zAxisPosition; set { _zAxisPosition = value; RaisePropertyChanged(); } }


        private string _aAxisPosition;
        public string AAxisPosition { get => _aAxisPosition; set { _aAxisPosition = value; RaisePropertyChanged(); } }

        private string _cAxisPosition;
        public string CAxisPosition { get => _cAxisPosition; set { _cAxisPosition = value; RaisePropertyChanged(); } }

        public MachineViewModel MachineVM { get; set; }
        private IContainerProvider containerProvider;
        public ICommand DeleteCommand { get; }
        public LaserDebugViewModel(IContainerProvider provider)
        {
            this.containerProvider = provider;
            MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();
            var dd = MachineVM.Axes;
            AddCommand = new RelayCommand(AddItem);
            MoveCommand = new RelayCommand(ExecuteMove);
            SaveCommand = new RelayCommand(SaveToFile);
            DeleteCommand = new RelayCommand(DeleteItem);
            GetPosCommand = new RelayCommand(GetPosition);
            PointFocusCommand= new RelayCommand(MoveFocusCommand);
            PointStartCommand = new RelayCommand(FullStartCommand);
            DisCommand = new RelayCommand(CalDisCommand);
            PointEndCommand = new RelayCommand(FullEndCommand);
 

            LoadFromFile();
            LoadFromFocus();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
        }

        private void FullEndCommand()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("请先在表格中选中一行！");
                return;
            }
            if (SelectedItem.Axis == "X")
            {
                SelectedItem.EndValuePosition = XAxisPosition;
            }
            if (SelectedItem.Axis == "Y")
            {
                SelectedItem.EndValuePosition = YAxisPosition;
            }
            if (SelectedItem.Axis == "Z")
            {
                SelectedItem.EndValuePosition = ZAxisPosition;
            }
            if (SelectedItem.Axis == "A")
            {
                SelectedItem.EndValuePosition = AAxisPosition;
            }
            if (SelectedItem.Axis == "C")
            {
                SelectedItem.EndValuePosition = CAxisPosition;
            }
        }

        private void CalDisCommand()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("请先在表格中选中一行！");
                return;
            }
            if (double.TryParse(SelectedItem.StartValuePosition, out double sPosition)&& double.TryParse(SelectedItem.EndValuePosition, out double ePosition))
            {
                SelectedItem.Distance = (sPosition - ePosition).ToString();
            }
           
        }

        private void FullStartCommand()
        {
           
           
            if (SelectedItem == null)
            {
                MessageBox.Show("请先在表格中选中一行！");
                return;
            }
            if(SelectedItem.Axis=="X")
            {
                SelectedItem.StartValuePosition = XAxisPosition;
            }
            if (SelectedItem.Axis == "Y")
            {
                SelectedItem.StartValuePosition = YAxisPosition;
            }
            if (SelectedItem.Axis == "Z")
            {
                SelectedItem.StartValuePosition = ZAxisPosition;
            }
            if (SelectedItem.Axis == "A")
            {
                SelectedItem.StartValuePosition = AAxisPosition;
            }
            if (SelectedItem.Axis == "C")
            {
                SelectedItem.StartValuePosition = CAxisPosition;
            }
           
        }

        private async void MoveFocusCommand()
        {
            var ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals("X", StringComparison.OrdinalIgnoreCase));
            if (ax != null)
            {
                if (double.TryParse(FocusPosition, out double xPosition))
                {
                    eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new("X", xPosition, true)); //y
                    await WaitAxisPositionAsync("X", xPosition, 0.001);//y
                }
            }


        }
        public async Task WaitAxisPositionAsync(string axis, double target, double Tolerance = 0.002)
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

            ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals("A", StringComparison.OrdinalIgnoreCase));
            if (ax == null) return;

            AAxisPosition = ax.Position.ToString();

            ax = MachineVM.Axes.FirstOrDefault(a => a.Name.Equals("C", StringComparison.OrdinalIgnoreCase));
            if (ax == null) return;

            CAxisPosition = ax.Position.ToString(); 
        }
        private void DeleteItem()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("请先选中要删除的行！");
                return;
            }

            var toDelete = SelectedItem;
            if (MessageBox.Show($"确定删除第 {toDelete.Index} 行？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            MoveItems.Remove(toDelete);

            // 重新编号
            for (int i = 0; i < MoveItems.Count; i++)
                MoveItems[i].Index = i + 1;

            //SaveToFile();
        }
        private void AddItem()
        {
            if (string.IsNullOrWhiteSpace(StartPosition) ||
                string.IsNullOrWhiteSpace(EndPosition) ||
                string.IsNullOrWhiteSpace(SelectedAxis)
                //string.IsNullOrWhiteSpace(Distance))
                )
            {
                MessageBox.Show("请先选择完整信息再新增！");
                return;
            }
            if (!double.TryParse(ZAxisPosition, out double ms) ||
              !double.TryParse(YAxisPosition, out double me) ||
              !double.TryParse(XAxisPosition, out double mki))

            {
                ZAxisPosition = "";
                YAxisPosition = "";
                XAxisPosition = "";

            }
            if (!double.TryParse(AAxisPosition, out double ms1))
            {
                AAxisPosition = "";
            }
            if (!double.TryParse(CAxisPosition, out double ms2))
            {
                CAxisPosition = "";
            }
           
            int maxIndex = 0;
            if (MoveItems.Count > 0)
            {
                var maxId = MoveItems.Max(item => item.Index);
                if (SelectedGroup == "是")
                {
                    var maxGroupNo = MoveItems.Where(d => d.Index == maxId).ToList().Max(item => item.GroupNo);
                    maxIndex = int.Parse(maxGroupNo);
                }
                else
                {
                    maxIndex = 1;
                }
            }
            else
            {
                var result = MessageBox.Show("列表没有数据，是否做了删除操作，如果没有保存,数据会被覆盖！", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    maxIndex = 1;
                }
                else
                {
                    return;
                }
                    
            }
            var newItem = new MoveItem(MoveItems)
            {
                Index = MoveItems.Count + 1,
                StartPosition = StartPosition,
                EndPosition = EndPosition,
                Axis = SelectedAxis,
                Distance = Distance,
                MoveNo = MoveNo,
                GroupNo = maxIndex.ToString(),
                ZAxisPosition = ZAxisPosition.ToString(),
                YAxisPosition = YAxisPosition.ToString(),
                XAxisPosition = XAxisPosition.ToString(),
                AAxisPosition = AAxisPosition.ToString(),
                CAxisPosition = CAxisPosition.ToString(),
                StartValuePosition = "",
                EndValuePosition = "",
                SafeX = SafeX,
                IsUse = true,


            };

            MoveItems.Add(newItem);
        }

        private void ExecuteMove()
        {
            var start = MoveItems.FirstOrDefault(x => x.IsStart);
            var end = MoveItems.FirstOrDefault(x => x.IsEnd);
            if (start == null || end == null)
            {
                //MessageBox.Show("请先选择起点和终点！");
                //return;
            }
            if (SelectedItem == null)
            {
                MessageBox.Show("请先在表格中选中一行！");
                return;
            }

            var item = SelectedItem;

            // MessageBox.Show($"从 {start.StartPosition} 移动到 {end.EndPosition}");
        }

        private void SaveToFile()
        {
            try
            {
                var result = MessageBox.Show("是否保存？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    LoadFromFile();
                    return;
                }
                //  if (!double.TryParse(ZAxisPosition, out double ms) ||
                //!double.TryParse(YAxisPosition, out double me) ||
                //!double.TryParse(XAxisPosition, out double mki))
                //  {
                //      MessageBox.Show("请在当前位置输入有效数字。");
                //      return;
                //  }
                // 用 DTO 列表序列化（剥离 Parent 等不可序列化信息）
                var dto = MoveItems.Select(m => new MoveItemDto
                {
                    Index = m.Index,
                    StartPosition = m.StartPosition,
                    EndPosition = m.EndPosition,
                    Axis = m.Axis,
                    Distance = m.Distance,
                    IsStart = m.IsStart,
                    IsEnd = m.IsEnd,
                    MoveNo = m.MoveNo,
                    GroupNo= m.GroupNo,
                    XAxisPosition = m.XAxisPosition,
                    YAxisPosition = m.YAxisPosition,
                    ZAxisPosition = m.ZAxisPosition,
                    AAxisPosition = m.AAxisPosition,
                    CAxisPosition = m.CAxisPosition,
                    StartValuePosition=m.StartValuePosition,
                    EndValuePosition = m.EndValuePosition,
                    SafeX = m.SafeX,
                    IsUse=m.IsUse,

                }).ToList();


               
                //var options = new JsonSerializerOptions { WriteIndented = true };
                //var json = JsonSerializer.Serialize(dto, options);
                //File.WriteAllText(SaveFileName, json);
                //MessageBox.Show("保存成功！");

                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(dto, options);
                File.WriteAllText(SaveFileName, json, Encoding.UTF8);
                MessageBox.Show("保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}");
            }
        }

        private void LoadFromFile()
        {
            try
            {
                if (!File.Exists(SaveFileName))
                {
                    MessageBox.Show("没有找到保存文件，或者尚未保存任何数据。");
                    return;
                }

                var json = File.ReadAllText(SaveFileName);
                var dtoList = JsonSerializer.Deserialize<List<MoveItemDto>>(json);
                if (dtoList == null) return;

                MoveItems.Clear();

                // 把 DTO 重新包装成 MoveItem（新对象要注入 Parent，让互斥逻辑生效）
                foreach (var dto in dtoList)
                {
                    var newItem = new MoveItem(MoveItems)
                    {
                        Index = dto.Index,
                        StartPosition = dto.StartPosition,
                        EndPosition = dto.EndPosition,
                        Axis = dto.Axis,
                        Distance = dto.Distance,
                        MoveNo=dto.MoveNo,
                        GroupNo=dto.GroupNo,
                        XAxisPosition=dto.XAxisPosition,
                        YAxisPosition = dto.YAxisPosition,
                        ZAxisPosition = dto.ZAxisPosition,
                        AAxisPosition = dto.AAxisPosition,
                        CAxisPosition = dto.CAxisPosition,
                        StartValuePosition=dto.StartValuePosition,
                        EndValuePosition=dto.EndValuePosition,
                        SafeX =dto.SafeX,
                        IsUse = dto.IsUse,
                        // 注意：不要直接赋内部字段，调用属性，从而保证互斥/通知逻辑运行
                    };

                    // 先添加到集合（Parent 已存在），再设置 IsStart/IsEnd 保证互斥逻辑能正确运作
                    MoveItems.Add(newItem);

                    // 设置后会触发互斥逻辑（如果有），并触发 PropertyChanged
                    newItem.IsStart = dto.IsStart;
                    newItem.IsEnd = dto.IsEnd;
                }

                if(dtoList.Count>0)
                {
                    SafeX = dtoList.Where(d=>d.SafeX!=null&&d.SafeX!="").FirstOrDefault().SafeX;
                }
              
                // 重新编号确保 Index 连续（可选）
                for (int i = 0; i < MoveItems.Count; i++)
                    MoveItems[i].Index = i + 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败：{ex.Message}");
            }
        }

        private  void LoadFromFocus()
        {
            try
            {
                if (!File.Exists(DataFile)) return;
                var json = File.ReadAllText(DataFile);
                var container = JsonSerializer.Deserialize<SaveDataContainer>(json);
                if (container == null) return;

                FocusPoint = new ObservableCollection<string>();
                Masters.Clear();
                Details.Clear();
                DisItems.Clear();
                if (container.Masters != null)
                    foreach (var m in container.Masters) Masters.Add(m);

                if (container.Details != null)
                    foreach (var d in container.Details)
                    {
                        if (d.IsFocus)
                        {
                            FocusPoint.Add(d.MarkPosition.ToString());
                        }
                        Details.Add(d);
                    }
            }
            catch (Exception ex)
            {
                // 读取失败时仅输出调试信息，不阻止程序
                System.Diagnostics.Debug.WriteLine("数据加载 failed: " + ex.Message);
            }
        }
    }

   

}
public class SaveDataContainer
{
    public System.Collections.Generic.List<MasterItem> Masters { get; set; }
    public System.Collections.Generic.List<DetailItem> Details { get; set; }

    public System.Collections.Generic.List<DisItem> DisItems { get; set; }

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
public class MoveItem : BindableBase
{
    // Parent 不参与序列化，且用于互斥逻辑
    [JsonIgnore]
    public ObservableCollection<MoveItem> Parent { get; set; }

    // 无参构造，供序列化/反序列化使用
    public MoveItem() { }

    // 常规构造，新增时使用
    public MoveItem(ObservableCollection<MoveItem> parent)
    {
        Parent = parent;
    }

    private bool _isStart;
    public bool IsStart
    {
        get => _isStart;
        set
        {
            // SetProperty 会自动 raise PropertyChanged；返回 true 表示值发生变化
            if (SetProperty(ref _isStart, value))
            {
                // 互斥：当某项被设置为 true，则把 Parent 中其他项的 IsStart 设为 false
                if (value && Parent != null)
                {
                    foreach (var item in Parent.Where(x => x != this && x._isStart).ToList())
                    {
                        // 直接用属性以触发通知
                        item.IsStart = false;
                    }
                }
            }
        }
    }

    private bool _isUse;
    public bool IsUse
    {
        get => _isUse;
        set
        {
            // SetProperty 会自动 raise PropertyChanged；返回 true 表示值发生变化
            SetProperty(ref _isUse, value);
        }
    }

    private bool _isEnd;
    public bool IsEnd
    {
        get => _isEnd;
        set
        {
            if (SetProperty(ref _isEnd, value))
            {
                if (value && Parent != null)
                {
                    foreach (var item in Parent.Where(x => x != this && x._isEnd).ToList())
                    {
                        item.IsEnd = false;
                    }
                }
            }
        }
    }

    private int _index;
    public int Index { get => _index; set => SetProperty(ref _index, value); }

    private string _startPosition;
    public string StartPosition { get => _startPosition; set => SetProperty(ref _startPosition, value); }

    private string _endPosition;
    public string EndPosition { get => _endPosition; set => SetProperty(ref _endPosition, value); }

    private string _axis;
    public string Axis { get => _axis; set => SetProperty(ref _axis, value); }

    private string _distance;
    public string Distance { get => _distance; set => SetProperty(ref _distance, value); }


    private string _moveNo;
    public string MoveNo { get => _moveNo; set => SetProperty(ref _moveNo, value); }


    private string _groupNo;
    public string GroupNo { get => _groupNo; set => SetProperty(ref _groupNo, value); }


    private string _xAxisPosition;
    public string XAxisPosition { get => _xAxisPosition; set => SetProperty(ref _xAxisPosition, value); }

    private string _yAxisPosition;
    public string YAxisPosition { get => _yAxisPosition; set => SetProperty(ref _yAxisPosition, value); }


    private string _zAxisPosition;
    public string ZAxisPosition { get => _zAxisPosition; set => SetProperty(ref _zAxisPosition, value); }


    private string _safeX;
    public string SafeX { get => _safeX; set => SetProperty(ref _safeX, value); }

    private string _aAxisPosition;
    public string AAxisPosition { get => _aAxisPosition; set => SetProperty(ref _aAxisPosition, value); }

    private string _cAxisPosition;
    public string CAxisPosition { get => _cAxisPosition; set => SetProperty(ref _cAxisPosition, value); }


    private string _startValuePosition;
    public string StartValuePosition { get => _startValuePosition; set => SetProperty(ref _startValuePosition, value); }


    private string _endValuePosition;
    public string EndValuePosition { get => _endValuePosition; set => SetProperty(ref _endValuePosition, value); }



}

// DTO 用于序列化（不包含 Parent / 不包含事件）
public class MoveItemDto
{
    public int Index { get; set; }
    public string StartPosition { get; set; }
    public string EndPosition { get; set; }
    public string Axis { get; set; }
    public string Distance { get; set; }

    public string MoveNo { get; set; }

    public string GroupNo { get; set; }



    public bool IsStart { get; set; }
    public bool IsEnd { get; set; }

    public bool IsUse { get; set; }


    public string XAxisPosition { get; set; }
    public string YAxisPosition { get; set; }
    public string ZAxisPosition { get; set; }

    public string AAxisPosition { get; set; }
    public string CAxisPosition { get; set; }

    public string StartValuePosition { get; set; }

    public string EndValuePosition { get; set; }
    public string SafeX { get; set; }
}