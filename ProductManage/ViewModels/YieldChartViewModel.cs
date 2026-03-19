using DiastimeterManager.libs;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Machine.Interfaces;
using Machine.ViewModels;
using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using ProductManage.libs;
using ProductManage.ViewModels;
using ServiceManager;
using SharedResource.events.Machine;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using static IronPython.Modules._ast;
using static IronPython.Runtime.Profiler;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using static System.Reflection.Metadata.BlobBuilder;

namespace ProductManage.ViewModels
{
    //public class YieldChartViewModel : BindableBase
    //{
    //    public LGQuick mitutoyo_EJ_Ranger { get; set; }

    //    public  MachineVM { get; set; }

    //    private System.Timers.Timer _readTimer;

    //    private bool isZ = false;
    //    public YieldChartViewModel(IContainerProvider provider)
    //    {
    //        InitTimer();
    //        containerProvider = provider;
    //        eventAggregator = containerProvider.Resolve<IEventAggregator>();
    //        productStatistics = containerProvider.Resolve<ProductStatistics>();
    //        InitializeChartData();
    //        MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();
    //        mitutoyo_EJ_Ranger = containerProvider.Resolve<LGQuick>();
    //        eventAggregator.GetEvent<MeasurementEvent>().Subscribe(() =>
    //        {
    //            Values.Clear();//数据
    //            Labels.Clear(); //横坐标

    //            var axn = MachineVM.Axes.Where(d => d.Name.ToUpper() == "Z");
    //            if (axn.Any())
    //            {
    //                var axPosition = axn.FirstOrDefault().Position;
    //                isZ = true;
    //                //z位置
    //            }
    //            _readTimer.Start();
    //            //double  ddd= mitutoyo_EJ_Ranger.LQData;
    //            //foreach (var item in mitutoyo_EJ_Ranger)
    //            //{
    //            //    Labels.Add(item.Key);
    //            //    Values.Add(item.Value);
    //            //}
    //        });

    //    }


    //    private void InitTimer()
    //    {
    //        _readTimer = new System.Timers.Timer(100);
    //        _readTimer.Elapsed += ReadTimerCallback;
    //        _readTimer.AutoReset = true;
    //    }

    //    private static object _Lock = new object();
    //    double j = 0;
    //    double k = 0;
    //    private void ReadTimerCallback(object sender, ElapsedEventArgs e)
    //    {
    //        try
    //        {
    //            double readValue;
    //            lock (_Lock)
    //            {
    //                j++;
    //                k++;
    //                readValue = mitutoyo_EJ_Ranger.LQData;
    //                Labels.Add(readValue.ToString());
    //                var axn = MachineVM.Axes.Where(d => d.Name.ToUpper() == "Z");
    //                if (isZ)
    //                {
    //                    var axPosition = axn.FirstOrDefault().Position;
    //                    Values.Add(axPosition);
    //                    // Values.Add(j++);
    //                    //z位置
    //                }


    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            LoggingService.Instance.LogError("定时读取失败", ex);
    //        }
    //    }

    //    private readonly IContainerProvider containerProvider;
    //    private readonly IEventAggregator eventAggregator;
    //    ProductStatistics productStatistics;

    //    private SeriesCollection _seriesCollection;
    //    public SeriesCollection SeriesCollection
    //    {
    //        get => _seriesCollection;
    //        set => SetProperty(ref _seriesCollection, value);
    //    }

    //    private ObservableCollection<double> _values = new ObservableCollection<double>();
    //    public ObservableCollection<double> Values
    //    {
    //        get => _values;
    //        set => SetProperty(ref _values, value);
    //    }

    //    private ObservableCollection<string> _labels = new ObservableCollection<string>();
    //    public ObservableCollection<string> Labels
    //    {
    //        get => _labels;
    //        set => SetProperty(ref _labels, value);
    //    }

    //    private void InitializeChartData()
    //    {
    //        var dailyData = productStatistics.GetLast15DaysProductionData();

