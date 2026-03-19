using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Runtime.Serialization;

namespace CCD.Controls
{
    public class AxisArrowControl : FrameworkElement
    {
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // 🔑 控件越小，scale 越小
            double scale = Math.Min(w, h);

            double margin = scale * 0.10;   // 边距
            double axisLen = scale * 0.65;   // 坐标轴长度
            double arrowSize = scale * 0.12;   // 箭头大小
            double thickness = Math.Max(1, scale * 0.06);
            double fontSize = scale * 0.20;

            var pen = new Pen(Brushes.Black, 2) //Pen(Brushes.Lime, thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            // 原点：左上角
            Point origin = new Point(margin, margin);

            // X → 右
            Point xEnd = new Point(origin.X + axisLen, origin.Y);
            DrawArrow(dc, pen, origin, xEnd, arrowSize);

            // Y ↓ 下
            Point yEnd = new Point(origin.X, origin.Y + axisLen);
            DrawArrow(dc, pen, origin, yEnd, arrowSize);

            // X 标签
            DrawText(dc, "X",
                new Point(xEnd.X + fontSize * 0.1, xEnd.Y - fontSize * 0.7),
                fontSize);

            // Y 标签（刻意避开箭头）
            DrawText(dc, "Y",
                new Point(yEnd.X - fontSize * 0.8, yEnd.Y + fontSize * 0.1),
                fontSize);
        }


        private void DrawArrow(DrawingContext dc, Pen pen, Point start, Point end, double headSize)
        {
            dc.DrawLine(pen, start, end);

            Vector dir = start - end;
            dir.Normalize();

            Vector side1 = new Vector(-dir.Y, dir.X);
            Vector side2 = new Vector(dir.Y, -dir.X);

            Point p1 = end + (dir + side1) * headSize;
            Point p2 = end + (dir + side2) * headSize;

            dc.DrawLine(pen, end, p1);
            dc.DrawLine(pen, end, p2);
        }

        private void DrawText(DrawingContext dc, string text, Point pos, double fontSize)
        {
            var textBrush = Brushes.Black;

            var ft = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                fontSize,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip); //Brushes.Lime
            ft.SetFontWeight(FontWeights.Bold);
            dc.DrawText(ft, pos);
        }
    }
}
