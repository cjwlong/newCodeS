using CCD.Controls;
using CCD.enums;
using CCD.libs;
using CCD.tools;
using CCD.Views;
using Microsoft.Win32;
using MvCamCtrl.NET;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.events.eventMeg;
using SharedResource.events.Machine;
using SharedResource.events.MVS_CCD;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static IronPython.Modules._ast;
using Shape = CCD.shapes.Shape;

namespace CCD.ViewModels
{
    public class CCDControlViewModel : BindableBase
    {
        public CCDControlViewModel()
        {
            timer = new Timer(new TimerCallback(Updata), null, -1, 33);
            //Locations = new ObservableCollection<LocationModel>();
            InitScoll();
            FocusCommand = new DelegateCommand(FocusProgress);
            FindPeakCommand = new DelegateCommand(OnFindPeak);
        }
        public IEventAggregator eventAggregator;

        private CameraState _cameraState = CameraState.Disconnected;
        public CameraState CameraState
        {
            get { return _cameraState; }
            set { SetProperty(ref _cameraState, value); }
        }

        private Visibility _getMidlineVisibvility = Visibility.Collapsed;
        public Visibility GetMidlineVisibvility
        {
            get => _getMidlineVisibvility;
            set => SetProperty(ref _getMidlineVisibvility, value);
        }

        private bool _isNotRectify = true;
        public bool IsNotRectify
        {
            get { return _isNotRectify; }
            set { SetProperty(ref _isNotRectify, value); }
        }

        private bool _isRecognize = false;

        public bool IsRecognize
        {
            get { return _isRecognize; }
            set { SetProperty(ref _isRecognize, value); }
        }

        private int _imgWidth = CamerasAbstract.DefaultSize;
        public int ImageWidth
        {
            get { return _imgWidth; }
            set { SetProperty(ref _imgWidth, value); }
        }

        private int _imgHeight = CamerasAbstract.DefaultSize;
        public int ImageHeight
        {
            get { return _imgHeight; }
            set { SetProperty(ref _imgHeight, value); }
        }

        private WriteableBitmap _imageBmp = null;
        public WriteableBitmap ImageBMP
        {
            get => _imageBmp;
            set { SetProperty(ref _imageBmp, value); }
        }

        private string _markedWord = string.Empty;
        public string MarkedWord
        {
            get => _markedWord;
            set { SetProperty(ref _markedWord, value); }
        }

        private int _initialImageWidth = CamerasAbstract.DefaultSize;
        public int InitialImageWidth
        {
            get => _initialImageWidth;
            set { SetProperty(ref _initialImageWidth, value); }
        }
        private int _initialImageHeight = CamerasAbstract.DefaultSize;
        public int InitialImageHeight
        {
            get => _initialImageHeight;
            set { SetProperty(ref _initialImageHeight, value); }
        }

        private double _scaleValue;
        public double ScaleValue
        {
            get => _scaleValue;
            set { SetProperty(ref _scaleValue, value); }
        }

        private Point _canvasTransformOrigin;
        public Point CanvasTransformOrigin
        {
            get => _canvasTransformOrigin;
            set { SetProperty(ref _canvasTransformOrigin, value); }
        }
        private Transform _canvasTransform;
        public Transform CanvasTransform
        {
            get => _canvasTransform;
            set { SetProperty(ref _canvasTransform, value); }
        }

        private double _horizontalOffset;
        public double HorizontalOffset
        {
            get => _horizontalOffset;
            set { SetProperty(ref _horizontalOffset, value); }
        }
        private double _verticalOffset;
        public double VerticalOffsetOffset
        {
            get => _verticalOffset;
            set { SetProperty(ref _verticalOffset, value); }
        }

        private string _mouseCoordinatesText;
        public string MouseCoordinatesText
        {
            get => _mouseCoordinatesText;
            set { SetProperty(ref _mouseCoordinatesText, value); }
        }

        private ShapeEnum _shapeEnum = ShapeEnum.None;
        public ShapeEnum ShapeEnum
        {
            get => _shapeEnum;
            set { SetProperty(ref _shapeEnum, value); }
        }

        private Brush _canvasBackground = Brushes.Transparent;
        public Brush CanvasBackground
        {
            get => _canvasBackground;
            set { SetProperty(ref _canvasBackground, value); }
        }

