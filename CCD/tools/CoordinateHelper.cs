using CCD.libs;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;

namespace CCD.tools
{
    public class CoordinateHelper : BindableBase
    {
        private static readonly CoordinateHelper instance = new();

        public static CoordinateHelper Instance => instance;

        /// <summary>
        /// 是否处在标定模式
        /// </summary>
        public bool isCalibrationMode = false;

        public Point CenterPoint { get; set; }

        public delegate double[] PointHandler();
        public PointHandler GetCameraPosition;


        public delegate void Move3DHandler(double[] nums);
        public Move3DHandler GetMoveHandler;


        public delegate void MoveHandler(double x, double y);
        public Move3DHandler GetMVCenterHandler;
        public MoveHandler GetMerCenterHandler;

        public delegate void CenterHandler(double x, double y);
        public CenterHandler GetCenterHandler;

        public delegate void RotationHandler(double angle);
        public RotationHandler GetRotationHandler;

        private Calibration _calibration;
        public Calibration Calibration
        {
            get { return _calibration; }
            set { SetProperty(ref _calibration, value); }
        }

        public Point MachinePoint
        {
            get { return GetCameraPoint(); }
        }
        /// <summary>
        /// 相机坐标系原点，相机坐标系Z轴方向，相机坐标系X轴方向
        /// </summary>
        public (Point3D, Vector3D, Vector3D) MachinePoint3D
        {
            get => Get3DCameraPoint();
        }

        private readonly string _binName = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/{Assembly.GetEntryAssembly().GetName().Name}/calibration.bin";

        private CoordinateHelper()
        {
            Calibration = Calibration.LoadInstanceFromFile(_binName);
        }


        private Point GetCameraPoint()
        {
            //获取机床坐标
            if (GetCameraPosition != null)
            {
                double[] cameraPosition;
                cameraPosition = GetCameraPosition();
                if (cameraPosition != null && cameraPosition.Length >= 2)
                {
                    return new Point(cameraPosition[0], cameraPosition[1]);
                }
            }
            return new Point(0, 0);
        }
        private (Point3D, Vector3D, Vector3D) Get3DCameraPoint()
        {
            //获取机床坐标
            if (GetCameraPosition != null)
            {
                double[] cameraPosition;
                cameraPosition = GetCameraPosition();
                if (cameraPosition != null && cameraPosition.Length >= 9)
                {
                    return (new(cameraPosition[0], cameraPosition[1], cameraPosition[2]),
                        new(cameraPosition[3], cameraPosition[4], cameraPosition[5]),
                        new(cameraPosition[6], cameraPosition[7], cameraPosition[8]));
                }
            }
            return (new(0, 0, 0), new(0, 0, 1), new(1, 0, 0));
        }
        public Vector GetCameraRelative()
        {
            if (Calibration != null)
            {
                return Calibration.careamRelative;
            }
            return new Vector(0, 0);
        }

        private Point GetRelativePoint(Point point)
        {
            if (Calibration == null)
            {
                return point;
            }

            return Calibration.GetRelativePoint(point);
        }

        /// <summary>
        /// 从相机系坐标转为像素坐标
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        public Point ConvertToPix(Point currentPoint)
        {
            if (Calibration == null)
            {
                return currentPoint;
            }

            return Calibration.ConvertToPix(currentPoint);
        }

        public Point ConvertToAbsolute(Point mPoint, Point currentPoint)
        {
            if (Calibration == null)
            {
                return currentPoint;
            }

            return ConvertToAbsoluteByReal(mPoint, GetRelativePoint(currentPoint));
        }

        public Point ConvertToAbsolute(Point currentPoint)
        {
            return ConvertToAbsolute(MachinePoint, currentPoint);
        }