    //        Values.Clear();
    //        Labels.Clear();
    //        int i = 0;
    //        foreach (var item in dailyData)
    //        {
    //            i++;
    //            Labels.Add(item.Key);
    //            Values.Add(i);
    //        }



    //        SeriesCollection = new SeriesCollection
    //        {
    //            new LineSeries
    //            {
    //                Values = new ChartValues<double>(Values),
    //                Title = "接触式位移传感器数据",
    //                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DAA520")),
    //                Fill = new LinearGradientBrush
    //                {
    //                    StartPoint = new System.Windows.Point(0, 0),
    //                    EndPoint = new System.Windows.Point(0, 1),
    //                    GradientStops = new GradientStopCollection
    //                    {
    //                        new GradientStop((Color)ColorConverter.ConvertFromString("#CD853F"), 0),
    //                        new GradientStop(Colors.Transparent, 1)
    //                    }
    //                },
    //                PointGeometrySize = 10
    //            }
    //        };
    //    }




    //}

    public class YieldChartViewModel : BindableBase
    {
        private bool _isTesting;
        public bool IsTesting
        {
            get => _isTesting;
            set => SetProperty(ref _isTesting, value);
        }
        // =============================
        // 采样数据（Z = 横坐标, Sensor = 纵坐标）
        // =============================
        public LGQuick mitutoyo_EJ_Ranger { get; set; }

        public ObservableCollection<double> ZValues { get; set; } = new();
        public ObservableCollection<double> SensorValues { get; set; } = new();

        public ObservableCollection<string> Labels { get; set; } = new();
        public SeriesCollection SeriesCollection { get; set; }

        // 采样定时器
        private Timer _timer;

        // 假设你的 Z 轴 和 传感器对象已经注入
        private MachineViewModel MachineVM;
        private LGQuick Sensor;

        ProductStatistics productStatistics;

        // =============================
        // 按钮命令
        // =============================
        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }

        public DelegateCommand CalculateCommand { get; }
        
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand ExportCsvCommand { get; }
        public DelegateCommand ExportExcelCommand { get; }

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;



        public List<int> KeyIndices { get; private set; } = new List<int>();

        // Y轴范围和刻度


        public double YMin { get; set; } = 0;   // 初始值可以设为 0 或你期望的最小值
        public double YMax { get; set; } = 1;
        public List<double> YTicks { get; private set; } = new List<double>();



        public Axis YAxis { get; set; } = new Axis();

        private ObservableCollection<Point> _chartPoints;
        public ObservableCollection<Point> ChartPoints
        {
            get => _chartPoints;
            set => SetProperty(ref _chartPoints, value);
        }


        private List<PointXY> _chartOldPoints;
        public List<PointXY> ChartOldPoints
        {
            get => _chartOldPoints;
            set => SetProperty(ref _chartOldPoints, value);
        }

        public YieldChartViewModel(IContainerProvider provider)
        {
            // 模拟注入（你项目里请自动注入）
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            productStatistics = containerProvider.Resolve<ProductStatistics>();
       
            MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();
            mitutoyo_EJ_Ranger = containerProvider.Resolve<LGQuick>();
           // Sensor = new LGQuick();

            //InitChart();
            InitTimer();
            ChartPoints = new ObservableCollection<Point>();

            //for (int i = 0; i < 50; i++)
            //{
            //    double yValue = (Math.Sin(i * 0.2) * 10 + 20) * (-1);
            //    ChartPoints.Add(new Point(i, yValue));
            //   // ChartPoints.Add(new Point(i, (Math.Sin(i * 0.2) * 10 + 20)));
            //}
            StartCommand = new DelegateCommand(StartSampling);
            StopCommand = new DelegateCommand(StopSampling);
            CalculateCommand = new DelegateCommand(CalculateSampling);
            ClearCommand = new DelegateCommand(ClearData);
            ExportCsvCommand = new DelegateCommand(ExportCsv);
            ExportExcelCommand = new DelegateCommand(ExportExcel);
        }