        private bool _isCross = false;
        public bool IsCross
        {
            get => _isCross;
            set
            {
                SetProperty(ref _isCross, value);

            }
        }

        private bool _isSign = false;
        public bool IsSign
        {
            get => _isSign;
            set
            {
                SetProperty(ref _isSign, value);
            }
        }

        private bool _isGetCenter = false;
        public bool IsGetCenter
        {
            get => _isGetCenter;
            set
            {
                SetProperty(ref _isGetCenter, value);

            }
        }

        private string _timeOfExposure = "1000";
        public string TimeOfExposure
        {
            get => _timeOfExposure;
            set => SetProperty(ref _timeOfExposure, value);
        }

        private string _timeOfExp= "1000";
        public string TimeOfExp
        {
            get => _timeOfExp;
            set => SetProperty(ref _timeOfExp, value);
        }

        private string _gain;
        public string Gain
        {
            get => _gain;
            set => SetProperty(ref _gain, value);
        }

        private string _frameRate;
        public string FrameRate
        {
            get => _frameRate;
            set => SetProperty(ref _frameRate, value);
        }

        private bool _isContinueExposure = true;
        public bool IsContinueExposure
        {
            get => _isContinueExposure;
            set => SetProperty(ref _isContinueExposure, value);
        }

        private Visibility _listVisibility = Visibility.Visible;
        public Visibility ListVisibility
        {
            get { return _listVisibility; }
            set
            {
                SetProperty(ref _listVisibility, value);
            }
        }

        private Visibility _treeVisibility = Visibility.Collapsed;
        public Visibility TreeVisibility
        {
            get { return _treeVisibility; }
            set { SetProperty(ref _treeVisibility, value); }
        }
        public Point NowCenter;

        private ObservableCollection<Shape> _selectShapes = new ObservableCollection<Shape>();
        public ObservableCollection<Shape> SelectShapes
        {
            get => _selectShapes;
            set
            {
                SetProperty(ref _selectShapes, value);
            }
        }

        public bool IsCalibrationMode
        {
            get => CoordinateHelper.Instance.isCalibrationMode;
            set
            {
                if (CameraState == CameraState.Disconnected) return;
                CcdPointMessage current_point = new();
                eventAggregator?.GetEvent<CcdGetPointEvent>().Publish(current_point);
                var now_position = CoordinateHelper.Instance.MachinePoint3D;
                if (value == true &&    // 开启时判断转轴位置是否在零位
                    (Vector3D.AngleBetween(now_position.Item2, new(0, 0, 1)) > 1e-3 ||
                    Vector3D.AngleBetween(now_position.Item3, new(1, 0, 0)) > 1e-3))
                {
                    System.Windows.MessageBox.Show("提示：标定前请将转轴置于零位");
                    return;
                }

                if (value)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show("请确认当前位置在相机视野下", "提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (messageBoxResult != MessageBoxResult.Yes)
                    {
                        value = false;
                    }
                }
                SetProperty(ref CoordinateHelper.Instance.isCalibrationMode, value);
            }
        }

        // Focus

        private bool _isInFocus = false;
        public bool IsInFocus
        {
            get => _isInFocus;
            set
            {
                SetProperty(ref _isInFocus, value);
            }
        }

        private double _focusDistance = 1;
        private double _focusSpeed = 0.1; //速度小一点
        private bool _isOnDownFindPeak = false;
        private bool _isLeftRightFindPeak = false;
        private List<(DateTime, double)> _focusClarity = new();
        private List<(DateTime, double)> _peakDiff = new();
        public double FocusDistance { get => _focusDistance; set => SetProperty(ref _focusDistance, value); }
        public double FocusSpeed { get => _focusSpeed; set => SetProperty(ref _focusSpeed, value); }
        public DelegateCommand FocusCommand { get; private set; }
        public DelegateCommand FindPeakCommand { get; private set; }

        private DelegateCommand _autoExposureCommand;
        public DelegateCommand AutoExposureCommand =>
            _autoExposureCommand ?? (_autoExposureCommand = new DelegateCommand(Execute_SetAutoExposure));

        private DelegateCommand _getCameraParamCommand;
        public DelegateCommand GetCameraParamCommand =>
            _getCameraParamCommand ?? (_getCameraParamCommand = new DelegateCommand(Execute_GetCameraParam));

