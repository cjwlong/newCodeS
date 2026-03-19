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
    public class SquareContour : Shape
    {
        private int drawMode = 0;

        public SquareContour()
        {
            Name = "方形轮廓";
        }

        public List<Point> FirstPoints = new(4);
        public Point[] SecondPoints = new Point[8];
        public bool IsClockwise = true;

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

        public Point RealCenter { get; set; }
        public Point AbsoluteCenter { get; set; }

        public List<Point> GetFirstPoints()
        {
            return TransformPoints(FirstPoints);
        }

        public List<Point> GetSecondPoints()
        {
            return TransformPoints(SecondPoints);
        }

        private List<Point> TransformPoints(List<Point> points)
        {
            return points.Select(p => CoordinateHelper.Instance.ConvertToRealFromPix(p)).ToList();
        }

        private List<Point> TransformPoints(Point[] points)
        {
            return points.Select(p => CoordinateHelper.Instance.ConvertToRealFromPix(p)).ToList();
        }

        public void AddPoints(Point point)
        {
            FirstPoints.Add(point);
        }

        public void UpdatePoints(Point point)
        {
            if (drawMode < 2)
            {
                if (FirstPoints.Count < drawMode + 1)
                {
                    FirstPoints.Add(point);
                }
                else
                {
                    FirstPoints[^1] = point;
                }
            }
            else if (drawMode == 2)
            {
                GetParallelLine(new List<Point>() { FirstPoints[0], FirstPoints[1], point }, out Point p1, out Point p2);
                if (FirstPoints.Count < drawMode + 1)
                {
                    FirstPoints.Add(p1);
                    FirstPoints.Add(p2);
                }
                else
                {
                    FirstPoints[^2] = p2;
                    FirstPoints[^1] = p1;
                }
                IsClockwise = AreVerticesClockwise(FirstPoints);
            }
            else
            {
                int index = GetNearestRectangleVertexIndex(FirstPoints, point);
                double distence = DistanceFromPointToSegment(FirstPoints[index % 4], FirstPoints[(index + 1) % 4], point);

                double centerX = 0;
                double centerY = 0;
                foreach (Point point1 in FirstPoints)
                {
                    centerX += point1.X;
                    centerY += point1.Y;
                }

                centerX /= FirstPoints.Count;
                centerY /= FirstPoints.Count;

                Center = new Point(centerX, centerY);

                for (int i = 0; i < 4; i++)
                {
                    GetParallelLine(FirstPoints[index % 4], FirstPoints[(index + 1) % 4], distence, Center, out Point p1, out Point p2);

                    SecondPoints[2 * index % 8] = p1;
                    SecondPoints[(2 * index + 1) % 8] = p2;
                    index++;
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
            var point = CoordinateHelper.Instance.ConvertToPix(RealCenter);
            Vector pixVector = point - _center;
            _center = point;
            for (int i = 0; i < FirstPoints.Count; i++)
            {
                FirstPoints[i] += pixVector;
            }

            for (int i = 0; i < SecondPoints.Length; i++)
            {
                SecondPoints[i] += pixVector;
            }
        }

        public override void Draw(DrawingContext drawingContext)
        {
            // 创建四点形的路径
            PathGeometry pathGeometry = new();
            if (FirstPoints.Count > 0)
            {
                pathGeometry.Figures.Add(CreateFirstPathFigure());
            }
            if (drawMode > 2)
            {
                pathGeometry.Figures.Add(CreateSecondPathFigure());
            }

            // 绘制路径
            drawingContext.DrawGeometry(null, Pen, pathGeometry);
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            // 创建四点形的路径
            PathGeometry pathGeometry = new();
            pathGeometry.Figures.Add(CreateFirstPathFigure());
            pathGeometry.Figures.Add(CreateSecondPathFigure());

            // 绘制路径
            drawingContext.DrawGeometry(null, LightShape(Pen), pathGeometry);
        }

        private PathFigure CreateFirstPathFigure()
        {
            PathFigure pathFigure = new()
            {
                StartPoint = FirstPoints[0]
            };

            // 创建第一个子路径的线段
            for (int i = 1; i < FirstPoints.Count; i++)
            {
                LineSegment lineSegment = new LineSegment(FirstPoints[i], true);
                pathFigure.Segments.Add(lineSegment);
            }

            // 封闭第一个子路径
            if (drawMode > 1)
            {
                pathFigure.IsClosed = true;
            }

            pathFigure.IsFilled = false;

            return pathFigure;
        }

        private PathFigure CreateSecondPathFigure()
        {
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = SecondPoints[0]
            };

            // 创建第一个子路径的线段
            for (int i = 1; i < 8; i += 2)
            {
                LineSegment lineSegment = new LineSegment(SecondPoints[i], true);
                pathFigure.Segments.Add(lineSegment);

                SweepDirection sweepDirection = IsClockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise; // 顺时针方向
                double h = CalculateDistance(SecondPoints[i], FirstPoints[(i / 2 + 1) % 4]);
                ArcSegment arcSegment = new ArcSegment(SecondPoints[(i + 1) % 8], new Size(h, h), 0, false, sweepDirection, true);
                pathFigure.Segments.Add(arcSegment);
            }


            pathFigure.IsFilled = false;

            return pathFigure;
        }
        private void GetParallelLine(Point Start, Point End, double h, Point center, out Point newStart, out Point newEnd)
        {
            Vector vector = Start - End;
            vector.Normalize();

            Vector verticalVector1 = new Vector(-vector.Y, vector.X);
            verticalVector1 = verticalVector1 * h;
            Point nStart1 = Start + verticalVector1;
            Point nEnd1 = End + verticalVector1;

            verticalVector1.Negate();
            Point nStart2 = Start + verticalVector1;
            Point nEnd2 = End + verticalVector1;

            double d1 = CalculateDistance(nStart1, center) + CalculateDistance(nEnd1, center);
            double d2 = CalculateDistance(nStart2, center) + CalculateDistance(nEnd2, center);

            if (d1 >= d2)
            {
                newStart = nStart1;
                newEnd = nEnd1;
            }
            else
            {
                newStart = nStart2;
                newEnd = nEnd2;
            }

        }

        private void GetParallelLine(List<Point> points, out Point newStart, out Point newEnd)
        {
            Point start = points[0];
            Point end = points[1];
            Point point = points[2];

            // 得到一般式的参数
            double A = -(start.Y - end.Y);
            double B = (start.X - end.X);
            double C = -A * point.X - B * point.Y;

            double A1 = -B;
            double B1 = A;
            double C1 = B * start.X - A * start.Y;

            double A2 = -B;
            double B2 = A;
            double C2 = B * end.X - A * end.Y;

            double x = (B1 * C - B * C1) / (A1 * B - A * B1);
            double y = (-A1 * x - C1) / B1;
            newStart = new Point(x, y);

            x = (B2 * C - B * C2) / (A2 * B - A * B2);
            y = (-A2 * x - C2) / B2;
            newEnd = new Point(x, y);
        }

        public int GetNearestRectangleVertexIndex(List<Point> rectangleVertices, Point point)
        {
            double minDistance = double.MaxValue;
            int nearestVertexIndex = -1;

            for (int i = 0; i < rectangleVertices.Count; i++)
            {
                double vertexDistance = CalculateDistance(rectangleVertices[i], point);
                double nextVertexDistance = CalculateDistance(rectangleVertices[(i + 1) % 4], point);
                double distanceSum = vertexDistance + nextVertexDistance;
                if (distanceSum < minDistance)
                {
                    minDistance = distanceSum;
                    nearestVertexIndex = i;
                }
            }

            return nearestVertexIndex;
        }

        private double CalculateDistance(Point point1, Point point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }


        public static double DistanceFromPointToSegment(Point startPoint, Point endPoint, Point targetPoint)
        {
            Vector segmentVector = endPoint - startPoint;
            Vector pointToStartVector = targetPoint - startPoint;
            double segmentLength = segmentVector.Length;

            double distance = Math.Abs(Vector.CrossProduct(segmentVector, pointToStartVector)) / segmentLength;
            return distance;
        }


        public override ShapeDto GetDto()
        {
            return new SquareContourDto(this);
        }

        public bool AreVerticesClockwise(List<Point> vertices)
        {
            if (vertices.Count != 4)
            {
                throw new ArgumentException("The list of vertices must contain exactly 4 points.");
            }

            Point A = vertices[0];
            Point B = vertices[1];
            Point C = vertices[2];
            Point D = vertices[3];

            Vector AB = B - A;
            Vector BC = C - B;
            Vector CD = D - C;
            Vector DA = A - D;

            double crossProduct1 = Vector.CrossProduct(AB, BC);
            double crossProduct2 = Vector.CrossProduct(CD, DA);
            double totalCrossProduct = crossProduct1 + crossProduct2;
            if (totalCrossProduct >= 0)
            {
                return true;  // 顺时针排列
            }
            else
            {
                return false; // 逆时针排列
            }
        }
    }

    public class SquareContourDto : ShapeDto
    {
        public List<Point> FirstPoints = new List<Point>(4);
        public List<Point> SecondPoints = new List<Point>(4);
        public bool IsClockwise = true;

        public SquareContourDto(SquareContour shape) : base(shape)
        {
            FirstPoints = shape.GetFirstPoints();
            SecondPoints = shape.GetSecondPoints();
            IsClockwise = shape.IsClockwise;
        }

        public override List<netDxf.Entities.EntityObject> ToDxf()
        {
            List<netDxf.Entities.EntityObject> entityObjects = new List<netDxf.Entities.EntityObject>();

            List<Vector2> points = new List<Vector2> {
                    new Vector2(FirstPoints[0].X, FirstPoints[0].Y),
                    new Vector2(FirstPoints[1].X, FirstPoints[1].Y),
                    new Vector2(FirstPoints[2].X, FirstPoints[2].Y),
                    new Vector2(FirstPoints[3].X, FirstPoints[3].Y)
            };

            var polyline = new netDxf.Entities.Polyline2D(points, true);
            entityObjects.Add(polyline);

            netDxf.Entities.Polyline2D polyline2 = new netDxf.Entities.Polyline2D();

            // 遍历 SecondPoints 中的点，每次取两个点创建直线和圆弧
            polyline2.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(SecondPoints[0].X, SecondPoints[0].Y));
            for (int i = 0; i < SecondPoints.Count - 1; i += 2)
            {
                Vector2 endPoint = new Vector2(SecondPoints[i + 1].X, SecondPoints[i + 1].Y);
                Vector2 endPoint2 = new Vector2(SecondPoints[(i + 2) % 8].X, SecondPoints[(i + 2) % 8].Y);
                Vector2 centerPoint = new Vector2(FirstPoints[(i / 2 + 1) % 4].X, FirstPoints[(i / 2 + 1) % 4].Y);

                // 创建直线段，添加起点和终点
                double bulge = CalculateBulge(endPoint, endPoint2, centerPoint, IsClockwise);
                polyline2.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(endPoint, bulge));

                polyline2.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(endPoint2));
            }
            entityObjects.Add(polyline2);

            return entityObjects;
        }


        public override MeshGeometry3D ToSTL()
        {
            if (FirstPoints.Count != SecondPoints.Count && SecondPoints.Count != 4)
            {
                return null;
            }

            MeshGeometry3D rectangleMesh = new MeshGeometry3D();

            foreach (var item in FirstPoints)
            {
                rectangleMesh.Positions.Add(new Point3D(item.X, item.Y, 0));
            }

            foreach (var item in SecondPoints)
            {
                rectangleMesh.Positions.Add(new Point3D(item.X, item.Y, 0));
            }

            for (var i = 0; i < 4; i++)
            {
                rectangleMesh.TriangleIndices.Add(i);
                rectangleMesh.TriangleIndices.Add((i + 1) % 4);
                rectangleMesh.TriangleIndices.Add((i + 4) % 8);

                rectangleMesh.TriangleIndices.Add(i + 1);
                rectangleMesh.TriangleIndices.Add((i + 4) % 8);
                rectangleMesh.TriangleIndices.Add((i + 5) % 8);
            }


            return null;
        }

        public double CalculateBulge(Vector2 startPoint, Vector2 endPoint, Vector2 centerPoint, bool clockwise)
        {
            Vector2 startDir = Vector2.Normalize(startPoint - centerPoint);
            Vector2 endDir = Vector2.Normalize(endPoint - centerPoint);
            double theta = Math.Atan2(startDir.X * endDir.Y - startDir.Y * endDir.X,
                startDir.X * endDir.X + startDir.Y * endDir.Y);
            double bulge = Math.Tan(theta / 4);

            return bulge;

        }
    }
}
