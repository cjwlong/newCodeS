using CCD.libs;
using CCD.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CCD.shapes
{
    public class IntersectionLines : Shape
    {
        private int drawMode = 0;

        public List<Point> FourPoints = new(4);
        public List<Point> GetFourPoints()
        {
            return TransformPoints(FourPoints);
        }
        private List<Point> TransformPoints(List<Point> points)
        {
            return points.Select(p => CoordinateHelper.Instance.ConvertToRealFromPix(p)).ToList();
        }
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

        public double RealAngel
        {
            get
            {
                List<Point> realpoints = GetFourPoints();

                return CalculateAngleBetweenLines(realpoints);
            }
        }




        public IntersectionLines()
        {
            Name = "相交线";
        }

        public void UpdatePoints(Point point)
        {
            if (FourPoints.Count < drawMode + 1)
            {
                FourPoints.Add(point);
            }
            else
            {
                FourPoints[FourPoints.Count - 1] = point;
            }


            if (FourPoints.Count == 4)
            {
                double centerX = 0;
                double centerY = 0;
                foreach (Point point1 in FourPoints)
                {
                    centerX += point1.X;
                    centerY += point1.Y;
                }

                centerX /= FourPoints.Count;
                centerY /= FourPoints.Count;

                Center = new Point(centerX, centerY);
            }
        }
        private double CalculateAngleBetweenLines(List<Point> points)
        {
            if (points.Count != 4)
            {
                return 0;
            }

            // 提取点
            Point p1 = points[0];
            Point p2 = points[1];
            Point p3 = points[2];
            Point p4 = points[3];

            // 计算向量
            double vector1X = p2.X - p1.X;
            double vector1Y = p2.Y - p1.Y;
            double vector2X = p4.X - p3.X;
            double vector2Y = p4.Y - p3.Y;

            // 计算向量的长度
            double length1 = Math.Sqrt(vector1X * vector1X + vector1Y * vector1Y);
            double length2 = Math.Sqrt(vector2X * vector2X + vector2Y * vector2Y);

            // 计算向量的点积
            double dotProduct = vector1X * vector2X + vector1Y * vector2Y;

            // 计算两向量的夹角
            double angle = Math.Acos(dotProduct / (length1 * length2));

            // 将夹角转换为度数
            double angleInDegrees = angle * (180.0 / Math.PI);
            if (angleInDegrees > 90)
            {
                return 180 - angleInDegrees;
            }
            else
            {
                return angleInDegrees;
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
            for (int i = 0; i < FourPoints.Count; i++)
            {
                FourPoints[i] += pixVector;
            }

        }


        public override void Draw(DrawingContext drawingContext)
        {
            // 绘制圆形
            if (FourPoints.Count > 1)
            {
                drawingContext.DrawLine(Pen, FourPoints[0], FourPoints[1]);
            }
            if (FourPoints.Count == 4)
            {
                drawingContext.DrawLine(Pen, FourPoints[2], FourPoints[3]);
            }
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            if (FourPoints.Count == 4)
            {
                drawingContext.DrawLine(LightShape(Pen), FourPoints[0], FourPoints[1]);
                drawingContext.DrawLine(LightShape(Pen), FourPoints[2], FourPoints[3]);
            }
        }
    }
}
