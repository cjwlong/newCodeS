using ACS.SPiiPlusNET;
using IronPython.Runtime;
using Machine.Interfaces;
using Machine.Models;
using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SharedResource.events;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Services;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace Machine.ViewModels
{
    internal class AxisPanelUserControlViewModel : BindableBase
    {
        public ObservableCollection<string> Positions { get; set; }=new() { "接触式位移传感器", "聚焦透镜", "相机镜头", "激光位移传感器" };


        //private ObservableCollection<string> _positions = new();

        //public ObservableCollection<string> Positions
        //{
        //    get => _positions;
        //    set
        //    {
        //        _positions = value;
        //        OnPropertyChanged();
        //    }
        //}

        public ObservableCollection<string> PositionsType { get; set; } = new() { "当前位置"};

        private string _startPosition;
        public string StartPosition
        {
            get => _startPosition;
            set
            {
                if (_startPosition == value)
                    return;

                _startPosition = value;

                // ⭐ 你要触发的代码
                OnStartPositionChanged(value);

                OnPropertyChanged();
            }
        }

        private string _zPosition;
        public string ZPosition
        {
            get => _zPosition;
            set
            {
                if (_zPosition == value)
                    return;

                _zPosition = value;
            }
        }

        private string _zXPosition;
        public string ZXPosition
        {
            get => _zXPosition;
            set
            {
                if (_zXPosition == value)
                    return;

                _zXPosition = value;
            }
        }

        private string _zYPosition;
        public string ZYPosition
        {
            get => _zYPosition;
            set
            {
                if (_zYPosition == value)
                    return;

                _zYPosition = value;
            }
        }


        private string _zAPosition;
        public string ZAPosition
        {
            get => _zAPosition;
            set
            {
                if (_zAPosition == value)
                    return;

                _zAPosition = value;
            }
        }

        private string _zCPosition;
        public string ZCPosition
        {
            get => _zCPosition;
            set
            {
                if (_zCPosition == value)
                    return;

                _zCPosition = value;
            }
        }

        private string _currentPosition;
        public string CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition == value)
                    return;

                _currentPosition = value;

                // ⭐ 你要触发的代码
                OnStartTypePositionChanged(value);

                OnPropertyChanged();
                //bool iscurrent = _currentPosition.Trim() != "零点位置";
                //IsSecondEnabled = iscurrent;
            }
        }

        private bool _isSecondEnabled=true;
        public bool IsSecondEnabled
        {
            get => _isSecondEnabled;
            set
            {
                _isSecondEnabled = value;
                OnPropertyChanged();
            }
        }


        double currentXPos = 0.0;
        double currentYPos = 0.0;
        double currentZPos = 0.0;
        double currentAPos = 0.0;
        double currentCPos = 0.0;

        private void OnStartTypePositionChanged(string typeValue)
        {
            //if (typeValue != "零点位置")
            //{
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        Positions.Clear();
            //        Positions.Add("接触式位移传感器");
            //        Positions.Add("聚焦透镜");
            //        Positions.Add("相机镜头");
            //        Positions.Add("激光位移传感器");
            //    });
            //}
            //else
            //{
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        Positions.Clear();
            //        Positions.Add("接触式位移传感器");
            //    });
            //}
           
        }
        private void OnStartPositionChanged(string newValue)
        {
            // 示例：日志 / 校验 / 通知 / 业务逻辑
            Console.WriteLine($"StartPosition 被设置为: {newValue}");

           
            if(CurrentPosition== "零点位置")
            {
                var spos = moveItemList.Where(d => d.StartPosition == "接触式位移传感器");
                if (spos != null)
                {
                    ZXPosition = spos.FirstOrDefault().XAxisPosition;
                    ZYPosition = spos.FirstOrDefault().YAxisPosition;
                    ZPosition = spos.FirstOrDefault().ZAxisPosition;
                    ZCPosition= spos.FirstOrDefault().CAxisPosition;
                    ZAPosition = spos.FirstOrDefault().ZAxisPosition;
           
                }
            }
            else
            {
                currentXPos = Axes.Where(a => a.Name == "X").FirstOrDefault().Position;
                currentYPos = Axes.Where(a => a.Name == "Y").FirstOrDefault().Position;
                currentZPos = Axes.Where(a => a.Name == "Z").FirstOrDefault().Position;
                currentAPos = Axes.Where(a => a.Name == "A").FirstOrDefault().Position;
                currentCPos = Axes.Where(a => a.Name == "C").FirstOrDefault().Position;
            }
            // 比如：解析坐标
            // UpdateStartPoint(newValue);

            // 比如：联动其他属性
            // EndPosition = CalculateEnd(newValue);
        }

        private string _endPosition;
        public string EndPosition { get => _endPosition; set { _endPosition = value; OnPropertyChanged(); } }

        List<MoveItemDto> moveItemList=new List<MoveItemDto>();

        private readonly IEventAggregator eventAggregator;
        public AxisPanelUserControlViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();
            dialogService = containerProvider.Resolve<IDialogService>();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            Axes = MachineVM.Axes;
            foreach (var axis in Axes)
                axis.LoadTemp();
            string SaveFileName = Path.Combine(ConfigStore.StoreDir, "MoveItems.json");
            var json = File.ReadAllText(SaveFileName);
            var dtoList = JsonSerializer.Deserialize<List<MoveItemDto>>(json);
            if (dtoList == null) return;
            moveItemList=dtoList;

        }


   
        
       

        private IContainerProvider containerProvider;
        private IDialogService dialogService;

        public MachineViewModel MachineVM { get; set; }
        private readonly DispatcherTimer _refreshTimer;

        public ObservableCollection<AxisViewModel> Axes { get;  set; }

        private bool _isAllSelected;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    foreach (var axis in Axes)
                        axis.IsSelected = value;
                }
                SetProperty(ref _isAllSelected, value);
            }
        }

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage;
            set { SetProperty(ref statusMessage, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private DelegateCommand _homeSelectedCommand;
        public DelegateCommand HomeSelectedCommand => _homeSelectedCommand ??
            (_homeSelectedCommand = new DelegateCommand(HomeAll));

        private DelegateCommand _saveSelectedCommand;
        public DelegateCommand SaveSelectedCommand => _saveSelectedCommand ??
            (_saveSelectedCommand = new DelegateCommand(SaveSelected));


        private DelegateCommand _moveSelectedCommand;
        public DelegateCommand MoveSelectedCommand => _moveSelectedCommand ??
            (_moveSelectedCommand = new DelegateCommand(MoveCommand)); 


        private async void MoveCommand()
        {
            List<MoveItemDto> BeforeStartList;
            List<MoveItemDto> StartList;
            List<MoveItemDto> EndList;
            double totalx = 0;
            double totaly = 0;
            double totalz = 0;
            double totala = 0;
            double totalc= 0; 
                
            var spos = moveItemList.Where(d => d.StartPosition == StartPosition && d.EndPosition == EndPosition);
            if (spos.Any())
            {
                StartList = spos.ToList().OrderBy(d=>d.MoveNo).ToList();
                if (CurrentPosition == "零点位置")
                {
                    var spos1 = moveItemList.Where(d => d.StartPosition == "接触式位移传感器" && d.EndPosition == StartPosition);
                    if (spos1.Any())
                    {
                        BeforeStartList = spos1.ToList().OrderBy(d => d.MoveNo).ToList();
                        foreach (var item in BeforeStartList)
                        {
                            if (item.Axis.Contains("X"))
                            {
                                totalx += Convert.ToDouble(item.Distance);
                            }
                            if (item.Axis.Contains("Y"))
                            {
                                totaly += Convert.ToDouble(item.Distance);
                            }
                            if (item.Axis.Contains("Z"))
                            {
                                totalz += Convert.ToDouble(item.Distance);
                            }
                            if (item.Axis.Contains("A"))
                            {
                                totala+= Convert.ToDouble(item.Distance);
                            }
                            if (item.Axis.Contains("C"))
                            {
                                totalc += Convert.ToDouble(item.Distance);
                            }
                        }

                        totalx += Convert.ToDouble(ZXPosition);
                        totaly += Convert.ToDouble(ZYPosition);
                        totalz += Convert.ToDouble(ZPosition);
                    }


                }
                //double currentXPos = Axes.Where(a => a.Name == "X").FirstOrDefault().Position;  ZAxisPosition
                //double currentYPos = Axes.Where(a => a.Name == "Y").FirstOrDefault().Position;
                //double currentZPos = Axes.Where(a => a.Name == "Z").FirstOrDefault().Position;
                //double currentAPos = Axes.Where(a => a.Name == "A").FirstOrDefault().Position;
                //double currentCPos = Axes.Where(a => a.Name == "C").FirstOrDefault().Position;

                eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new("X", -140, true));
                await WaitAxisPositionAsync("X", -140, 0.001);
                await Task.Delay(150);
                double currentPos = 0.0;
                foreach (var item in StartList)
                {
                  
                    if (Axes.Where(a => a.Name == item.Axis).Any())
                    {
                        switch (item.Axis)
                        {
                            case "X":
                                if (CurrentPosition == "零点位置")
                                {
                                    currentXPos = totalx;

                                }
                                currentPos = currentXPos;
                                break;
                            case "Y":
                                if (CurrentPosition == "零点位置")
                                {
                                    currentYPos = totaly;

                                }
                                currentPos = currentYPos;
                                break;
                            case "Z":
                                if (CurrentPosition == "零点位置")
                                {
                                    currentZPos = totalz;

                                }
                                currentPos = currentZPos;
                                break;
                            case "A":
                                if (CurrentPosition == "零点位置")
                                {
                                    currentAPos = totala;

                                }
                                currentPos = currentAPos;
                                break;
                            case "C":
                                if (CurrentPosition == "零点位置")
                                {
                                    currentCPos = totalc;

                                }
                                currentPos = currentCPos;
                                break;
                        }
                        eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(item.Axis, currentPos+Convert.ToDouble(item.Distance), true));

                        await WaitAxisPositionAsync(item.Axis, currentPos + Convert.ToDouble(item.Distance), 0.003);
                        await Task.Delay(150);
                    }
                  
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
        private void SaveSelected()
        {
            // 1. 先还原所有未选中项的临时修改
            var unselected = Axes.Where(a => !a.IsSelected);
            foreach (var axis in unselected)
                axis.RestoreOriginal();

            // 2. 再保存所有已选中项
            var selected = Axes.Where(a => a.IsSelected);
            foreach (var axis in selected)
                axis.SaveTemp();

            // 3. 最后刷新所有行的临时字段，使界面值回到“源值”（主字段）状态
            foreach (var axis in Axes)
            {
                axis.LoadTemp();
            }

            // 4. 更新状态提示
            StatusMessage = selected.Any()
                ? "修改已保存。"
                : "未选择任何项，所有修改已被还原。";
        }


        //private void HomeSelected()
        //{
        //    var selected = Axes.Where(a => a.IsSelected);
        //    bool need_z = false;

        //    string axis_name = "";
        //    foreach (var axis in selected)
        //    {
        //        if (axis.Name == "Z") need_z = true;
        //        axis_name += axis.Name;
        //    }

        //    if (!need_z && Axes[2].IsHomeed == false)
        //    {
        //        MessageBox.Show("请先回零Z轴");
        //        return;
        //    }

        //    dialogService.ShowDialog("ConfirmBox", new DialogParameters($"message=确认是否回零{axis_name}轴"), r =>
        //    {                
        //        if (r.Result == ButtonResult.OK)
        //        {
        //            if (need_z)
        //            {
        //                Axes[2].Home();
        //                LoggingService.Instance.LogInfo("Z轴回零");
        //            }
        //            foreach (var axis in selected)
        //            {
        //                if (axis.Name == "Z" && axis.IsHomeed) continue;
        //                axis.Home();
        //                LoggingService.Instance.LogInfo($"{axis.Name}轴回零");
        //            }
        //        }
        //    });            
        //}

        private void HomeAll()
        {
            dialogService.ShowDialog("ConfirmBox", new DialogParameters($"message=确认是否回零"), r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    MachineVM.HomeAll();
                }
            });
        }
    }

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

        public string XAxisPosition { get; set; }
        public string YAxisPosition { get; set; }
        public string ZAxisPosition { get; set; }

        public string AAxisPosition { get; set; }
        public string CAxisPosition { get; set; }
        

        public string SafeX { get; set; }
    }
}
