using CCD.tools;
using PublishTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IronPython.Modules._ast;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using CCD.shapes;
using System.Windows;

namespace CCD.libs
{
    internal class PointCircle : Circle
    {
        public List<CameraPoint> ThreePoints = new();
        private int drawMode = 0;


        public PointCircle()
        {
            Name = "圆";
        }

        public PointCircle(Point p1, Point p2, Point p3)
        {
            Name = "圆";
            List<Point> points = new List<Point>() { p1, p2, p3 };
            GeometryHelper.CalculateCircle(points, out Point center, out double radius);
            Center = new() { SetPixPoint = center };
            Radius = radius;

            //SetCachedValue(nameof(AbsoluteCenter), center);
            //SetCachedValue(nameof(RealRadius), radius);
            //SetCachedValue(nameof(RealCenter), new Point());
        }
        public PointCircle(List<Point> points)
        {
            Name = "圆";
            GeometryHelper.CalculateCircle(points, out Point center, out double radius);

            //AbsoluteCenter = center;
            //Center = CoordinateHelper.Instance.ConvertToPix(CoordinateHelper.Instance.ConvertToRealByAbsolute(CoordinateHelper.Instance.MachinePoint, center));
            //Radius = CoordinateHelper.Instance.LineDistance(Center, CoordinateHelper.Instance.ConvertToPix(CoordinateHelper.Instance.ConvertToRealByAbsolute(CoordinateHelper.Instance.MachinePoint, points[0])));

            //_center = CoordinateHelper.Instance.ConvertToPix(CoordinateHelper.Instance.ConvertToReal(center));
            //Radius = CoordinateHelper.Instance.LineDistance(Center, CoordinateHelper.Instance.ConvertToReal(points[0]));

            //SetCachedValue(nameof(AbsoluteCenter), center);
            //SetCachedValue(nameof(RealRadius), radius);
            //SetCachedValue(nameof(RealCenter), new Point());
        }
        public void AddPoints(Point point)
        {
            ThreePoints.Add(new() { SetPixPoint = point });
        }
        public override void ShapeMove(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            Center?.MachineMoved();
            foreach (var p in ThreePoints)
                p.MachineMoved();
        }

        public void UpdatePoints(Point point)
        {
            if (ThreePoints.Count < drawMode + 1)
                ThreePoints.Add(new() { SetPixPoint = point });
            else
                ThreePoints.Last().SetPixPoint = point;

            if (ThreePoints.Count == 3)
                RefreshPointCircle();
        }

        public void NextStep()
        {
            drawMode++;
        }

        public bool IsFinish()
        {
            return drawMode == 3;
        }

        private void RefreshPointCircle()
        {
            Point pix_center;
            double radius;
            if (CoordinateHelper.Instance.isCalibrationMode) // 标定模式
            {
                GeometryHelper.CalculateCircle(ThreePoints.Select(x => x.PixPoint).ToList(), out pix_center, out radius);
            }
            else
            {
                GeometryHelper.CalculateCircle(ThreePoints.Select(x => x.MacPoint).ToList(), out Point center, out radius);
                pix_center = CoordinateHelper.Instance.ConvertToPix(CoordinateHelper.Instance.ConvertToReal(center));
            }
            Center = new() { SetPixPoint = pix_center };
            Radius = CoordinateHelper.LineDistance(ThreePoints.First().PixPoint, Center.PixPoint);
            RealRadius = radius;
        }
        public override void Draw(DrawingContext drawingContext)
        {
            // 绘制圆形
            if (ThreePoints.Count == 2)
            {
                drawingContext.DrawLine(Pen, ThreePoints[0].PixPoint, ThreePoints[1].PixPoint);
            }
            else if (ThreePoints.Count == 3)
            {
                drawingContext.DrawEllipse(null, Pen, Center.PixPoint, Radius, Radius);
                drawingContext.DrawEllipse(null, Pen, Center.PixPoint, 1, 1);   // 绘制一个点
            }

        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            if (ThreePoints.Count == 3)
            {
                drawingContext.DrawLine(LightShape(Pen), ThreePoints[0].PixPoint, ThreePoints[1].PixPoint);
                drawingContext.DrawLine(LightShape(Pen), ThreePoints[1].PixPoint, ThreePoints[2].PixPoint);
                drawingContext.DrawEllipse(null, LightShape(Pen), Center.PixPoint, Radius, Radius);
                drawingContext.DrawEllipse(null, LightShape(Pen), Center.PixPoint, 1, 1);   // 绘制一个点
            }
        }
    }
}
