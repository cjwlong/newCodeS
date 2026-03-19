using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CCD.tools
{
    public class BackgroundHelper
    {

        private static BackgroundHelper instance;

        private double cachedWidth;
        private double cachedHeight;
        private double cachedLineWidth;
        private VisualBrush cachedBrush;

        public Brush CrosshairColor
        {
            get
            {
                return Brushes.Green;
            }
        }
        public int CrosshairColorIndex { get; set; }
        public double CrosshairLineWidth { get; set; }

        public Brush ShapeColor
        {
            get
            {
                return Brushes.Red;
            }
        }

        public Brush LightShapeColor
        {
            get
            {
                return Brushes.Red;
            }
        }
        public int ShapeColorIndex { get; set; }
        public int LightShapeColorIndex { get; set; }
        public double ShapeLineWidth { get; set; }

        public Pen CachedPen { get; set; }
        public Pen CachedLightPen { get; set; }


        public Dictionary<int, Brush> ColorIndexMap;

        private BackgroundHelper()
        {
            ColorIndexMap = new Dictionary<int, Brush>
            {
                { 0, Brushes.Red },
                { 1, Brushes.Black },
                { 2, Brushes.Green },
                { 3, Brushes.Purple },
                { 4, Brushes.Blue },
                { 5, Brushes.Yellow },
                { 6, Brushes.White }
            };

            ReShapeColor();
            ReLightColor();

        }


        public static BackgroundHelper Instance
        {
            get
            {
                instance ??= new BackgroundHelper();
                return instance;
            }
        }

        public VisualBrush GetCrosshairBrush(double canvasWidth, double canvasHeight)
        {
            if (cachedBrush != null && Math.Abs(canvasWidth - cachedWidth) < double.Epsilon && Math.Abs(canvasHeight - cachedHeight) < double.Epsilon && Math.Abs(CrosshairLineWidth - cachedLineWidth) < double.Epsilon)
            {
                return cachedBrush;
            }

            var pathGeometry = new PathGeometry();

            // 添加垂直线段
            pathGeometry.AddGeometry(new LineGeometry(new Point(canvasWidth / 2, 0), new Point(canvasWidth / 2, canvasHeight)));

            // 添加水平线段
            pathGeometry.AddGeometry(new LineGeometry(new Point(0, canvasHeight / 2), new Point(canvasWidth, canvasHeight / 2)));
            pathGeometry.Freeze();
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // 绘制背景
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, canvasWidth, canvasHeight));

                // 绘制路径
                drawingContext.DrawGeometry(null, new Pen(CrosshairColor, 5), pathGeometry);
            }

            cachedBrush = new VisualBrush
            {
                Visual = drawingVisual
            };
            cachedWidth = canvasWidth;
            cachedHeight = canvasHeight;
            cachedLineWidth = CrosshairLineWidth;

            return cachedBrush;
        }

        public bool SetCrosshairProperties(int brushIndex, double lineWidth)
        {
            if (CrosshairColorIndex == brushIndex && CrosshairLineWidth == lineWidth)
            {
                return false; // 值相同，无需更新
            }

            cachedBrush = null; // 重置缓存的背景画刷

            // 设置新的颜色和线宽
            CrosshairColorIndex = brushIndex;
            CrosshairLineWidth = lineWidth;
            return true; // 更新成功
        }

        public bool SetCrosshairColor(int brushIndex)
        {
            if (brushIndex == CrosshairColorIndex)
            {
                return false;
            }
            cachedBrush = null; // 重置缓存的背景画刷

            CrosshairColorIndex = brushIndex;
            return true;
        }

        public bool SetCrosshairLineWidth(double lineWidth)
        {
            if (lineWidth == CrosshairLineWidth)
            {
                return false;
            }
            cachedBrush = null; // 重置缓存的背景画刷

            CrosshairLineWidth = lineWidth;
            return true;
        }

        public bool SetShapeColor(int brushIndex)
        {
            if (brushIndex == ShapeColorIndex)
            {
                return false;
            }

            ShapeColorIndex = brushIndex;
            ReShapeColor();
            return true;
        }

        public bool SetLightShapeColor(int brushIndex)
        {
            if (brushIndex == LightShapeColorIndex)
            {
                return false;
            }

            LightShapeColorIndex = brushIndex;
            ReLightColor();
            return true;
        }

        public bool SetShapeLineWidth(double lineWidth)
        {
            if (lineWidth == ShapeLineWidth)
            {
                return false;
            }
            ShapeLineWidth = lineWidth;
            ReShapeColor();
            ReLightColor();
            return true;
        }

        private void ReShapeColor()
        {
            CachedPen = new(Brushes.Red, 5);
            CachedPen.Freeze();
        }

        private void ReLightColor()
        {
            CachedLightPen = new(LightShapeColor, ShapeLineWidth);
            CachedLightPen.Freeze();
        }

    }
}