        private DelegateCommand _setCameraParamCommand;
        public DelegateCommand SetCameraParamCommand =>
            _setCameraParamCommand ?? (_setCameraParamCommand = new DelegateCommand(Execute_SetCameraParam));

        public double WinW { get; set; }
        public double WinH { get; set; }


        public Point SignPoint { get; set; }

        public bool IsPause = false;

        private readonly Timer timer;
        private CamerasAbstract _cameras;

        private Point CCDPoint = new Point(50, 50);
        private double PxLen = 10;
        private int LocCount = 1;

        //public ObservableCollection<LocationModel> Locations { get; set; }
        private double scaleFactorValue = 1;
        private IntPtr _test = IntPtr.Zero;


        public CCDControl CurrentWindow { get; set; }

        private void FocusProgress()
        {
            bool result = PopupHelpercs.ShowConfirmationDialog("确定要进行自动对焦吗？", "确认");
            if (!result)
            {

                return;
            }
            else
            {
                result = PopupHelpercs.ShowConfirmationDialog("确定各轴位置都在安全位置吗？如果确定即将进行对焦动作了！", "确认");
                if (!result)
                {
                    return;
                }
            }
            CcdFocusMoveMessage message = new()
            {
                FocusDistance = FocusDistance,
                FocusSpeed = FocusSpeed
            };
            Task.Run(() =>
            {
                _focusClarity.Clear();
                IsInFocus = true;
                try
                {
                    MessageBox.Show("设置曝光时间失败！");
                    if (CameraState == CameraState.Disconnected) { return; }
                    if (_cameras == null)
                    {
                        return;
                    }
                    
                    _cameras.SetCameraParam(TimeOfExp, "1000", "19.2");

                    eventAggregator.GetEvent<CcdFocusMoveEvent>().Publish(message);
                    //Thread.Sleep(CalculateDelay(message.FocusDistance, message.FocusSpeed));
                    IsInFocus = false;
                    var best_frame = _focusClarity.MaxBy(x => x.Item2);
                    var best_position = message.TimePositions.MinBy(x => Math.Abs((best_frame.Item1 - x.Item1).TotalMilliseconds));
                    var guess_position = best_position.Item2 - ((best_position.Item1 - best_frame.Item1).TotalMilliseconds / 1000 * message.FocusSpeed);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxResult result = MessageBox.Show($"对焦位置{guess_position}", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            //CoordinateHelper.Instance.ToolMoveAbsolute(guess_position);
                            eventAggregator.GetEvent<FinishFocuseEvent>().Publish(guess_position);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"对焦发生错误,请检查机床是否连接", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoggingService.Instance.LogError($"对焦异常", ex);
                    });
                }
                finally
                {
                    IsInFocus = false;
                    _focusClarity.Clear();
                }
            });

        }

        private void OnFindPeak()
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(
                "校正前请确认是否已经对焦完毕",
                "确认提示",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information
                );

            if (result != MessageBoxResult.OK) return;

            IsInFocus = true;

            CcdFindPeakMessage message = new()
            {
                isOnDown = true,
                MoveSpeed = FocusSpeed
            };

