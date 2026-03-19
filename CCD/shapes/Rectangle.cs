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
    public class Rectangle : Shape
    {
        public Rect Rect { get; set; }

        private Point _start;
        public Point StartPoint
        {
            get { return _start; }
            set
            {
                _start = value;
            }
        }

        private Point _end;
        public Point EndPoint
        {
            get { return _end; }
            set
            {
                _end = value;
                Rect = new Rect(_start, _end);
                RealTL = CoordinateHelper.Instance.ConvertToRealFromPix(Rect.TopLeft);
                AbsoluteTL = CoordinateHelper.Instance.ConvertToAbsolute(MachinePoint, RealTL);
                AbsoluteTR = CoordinateHelper.Instance.ConvertToAbsolute(MachinePoint, Rect.TopRight);
                AbsoluteBL = CoordinateHelper.Instance.ConvertToAbsolute(MachinePoint, Rect.BottomLeft);
                AbsoluteBR = CoordinateHelper.Instance.ConvertToAbsolute(MachinePoint, Rect.BottomRight);
            }
        }

        private Point _realTL;
        public Point RealTL
        {
            get { return _realTL; }
            set { _realTL = value; }
        }
        //public Point RealTR
        //{
        //    get
        //    {
        //        return GetCachedValueOrDefault(nameof(RealTR), () =>
        //        {
        //            return CoordinateHelper.Instance.ConvertToRealPosition(Rect.TopRight);
        //        });
        //    }
        //}
        //public Point RealBL
        //{
        //    get
        //    {
        //        return GetCachedValueOrDefault(nameof(RealBL), () =>
        //        {
        //            return CoordinateHelper.Instance.ConvertToRealPosition(Rect.BottomLeft);
        //        });
        //    }
        //}
        //public Point RealBR
        //{
        //    get
        //    {
        //        return GetCachedValueOrDefault(nameof(RealBR), () =>
        //        {
        //            return CoordinateHelper.Instance.ConvertToRealPosition(Rect.BottomRight);
        //        });
        //    }
        //}

        public double RealWeight
        {
            get { return CoordinateHelper.LineDistance(AbsoluteTL, AbsoluteTR); }
        }

        public double RealHight
        {
            get { return CoordinateHelper.LineDistance(AbsoluteBL, AbsoluteTR); }
        }

        private Point _absoluteTL;
        public Point AbsoluteTL
        {
            get { return _absoluteTL; }
            set { _absoluteTL = value; }
        }

        private Point _absoluteTR;
        public Point AbsoluteTR
        {
            get { return _absoluteTR; }
            set { _absoluteTR = value; }
        }

        private Point _absoluteBL;
        public Point AbsoluteBL
        {
            get { return _absoluteBL; }
            set { _absoluteBL = value; }
        }
        private Point _absoluteBR;
        public Point AbsoluteBR
        {
            get { return _absoluteBR; }
            set { _absoluteBR = value; }
        }

        public double AbsoluteArea
        {
            get { return CalculateRectangleArea(RealHight, RealWeight); }
        }

        public double AbsolutePerimeter
        {
            get { return CalculateRectanglePerimeter(RealHight, RealWeight); }
        }



        public Rectangle()
        {
            Name = "矩形";
        }

        public override void ShapeMove(Vector vector)
        {
            RealTL += vector;
            var point = CoordinateHelper.Instance.ConvertToPix(RealTL);
            Rect = Rect.Offset(Rect, point.X - Rect.X, point.Y - Rect.Y);
        }

        public override Point? MoveToShape()
        {
            return AbsoluteTL;
        }

        public override double[] MoveToCenter()
        {
            return new double[] { AbsoluteTL.X, AbsoluteTL.Y, 0 };
        }

        private double CalculateRectanglePerimeter(double length, double width)
        {
            return 2 * (length + width);
        }

        // 计算矩形面积
        private double CalculateRectangleArea(double length, double width)
        {
            return length * width;
        }


        public override void Draw(DrawingContext drawingContext)
        {
            // 绘制矩形
            drawingContext.DrawRectangle(null, Pen, Rect);
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(null, LightShape(Pen), Rect);
        }

        public override ShapeDto GetDto()
        {
            return new RectangleDto(this);
        }
    }

    public class RectangleDto : ShapeDto
    {
        public Point TopLeft { get; set; }
        public Point TopRight { get; set; }
        public Point BottomLeft { get; set; }
        public Point BottomRight { get; set; }

        public RectangleDto(Rectangle shape) : base(shape)
        {
            TopLeft = shape.AbsoluteTL;
            TopRight = shape.AbsoluteTR;
            BottomLeft = shape.AbsoluteBL;
            BottomRight = shape.AbsoluteBR;
        }

        public override List<netDxf.Entities.EntityObject> ToDxf()
        {
            // 定义矩形的四个角点
            var topLeft = new Vector2(TopLeft.X, TopLeft.Y);
            var topRight = new Vector2(TopRight.X, TopRight.Y);
            var bottomRight = new Vector2(BottomRight.X, BottomRight.Y);
            var bottomLeft = new Vector2(BottomLeft.X, BottomLeft.Y);

            // 创建一个 LwPolyline 对象
            netDxf.Entities.Polyline2D rectangle = new netDxf.Entities.Polyline2D();
            rectangle.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(topLeft));
            rectangle.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(topRight));
            rectangle.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(bottomRight));
            rectangle.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(bottomLeft));

            // 将矩形的起点和终点设置为同一个点，以闭合矩形
            rectangle.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(topLeft));
            return new List<netDxf.Entities.EntityObject> { rectangle };
        }

        public override MeshGeometry3D ToSTL()
        {
            MeshGeometry3D rectangleMesh = new MeshGeometry3D();
            rectangleMesh.Positions.Add(new Point3D(TopLeft.X, TopLeft.Y, 0));
            rectangleMesh.Positions.Add(new Point3D(TopRight.X, TopRight.Y, 0));
            rectangleMesh.Positions.Add(new Point3D(BottomRight.X, BottomRight.Y, 0));
            rectangleMesh.Positions.Add(new Point3D(BottomLeft.X, BottomLeft.Y, 0));
            rectangleMesh.TriangleIndices.Add(0);
            rectangleMesh.TriangleIndices.Add(1);
            rectangleMesh.TriangleIndices.Add(2);
            rectangleMesh.TriangleIndices.Add(0);
            rectangleMesh.TriangleIndices.Add(2);
            rectangleMesh.TriangleIndices.Add(3);
            return rectangleMesh;
        }
    }
}