        // =============================
        // 图表初始化
        // =============================
        //private void InitChart()
        //{
        //    var dailyData = productStatistics.GetLast15DaysProductionData();

        //    ZValues.Clear();
        //    SensorValues.Clear();
        //    double i = 0;
        //    double j = 0;
        //    foreach (var item in dailyData)
        //    {
        //        i+=0.1;
        //        j += 0.1;
        //        SensorValues.Add(j);
        //        ZValues.Add(i);
        //    }
        //    Labels = new ObservableCollection<string>();

        //    //////    SeriesCollection = new SeriesCollection
        //    //////{
        //    //////    new LineSeries
        //    //////    {
        //    //////        Values = new ChartValues<double>(),
        //    //////        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DAA520")),
        //    //////        Fill = Brushes.Transparent,
        //    //////        PointGeometrySize = 5 // 默认隐藏圆点
        //    //////    }
        //    //////};
        //    //            SeriesCollection = new SeriesCollection
        //    //{
        //    //    new LineSeries
        //    //    {
        //    //        Values = new ChartValues<double>(SensorValues),
        //    //        Stroke = Brushes.Orange,
        //    //        Fill = Brushes.Transparent,
        //    //        PointGeometrySize = 5 // 圆点大小
        //    //        // 不设置 Title
        //    //    }
        //    //};
        //    //SeriesCollection = new SeriesCollection
        //    //{
        //    //    new LineSeries
        //    //    {
        //    //        Values = new ChartValues<double>(),
        //    //        Title = "接触式位移传感器示数",
        //    //        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DAA520")),
        //    //        Fill = Brushes.Transparent,
        //    //        PointGeometrySize = 5
        //    //    }
        //    //};
        //    var line = new LineSeries
        //    {
        //        Values = new ChartValues<double>(SensorValues),
        //        Stroke = Brushes.Orange,
        //        Fill = Brushes.Transparent,
        //        PointGeometrySize = 5,
        //        Title = "传感器示数",
        //    };

        //    // 关键：禁用默认 Tooltip 内容

        //    SeriesCollection = new SeriesCollection { line };

        //    Labels.Clear();
        //    foreach (var z in ZValues)
        //        Labels.Add(z.ToString("F2"));

        //    var series = SeriesCollection[0].Values;
        //    series.Clear();
        //    foreach (var s in SensorValues)
        //        series.Add(s);

        //}
        private void InitChart()
        {
            var points = new ChartValues<ObservablePoint>();

            double z = 0.0;
            double sensor = 0.0;

            for (int i = 0; i < 30; i++)
            {
                z += 1;        // Z轴
                sensor += 8; // 传感器值
                points.Add(new ObservablePoint(z, sensor));
            }

            // ===== Series =====
            var line = new LineSeries
            {
                Title = "传感器示数",
                Values = points,

                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,

                PointGeometrySize = 6,
                LineSmoothness = 0
            };

            SeriesCollection = new SeriesCollection { line };

            // ===== Y轴范围 =====
            YMin = points.Min(p => p.Y) - 0.1;
            YMax = points.Max(p => p.Y) + 0.1;
        }
        // =============================
        // 定时器初始化
        // =============================
        private void InitTimer()
        {
            _timer = new Timer(100); // 每 50ms 采样
            _timer.Elapsed += Timer_Tick;
            _timer.AutoReset = true;
        }
        private readonly object _valueLock = new object();
        // =============================
        // 采样逻辑
        // =============================
        private void Timer_Tick(object sender, ElapsedEventArgs e)
        {
            //try
            //{
            //    // 获取 Z 轴
            //    double zValue = MachineVM.Axes.FirstOrDefault(a => a.Name.ToUpper() == "Z")?.Position ?? 0;

            //    // 获取 sensor
            //    double sensorValue = mitutoyo_EJ_Ranger.LGQuickValue;

            //    ZValues.Add(zValue);
            //    SensorValues.Add(sensorValue);
            //}
            //catch { }

            double zValue = 0;
            double sensorValue = 0;

            try
            {
                Parallel.Invoke(
                    () =>
                    {
                        var zAxis = MachineVM.Axes.FirstOrDefault(a =>
                            a.Name.Equals("C", StringComparison.OrdinalIgnoreCase));
                        zValue = zAxis?.Position ?? 0;
                    },
                    () =>
                    {
                        sensorValue = mitutoyo_EJ_Ranger.LGQuickValue;
                    }
                );

                lock (_valueLock)
                {
                    ZValues.Add(zValue);
                    SensorValues.Add(sensorValue);
                }
            }
            catch { }
        }