            Task.Run(() =>
            {
                _peakDiff.Clear();
                _isOnDownFindPeak = true;
                var OnDownFlag = false;
                var LeftRightFlag = false;
                try
                {
                    eventAggregator.GetEvent<CcdFindPeakEvent>().Publish(message);
                    _isOnDownFindPeak = false;

                    var small_Value = _peakDiff.MinBy(x => x.Item2);
                    var closestEntry = message.OnDown?.Where(x => x.Item1 != default)  // 过滤无效时间
                        .Select(x => new
                        {
                            entry = x,
                            Timediff = Math.Abs((x.Item1 - small_Value.Item1).TotalMilliseconds)
                        })
                        .MinBy(x => x.Timediff);    // 按时间差升序，取最小值
                    if (closestEntry != null)
                    {
                        foreach (var item in message.OnDown)
                        {
                            if (item.Item1 == closestEntry.entry.Item1)
                            {
                                OnDownFlag = true;
                                eventAggregator.GetEvent<FinishFindPeakEvent>().Publish(new(true, item.Item2));
                            }
                        }
                    }

                    //foreach (var item in message.OnDown)
                    //{
                    //    if (item.Item1 == small_Value.Item1)
                    //    {
                    //        OnDownFlag = true;
                    //        eventAggregator.GetEvent<FinishFindPeakEvent>().Publish(new(true, item.Item2));
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError($"寻找顶点失败", ex);
                }
                finally
                {
                    IsInFocus = false;
                    _isOnDownFindPeak = false;
                    _peakDiff.Clear();
                }

                if (OnDownFlag)
                {
                    IsInFocus = true;
                    _isLeftRightFindPeak = true;
                    try
                    {
                        message.isOnDown = false;
                        eventAggregator.GetEvent<CcdFindPeakEvent>().Publish(message);
                        _isLeftRightFindPeak = false;

                        var small_Value = _peakDiff.MinBy(x => x.Item2);
                        var closestEntry = message.LeftRight?.Where(x => x.Item1 != default)  // 过滤无效时间
                            .Select(x => new
                            {
                                entry = x,
                                Timediff = Math.Abs((x.Item1 - small_Value.Item1).TotalMilliseconds)
                            })
                            .MinBy(x => x.Timediff);    // 按时间差升序，取最小值
                        if (closestEntry != null)
                        {
                            foreach (var item in message.LeftRight)
                            {
                                if (item.Item1 == closestEntry.entry.Item1)
                                {
                                    LeftRightFlag = true;
                                    eventAggregator.GetEvent<FinishFindPeakEvent>().Publish(new(false, item.Item2));
                                }
                            }
                        }
                        //foreach (var item in message.LeftRight)
                        //{
                        //    if (item.Item1 == small_Value.Item1)
                        //    {
                        //        LeftRightFlag = true;
                        //        eventAggregator.GetEvent<FinishFindPeakEvent>().Publish(new(false, item.Item2));
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError($"寻找顶点失败", ex);
                    }
                    finally
                    {
                        IsInFocus = false;
                        _isLeftRightFindPeak = false;
                        _peakDiff.Clear();
                    }
                    if (!LeftRightFlag)
                    {
                        IsInFocus = false;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show("寻找焦点失败！");
                            LoggingService.Instance.LogError("寻找焦点失败", new Exception("未知错误"));
                        });
                        return;
                    }
                }
                else
                {
                    IsInFocus = false;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show("寻找焦点失败！");
                        //LoggingService.Instance.LogError("寻找焦点失败", new Exception("未知错误"));
                    });
                    return;
                }
                IsInFocus = false;
            });
        }

        private void InitScoll()
        {
            ScaleValue = scaleFactorValue;
            CanvasTransformOrigin = new Point(0, 0);
            CanvasTransform = new ScaleTransform(scaleFactorValue, scaleFactorValue);
        }

        public void Updata(object state)
        {
            if (_test != IntPtr.Zero)
            {
                _cameras.OnImageGet(_test);
            }

        }

        private void UpdateScoll(int picW, int picH)
        {
            double scale = Math.Min((double)WinW / picW, (double)WinH / picH);
            int i;

            if (scale < 0.1) scaleFactorValue = 0.1;
            else if (scale > 3) scaleFactorValue = 3;
            else scaleFactorValue = scale;
            ScaleValue = scaleFactorValue;
            CanvasTransformOrigin = new Point(0, 0);
            CanvasTransform = new ScaleTransform(scaleFactorValue, scaleFactorValue);

        }

        public void UpdateScrollViewerSize(double width, double height)
        {
            // 在这里可以对 ScrollViewer 的大小进行处理
            // 例如可以将大小值存储到相应的属性中
            WinW = width;
            WinH = height;
        }

        public void CCDOpen(CamerasAbstract cameras, System.Windows.Controls.ScrollViewer ImageViewer)
        {
            _cameras = cameras;
            if (_cameras != null)
            {
                try
                {
                    //string imagePath = @"D:\Images\test.png";

                    //BitmapImage bitmapImage = new BitmapImage();
                    //bitmapImage.BeginInit();
                    //bitmapImage.UriSource = new Uri(imagePath, UriKind.Absolute);
                    //bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 非常重要
                    //bitmapImage.EndInit();
                    //bitmapImage.Freeze(); // 推荐

                    //WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);

                   // ImageBMP = writeableBitmap();
                    if (cameras.CameraInit())
                    {
                        cameras.CameraImageEvent += Camera_CameraImageEvent;
                        CCDCaptureSnap(ImageViewer);
                        return;
                    }
                    CameraState = CameraState.Disconnected;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"发生错误,{ex.Message}");
                    CameraState = CameraState.Disconnected;
                }
            }
        }

        public bool OpenCapture(System.Windows.Controls.ScrollViewer ImageViewer)
        {
            if (CameraState == CameraState.Disconnected)
            {
                //string filePath = @"C:\Users\DELL\Desktop\2.png";
                //ImageBMP = ConvertToWriteableBitmap(filePath);
                CamerasAbstract cameras = new HikCamera();
                CCDOpen(cameras, ImageViewer);
                if (CameraState != CameraState.Disconnected)
                    IsContinueExposure = _cameras.SetAutoExposure(true, TimeOfExposure);
                return true;
            }
            else
            {
                CCDClose();
                return false;
            }
        }

        public async void Test_Clickdaoru()
        {
            string selectedFileName = await SelectFileAsync();
            if (string.IsNullOrEmpty(selectedFileName))
            {
                _test = IntPtr.Zero;
                return;
            }

            FileInfo selectedFile_info = new(selectedFileName);
            FileInfo file_name = new($"{ConfigStore.StoreDir}/calibration.bin");
            if (file_name.FullName != selectedFile_info.FullName)
            {
                if (file_name.Exists)
                {
                    file_name.IsReadOnly = false;
                    file_name.Delete();
                }
                selectedFile_info.CopyTo(file_name.FullName);
            }
            CoordinateHelper.Instance.Calibration = Calibration.LoadInstanceFromFile(file_name.FullName);

        }

        private void Camera_CameraImageEvent(IntPtr intPtr)
        {
            if (!(_cameras == null || intPtr == IntPtr.Zero || IsPause))
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CCDImageHelper.FromNativePointer(ImageBMP, intPtr, 3);
                }));
                if (IsInFocus)
                {
                    Task.Run(() =>
                    {
                        DateTime now = DateTime.Now;
                        var mat = CCDImageHelper.GetMatPointer(intPtr, 3, _imgWidth, _imgHeight);
                        _focusClarity.Add((now, ImageProcessHelper.CalClarity(mat)));
                    });
                }
                if (_isOnDownFindPeak)
                {
                    Task.Run(() =>
                    {
                        DateTime now = DateTime.Now;
                        var mat = CCDImageHelper.GetMatPointer(intPtr, 3, _imgWidth, _imgHeight);
                        _peakDiff.Add((now, ImageProcessHelper.CalculateOnDownDefinitionDifference(mat)));
                    });
                }
                else if (_isLeftRightFindPeak)
                {
                    Task.Run(() =>
                    {
                        DateTime now = DateTime.Now;
                        var mat = CCDImageHelper.GetMatPointer(intPtr, 3, _imgWidth, _imgHeight);
                        _peakDiff.Add((now, ImageProcessHelper.CalculateLeftRightDefinitionDifference(mat)));
                    });
                }                              
            }
        }

        public WriteableBitmap ConvertToWriteableBitmap(string filePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream; bitmapImage.EndInit();

            }
            bitmapImage.Freeze();
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;
            int stride = width * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[height * stride];
            bitmapImage.CopyPixels(pixels, stride, 0);
            WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, bitmapImage.Palette);
            writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return writeableBitmap;
        }

        public void CCDCaptureSnap(System.Windows.Controls.ScrollViewer ImageViewer)
        {
            int width = 100;
            int height = 100;
            _cameras?.KeepShot(out width, out height);
            InitialImageWidth = width;
            InitialImageHeight = height;
            ImageWidth = InitialImageWidth;
            ImageHeight = InitialImageHeight;
            ImageBMP = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, PixelFormats.Bgr24, null);
            CoordinateHelper.Instance.CenterPoint = new Point(ImageWidth / 2, ImageHeight / 2);
            UpdateScoll(InitialImageWidth, InitialImageHeight);

            CameraState = CameraState.CapturingImage;

        }

        public void CCDClose()
        {
            if (CameraState == CameraState.CapturingImage)
            {
                CCDCloseCapture();
            }
            _cameras.CamerasClose();
            CameraState = CameraState.Disconnected;
        }

        public void CCDCloseCapture()
        {
            _cameras?.Stop();
            if (ImageBMP != null)
            {
                ImageBMP = null;
            }
        }

        public void SetMouseCoordinatesText(Point mousePosition)
        {
            var point = CoordinateHelper.Instance.ConvertToAbsoluteFromReal(CoordinateHelper.Instance.ConvertToRealFromPix(mousePosition));
            MouseCoordinatesText = $"X: {point.X:0.000}, Y: {point.Y:0.000}";
        }

        public void GetScrollOffset(Point mousePosition, int delta, out double offsetX, out double offsetY)
        {
            if (ImageBMP == null)
            {
                offsetX = 0;
                offsetY = 0;
                return;
            }

            double oldMouseX = mousePosition.X;
            double oldMouseY = mousePosition.Y;

            double oldScaleFactor = scaleFactorValue;

            if (delta > 0 && scaleFactorValue < 3)
            {
                scaleFactorValue += 0.05;
            }
            else if (delta < 0 && scaleFactorValue > 0.2)
            {
                scaleFactorValue -= 0.05;
            }
            else
            {
                offsetX = 0;
                offsetY = 0;
                return;
            }

            double canvasScaleFactor = scaleFactorValue;

            offsetX = oldMouseX * (canvasScaleFactor - oldScaleFactor);
            offsetY = oldMouseY * (canvasScaleFactor - oldScaleFactor);

            CanvasTransformOrigin = new Point(0, 0);
            CanvasTransform = new ScaleTransform(canvasScaleFactor, canvasScaleFactor);
            ScaleValue = canvasScaleFactor;
        }

        //public void Setting()
        //{
        //    _cameras?.CamerasSetting();
        //}

        private async Task<string> SelectFileAsync()
        {
            string selectedFileName = null;

            await Task.Run(() =>
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    InitialDirectory = "c:\\",
                    Filter = "所有文件(*.*)|*.*",
                    RestoreDirectory = true
                };

                bool? result = dlg.ShowDialog();

                if (result == true)
                {
                    selectedFileName = dlg.FileName;
                }
            });

            return selectedFileName;
        }

        public void SetCrossBackground()
        {
            CanvasBackground = BackgroundHelper.Instance.GetCrosshairBrush(ImageWidth, ImageHeight);
        }

        public void SetBackground()
        {
            CanvasBackground = Brushes.Transparent;
        }

        private void OnShapePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectShape" && e is ValuePropertyChangedEventArgs valueArgs)
            {
                if (valueArgs.OldValue != null)
                {
                    Shape shape = (Shape)valueArgs.OldValue;
                    shape.IsSelect = false;
                }
                if (valueArgs.NewValue != null)
                {
                    Shape shape = (Shape)valueArgs.NewValue;
                    shape.IsSelect = true;
                }
            }
        }

        public void ReturnList()
        {
            TreeVisibility = Visibility.Collapsed;
            ListVisibility = Visibility.Visible;
        }

        public Point GetCameraPoint()
        {
            return CoordinateHelper.Instance.MachinePoint;
        }

        public bool SelectionContain(Shape shape)
        {
            return SelectShapes.Contains(shape);
        }

        public void DisplayShape()
        {
            if (SelectShapes.FirstOrDefault() is Shape shape)
            {
                MarkedWord = shape.DisplayShape();
            }
        }

        public Point? MoveShape()
        {
            if (SelectShapes.FirstOrDefault() is Shape shape && shape.MoveToShape() is Point target)
            {
                return target;
            }
            return CoordinateHelper.Instance.MachinePoint;  // 不动
        }

        public double[] MoveToCenter()
        {
            if (SelectShapes.FirstOrDefault() is Shape shape)
            {
                return shape.MoveToCenter();
            }
            return null;
        }

        public void ClearSelect()
        {
            List<Shape> newShapes = new List<Shape>();

            foreach (Shape shape in SelectShapes)
            {
                // 复制 Shape 对象并添加到新的 List<Shape>
                newShapes.Add(shape);
            }

            foreach (var shape in newShapes)
            {
                shape.IsSelect = false;
            }
        }

        public void PauseContinue()
        {
            IsPause = !IsPause;
            if (CameraState == CameraState.Paused)
            {
                CameraState = CameraState.CapturingImage;
            }
            else if (CameraState == CameraState.CapturingImage)
            {
                CameraState = CameraState.Paused;
            }
        }

        public void GetNowCenter()
        {
            NowCenter = CoordinateHelper.Instance.ConvertToAbsolute(new Point(InitialImageWidth / 2, InitialImageHeight / 2));
            IsGetCenter = true;
        }

        public void SetMVCenter(Point rightPosition)
        {
            if (IsGetCenter)
            {
                CoordinateHelper.Instance.ToolMVCenterPiex(rightPosition, NowCenter);
            }
            else
            {
                CoordinateHelper.Instance.ToolMVCenterPiex(rightPosition);
            }
            IsGetCenter = false;
        }

        public void Adjust()
        {
            _cameras?.Adjust();
        }

        public void DrawRule(Canvas cvRuler, Canvas cvVerticalRuler, System.Windows.Controls.ScrollViewer ImageViewer)
        {
            if (_cameraState == CameraState.Disconnected)
            {
                return;
            }
            if (CoordinateHelper.Instance.Calibration is Calibration)
            {
                DrawHorizontalRule(cvRuler, ImageViewer);
                DrawVerticalRule(cvVerticalRuler, ImageViewer);
            }

        }

        private void DrawHorizontalRule(Canvas cvRuler, System.Windows.Controls.ScrollViewer ImageViewer)
        {
            _cameras.GetHW(out int width, out int height);
            CoordinateHelper.Instance.GetFieldWH(width, height, out double fW, out double fH);

            if (cvRuler.Children != null)
            {
                cvRuler.Children.Clear();
            }

            System.Windows.Shapes.Line _line;
            TextBlock _textBlock;

            const double _minPixel = 30;
            string _unit = "mm";
            double _interval;
            double _intervalPixel;
            double _scientificF;
            int _scientificE;
            string[] _strTemp = (_minPixel / (ImageWidth * ScaleValue / fW)).ToString("E").Split('E');
            double.TryParse(_strTemp[0], out _scientificF);
            int.TryParse(_strTemp[1], out _scientificE);
            if (_scientificE >= 2 || (_scientificE >= 1 && _scientificF >= 5))
            {
                _unit = "m";
                _scientificE -= 3;
            }

            _textBlock = new TextBlock();
            _textBlock.Text = _unit;
            _textBlock.FontSize = 8;
            _textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            _textBlock.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            _textBlock.Margin = new Thickness(10, 25, 0, 0);
            cvRuler.Children.Add(_textBlock);

            if (_scientificF >= 5)
            {
                _interval = 10 * Math.Pow(10, _scientificE);
            }
            else if (_scientificF >= 2.5)
            {
                _interval = 5 * Math.Pow(10, _scientificE);
            }
            else
            {
                _interval = 2.5 * Math.Pow(10, _scientificE);
            }

            if (_unit == "mm")
            {
                _intervalPixel = _interval * (ImageWidth * ScaleValue / fW);
            }
            else
            {
                _intervalPixel = _interval * 1000 * (ImageWidth * ScaleValue / fW);
            }
            double left = ImageViewer.HorizontalOffset;
            int _lineIndex = 0;
            double _width = cvRuler.ActualWidth;
            double _pixelDistence = _intervalPixel / 5;
            for (double i = 0; i < _width; i += _pixelDistence)
            {
                _line = new System.Windows.Shapes.Line();
                if (_lineIndex % 5 == 0)
                {
                    _line.Stroke = Brushes.Black;
                    _line.StrokeThickness = 1;
                    _line.X1 = i;
                    _line.Y1 = 50;
                    _line.X2 = i;
                    _line.Y2 = 30;

                    _textBlock = new TextBlock();
                    _textBlock.Text = (_interval * (_lineIndex / 5 + (left / _intervalPixel))).ToString("0.00");
                    _textBlock.FontSize = 8;
                    _textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    _textBlock.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    _textBlock.Margin = new Thickness(i, 15, 0, 0);
                    cvRuler.Children.Add(_textBlock);
                }
                else
                {
                    _line.Stroke = Brushes.DimGray;
                    _line.StrokeThickness = 1;
                    _line.X1 = i;
                    _line.Y1 = 50;
                    _line.X2 = i;
                    _line.Y2 = 35;
                }
                cvRuler.Children.Add(_line);

                _lineIndex++;
            }
        }

        private void DrawVerticalRule(Canvas cvVerticalRuler, System.Windows.Controls.ScrollViewer ImageViewer)
        {
            _cameras.GetHW(out int width, out int height);
            CoordinateHelper.Instance.GetFieldWH(width, height, out double fW, out double fH);
            if (cvVerticalRuler.Children != null)
            {
                cvVerticalRuler.Children.Clear();
            }

            System.Windows.Shapes.Line _line;
            TextBlock _textBlock;

            const double _minPixel = 30;
            string _unit = "mm";
            double _interval;
            double _intervalPixel;
            double _scientificF;
            int _scientificE;
            string[] _strTemp = (_minPixel / (ImageHeight * ScaleValue / fH)).ToString("E").Split('E');
            double.TryParse(_strTemp[0], out _scientificF);
            int.TryParse(_strTemp[1], out _scientificE);
            if (_scientificE >= 2 || (_scientificE >= 1 && _scientificF >= 5))
            {
                _unit = "m";
                _scientificE -= 3;
            }

            _textBlock = new TextBlock();
            _textBlock.Text = _unit;
            _textBlock.FontSize = 8;
            _textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            _textBlock.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            _textBlock.Margin = new Thickness(25, 10, 0, 0);
            cvVerticalRuler.Children.Add(_textBlock);

            if (_scientificF >= 5)
            {
                _interval = 10 * Math.Pow(10, _scientificE);
            }
            else if (_scientificF >= 2.5)
            {
                _interval = 5 * Math.Pow(10, _scientificE);
            }
            else
            {
                _interval = 2.5 * Math.Pow(10, _scientificE);
            }

            if (_unit == "mm")
            {
                _intervalPixel = _interval * (ImageHeight * ScaleValue / fH);
            }
            else
            {
                _intervalPixel = _interval * 1000 * (ImageHeight * ScaleValue / fH);
            }


            double top = ImageViewer.VerticalOffset;

            int _lineIndex = 0;
            double _height = cvVerticalRuler.ActualHeight;
            double _pixelDistence = _intervalPixel / 5;
            for (double i = 0; i < _height; i += _pixelDistence)
            {
                _line = new System.Windows.Shapes.Line();
                if (_lineIndex % 5 == 0)
                {
                    _line.Stroke = Brushes.Black;
                    _line.StrokeThickness = 1;
                    _line.X1 = 50;
                    _line.Y1 = i;
                    _line.X2 = 30;
                    _line.Y2 = i;

                    _textBlock = new TextBlock();
                    _textBlock.Text = (_interval * (_lineIndex / 5 + (top / _intervalPixel))).ToString("0.00");
                    _textBlock.FontSize = 8;
                    _textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    _textBlock.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    _textBlock.Margin = new Thickness(15, i, 0, 0);
                    cvVerticalRuler.Children.Add(_textBlock);
                }
                else
                {
                    _line.Stroke = Brushes.DimGray;
                    _line.StrokeThickness = 1;
                    _line.X1 = 50;
                    _line.Y1 = i;
                    _line.X2 = 35;
                    _line.Y2 = i;
                }
                cvVerticalRuler.Children.Add(_line);

                _lineIndex++;
            }
        }

        private void Execute_SetAutoExposure()
        {
            if (CameraState == CameraState.Disconnected) { return; }

            IsContinueExposure = _cameras.SetAutoExposure(IsContinueExposure, TimeOfExposure);
        }

        private void Execute_GetCameraParam()
        {
            if (CameraState == CameraState.Disconnected) { return; }

            string timeOfExposure = "empty";
            string gain = "empty";
            string framerate = "empty";
            _cameras.GetCameraPatam(out timeOfExposure, out gain, out framerate);

            if (timeOfExposure != "empty") TimeOfExposure = timeOfExposure;
            if (gain != "empty") Gain = gain;
            if (framerate != "empty") FrameRate = framerate;
        }
        //IsContinueExposure
        private void Execute_SetCameraParam()
        {
            if (CameraState == CameraState.Disconnected) { return; }

            _cameras.SetCameraParam(TimeOfExposure, Gain, FrameRate);
        }
    }
}
