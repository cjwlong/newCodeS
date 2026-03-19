using CCD.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using static CCD.shapes.Shape;
using netDxf;
using CCD.tools;

namespace CCD.shapes
{
    internal class Line : Shape
    {
        public CameraPoint StartPoint;
        public CameraPoint EndPoint;
        public CameraPoint MidPoint { get; set; }
        public double RealDistance
        {
            get { return CoordinateHelper.LineDistance(StartPoint.MacPoint, EndPoint.MacPoint); }
        }

        public double RealAngel
        {
            get { return CoordinateHelper.LineAngle(StartPoint.MacPoint, EndPoint.MacPoint); }
        }

        public Line()
        {
            Name = "线段";
        }

        public Line(Point asPoint, Point aePoint)
        {
            Name = "线段";
            //Point amPoint = new Point((aePoint.X + asPoint.X) / 2, (asPoint.Y + aePoint.Y) / 2);
            //SetCachedValue(nameof(AbsoluteStartPoint), asPoint);
            //SetCachedValue(nameof(AbsoluteEndPoint), aePoint);
            //SetCachedValue(nameof(AbsoluteMidPoint), amPoint);
            //SetCachedValue(nameof(RealDistance), CoordinateHelper.Instance.LineDistance(asPoint, aePoint));
            //SetCachedValue(nameof(RealAngel), CoordinateHelper.Instance.LineAngle(asPoint, aePoint));
            //SetCachedValue(nameof(RealMidPoint), new Point());
            //SetCachedValue(nameof(RealEndPoint), new Point());
            //SetCachedValue(nameof(RealStartPoint), new Point());

            //AbsoluteStartPoint = asPoint;
            //AbsoluteEndPoint = aePoint;
        }

        public override void ShapeMove(Vector vector)
        {
            StartPoint = new() { SetPixPoint = CoordinateHelper.Instance.ConvertToPix(StartPoint.CamPoint + vector) };
            EndPoint = new() { SetPixPoint = CoordinateHelper.Instance.ConvertToPix(StartPoint.CamPoint + vector) };
        }
        public override void ShapeMove(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            StartPoint.MachineMoved();
            EndPoint.MachineMoved();
        }

        public override Point? MoveToShape()
        {
            return MidPoint.MacPoint;
        }
        public override double[] MoveToCenter()
        {
            return new double[] { MidPoint.MacPoint.X, MidPoint.MacPoint.Y, RealAngel };
        }

        public override string DisplayShape()
        {
            return $"两个断点坐标为({StartPoint.MacPoint:F3})和({StartPoint.MacPoint:F3}),中点坐标为({MidPoint.MacPoint:F3})";
        }

        public override void Draw(DrawingContext drawingContext)
        {
            DrawArrow(drawingContext, Pen);
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            DrawArrow(drawingContext, LightShape(Pen));
        }

        private void DrawArrow(DrawingContext drawingContext, Pen pen)
        {
            Point startPoint = StartPoint.PixPoint, endPoint = EndPoint.PixPoint;
            // 绘制直线
            drawingContext.DrawLine(pen, StartPoint.PixPoint, EndPoint.PixPoint);
            // 计算箭头的方向
            Vector direction = endPoint - startPoint;
            double arrowAngle = 30, arrowLength = pen.Thickness * 5;

            direction.Normalize();

            // 计算箭头两侧的点
            Vector arrowVector1 = new Vector(
                direction.X * Math.Cos(-arrowAngle * Math.PI / 180) - direction.Y * Math.Sin(-arrowAngle * Math.PI / 180),
                direction.X * Math.Sin(-arrowAngle * Math.PI / 180) + direction.Y * Math.Cos(-arrowAngle * Math.PI / 180)
            ) * arrowLength;

            Vector arrowVector2 = new Vector(
                direction.X * Math.Cos(arrowAngle * Math.PI / 180) - direction.Y * Math.Sin(arrowAngle * Math.PI / 180),
                direction.X * Math.Sin(arrowAngle * Math.PI / 180) + direction.Y * Math.Cos(arrowAngle * Math.PI / 180)
            ) * arrowLength;

            Point arrowPoint1 = endPoint - arrowVector1;
            Point arrowPoint2 = endPoint - arrowVector2;

            // 绘制箭头头部
            drawingContext.DrawLine(pen, endPoint, arrowPoint1);
            drawingContext.DrawLine(pen, endPoint, arrowPoint2);
        }

        public override ShapeDto GetDto()
        {
            return new LineDto(this);
        }
    }

    internal class LineDto : ShapeDto
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public Point Mid { get; set; }

        public LineDto(Line shape) : base(shape)
        {
            Start = shape.StartPoint.CamPoint;
            End = shape.StartPoint.CamPoint;
            Mid = shape.MidPoint.CamPoint;
        }

        public override List<netDxf.Entities.EntityObject> ToDxf()
        {
            netDxf.Entities.Line line = new netDxf.Entities.Line(new Vector2(Start.X, Start.Y),
                                                                    new Vector2(End.X, End.Y));
            return new List<netDxf.Entities.EntityObject> { line };
        }

        public override MeshGeometry3D ToSTL()
        {
            return null;
        }
    }
}
