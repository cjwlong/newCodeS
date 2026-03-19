using CCD.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using CCD.tools;

namespace CCD.shapes
{
    internal class ParallelLines : Shape
    {
        private int drawMode = 0;

        // 第一条线的起点
        public CameraPoint StartPoint { get; set; } = null;
        // 第一条线的终点
        public CameraPoint EndPoint { get; set; } = null;
        // 第二条线上的一个点
        public CameraPoint OtherPoint { get; set; } = null;

        public Point newPixStart;
        public Point newPixEnd;

        public double LinesDistance
        {
            get
            {
                if (StartPoint == null || EndPoint == null || OtherPoint == null)
                {
                    return 0;
                }
                Point startPoint = StartPoint.CamPoint;
                Point endPoint = EndPoint.CamPoint;
                Point otherPoint = OtherPoint.CamPoint;


                // 计算两条平行线之间的距离
                double A = endPoint.Y - startPoint.Y;
                double B = startPoint.X - endPoint.X;
                double C = endPoint.X * startPoint.Y - startPoint.X * endPoint.Y;
                return Math.Abs(A * otherPoint.X + B * otherPoint.Y + C) / Math.Sqrt(A * A + B * B);
            }


        }

        public ParallelLines()
        {
            Name = "平行线";
        }
        public void AddPoints(Point point)
        {
            if (StartPoint == null) StartPoint = new() { SetPixPoint = point };
            else if (EndPoint == null) EndPoint = new() { SetPixPoint = point };
            else if (OtherPoint == null) OtherPoint = new() { SetPixPoint = point };
            else throw new Exception("给出点的数量超出限制");
        }

        public void UpdatePoints(Point point)
        {
            if (StartPoint == null) StartPoint = new() { SetPixPoint = point };
            else if (EndPoint == null || drawMode == 1) EndPoint = new() { SetPixPoint = point };
            else OtherPoint = new() { SetPixPoint = point };

            if (StartPoint != null && EndPoint != null && OtherPoint != null)
                RefreshParallelLine();
        }

        public void NextStep()
        {
            drawMode++;
        }

        public bool IsFinish()
        {
            return drawMode == 3;
        }
        private void RefreshParallelLine()
        {
            // 使用绝对坐标计算，用像素坐标计算误差很大，不可用
            var half_vector = (EndPoint.MacPoint - StartPoint.MacPoint) / 2;  // 得到一半的向量

            newPixStart = CoordinateHelper.Instance.ConvertToPix(CoordinateHelper.Instance.ConvertToReal(OtherPoint.MacPoint - half_vector));
            newPixEnd = CoordinateHelper.Instance.ConvertToPix(CoordinateHelper.Instance.ConvertToReal(OtherPoint.MacPoint + half_vector));
        }

        public override void ShapeMove(Vector vector)
        {
            StartPoint.SetCamPoint = StartPoint.CamPoint + vector;
            var point = CoordinateHelper.Instance.ConvertToPix(StartPoint.CamPoint);
            Vector pixVector = point - StartPoint.PixPoint;

            StartPoint.RefreshPix = StartPoint.PixPoint + pixVector;
            EndPoint.RefreshPix = EndPoint.PixPoint + pixVector;
            OtherPoint.RefreshPix = OtherPoint.PixPoint + pixVector;
        }
        public override void ShapeMove(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            StartPoint.MachineMoved();
            EndPoint.MachineMoved();
            OtherPoint.MachineMoved();
        }


        public override void Draw(DrawingContext drawingContext)
        {
            if (StartPoint != null && EndPoint != null)
            {
                drawingContext.DrawLine(Pen, StartPoint.PixPoint, EndPoint.PixPoint);
                if (OtherPoint != null)
                {
                    RefreshParallelLine();
                    drawingContext.DrawLine(Pen, newPixStart, newPixEnd);
                }
            }
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            if (StartPoint != null && EndPoint != null)
            {
                drawingContext.DrawLine(LightShape(Pen), StartPoint.PixPoint, EndPoint.PixPoint);
                if (OtherPoint != null)
                {
                    RefreshParallelLine();
                    drawingContext.DrawLine(LightShape(Pen), newPixStart, newPixEnd);
                }
            }
        }
    }
}
