using CCD.Controls;
using CCD.enums;
using CCD.libs;
using CCD.shapes;
using CCD.Strategy;
using CCD.tools;
using Microsoft.Win32;
using OfficeOpenXml;
using Prism.Events;
using Prism.Ioc;
using SharedResource.events;
using SharedResource.events.MVS_CCD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static CCD.tools.CoordinateHelper;
using Shape = CCD.shapes.Shape;

namespace CCD.Views
{
    /// <summary>
    /// CCDControl.xaml 的交互逻辑
    /// </summary>
    public partial class CCDControl : UserControl
    {
        public CCDControl(IContainerProvider provider)
        {
            InitializeComponent();

            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<CcdToggleConnectionEvent>().Subscribe(() =>
            {
                if (ViewModel.OpenCapture(ImageViewer))
                {
                    Start_Timer();
                    eventAggregator.GetEvent<CamerConnectStatus>().Publish(ViewModel.CameraState);
                }
                else
                {
                    timer.Stop();
                    eventAggregator.GetEvent<CamerConnectStatus>().Publish(ViewModel.CameraState);
                }
            });

            eventAggregator.GetEvent<InputCalibrationEvent>().Subscribe(() => ViewModel.Test_Clickdaoru());

            doubleClickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // 定义两次单击之间的最大时间间隔（单位为毫秒）
            };
            doubleClickTimer.Tick += DoubleClickTimer_Tick;

            ViewModel.CurrentWindow = this;
            ViewModel.eventAggregator = containerProvider.Resolve<IEventAggregator>();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 每隔1秒触发一次事件
            };
            timer.Tick += Timer_Tick; // 设置事件处理方法

