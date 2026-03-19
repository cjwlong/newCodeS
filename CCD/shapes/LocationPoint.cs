using CCD.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CCD.shapes.Shape;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using netDxf;

namespace CCD.shapes
{
    internal class LocationPoint : Shape
    {
        public CameraPoint Point { get; set; }

        public double X
        {
            get { return Point.MacPoint.X; }
        }

        public double Y
        {
            get { return Point.MacPoint.Y; }
        }

        //public double Angle { get; set; }

        public LocationPoint()
        {
            Name = "点";
        }

        public LocationPoint(double x, double y)
        {
            Name = "点";
            //PixPoint = new Point(x, y);
            Point = new() { SetPixPoint = new(x, y) };
        }

        public override void ShapeMove(Vector vector)
        {
            Point.MachineMovedInCalibration(vector);
        }
        public override void ShapeMove(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            //RealPoint = CoordinateHelper.Instance.ConvertToReal(AbsolutePoint); // 计算相机系下的坐标
            //_pixPoint = CoordinateHelper.Instance.ConvertToPix(RealPoint);      // 计算像素坐标
            Point.MachineMoved();
        }

        public override Point? MoveToShape()
        {
            return Point.MacPoint;
        }

        public override double[] MoveToCenter()
        {
            return new double[] { Point.MacPoint.X, Point.MacPoint.Y, 0 };
        }

        public override void Draw(DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(new SolidColorBrush(Colors.Red), null, Point.PixPoint, 1, 1); // 绘制一个椭圆，即点
            drawingContext.DrawEllipse(null, Pen, Point.PixPoint, 20, 20);
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(new SolidColorBrush(Colors.Red), null, Point.PixPoint, 1, 1); // 绘制一个椭圆，即点
            drawingContext.DrawEllipse(null, LightShape(Pen), Point.PixPoint, 20, 20);
        }

        public override ShapeDto GetDto()
        {
            return new LocationPointDto(this);
        }

        public override string ToString()
        {
            return $"({Point.MacPoint.X:F3}, {Point.MacPoint.Y:F3})";
        }
    }

    internal class LocationPointDto : ShapeDto
    {
        public Point Center { get; set; }

        public LocationPointDto(LocationPoint shape) : base(shape)
        {
            Center = shape.Point.CamPoint;
        }

        public override List<netDxf.Entities.EntityObject> ToDxf()
        {
            netDxf.Entities.Point point = new netDxf.Entities.Point(new Vector2(Center.X, Center.Y));
            return new List<netDxf.Entities.EntityObject> { point };
        }

        public override MeshGeometry3D ToSTL()
        {
            return null;
        }
    }
}