        /// <summary>
        /// 通过机床坐标和相机系坐标得到机床系坐标
        /// </summary>
        /// <param name="mPoint">机床坐标</param>
        /// <param name="realPoint">相机系坐标</param>
        /// <returns></returns>
        public Point ConvertToAbsoluteByReal(Point mPoint, Point realPoint)
        {
            return RealPoint(mPoint, realPoint);
        }
        public Point ConvertToRealByAbsolute(Point mPoint, Point AbsolutePoint)
        {
            return Absolute(mPoint, AbsolutePoint);
        }
        /// <summary>
        /// 由绝对坐标转换到相机系下的坐标
        /// </summary>
        /// <param name="AbsolutePoint"></param>
        /// <returns></returns>
        public Point ConvertToReal(Point AbsolutePoint)
        {
            (Point3D point, Vector3D dir_z, Vector3D dir_x) = MachinePoint3D;   // 当前相机中心点的坐标
            Point origin_camera = new(point.X, point.Y);    // 相机原点坐标

            var new_real_without_rotate = AbsolutePoint - origin_camera;  // 得到新的相机系坐标(旋转之前)
            double angle = Vector.AngleBetween(new Vector(1, 0), new Vector(dir_x.X, dir_x.Y));
            Matrix rotationMatrix = Matrix.Identity;
            rotationMatrix.Rotate(-angle);
            var new_real = (Point)Vector.Multiply(new_real_without_rotate, rotationMatrix); // 得到旋转之后的相机系坐标
            return new_real;
        }
        public Point ConvertToAbsoluteFromReal(Point RealPoint)
        {
            (Point3D point, Vector3D dir_z, Vector3D dir_x) = MachinePoint3D;   // 当前相机中心点的坐标
            if (isCalibrationMode)
            {
                dir_x = new(1, 0, 0);
                dir_z = new(0, 0, 1);
            }
            Vector origin_camera = new(point.X, point.Y);    // 相机原点坐标

            Vector real_with_rotate = (Vector)RealPoint; // 变成向量
            double angle = Vector.AngleBetween(new Vector(1, 0), new Vector(dir_x.X, dir_x.Y));
            Matrix rotationMatrix = Matrix.Identity;
            rotationMatrix.Rotate(angle);
            var real_point = (Point)Vector.Multiply(real_with_rotate, rotationMatrix); // 得到旋转之后的相机系坐标

            var abso = real_point + origin_camera;  // 加入机床偏移
            return abso;
        }
        /// <summary>
        /// 从像素坐标转换为相机系坐标
        /// </summary>
        /// <param name="currentPoint">像素坐标</param>
        /// <returns></returns>
        public Point ConvertToRealFromPix(Point currentPoint)
        {
            if (Calibration == null)
            {
                return currentPoint;
            }

            return GetRelativePoint(currentPoint);

        }

        private Point RealPoint(Point point1, Point point2)
        {
            return new Point(point1.X + point2.X, point1.Y + point2.Y);
        }
        private Point Absolute(Point point1, Point point2)
        {
            return new Point(point2.X - point1.X, point2.Y - point1.Y);
        }
        public double CalculateDistance(Point point1, Point point2)
        {

            if (Calibration == null)
            {
                return LineDistance(point1, point2);
            }

            //Point rPoint1 = Calibration.GetRelativePoint(point1);
            Point rPoint2 = GetRelativePoint(point2);

            return LineDistance(point1, rPoint2);
        }

        /// <summary>
        /// 从像素点获取两点之间的距离
        /// </summary>
        /// <returns></returns>
        public double GetRealDistanceFromPix(Point p1, Point p2)
        {
            if (Calibration == null)
                return LineDistance(p1, p2);

            var p1_real = ConvertToRealFromPix(p1);
            var p2_real = ConvertToRealFromPix(p2);

            return LineDistance(p1_real, p2_real);
        }


        public string FittingCoord(Point mirrorPoint, Point nowPoint, List<Point> points, List<Point> points2)
        {
            Calibration = Calibration.CreateAndSaveInstance(nowPoint - mirrorPoint, CenterPoint, points, points2, _binName);
            if (Calibration == null)
            {
                return "标定失败";
            }

            Calibration = Calibration.LoadInstanceFromFile(_binName);
            return $"相机相对振镜坐标{Calibration.careamRelative:F04}。标定成功,请关闭界面";
        }

