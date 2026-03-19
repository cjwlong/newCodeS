using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace CCD.Controls
{
    public class MoveAxisControl : FrameworkElement
    {
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0) return;

            var pen = new Pen(Brushes.Black, 1.6);

            // 原点：右下角
            Point origin = new Point(width, height);

            // 轴长度：控件的70%
            double axisLenX = width * 0.7;
            double axisLenY = height * 0.7;

            // 箭头绝对放大：用轴长度的 35%，视觉明显大
            double arrow = Math.Min(axisLenX, axisLenY) * 0.3;

            // ===================== Y 轴（向左） =====================
            Point yEnd = new Point(origin.X - axisLenX, origin.Y);
            dc.DrawLine(pen, origin, yEnd);

            // Y 箭头
            dc.DrawLine(pen, yEnd, new Point(yEnd.X + arrow, yEnd.Y - arrow / 2));
            dc.DrawLine(pen, yEnd, new Point(yEnd.X + arrow, yEnd.Y + arrow / 2));

            // ===================== Z 轴（向上） =====================
            Point zEnd = new Point(origin.X, origin.Y - axisLenY);
            dc.DrawLine(pen, origin, zEnd);

            // Z 箭头
            dc.DrawLine(pen, zEnd, new Point(zEnd.X - arrow / 2, zEnd.Y + arrow));
            dc.DrawLine(pen, zEnd, new Point(zEnd.X + arrow / 2, zEnd.Y + arrow));

            // ===================== 轴名 =====================
            double fontSize = arrow * 1.3; // 字母稍小于箭头，但视觉明显

            var textY = new FormattedText(
                "Y",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                fontSize,
                Brushes.Black,
                1.25
            );

            var textZ = new FormattedText(
                "Z",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                fontSize,
                Brushes.Black,
                1.25
            );

            // 绘制字母
            dc.DrawText(textY,
                new Point(yEnd.X - textY.Width, yEnd.Y - textY.Height / 2));

            dc.DrawText(textZ,
                new Point(zEnd.X - textZ.Width / 2, zEnd.Y - textZ.Height));
        }

    }
}
