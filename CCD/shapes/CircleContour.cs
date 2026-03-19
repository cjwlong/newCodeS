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
using CCD.tools;

namespace CCD.shapes
{
    public class CircleContour : Shape
    {
        private int drawMode = 0;

        private Point _center;
        public Point Center
        {
            get { return _center; }
            set
            {
                _center = value;
                RealCenter = CoordinateHelper.Instance.ConvertToRealFromPix(value);
                AbsoluteCenter = CoordinateHelper.Instance.ConvertToAbsoluteByReal(MachinePoint, RealCenter);
            }
        }
        private double _radius1;
        public double Radius1
        {
            get { return _radius1; }
            set
            {
                _radius1 = value;
                RealRadius1 = CoordinateHelper.Instance.CalculateDistance(RealCenter, new Point(Center.X + value, Center.Y));
            }
        }

        private double _radius2;
        public double Radius2
        {
            get { return _radius2; }
            set
            {
                _radius2 = value;
                RealRadius2 = CoordinateHelper.Instance.CalculateDistance(RealCenter, new Point(Center.X + value, Center.Y));
            }
        }

        private Point _realCenter;
        public Point RealCenter
        {
            get { return _realCenter; }
            set { _realCenter = value; }

        }

        private double _realRadius1;
        public double RealRadius1
        {
            get { return _realRadius1; }
            set { _realRadius1 = value; }
        }

        private double _realRadius2;
        public double RealRadius2
        {
            get { return _realRadius2; }
            set { _realRadius2 = value; }
        }

        private Point _absoluteCenter;
        public Point AbsoluteCenter
        {
            get { return _absoluteCenter; }
            set { _absoluteCenter = value; }
        }


        public CircleContour()
        {
            Name = "圆形轮廓";
            Pen.Freeze();
        }

        public List<Point> FirstPoints = new(3);
        public List<Point> SecondPoints = new(1);

        public void AddPoints(Point point)
        {
            FirstPoints.Add(point);
        }

        public void UpdatePoints(Point point)
        {
            if (drawMode < 3)
            {
                if (FirstPoints.Count < drawMode + 1)
                {
                    FirstPoints.Add(point);
                }
                else
                {
                    FirstPoints[^1] = point;
                }
                if (FirstPoints.Count == 3)
                {
                    CalculateCircle(FirstPoints, out Point center, out double radius);
                    Center = center;
                    Radius1 = radius;

                }
            }
            else
            {
                if (SecondPoints.Count == 0)
                {
                    SecondPoints.Add(point);
                }
                else
                {
                    SecondPoints[^1] = point;
                }
                if (SecondPoints.Count == 1)
                {
                    Radius2 = (SecondPoints[0] - Center).Length;
                }
            }


        }


        public void NextStep()
        {
            drawMode++;

        }

        public bool IsFinish()
        {
            return drawMode == 4;
        }

        public override void ShapeMove(Vector vector)
        {
            RealCenter += vector;
            _center = CoordinateHelper.Instance.ConvertToPix(RealCenter);
        }

        public override void Draw(DrawingContext drawingContext)
        {
            // 创建四点形的路径
            GeometryGroup geometryGroup = new GeometryGroup();
            Geometry geometry = CreatePathGeometry(FirstPoints);
            if (geometry != null)
            {
                geometryGroup.Children.Add(geometry);
            }

            if (SecondPoints.Count == 1)
            {
                geometryGroup.Children.Add(new EllipseGeometry(Center, Radius2, Radius2));
            }
            geometryGroup.Freeze();
            // 绘制路径
            drawingContext.DrawGeometry(null, Pen, geometryGroup);
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            // 创建四点形的路径
            GeometryGroup geometryGroup = new GeometryGroup();
            if (FirstPoints.Count == 3)
            {
                geometryGroup.Children.Add(new EllipseGeometry(Center, Radius1, Radius1));
            }

            if (SecondPoints.Count == 1)
            {
                geometryGroup.Children.Add(new EllipseGeometry(Center, Radius2, Radius2));
            }

            geometryGroup.Freeze();
            // 绘制路径
            drawingContext.DrawGeometry(null, LightShape(Pen), geometryGroup);
        }