        public static double LineDistance(Point point1, Point point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double LineAngle(Point point1, Point point2)
        {
            double deltaX = point2.X - point1.X;
            double deltaY = point2.Y - point1.Y;

            // 处理直线斜率为无穷大或负无穷大的情况
            if (deltaX == 0)
            {
                return 90;
            }

            double radians = Math.Atan2(deltaY, deltaX);
            double angle = radians * (180 / Math.PI);

            // 调整角度到 [-90, 90] 范围内
            if (angle > 90)
            {
                angle -= 180;
            }
            else if (angle < -90)
            {
                angle += 180;
            }

            return angle;
        }
        public Point LineMidPoint(Point point1, Point point2)
        {
            return new Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        public void GetFieldWH(int pixW, int pixH, out double width, out double height)
        {
            Point LT = GetRelativePoint(new(0, 0));
            Point RB = GetRelativePoint(new(pixW, pixH));

            width = RB.X - LT.X;
            height = LT.Y - RB.Y;
        }

        public void ToolMovePiex(Point point)
        {
            Point point1 = ConvertToRealFromPix(point);
            (Point3D point3D, Vector3D vectorz, Vector3D vectorx) = Get3DCameraPoint();
            Vector3D diry = Vector3D.CrossProduct(vectorz, vectorx);
            var newpoint = point3D + vectorx * point1.X + diry * point1.Y;
            double[] nums = new double[9] { newpoint.X, newpoint.Y, newpoint.Z, vectorz.X, vectorz.Y, vectorz.Z, vectorx.X, vectorx.Y, vectorx.Z };
            ToolMove(nums);
        }

        public void ToolCenterPiex(Point point)
        {
            Point point1 = ConvertToAbsolute(MachinePoint, point);
            ToolCenter(point1.X, point1.Y);
        }

        public void ToolMVCenterPiex(Point point)
        {
            //Point point1 = GetRelativePoint(point);
            double[] nums = new double[4] { point.X, point.Y, 0, 0 };
            ToolMVCenter(nums);
        }

        public void ToolMerCenterPiex(Point point)
        {
            Point point1 = ConvertToAbsolute(point);
            ToolMerCenter(point1.X, point1.Y);
        }

        public void ToolMerCenterAbsolute(Point point)
        {
            ToolMerCenter(point.X, point.Y);
        }

        public void ToolMVCenterPiex(Point point, Point center)
        {
            //Point point1 = ConvertToAbsolute(point);
            double[] nums = new double[4] { point.X, point.Y, center.X, center.Y };
            ToolMVCenter(nums);
        }

        public void ToolMoveAbsolute(Point point)
        {
            (Point3D point3D, Vector3D vectorz, Vector3D vectorx) = Get3DCameraPoint();
            double[] nums = new double[9] { point.X, point.Y, point3D.Z, vectorz.X, vectorz.Y, vectorz.Z, vectorx.X, vectorx.Y, vectorx.Z };
            ToolMove(nums);
        }
        public void ToolMoveAbsolute(Point3D point)
        {
            (_, Vector3D vectorz, Vector3D vectorx) = Get3DCameraPoint();
            double[] nums = new double[9] { point.X, point.Y, point.Z, vectorz.X, vectorz.Y, vectorz.Z, vectorx.X, vectorx.Y, vectorx.Z };
            ToolMove(nums);
        }

        public void ToolCenterAbsolute(double x, double y)
        {
            ToolCenter(x, y);
        }
        public void ToolRotationAbsolute(double angle)
        {
            ToolRotation(angle);
        }

        private void ToolMVCenter(double[] nums)
        {
            try
            {
                GetMVCenterHandler?.Invoke(nums);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ToolMerCenter(double x, double y)
        {
            try
            {
                GetMerCenterHandler?.Invoke(x, y);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ToolMove(double[] nums)
        {
            try
            {
                GetMoveHandler?.Invoke(nums);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ToolCenter(double x, double y)
        {
            try
            {
                GetCenterHandler?.Invoke(x, y);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void ToolRotation(double angle)
        {
            try
            {
                GetRotationHandler?.Invoke(angle);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
