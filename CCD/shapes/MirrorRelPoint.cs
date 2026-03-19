using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CCD.shapes
{
    public class MirrorRelPoint : Shape
    {
        private Point _center;
        public Point Center
        {
            get
            {
                return _center;
            }
            set
            {
                _center = value;
            }
        }

        private int _index;
        public int Index
        {
            get { return _index; }
            set
            {
                SetProperty(ref _index, value);
            }
        }

        public int PerIdex { get; set; }

        private Point? _relPoint = null;
        public Point? RelPoint
        {
            get { return _relPoint; }
            set
            {
                SetProperty(ref _relPoint, value);
            }
        }

        public Point ShouldPoint { get; set; }

        private bool _isLight = false;
        public bool IsLight
        {
            get { return _isLight; }
            set
            {
                SetProperty(ref _isLight, value);
            }
        }

        public MirrorRelPoint(Point point, int index, int perIndex)
        {
            ShouldPoint = point;
            Index = index + 1;
            PerIdex = perIndex + 1;
        }

        public double CalculateDistance()
        {
            if (RelPoint.HasValue)
            {
                return Math.Sqrt(Math.Pow(RelPoint.Value.X - ShouldPoint.X, 2) + Math.Pow(RelPoint.Value.Y - ShouldPoint.Y, 2));
            }
            else
            {
                return double.MaxValue;
            }
        }

        public double CalculateDistance(Point point)
        {
            return Math.Sqrt(Math.Pow(point.X - ShouldPoint.X, 2) + Math.Pow(point.Y - ShouldPoint.Y, 2));
        }

        public override void Draw(DrawingContext drawingContext)
        {
            if (RelPoint == null)
            {
                return;
            }
            drawingContext.DrawEllipse(new SolidColorBrush(Colors.Red), null, Center, 1, 1); // 绘制一个椭圆，即点
            drawingContext.DrawEllipse(null, Pen, Center, 20, 20);
        }

        public override void LightDraw(DrawingContext drawingContext)
        {
            if (RelPoint == null)
            {
                return;
            }
            drawingContext.DrawEllipse(new SolidColorBrush(Colors.Red), null, Center, 1, 1); // 绘制一个椭圆，即点
            drawingContext.DrawEllipse(null, LightShape(Pen), Center, 20, 20);
        }
    }
}