        private Geometry CreatePathGeometry(List<Point> points)
        {
            if (points.Count == 1)
            {
                return new LineGeometry(points[0], points[0]);
            }
            else if (points.Count == 2)
            {
                return new LineGeometry(points[0], points[1]);
            }
            else if (points.Count == 3)
            {

                return new EllipseGeometry(Center, Radius1, Radius1);
            }
            else
            {
                return null;
            }
        }

        public void CalculateCircle(List<Point> points, out Point center, out double radius)
        {
            // 令：
            // A1 = 2 * pt2.X - 2 * pt1.X       B1 = 2 * pt1.Y - 2 * pt2.Y        C1 = pt1.Y² + pt2.X² - pt1.X² - pt2.Y²
            // A2 = 2 * pt3.X - 2 * pt2.X       B2 = 2 * pt2.Y - 2 * pt3.Y        C2 = pt2.Y² + pt3.X² - pt2.X² - pt3.Y²
            double A1, A2, B1, B2, C1, C2, temp;
            Point pt1 = points[0];
            Point pt2 = points[1];
            Point pt3 = points[2];
            A1 = pt1.X - pt2.X;
            B1 = pt1.Y - pt2.Y;
            C1 = (Math.Pow(pt1.X, 2) - Math.Pow(pt2.X, 2) + Math.Pow(pt1.Y, 2) - Math.Pow(pt2.Y, 2)) / 2;
            A2 = pt3.X - pt2.X;
            B2 = pt3.Y - pt2.Y;
            C2 = (Math.Pow(pt3.X, 2) - Math.Pow(pt2.X, 2) + Math.Pow(pt3.Y, 2) - Math.Pow(pt2.Y, 2)) / 2;

            // 为了方便编写程序，令 temp = A1*B2 - A2*B1
            temp = A1 * B2 - A2 * B1;

            // 判断三点是否共线
            if (temp == 0)
            {
                // 共线则将第一个点 pt1 作为圆心
                center = new Point(pt1.X, pt1.Y);
                radius = 0;
            }
            else
            {
                center = new Point((C1 * B2 - C2 * B1) / temp, (A1 * C2 - A2 * C1) / temp);
                radius = Math.Sqrt(Math.Pow(center.X - pt1.X, 2) + Math.Pow(center.Y - pt1.Y, 2));
            }
        }

        public int DetermineClosestCircle(Point point)
        {
            double distance = CalculateDistance(point, Center);
            double distanceToInnerCircle = Math.Abs(distance - Radius1);
            double distanceToOuterCircle = Math.Abs(distance - Radius2);

            if (distanceToInnerCircle <= distanceToOuterCircle)
            {
                return 1; // 表示离内圆更近
            }
            else
            {
                return 2; // 表示离外圆更近
            }

        }

        public void UpdateProperty(int propertyIndex, Point point)
        {
            if (propertyIndex == 1)
            {
                Radius1 = CalculateDistance(point, Center);
            }
            else if (propertyIndex == 2)
            {
                Radius2 = CalculateDistance(point, Center);
            }
        }

        private double CalculateDistance(Point point1, Point point2)
        {
            double deltaX = point2.X - point1.X;
            double deltaY = point2.Y - point1.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public override ShapeDto GetDto()
        {
            return new CircleContourDto(this);
        }
    }

    public class CircleContourDto : ShapeDto
    {
        public Point Center { get; set; }
        public double Radius1 { get; set; }
        public double Radius2 { get; set; }

        public CircleContourDto(CircleContour shape) : base(shape)
        {
            Center = shape.RealCenter;
            Radius1 = shape.RealRadius1;
            Radius2 = shape.RealRadius2;
        }