        // =============================
        // 按钮：开始采样
        // =============================
        private void StartSampling()
        {
            ClearData();
            IsTesting = true;
            _timer.Start();
            // ZValues = new ObservableCollection<double>();
            //double[] zMock = { 0, 1.2, 6.8, 49.9, 50.1, 60.2, 99.7, 100.3 };
            //double[] sMock = { 0.1, 0.2, 0.3, 0.5, 0.7, 0.6, 0.9, 0.11 };

            //ZValues.Clear();
            //SensorValues.Clear();
            //foreach (var v in zMock) ZValues.Add(v);
            //foreach (var v in sMock) SensorValues.Add(v);

          
        }

        /// <summary>
        /// X轴 = 旋转角度 Angle，Y轴 = Depth（槽深）

//        槽峰谷识别：连续槽点找最低点

//        左右球面值：

//平均值或峰值可选

//槽深计算：平均左右球面减去槽点

//过渡区处理：左/右球面减去过渡点 Raw

//球面区域：Depth = 0

//多槽处理：支持整圈多槽
        /// </summary>
        /// <param name="data"></param>
        private void GetData(List<MeasurePoint> data)
        {
            double surfaceThreshold = 1.7;
            double slotUpper = 2.0;
            double slotLower = 1.5;

            // 1️⃣ 分类球面 / 槽 / 过渡
            foreach (var p in data)
            {
                if (p.Raw >= surfaceThreshold) p.IsSurface = true;
                else if (p.Raw < slotUpper && p.Raw > slotLower) p.IsSlot = true;
                else p.IsTransition = true;
            }

            // 2️⃣ 识别槽段并找最低点（峰谷）
            List<SlotInfo> slots = new List<SlotInfo>();
            int? slotStart = null;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].IsSlot)
                {
                    if (slotStart == null) slotStart = i;
                }
                else
                {
                    if (slotStart != null)
                    {
                        AddSlotInfo(data, slots, slotStart.Value, i - 1);
                        slotStart = null;
                    }
                }
            }
            if (slotStart != null)
                AddSlotInfo(data, slots, slotStart.Value, data.Count - 1);

            for (int i = 0; i < data.Count; i++)
            {
                var p = data[i];

                if (!p.IsSlot && !p.IsTransition)
                {
                    p.Depth = 0;
                    p.LeftSurface = p.Raw;
                    p.RightSurface = p.Raw;
                    continue;
                }

                // 左球面点集合
                var leftSurfaces = new List<double>();
                for (int j = i; j >= 0; j--)
                    if (data[j].IsSurface) leftSurfaces.Add(data[j].Raw);

                // 右球面点集合
                var rightSurfaces = new List<double>();
                for (int j = i; j < data.Count; j++)
                    if (data[j].IsSurface) rightSurfaces.Add(data[j].Raw);

                // 策略选择：峰值或平均值
                double leftPeak = leftSurfaces.Count > 0 ? leftSurfaces.Max() : 2.0;
                double rightPeak = rightSurfaces.Count > 0 ? rightSurfaces.Max() : 2.0;

                double leftAvg = leftSurfaces.Count > 0 ? leftSurfaces.Average() : 2.0;
                double rightAvg = rightSurfaces.Count > 0 ? rightSurfaces.Average() : 2.0;

                // 计算槽深
                if (p.IsSlot)
                {
                    // 这里默认使用平均值策略
                    p.LeftSurface = leftAvg;
                    p.RightSurface = rightAvg;
                    p.Depth = ((p.LeftSurface - p.Raw) + (p.RightSurface - p.Raw)) / 2.0;
                }
                else if (p.IsTransition)
                {
                    double midSurface = (leftAvg + rightAvg) / 2.0;
                    p.Depth = p.Raw < midSurface ? (leftAvg - p.Raw) : (rightAvg - p.Raw);
                    p.LeftSurface = leftAvg;
                    p.RightSurface = rightAvg;
                }
            }

            // 4️⃣ 输出槽段峰谷信息
            Console.WriteLine("槽段信息：StartIndex\tEndIndex\tMinRaw\tMinIndex");
            foreach (var s in slots)
            {
                Console.WriteLine($"{s.StartIndex}\t{s.EndIndex}\t{s.MinRaw:F3}\t{s.MinIndex}");
            }

            // 5️⃣ 输出整条曲线数据，X=Angle, Y=Depth
            Console.WriteLine("\nAngle\tRaw\tType\tLeftSurface\tRightSurface\tDepth");
            foreach (var p in data)
            {
                string type = p.IsSurface ? "Surface" : p.IsSlot ? "Slot" : "Transition";
                Console.WriteLine($"{p.Angle:F2}\t{p.Raw:F2}\t{type}\t{p.LeftSurface:F2}\t{p.RightSurface:F2}\t{p.Depth:F3}");
            }

            // ✅ depthCurve depthCurve 可以直接绘图：X=data[i].Angle, Y=data[i].Depth
        }
        void AddSlotInfo(List<MeasurePoint> data, List<SlotInfo> slots, int start, int end)
        {
            double minVal = data.Skip(start).Take(end - start + 1).Min(d => d.Raw);
            int minIndex = start + data.Skip(start).Take(end - start + 1)
                .Select(d => d.Raw).ToList().IndexOf(minVal);
            slots.Add(new SlotInfo { StartIndex = start, EndIndex = end, MinRaw = minVal, MinIndex = minIndex });
        }
        private void RefreshFullCurve()
        {
            var series = SeriesCollection[0] as LineSeries;
            series.Values.Clear();
            Labels.Clear();

            for (int i = 0; i < ZValues.Count; i++)
            {
                series.Values.Add(SensorValues[i]);
                Labels.Add(ZValues[i].ToString("F1"));
            }

            // ==============================
            // 动态计算 Y 轴范围 ±0.5
            // ==============================
            if (SensorValues.Count > 0)
            {
                double min = SensorValues.Min();
                double max = SensorValues.Max();
                YMin = min - 0.5;
                YMax = max + 0.5;

                RaisePropertyChanged(nameof(YMin));
                RaisePropertyChanged(nameof(YMax));
            }
        }
        // =============================
        // 按钮：停止采样/        var merged = MergeCloseXYPoints(rawPoints, 0.03);

        //        // 2️⃣ 找波峰 / 波谷
        //        FindPeaksAndValleys(
        //            merged,
        //            epsTrend: 0.01,
        //            peakMinY: 1.95,
        //            valleyMinY: 1.5,
        //            valleyMaxY: 2.0,
        //            out var peaks,
        //            out var valleys);

        //        // 3️⃣ 计算槽深
        //        var grooveDepths = CalculateGrooveDepths(peaks, valleys);

        //// 4️⃣ 输出
        //foreach (var g in grooveDepths)
        //{
        //    Console.WriteLine(
        //        $"槽深点: X={g.X:F3}, Depth={g.Depth:F4}");
        //}
        //
        List<GrooveDepthPoint> grooveDepths;
        //计算绘制
        private void CalculateSampling()
        {

            _timer.Stop();

            List<PointXY> rawPoints = BuildPointList(ZValues, SensorValues);
            var merged = ChartMath.MergeCloseXYPoints(
                        rawPoints,
                        yThreshold: 0.03);//去重的点

            ChartOldPoints = new List<PointXY>();
            ChartOldPoints = rawPoints; //原来的数据点展示
            return;
            //        // 2️⃣ 找波峰 / 波谷
            ChartMath.FindPeaksAndValleys(
                merged,
                epsTrend: 0.01,
                peakMinY: 1.95,
                valleyMinY: 1.5,
                valleyMaxY: 2.0,
                out var peaks,
                out var valleys);

            // 3️⃣ 计算槽深
            grooveDepths = ChartMath.CalculateGrooveDepths(peaks, valleys);
        }
        // =============================
        private void StopSampling()
        {


            ChartPoints = new ObservableCollection<Point>();
            for (int i = 0; i < ChartOldPoints.Count; i++)
            {

                ChartPoints.Add(new Point(ChartOldPoints[i].X, ChartOldPoints[i].Y));
            }

            return;
            ChartPoints = new ObservableCollection<Point>();// test
            for (int i = 0; i < 50; i++)
            {
                //double yValue = (Math.Sin(i * 0.2) * 10 + 20) * (-1);
                //ChartPoints.Add(new Point(i, yValue));
                 ChartPoints.Add(new Point(i, (Math.Sin(i * 0.2) * 10 + 20)));
            }
            return;
            //ChartPoints = new ObservableCollection<Point>();
            //for (int i = 0; i < grooveDepths.Count; i++)
            //{

            //    ChartPoints.Add(new Point(grooveDepths[i].X, grooveDepths[i].Depth));
            //}

            _timer.Stop();
            List<PointXY> rawPoints = BuildPointList(ZValues, SensorValues);
            var merged = ChartMath.MergeCloseXYPoints(
                        rawPoints,
                        yThreshold: 0.03);

            ChartOldPoints = new List<PointXY>();
            ChartOldPoints = merged; //原来的数据点展示

            //        // 2️⃣ 找波峰 / 波谷
            ChartMath.FindPeaksAndValleys(
                merged,
                epsTrend: 0.01,
                peakMinY: 1.95,
                valleyMinY: 1.5,
                valleyMaxY: 2.0,
                out var peaks,
                out var valleys);

            // 3️⃣ 计算槽深
            var grooveDepths = ChartMath.CalculateGrooveDepths(peaks, valleys);

            ChartPoints = new ObservableCollection<Point>();
            for (int i = 0; i < grooveDepths.Count; i++)
            {
               
                ChartPoints.Add(new Point(grooveDepths[i].X, grooveDepths[i].Depth));
            }

            //结束
            RefreshFullCurve();
            IsTesting = false;
            // 采样结束后一次性刷新 UI
            Labels.Clear();
            foreach (var z in ZValues)
                Labels.Add(z.ToString("F2"));

            ////var series = SeriesCollection[0].Values;
            ////series.Clear();
            ////foreach (var s in SensorValues)
            ////    series.Add(s);
            ///

            // 1️⃣ 找到关键点索引（每跨 50° 的第一个点）
            KeyIndices.Clear();
            double nextMark = 0;
            var keyIndices = new ObservableCollection<int>();
            nextMark = ZValues.First();

            for (int i = 0; i < ZValues.Count; i++)
            {
                if (i == 0)
                {
                    keyIndices.Add(i);
                    nextMark += 10; //角度
                }
                else if (ZValues[i] >= nextMark)
                {
                    keyIndices.Add(i);
                    nextMark += 10;
                }
            }

            // 2️⃣ 更新 Series（显示所有点，折线完整）
            var series = SeriesCollection[0] as LineSeries;
            series.Values.Clear();
            Labels.Clear();
            foreach (var idx in keyIndices)
            {
                series.Values.Add(SensorValues[idx]);
                Labels.Add(ZValues[idx].ToString("F1"));
            }

            //for (int i = 0; i < SensorValues.Count; i++)
            //    series.Values.Add(SensorValues[i]);

            // 3️⃣ 更新 Labels（只有关键点显示真实 Z 值）
            
            //for (int i = 0; i < ZValues.Count; i++)
            //    Labels.Add(KeyIndices.Contains(i) ? ZValues[i].ToString("F1") : "");
        }

        // 5️⃣ 设置关键点圆点可视化（Tooltip 可见）
        // series.PointGeometrySize = 0; // 中间点隐藏圆点



        public static List<PointXY> BuildPointList(
        ObservableCollection<double> xs,
        ObservableCollection<double> ys)
        {
            var result = new List<PointXY>();

            if (xs == null || ys == null)
                return result;

            int count = Math.Min(xs.Count, ys.Count);

            for (int i = 0; i < count; i++)
            {
                result.Add(new PointXY(xs[i], ys[i]));
            }

            return result;
        }
        // =============================
        // 按钮：清空数据
        // =============================
        private void ClearData()
        {
            _timer.Stop();
            ZValues.Clear();
            SensorValues.Clear();
            ChartPoints = new ObservableCollection<Point>();
            //Labels.Clear();
            //SeriesCollection[0].Values.Clear();
        }

        // =============================
        // 按钮：导出 CSV
        // =============================
        private void ExportCsv()
        {
            var path1 = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "SensorOriginalData.csv");

            ChartOldPoints = new List<PointXY>();

            using (var sr = new StreamReader(path1))
            {
                string? line;
                bool isHeader = true;

                while ((line = sr.ReadLine()) != null)
                {
                    // 跳过表头
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    var parts = line.Split(',');

                    if (parts.Length < 2)
                        continue;

                    if (double.TryParse(parts[0], out double x) &&
                        double.TryParse(parts[1], out double y))
                    {
                        ChartOldPoints.Add(new PointXY
                        {
                            X = x,
                            Y = y
                        });
                    }
                }
            }
            return;
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SensorData.csv");

             path1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SensorOriginalData.csv");

            ////using var sw = new StreamWriter(path, false);
            ////sw.WriteLine("C,Sensor");

            ////for (int i = 0; i < ZValues.Count; i++)
            ////    sw.WriteLine($"{ZValues[i]},{SensorValues[i]}"); //ChartOldPoints

            using var sw1 = new StreamWriter(path1, false);
            sw1.WriteLine("OriginalCValue,OriginalSensor");

            for (int i = 0; i < ChartOldPoints.Count; i++)
                sw1.WriteLine($"{ChartOldPoints[i].X},{ChartOldPoints[i].Y}"); //ChartOldPoints



        }

        // =============================
        // 按钮：导出 Excel
        // =============================
        private void ExportExcel()
        {
            //string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SensorData.xlsx");

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            //using var pkg = new ExcelPackage();
            //var sheet = pkg.Workbook.Worksheets.Add("Data");

            //sheet.Cells[1, 1].Value = "Z 轴值";
            //sheet.Cells[1, 2].Value = "传感器值";

            //for (int i = 0; i < ZValues.Count; i++)
            //{
            //    sheet.Cells[i + 2, 1].Value = ZValues[i];
            //    sheet.Cells[i + 2, 2].Value = SensorValues[i];
            //}

            //pkg.SaveAs(new FileInfo(path));
        }
    }


    class MeasurePoint
    {
        public double Angle; // 测量角度
        public double Raw;   // 原始传感器值
        public bool IsSlot;
        public bool IsTransition;
        public bool IsSurface;

        public double LeftSurface;  // 左球面（峰值或平均值）
        public double RightSurface; // 右球面（峰值或平均值）
        public double Depth;        // 槽深
    }

    class SlotInfo
    {
        public int StartIndex;
        public int EndIndex;
        public double MinRaw;  // 最低点
        public int MinIndex;
    }

  
    
}