            tools.EventAggregator.Instance.Subscribe<int>(ShapePenChange);
            ImageViewer.ScrollChanged += (sender, e) =>
            {
            };
        }
        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;

        private DispatcherTimer doubleClickTimer;
        private DispatcherTimer timer;
        private (Point3D, Vector3D, Vector3D) perMachine;
        private Point rightPosition;
        private ImgDrawingVisual drawingVisual = null;
        bool isDragging = false;
        private Point lastMousePosition;
        private int editIndex = -1;
        private ImgDrawingVisual editVisual = null;
        private IShapeStrategy shapeStrategy;

        public bool IsRectifyDraw = false;

        private bool isDrawing = false;
        public bool IsDrawing
        {
            get { return isDrawing; }
            set
            {
                isDrawing = value;
                if (!isDrawing && drawingVisual != null)
                {
                    if (ViewModel.ListVisibility == Visibility.Visible)
                    {
                        canvas.Shapes.Add(drawingVisual.Shape);
                    }

                    ViewModel.ShapeEnum = ShapeEnum.None;
                    shapeStrategy = null;
                    drawingVisual = null;
                }
            }
        }

        private void ImageViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scrollViewerWidth = e.NewSize.Width;
            double scrollViewerHeight = e.NewSize.Height;

            ViewModel.UpdateScrollViewerSize(scrollViewerWidth, scrollViewerHeight);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control) // 缩放
            {
                Point oldMousePosition = e.GetPosition(canvas);

                if ((image.ActualHeight < ImageContainer.ActualHeight && image.ActualWidth < ImageContainer.ActualWidth) && e.Delta < 0)
                    return;
                ViewModel.GetScrollOffset(oldMousePosition, e.Delta, out double offsetX, out double offsetY);
                if (offsetX != 0 && offsetY != 0)
                {
                    ImageViewer.ScrollToHorizontalOffset(ImageViewer.HorizontalOffset + offsetX);
                    ImageViewer.ScrollToVerticalOffset(ImageViewer.VerticalOffset + offsetY);
                }
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)  // 水平滚动
            {
                ImageViewer.ScrollToHorizontalOffset(ImageViewer.HorizontalOffset + e.Delta);
                e.Handled = true;
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Canvas) || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (ViewModel.ShapeEnum == ShapeEnum.None)
            {
                if (doubleClickTimer.IsEnabled)
                {
                    doubleClickTimer.Stop();
                    //MessageBox.Show("双击");
                    Point mousePosition = e.GetPosition(canvas);
                    Instance.ToolMovePiex(mousePosition);
                }
                else
                {
                    doubleClickTimer.Start();
                    isDragging = true;
                    lastMousePosition = e.GetPosition(ImageViewer);
                    canvas.CaptureMouse();
                }
            }
            else if (ViewModel.ShapeEnum == ShapeEnum.Edit)
            {
                if (editIndex == -1)
                {
                    Point mousePosition = e.GetPosition(canvas);
                    VisualTreeHelper.HitTest(canvas, null, HitTestResultCallback, new PointHitTestParameters(mousePosition));
                    if (editVisual != null)
                    {
                        CircleContour contour = (CircleContour)editVisual.Shape;
                        editIndex = contour.DetermineClosestCircle(mousePosition);
                    }
                }
                else
                {
                    ViewModel.ShapeEnum = ShapeEnum.None;
                }

            }
            else
            {
                Point mousePosition = e.GetPosition(canvas);
                if (IsDrawing)
                {
                    if (shapeStrategy != null)
                    {
                        IsDrawing = shapeStrategy.FinishShape(drawingVisual, mousePosition);
                    }
                }
                else
                {
                    shapeStrategy = StrategyFactory.CreateStrategy(ViewModel.ShapeEnum);
                    if (shapeStrategy != null)
                    {
                        Shape shape = shapeStrategy.CreateShape(mousePosition, out bool isD);
                        drawingVisual = new ImgDrawingVisual(shape);
                        canvas.AddVisual(drawingVisual);
                        shapeStrategy.UpdateShape(drawingVisual, mousePosition);
                        IsDrawing = isD;
                    }
                }
            }

            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(canvas);
            ViewModel.SetMouseCoordinatesText(mousePosition);
            if (isDragging)
            {
                Point newMousePosition = e.GetPosition(ImageViewer);
                double offsetX = newMousePosition.X - lastMousePosition.X;
                double offsetY = newMousePosition.Y - lastMousePosition.Y;

                double newScrollHorizontalOffset = ImageViewer.HorizontalOffset - offsetX;
                double newScrollVerticalOffset = ImageViewer.VerticalOffset - offsetY;

                ImageViewer.ScrollToHorizontalOffset(newScrollHorizontalOffset);
                ImageViewer.ScrollToVerticalOffset(newScrollVerticalOffset);

                lastMousePosition = newMousePosition;
                //ViewModel.DrawRule(cvRuler, cvVerticalRuler, ImageViewer);
            }
            else if (IsDrawing)
            {
                shapeStrategy?.UpdateShape(drawingVisual, mousePosition);
            }
            else if (editIndex != -1)
            {
                CircleContour contour = (CircleContour)editVisual.Shape;
                contour.UpdateProperty(editIndex, mousePosition);
                editVisual.DrawShape();
            }
            e.Handled = true;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.ShapeEnum == ShapeEnum.None)
            {
                isDragging = false;
                canvas.ReleaseMouseCapture();
            }
            e.Handled = true;
        }

        private void ListView1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 如果点击的地方不是任何项，则设置选中项为 null
            if (Mouse.Captured == null)
            {
                ViewModel.ClearSelect();
            }
            e.Handled = true;
        }

        private void ListView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                ViewModel.SelectShapes.Add((Shape)item);
            }

            foreach (var item in e.RemovedItems)
            {
                ViewModel.SelectShapes.Remove((Shape)item);
            }

            if (ViewModel.SelectShapes.Count == 1 && ViewModel.SelectShapes.Any(x => x is ParallelLines))
                ViewModel.GetMidlineVisibvility = Visibility.Visible;
            else
                ViewModel.GetMidlineVisibvility = Visibility.Collapsed;
            e.Handled = true;
        }

        private void Deletloc_Click(object sender, RoutedEventArgs e)
        {
            List<Shape> newShapes = new List<Shape>();

            foreach (Shape shape in ViewModel.SelectShapes)
            {
                // 复制 Shape 对象并添加到新的 List<Shape>
                newShapes.Add(shape);
            }
            foreach (var item in newShapes)
            {
                canvas.DeleteVisualById(item.Id);
                canvas.Shapes.Remove(item);
            }
        }

        private void Clearloc_Click(object sender, RoutedEventArgs e)
        {
            bool result = PopupHelpercs.ShowConfirmationDialog("确定要清空吗？", "确认清空");
            if (result)
            {
                canvas.ClearVisual();
            }
        }

        private void CaliWindow_Click(object sender, RoutedEventArgs e)
        {
            var customWindow = new PolynomialWindow()
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            customWindow.InitWindow(Instance.MachinePoint, GetPolyPoint, customWindow);
            if(GetPolyPoint==null)
            {
                return;
            }
            customWindow.Closed += (s, args) =>
            {
                // 在窗体关闭时刷新ListView
                canvas.ClearShapeCache();
                listView1.Items.Refresh();
            };
            customWindow.Show();
            e.Handled = true;
        }

        private void Saveloc_Click(object sender, RoutedEventArgs e)
        {
            // 设置 EPPlus 的许可证上下文
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            // 创建保存文件对话框
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = DateTime.Now.ToString("yyyyMMdd") + ".xlsx", // 默认文件名为当前日期
                Filter = "Excel Files|*.xlsx|All Files|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // 使用EPPlus创建Excel文件
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Shapes");

                    // 设置标题行
                    worksheet.Cells[1, 1].Value = "Shape Type";
                    worksheet.Cells[1, 2].Value = "X Coordinate";
                    worksheet.Cells[1, 3].Value = "Y Coordinate";
                    worksheet.Cells[1, 4].Value = "Radius";

                    int row = 2;

                    // 保存形状的相关信息
                    foreach (Shape shape in canvas.Shapes)
                    {
                        if (shape is LocationPoint point)
                        {
                            worksheet.Cells[row, 1].Value = "LocationPoint";
                            worksheet.Cells[row, 2].Value = point.X;
                            worksheet.Cells[row, 3].Value = point.Y;
                            row++;
                        }
                        else if (shape is Circle circle)
                        {
                            worksheet.Cells[row, 1].Value = "Circle";
                            worksheet.Cells[row, 2].Value = circle.Center.MacPoint.X;
                            worksheet.Cells[row, 3].Value = circle.Center.MacPoint.Y;
                            worksheet.Cells[row, 4].Value = circle.RealRadius;
                            row++;
                        }
                    }

                    // 保存Excel文件
                    package.SaveAs(new FileInfo(saveFileDialog.FileName));
                }

                MessageBox.Show("坐标保存成功!");
            }
        }

        private void CrossCheck_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.SetCrossBackground();
        }

        private void CrossCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.SetBackground();
        }

        private void DoubleClickTimer_Tick(object sender, EventArgs e)
        {
            doubleClickTimer.Stop();
        }

        private void openCamer_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PauseContinue();

        }

        private void Start_Timer()
        {
            timer.Start();
            perMachine = Instance.MachinePoint3D;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //if (!isDrawing)
            //{
            var point = Instance.MachinePoint3D;

            if (Instance.isCalibrationMode)  // 校准模式只考虑平移
            {
                var vector_3D = point.Item1 - perMachine.Item1;
                var vector = new Vector(vector_3D.X, vector_3D.Y);
                if (vector.Length < 0.001)
                    return;

                var list = canvas.GetAllImgDrawingVisuals();
                foreach (var visual in list)
                    visual.MoveReDrawShape(-vector);
            }
            else
            {
                var vector_3D = point.Item1 - perMachine.Item1;
                var vector = new Vector(vector_3D.X, vector_3D.Y);
                if (!(vector.Length > 0.001 || Vector3D.AngleBetween(perMachine.Item3, point.Item3) > 0.01))
                    return;
                var list = canvas.GetAllImgDrawingVisuals();
                foreach (var visual in list)
                    visual.MoveReDrawShape(point.Item1, point.Item2, point.Item3);
            }
            perMachine = point;
            //}
        }

        private void ShapePenChange(int data)
        {
            if (data / 10 == 2)
            {
                foreach (var visual in canvas.GetAllImgDrawingVisuals())
                {
                    visual.ReDrawShape();
                }
            }
        }

        public HitTestResultBehavior HitTestResultCallback(HitTestResult result)
        {
            // 检查命中的元素类型是否为 DrawingVisual
            if (result.VisualHit is ImgDrawingVisual visual)
            {
                // 处理命中的 DrawingVisual
                // ...
                // 返回 Stop，以停止 HitTest 组件的继续搜索
                if (visual.Shape is CircleContour)
                {
                    editVisual = visual;
                    return HitTestResultBehavior.Stop;
                }

            }
            // 返回 Continue，以继续搜索
            return HitTestResultBehavior.Continue;
        }

        private List<Point> GetPolyPoint(PolynomialWindow customWindow)
        {
            var pointsList = new List<Point>();
            foreach (Shape shape in canvas.Shapes)
            {
                if (shape is LocationPoint point)
                {
                    pointsList.Add(point.Point.PixPoint);
                }
            }
            pointsList.Sort(new CustomPointComparer());
            if(pointsList.Count > 9|| pointsList.Count<9)
            {
                MessageBox.Show("请在图像上面圈出9个标定点!标点的样式和顺序如图所示！");
                return null;
            }
            return pointsList;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                canvas.DeleteVisual(drawingVisual);
                drawingVisual = null;
            }

            if (editIndex != -1)
            {
                editVisual = null;
                editIndex = -1;
            }
        }

        public void SetToolMove(Move3DHandler handler)
        {
            Instance.GetMoveHandler = handler;
        }

        public void SetShapeCenter(CenterHandler handler)
        {
            Instance.GetCenterHandler = handler;
        }
    }
}
