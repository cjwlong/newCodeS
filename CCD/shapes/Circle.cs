using CCD.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CCD.shapes.Shape;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using netDxf;
using System.Windows;
using CCD.tools;

namespace CCD.shapes
{
    internal class Circle : Shape
    {
        public CameraPoint Center { get; set; }
        protected double _radius;
        public double Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                RealRadius = CoordinateHelper.Instance.GetRealDistanceFromPix(Center.PixPoint, new Point(Center.PixPoint.X + value, Center.PixPoint.Y));
            }
        }

        private double _realRadius;
        public double RealRadius
        {
            get { return _realRadius; }
            set { _realRadius = value; }
        }

        public double AbsoluteArea
        {
            get { return CalculateCircleArea(RealRadius); }
        }

        public double AbsolutePerimeter
        {
            get { return CalculateCirclePerimeter(RealRadius); }
        }

        public Circle()
        {
            Name = "圆";
        }

        // 计算圆形周长
        private double CalculateCirclePerimeter(double radius)
        {
            return 2 * Math.PI * radius;
        }

        // 计算圆形面积
        private double CalculateCircleArea(double radius)
        {
            return Math.PI * radius * radius;
        }

        public override void ShapeMove(Vector vector)
        {
            Center?.MachineMovedInCalibration(vector);
        }
        public override void ShapeMove(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            //RealCenter = CoordinateHelper.Instance.ConvertToReal(AbsoluteCenter); // 计算相机系下的坐标
            //_center = CoordinateHelper.Instance.ConvertToPix(RealCenter);      // 计算像素坐标
            Center?.MachineMoved();
        }


        public override Point? MoveToShape()
        {
            return Center.MacPoint;
        }

        public override double[] MoveToCenter()
        {
            return new double[] { Center.MacPoint.X, Center.MacPoint.Y, 0 };
        }

        public override string DisplayShape()
        {
            return $"圆心坐标({Center.MacPoint:F3}),半径为{RealRadius:F3}mm";
        }

        public override void Draw(DrawingContext drawingContext)
        {
            // 绘制圆形
            drawingContext.DrawEllipse(null, Pen, Center.PixPoint, Radius, Radius);
            drawingContext.DrawEllipse(null, Pen, Center.PixPoint, 1, 1);   // 绘制一个点
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(null, LightShape(Pen), Center.PixPoint, Radius, Radius);
            drawingContext.DrawEllipse(null, LightShape(Pen), Center.PixPoint, 1, 1);   // 绘制一个点
        }

        public override ShapeDto GetDto()
        {
            return new CircleDto(this);
        }
    }

    internal class CircleDto : ShapeDto
    {
        public Point Center { get; set; }
        public double Radius { get; set; }

        public CircleDto(Circle shape) : base(shape)
        {
            Center = shape.Center.CamPoint;
            Radius = shape.RealRadius;
        }

        public override List<netDxf.Entities.EntityObject> ToDxf()
        {
            netDxf.Entities.Circle circle = new netDxf.Entities.Circle(new Vector2(Center.X, Center.Y), Radius);
            return new List<netDxf.Entities.EntityObject> { circle };
        }

        public override MeshGeometry3D ToSTL()
        {
            int segments = 32;
            MeshGeometry3D circleMesh = new MeshGeometry3D();

            Point3D center = new Point3D(Center.X, Center.Y, 0); // 圆心
            double angleIncrement = 2 * Math.PI / segments;

            // 添加圆心
            circleMesh.Positions.Add(center);

            // 添加圆上的顶点
            for (int i = 0; i < segments; i++)
            {
                double x = Radius * Math.Cos(i * angleIncrement);
                double y = Radius * Math.Sin(i * angleIncrement);
                circleMesh.Positions.Add(new Point3D(Center.X + x, Center.Y + y, 0));
            }

            // 添加三角面片
            for (int i = 1; i <= segments; i++)
            {
                if (i == segments)
                {
                    circleMesh.TriangleIndices.Add(0);
                    circleMesh.TriangleIndices.Add(i);
                    circleMesh.TriangleIndices.Add(1);
                }
                else
                {
                    circleMesh.TriangleIndices.Add(0);
                    circleMesh.TriangleIndices.Add(i);
                    circleMesh.TriangleIndices.Add(i + 1);
                }
            }

            return circleMesh;
        }
    }
}
