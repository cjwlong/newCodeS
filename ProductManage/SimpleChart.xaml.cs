using Newtonsoft.Json.Linq;
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
using static IronPython.Modules._ast;
using static System.Windows.Forms.LinkLabel;

namespace ProductManage
{
    /// <summary>
    /// SimpleChart.xaml 的交互逻辑
    /// </summary>
    public partial class SimpleChart : UserControl
    {
        #region ===== 可配置参数 =====

        public IEnumerable<Point> DataPoints
        {
            get => (IEnumerable<Point>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(
                nameof(DataPoints),
                typeof(IEnumerable<Point>),
                typeof(SimpleChart),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnDataChanged));

        public double YAxisPadding { get; set; } = 0.005;          // 上下 ±5微米mm单位
        public double MinXPixelPerPoint { get; set; } = 50;   // 控制滚动密度

        #endregion

        #region ===== 布局参数 =====

        private const double MarginLeft = 50;
        private const double MarginRight = 20;
        private const double MarginTop = 20;
        private const double MarginBottom = 40;

        private readonly ToolTip _toolTip = new ToolTip
        {
            StaysOpen = true,
            HorizontalOffset = 15,
            VerticalOffset = 15
        };

        #endregion

        public SimpleChart()
        {
            InitializeComponent();
            ChartCanvas.MouseWheel += ChartCanvas_MouseWheel;
        }
        private double _yAmplify = 3000;

        // 可选：限制范围，防止炸
        private const double MinAmplify = 100;
        private const double MaxAmplify = 100000;
        private void ChartCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // ⭐ 每一格滚轮的缩放比例
            double factor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;

            _yAmplify *= factor;

            // ⭐ 限制范围
            _yAmplify = Math.Max(MinAmplify, Math.Min(MaxAmplify, _yAmplify));

            //Redraw();   // ⭐ 你自己的重绘函数
            e.Handled = true;
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimpleChart chart)
                chart.InvalidateArrange();   // 通知布局系统重画
        }

        #region ===== 核心：控件级绘制 =====

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (finalSize.Width > 0 && finalSize.Height > 0)
                DrawChart(finalSize);