        public override List<netDxf.Entities.EntityObject> ToDxf()
        {
            //netDxf.Entities.Circle circle = new netDxf.Entities.Circle(new Vector2(Center.X, Center.Y), Radius1);
            //netDxf.Entities.Circle circle2 = new netDxf.Entities.Circle(new Vector2(Center.X, Center.Y), Radius2);

            netDxf.Entities.Polyline2D polyline1 = new netDxf.Entities.Polyline2D();

            double angleIncrement = 2 * Math.PI / 100; // 每个顶点之间的角度增量

            for (int i = 0; i < 100; i++)
            {
                double angle = i * angleIncrement;
                double x = Center.X + Radius1 * Math.Cos(angle);
                double y = Center.Y + Radius1 * Math.Sin(angle);
                polyline1.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(x, y));
            }

            netDxf.Entities.Polyline2D polyline2 = new netDxf.Entities.Polyline2D();

            for (int i = 0; i < 100; i++)
            {
                double angle = i * angleIncrement;
                double x = Center.X + Radius2 * Math.Cos(angle);
                double y = Center.Y + Radius2 * Math.Sin(angle);
                polyline2.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(x, y));
            }

            return new List<netDxf.Entities.EntityObject> { polyline1, polyline2 };
        }

        public override MeshGeometry3D ToSTL()
        {
            // 判断是否为包含关系
            if (Radius1 > Radius2 && Point.Subtract(Center, Center).Length <= (Radius1 - Radius2))
            {
                // 较小圆在内部，需要切割较大圆的三角面片
                return CreateTriangleMeshForCircle(Center, Radius1, Center, Radius2);
            }
            else if (Radius2 > Radius1 && Point.Subtract(Center, Center).Length <= (Radius2 - Radius1))
            {
                // 较小圆在外部，需要切割较小圆的三角面片
                return CreateTriangleMeshForCircle(Center, Radius2, Center, Radius1);
            }

            // 默认情况下返回空
            return null;
        }

        private static MeshGeometry3D CreateTriangleMeshForCircle(Point center1, double radius1, Point center2, double radius2)
        {
            const int Segments = 32;
            const double angleIncrement = 2 * Math.PI / Segments;

            MeshGeometry3D circleRingMesh = new MeshGeometry3D();

            for (int i = 0; i < Segments; i++)
            {
                double innerX = radius1 * Math.Cos(i * angleIncrement);
                double innerY = radius1 * Math.Sin(i * angleIncrement);
                Point3D innerVertex = new Point3D(center1.X + innerX, center1.Y + innerY, 0);
                circleRingMesh.Positions.Add(innerVertex);

                double outerX = radius2 * Math.Cos(i * angleIncrement);
                double outerY = radius2 * Math.Sin(i * angleIncrement);
                Point3D outerVertex = new Point3D(center2.X + outerX, center2.Y + outerY, 0);
                circleRingMesh.Positions.Add(outerVertex);
            }


            // 向Mesh添加三角面片
            foreach (int i in Enumerable.Range(0, Segments))
            {
                int currentInnerIndex = i * 2;
                int currentOuterIndex = i * 2 + 1;
                int nextInnerIndex = ((i + 1) % Segments) * 2;
                int nextOuterIndex = ((i + 1) % Segments) * 2 + 1;

                // 添加第一个三角形
                circleRingMesh.TriangleIndices.Add(currentInnerIndex);
                circleRingMesh.TriangleIndices.Add(currentOuterIndex);
                circleRingMesh.TriangleIndices.Add(nextOuterIndex);

                // 添加第二个三角形
                circleRingMesh.TriangleIndices.Add(currentInnerIndex);
                circleRingMesh.TriangleIndices.Add(nextOuterIndex);
                circleRingMesh.TriangleIndices.Add(nextInnerIndex);
            }

            return circleRingMesh;
        }
    }
}