            return base.ArrangeOverride(finalSize);
        }

        private void DrawChart(Size size)
        {
            if (DataPoints == null || !DataPoints.Any())
                return;

            var points = DataPoints.ToList();
            ChartCanvas.Children.Clear();

            // X 方向滚动
            ChartCanvas.Width = Math.Max(
                size.Width,
                points.Count * MinXPixelPerPoint);

            ChartCanvas.Height = size.Height;

            double width = ChartCanvas.Width - MarginLeft - MarginRight;
            double height = ChartCanvas.Height - MarginTop - MarginBottom;

            //ChartCanvas.Children.Clear();

            //double availableHeight = ScrollViewerX.ActualHeight;
            //double availableWidth = ScrollViewerX.ActualWidth;

            //ChartCanvas.Height = Math.Max(availableHeight, 200);
            //ChartCanvas.Width = Math.Max(
            //    availableWidth,
            //    DataPoints.Count() * MinXPixelPerPoint
            //);

            //double width = ChartCanvas.Width - MarginLeft - MarginRight;
            //double height = ChartCanvas.Height - MarginTop - MarginBottom;

            if (width <= 0 || height <= 0)
                return;



            double xMin = points.Min(p => p.X);
            double xMax = points.Max(p => p.X);

            double yMin = points.Min(p => p.Y) - YAxisPadding;
            double yMax = points.Max(p => p.Y) + YAxisPadding;

            // y = 0 对应的 Canvas Y
            double zeroY = (yMin <= 0 && yMax >= 0)
                ? MarginTop + (yMax - 0) / (yMax - yMin) * height
                : MarginTop + height;

            DrawAxes(width, height, zeroY);
            DrawYAxisTicks_DeltaY3(yMin, yMax, height);
            DrawXAxisTicks(points,xMin, xMax, width, zeroY);
            DrawPolyline2(points, xMin, xMax, yMin, yMax, width, height);
        }

        #endregion

        #region ===== 坐标轴 =====

        private void DrawAxes(double width, double height, double zeroY)
        {
            // Y 轴
            ChartCanvas.Children.Add(new Line
            {
                X1 = MarginLeft,
                Y1 = MarginTop,
                X2 = MarginLeft,
                Y2 = MarginTop + height,
                Stroke = Brushes.Black
            });

            // X 轴（y = 0）
            ChartCanvas.Children.Add(new Line
            {
                X1 = MarginLeft,
                Y1 = zeroY,
                X2 = MarginLeft + width,
                Y2 = zeroY,
                Stroke = Brushes.Black
            });
        }

        #endregion

        #region ===== Y 轴刻度（自适应） ===== 版本1

        //private void DrawYAxisTicks(double yMin, double yMax, double height)
        //{
        //    double step = GetNiceStep(yMin, yMax);
        //    //double start = Math.Floor(yMin / step) * step;
        //    //double end = Math.Ceiling(yMax / step) * step;
        //    double start = Math.Floor(yMin / step) * step;
        //    double end = Math.Ceiling(yMax / step) * step;

        //    // ⭐ 关键：顶部 & 底部各留一个 step
        //    start -= step;
        //    end += step;
        //    for (double v = start; v <= end; v += step)
        //    {
        //        if (Math.Abs(v) < 1e-6)
        //            continue;
        //        double y = MarginTop + (yMax - v) / (yMax - yMin) * height;

        //        ChartCanvas.Children.Add(new Line
        //        {
        //            X1 = MarginLeft - 5,
        //            Y1 = y,
        //            X2 = MarginLeft,
        //            Y2 = y,
        //            Stroke = Brushes.Black
        //        });

        //        var tb = new TextBlock
        //        {
        //            Text = v.ToString("F1"),
        //            FontSize = 11
        //        };
        //        Canvas.SetLeft(tb, 0);
        //        ////if (Math.Abs(v) < 1e-6)
        //        ////{
        //        ////    Canvas.SetLeft(tb, MarginLeft + 5);
        //        ////}
        //        ////else
        //        ////{
        //        ////    Canvas.SetLeft(tb, 0);
        //        ////}
        //        //Canvas.SetTop(tb, y - 8);
        //        double textTop = y - 8;

        //        //如果是 0 刻度，向上或向下避让 X 轴
        //        if (Math.Abs(v) < 1e-6) // v == 0（浮点安全）
        //        {
        //            textTop = y - 20;   // 往上挪
        //        }

        //        Canvas.SetTop(tb, textTop);

        //        ChartCanvas.Children.Add(tb);
        //    }
        //}
        //private void DrawYAxisTicks(double yMin, double yMax, double height)  版本2
        //{
        //    double step = GetNiceStep(yMin, yMax);
        //    double start = Math.Floor(yMin / step) * step - step;
        //    double end = Math.Ceiling(yMax / step) * step + step;

        //    for (double v = start; v <= end; v += step)
        //    {
        //        double y = MarginTop + (end - v) / (end - start) * height;

        //        ChartCanvas.Children.Add(new Line
        //        {
        //            X1 = MarginLeft - 5,
        //            Y1 = y,
        //            X2 = MarginLeft,
        //            Y2 = y,
        //            Stroke = Brushes.Black
        //        });

        //        var tb = new TextBlock
        //        {
        //            Text = v.ToString("F1"),
        //            FontSize = 11
        //        };

        //        double textY = y - 8;
        //        if (textY < MarginTop)
        //            textY = MarginTop;

        //        Canvas.SetLeft(tb, 0);
        //        Canvas.SetTop(tb, textY);
        //        ChartCanvas.Children.Add(tb);
        //    }
        //}

        /// <summary>
        /// 第二次版本
        /// </summary>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <param name="height"></param>
        //private void DrawYAxisTicks(double yMin, double yMax, double height)
        //{
        //    // 1️⃣ 显示范围：严格等于数据 ± 5
        //    double range = yMax - yMin;

        //    // 2️⃣ 算一个“好看”的刻度步长（5~7 条线）
        //    double step = GetNiceStep(yMin, yMax);

        //    // 3️⃣ 从 yMin 开始画，不再 Floor / Ceil
        //    for (double v = yMin; v <= yMax + 0.0001; v += step)
        //    {
        //        double y = MarginTop + (yMax - v) / range * height;

        //        // 刻度线
        //        ChartCanvas.Children.Add(new Line
        //        {
        //            X1 = MarginLeft - 5,
        //            Y1 = y,
        //            X2 = MarginLeft,
        //            Y2 = y,
        //            Stroke = Brushes.Black
        //        });

        //        // 刻度文字
        //        var tb = new TextBlock
        //        {
        //            Text = v.ToString("F1"),
        //            FontSize = 11
        //        };

        //        // ⭐ 防止被裁剪
        //        double textY = y - 8;
        //        if (textY < MarginTop)
        //            textY = MarginTop;
        //        if (textY > MarginTop + height - 16)
        //            textY = MarginTop + height - 16;

        //        Canvas.SetLeft(tb, 0);
        //        Canvas.SetTop(tb, textY);
        //        ChartCanvas.Children.Add(tb);
        //    }
        //}

        private void DrawYAxisTicks(double yMin, double yMax, double height)
        {
            // ⭐ 1️⃣ 数据中心
            double center = (yMin + yMax) / 2.0;

            // ⭐ 2️⃣ 放大显示范围（±0.001mm）
            double displayHalfRange = 0.001;
            double displayMin = center - displayHalfRange;
            double displayMax = center + displayHalfRange;
            double range = displayMax - displayMin;

            // ⭐ 3️⃣ 最小刻度 0.0001mm
            double step = 0.0001;

            for (double v = displayMin; v <= displayMax + 1e-9; v += step)
            {
                double y = MarginTop + (displayMax - v) / range * height;

                // 刻度线
                ChartCanvas.Children.Add(new Line
                {
                    X1 = MarginLeft - 6,
                    Y1 = y,
                    X2 = MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });

                // 每 0.0005mm 标一个数，其余只画线
                if (Math.Round((v - displayMin) / step) % 5 == 0)
                {
                    var tb = new TextBlock
                    {
                        Text = v.ToString("F4"), // ⭐ 显示到 0.0001
                        FontSize = 11
                    };

                    Canvas.SetLeft(tb, 0);
                    Canvas.SetTop(tb, y - 8);
                    ChartCanvas.Children.Add(tb);
                }
            }
        }


        private void DrawYAxisTicks_DeltaY(
     double deltaMin,
     double deltaMax,
     double height)
        {
            // ⭐ 1. 用数据中值作为视觉中心
            double center = (deltaMin + deltaMax) / 2.0;

            // ⭐ 2. 人为设定显示范围（核心）
            double displayRange = 0.02; // mm，越大越夸张
            double displayMin = center - displayRange / 2;
            double displayMax = center + displayRange / 2;

            double step = displayRange / 6; // 只画 6 根刻度线

            for (double v = displayMin; v <= displayMax + 1e-6; v += step)
            {
                double y = MarginTop + (displayMax - v) / displayRange * height;

                ChartCanvas.Children.Add(new Line
                {
                    X1 = MarginLeft - 6,
                    Y1 = y,
                    X2 = MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = Math.Abs(v - center) < 1e-6 ? 2 : 1
                });
                string vtext = "";
                if (v == displayMax + 1e-6)
                {
                    vtext = v.ToString("F4") + " mm";
                }
                else
                {
                    vtext = v.ToString("F4");
                }
                var tb = new TextBlock
                {
                    Text = vtext,
                    FontSize = 11
                };

                Canvas.SetLeft(tb, 2);
                Canvas.SetTop(tb, y - 8);
                ChartCanvas.Children.Add(tb);
            }
        }

        private void DrawYAxisTicks_DeltaY1(
            double deltaMin,
            double deltaMax,
            double height)
        {
            double range = deltaMax - deltaMin;
            if (range <= 0)
                return;

            // ⭐ 刻度数量（你可以改 5 / 6 / 8）
            int tickCount = 6;
            double step = range / tickCount;

            for (int i = 0; i <= tickCount; i++)
            {
                double v = deltaMin + i * step;

                double y = MarginTop + (deltaMax - v) / range * height;

                // 刻度线
                ChartCanvas.Children.Add(new Line
                {
                    X1 = MarginLeft - 6,
                    Y1 = y,
                    X2 = MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness =
                        Math.Abs(v - deltaMin) < 1e-9 ||
                        Math.Abs(v - deltaMax) < 1e-9
                        ? 2.0     // ⭐ 最大 / 最小值刻度加粗
                        : 1.0
                });

                // ⭐ 只有最大值显示单位 mm
                string text =
                    Math.Abs(v - deltaMax) < 1e-9
                    ? v.ToString("F4") + " mm"
                    : v.ToString("F4");

                var tb = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight =
                        Math.Abs(v - deltaMax) < 1e-9
                        ? FontWeights.Bold
                        : FontWeights.Normal
                };

                Canvas.SetLeft(tb, 2);
                Canvas.SetTop(tb, y - 8);
                ChartCanvas.Children.Add(tb);
            }
        }

        private void DrawYAxisTicks_DeltaY2(
    double deltaMin,
    double deltaMax,
    double height)
        {
            double realRange = deltaMax - deltaMin;
            if (realRange <= 0)
                return;

            // ⭐⭐ 关键：Y 轴视觉放大倍数
            double amplify = 50;   // 10=轻微，20=明显，50=轮廓仪
            double range = realRange / amplify;

            double center = (deltaMin + deltaMax) / 2.0;
            double displayMin = center - range / 2;
            double displayMax = center + range / 2;

            int tickCount = 6;
            double step = (displayMax - displayMin) / tickCount;

            for (int i = 0; i <= tickCount; i++)
            {
                double v = displayMin + i * step;

                double y = MarginTop + (displayMax - v) / range * height;

                ChartCanvas.Children.Add(new Line
                {
                    X1 = MarginLeft - 6,
                    Y1 = y,
                    X2 = MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness =
                        Math.Abs(v - center) < 1e-9 ? 2.0 : 1.0
                });

                string text =
                    Math.Abs(v - displayMax) < 1e-9
                    ? v.ToString("F4") + " mm"
                    : v.ToString("F4");

                var tb = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight =
                        Math.Abs(v - displayMax) < 1e-9
                        ? FontWeights.Bold
                        : FontWeights.Normal
                };

                Canvas.SetLeft(tb, 2);
                Canvas.SetTop(tb, y - 8);
                ChartCanvas.Children.Add(tb);
            }
        }

        //private void Redraw()
        //{
        //    ChartCanvas.Children.Clear();

        //    // 1️⃣ 计算数据范围（你原来就有）
        //    double yMin = YValues.Min();
        //    double yMax = YValues.Max();

        //    double height = ChartCanvas.ActualHeight
        //                    - MarginTop
        //                    - MarginBottom;

        //    // 2️⃣ 画 Y 轴刻度（你刚才那套放大版）
        //    DrawYAxisTicks_DeltaY1(yMin, yMax, height);

        //    // 3️⃣ 画曲线（你原来那段 Line 连线）
        //    DrawPolyline2(points, xMin, xMax, yMin, yMax, width, height);
        //}
        private void DrawYAxisTicks_DeltaY3(
        double deltaMin,
        double deltaMax,
        double height)
        {
            double realRange = deltaMax - deltaMin;
            if (realRange <= 0)
                return;

            // ⭐ 1. 中心值（真实）
            double center = (deltaMin + deltaMax) / 2.0;

            // ⭐ 2. 显示放大倍数（关键！！）
            // 1000：0.0001 → 0.1
            // 2000：像正弦波
            // 5000：轮廓仪效果
            double amplify = _yAmplify;// 5000;

            // ⭐ 3. 显示用 range（假的，用来画）
            double displayRange = realRange * amplify;

            double displayMin = center - displayRange / 2;
            double displayMax = center + displayRange / 2;

            int tickCount = 6;
            double step = displayRange / tickCount;

            for (int i = 0; i <= tickCount; i++)
            {
                double vDisplay = displayMin + i * step;

                // ⭐ 显示坐标用 displayRange
                double y = MarginTop
                    + (displayMax - vDisplay) / displayRange * height;

                ChartCanvas.Children.Add(new Line
                {
                    X1 = MarginLeft - 6,
                    Y1 = y,
                    X2 = MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness =
                        i == 0 || i == tickCount ? 2.0 : 1.0
                });

                // ⭐ 刻度文字用“真实值”
                double vReal = center + (vDisplay - center) / amplify;

                string text =
                    i == tickCount
                    ? vReal.ToString("F4") + " mm"
                    : vReal.ToString("F4");

                var tb = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight = i == tickCount
                        ? FontWeights.Bold
                        : FontWeights.Normal
                };

                Canvas.SetLeft(tb, 2);
                Canvas.SetTop(tb, y - 8);
                ChartCanvas.Children.Add(tb);
            }
        }
        private void DrawYAxisTicks_VisualMm(
    double yMin, double yMax, double height, double magnification = 50)
        {
            if (yMax <= yMin) return;

            // 1️⃣ 数据中心
            double yBase = (yMin + yMax) / 2.0;

            // 2️⃣ 放大显示范围（视觉上放大波动）
            double displayHalfRange = (yMax - yMin) / 2.0 * magnification;
            double displayMin = yBase - displayHalfRange;
            double displayMax = yBase + displayHalfRange;
            double range = displayMax - displayMin;

            // 3️⃣ 主刻度数量（5~10条）
            int numTicks = 6;
            double step = (yMax - yMin) / (numTicks - 1); // 按真实 mm 计算

            for (int i = 0; i < numTicks; i++)
            {
                double v = yMin + i * step;

                // 计算像素 Y（视觉放大映射）
                double y = MarginTop + (displayMax - v) / range * height;

                // 刻度线
                ChartCanvas.Children.Add(new Line
                {
                    X1 = MarginLeft - 6,
                    Y1 = y,
                    X2 = MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });

                // 刻度文字显示 mm（绝对值）
                var tb = new TextBlock
                {
                    Text = v.ToString("F4") + " mm",
                    FontSize = 11
                };
                Canvas.SetLeft(tb, 2);
                Canvas.SetTop(tb, y - 8);
                ChartCanvas.Children.Add(tb);
            }
        }

        private double GetNiceStep(double min, double max, int target = 5)
        {
            double range = max - min;
            double rough = range / target;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(rough)));
            double r = rough / mag;

            if (r <= 1) return mag;
            if (r <= 2) return 2 * mag;
            if (r <= 5) return 5 * mag;
            return 10 * mag;
        }

        #endregion

        #region ===== X 轴刻度（自适应） =====

        private void DrawXAxisTicks(
            List<Point> points,
            double xMin,
            double xMax,
            double width,
            double zeroY)
        {
            int step = GetXAxisStep(points.Count, width);

            for (int i = 0; i < points.Count; i += step)
            {
                var p = points[i];
                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;

                ChartCanvas.Children.Add(new Line
                {
                    X1 = x,
                    Y1 = zeroY,
                    X2 = x,
                    Y2 = zeroY + 5,
                    Stroke = Brushes.Black
                });
                ////double gap = 25;

                ////ChartCanvas.Children.Add(new Line
                ////{
                ////    X1 = MarginLeft,
                ////    Y1 = zeroY,
                ////    X2 = MarginLeft - gap,
                ////    Y2 = zeroY,
                ////    Stroke = Brushes.Black
                ////});

                ////ChartCanvas.Children.Add(new Line
                ////{
                ////    X1 = MarginLeft + gap,
                ////    Y1 = zeroY,
                ////    X2 = MarginLeft + width,
                ////    Y2 = zeroY,
                ////    Stroke = Brushes.Black
                ////});

                var tb = new TextBlock
                {
                    Text = p.X.ToString("F4"),//ToString("F1"),
                    FontSize = 11
                };
                Canvas.SetLeft(tb, x - 10);
                Canvas.SetTop(tb, zeroY + 5);
                ChartCanvas.Children.Add(tb);
            }
        }

        private void DrawXAxisTicks1(
     double xMin,
     double xMax,
     double width,
     double zeroY)
        {
            double displayXRange = 30.0; // ⭐ 核心参数（试 20 / 30 / 40）
            double center = (xMin + xMax) / 2.0;

            double displayMin = center - displayXRange / 2;
            double displayMax = center + displayXRange / 2;

            double tickStep = 5.0; // ✅ 仍然是 5°

            for (double v = Math.Ceiling(displayMin / tickStep) * tickStep;
                 v <= displayMax + 1e-6;
                 v += tickStep)
            {
                double x = MarginLeft + (v - displayMin) / displayXRange * width;

                ChartCanvas.Children.Add(new Line
                {
                    X1 = x,
                    Y1 = zeroY,
                    X2 = x,
                    Y2 = zeroY + 5,   // ✅ 长度没变
                    Stroke = Brushes.Black
                });

                var tb = new TextBlock
                {
                    Text = v.ToString("F0"),
                    FontSize = 11
                };

                Canvas.SetLeft(tb, x - 10);
                Canvas.SetTop(tb, zeroY + 5);
                ChartCanvas.Children.Add(tb);
            }
        }
        private void DrawXAxisTicks4(
        List<Point> points,
        double xMin,
        double xMax,
        double width,
        double zeroY)
        {
            int step = GetXAxisStep(points.Count, width);

            for (int i = 0; i < points.Count; i += step)
            {
                var p = points[i];

                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;

                // ✅ 刻度线：完全和你原来一样（长度、粗细都不变）
                ChartCanvas.Children.Add(new Line
                {
                    X1 = x,
                    Y1 = zeroY,
                    X2 = x,
                    Y2 = zeroY + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });

                // ⭐ 只在 5° 的时候显示文字
                if (Math.Abs(p.X % 5.0) < 1e-6)
                {
                    var tb = new TextBlock
                    {
                        Text = p.X.ToString("F0"), // 5, 10, 15 ...
                        FontSize = 11
                    };

                    Canvas.SetLeft(tb, x - 10);
                    Canvas.SetTop(tb, zeroY + 5);
                    ChartCanvas.Children.Add(tb);
                }
            }
        }

        private int GetXAxisStep(int count, double width, double minPixel = 60)
        {
            int maxTicks = Math.Max(1, (int)(width / minPixel));
            return Math.Max(1, (int)Math.Ceiling((double)count / maxTicks));
        }


        
        #endregion

        #region ===== 折线 + Tooltip =====

        private void DrawPolyline(
            List<Point> points,
            double xMin,
            double xMax,
            double yMin,
            double yMax,
            double width,
            double height)
        {
            var line = new Polyline
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 2
            };

            foreach (var p in points)
            {
                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;
                double y = MarginTop + (yMax - p.Y) / (yMax - yMin) * height;

                line.Points.Add(new Point(x, y));

                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Blue,
                    Tag = p
                };
                Canvas.SetLeft(dot, x - 3);
                Canvas.SetTop(dot, y - 3);
                ChartCanvas.Children.Add(dot);
            }

            ChartCanvas.Children.Add(line);

        }


        private void DrawPolyline1(
    List<Point> points,
    double xMin,
    double xMax,
    double yMin,
    double yMax,
    double width,
    double height)
        {
            if (points == null || points.Count == 0)
                return;

            // ===== 1️⃣ 防止除 0 =====
            if (Math.Abs(xMax - xMin) < 1e-9)
                return;

            if (Math.Abs(yMax - yMin) < 1e-9)
                return;

            // ===== 2️⃣ 创建曲线（先画线）=====
            Polyline line = new Polyline
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            Panel.SetZIndex(line, 1);

            // ===== 3️⃣ 坐标转换 + 画线 =====
            foreach (var p in points)
            {
                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;
                double y = MarginTop + (yMax - p.Y) / (yMax - yMin) * height;

                if (double.IsNaN(x) || double.IsNaN(y))
                    continue;

                line.Points.Add(new Point(x, y));
            }

            ChartCanvas.Children.Add(line);

            // ===== 4️⃣ 再画点（保证点在最上层）=====
            int index = 0;
            foreach (var p in points)
            {
                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;
                double y = MarginTop + (yMax - p.Y) / (yMax - yMin) * height;

                if (double.IsNaN(x) || double.IsNaN(y))
                    continue;

                // 防止完全重叠（轻微抖动，调试用）
                double jitter = (index % 3 - 1) * 1.0;

                Ellipse dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.DodgerBlue,
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Tag = p
                };

                Panel.SetZIndex(dot, 2);

                Canvas.SetLeft(dot, x - 3 + jitter);
                Canvas.SetTop(dot, y - 3 + jitter);

                ChartCanvas.Children.Add(dot);
                index++;
            }
        }


        private void DrawPolyline2(
        List<Point> points,
        double xMin,
        double xMax,
        double yMin,
        double yMax,
        double width,
        double height)
        {
            if (points == null || points.Count == 0)
                return;

            // ===== 1️⃣ 防止除 0 =====
            if (Math.Abs(xMax - xMin) < 1e-9)
                return;

            if (Math.Abs(yMax - yMin) < 1e-9)
                return;

            // ===== 2️⃣ 创建曲线（先画线）=====
            Polyline line = new Polyline
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            Panel.SetZIndex(line, 1);

            // ===== 3️⃣ 坐标转换 + 画线 =====
            foreach (var p in points)
            {
                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;
                double y = MarginTop + (yMax - p.Y) / (yMax - yMin) * height;

                if (double.IsNaN(x) || double.IsNaN(y))
                    continue;

                line.Points.Add(new Point(x, y));
            }

            ChartCanvas.Children.Add(line);

            // ===== 4️⃣ 画点（保证在最上层，显示真实数据）=====
            foreach (var p in points)
            {
                double x = MarginLeft + (p.X - xMin) / (xMax - xMin) * width;
                double y = MarginTop + (yMax - p.Y) / (yMax - yMin) * height;

                if (double.IsNaN(x) || double.IsNaN(y))
                    continue;

                Ellipse dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.DodgerBlue,
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Tag = p
                };

                // 设置 ZIndex 保证点在上层
                Panel.SetZIndex(dot, 2);

                // 将点中心对齐到坐标
                Canvas.SetLeft(dot, x - dot.Width / 2);
                Canvas.SetTop(dot, y - dot.Height / 2);

                // Tooltip 显示原始 double 值，不四舍五入
                //dot.ToolTip = $"X = {p.X:G17}, Y = {p.Y:G17}";
                dot.ToolTip = $"X = {p.X:F4}, Y = {p.Y:F4}";
                // 禁用抗锯齿以保证像素清晰（可选）
                RenderOptions.SetEdgeMode(dot, EdgeMode.Aliased);

                ChartCanvas.Children.Add(dot);
            }
        }

        private void ChartCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(ChartCanvas);

            foreach (var child in ChartCanvas.Children)
            {
                if (child is Ellipse el && el.Tag is Point p)
                {
                    double cx = Canvas.GetLeft(el) + 3;
                    double cy = Canvas.GetTop(el) + 3;

                    if (Math.Abs(pos.X - cx) < 5 &&
                        Math.Abs(pos.Y - cy) < 5)
                    {
                        _toolTip.Content = $"C轴角度: {p.X:F4}\n槽深: {p.Y:F4}";
                        _toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
                        _toolTip.IsOpen = true;
                        return;
                    }
                }
            }
            _toolTip.IsOpen = false;
        }

        private void ChartCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _toolTip.IsOpen = false;
        }

        #endregion
    }
}