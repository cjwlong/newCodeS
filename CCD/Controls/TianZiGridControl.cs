using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace CCD.Controls
{
    public class TianZiGridControl : Canvas
    {

        // 圆圈半径占格子大小比例
        public double CircleRatio { get; set; } = 0.1;

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0) return;

            // 田字格划分: 2x2格子，所以竖线横线各划两条
            double cellWidth = width / 2;
            double cellHeight = height / 2;

            var pen = new Pen(Brushes.Black, 1.5);

            // 外框
            dc.DrawRectangle(null, pen, new Rect(0, 0, width, height));

            // 内部竖线
            for (int c = 1; c <= 1; c++)
            {
                double x = c * cellWidth;
                dc.DrawLine(pen, new Point(x, 0), new Point(x, height));
            }

            // 内部横线
            for (int r = 1; r <= 1; r++)
            {
                double y = r * cellHeight;
                dc.DrawLine(pen, new Point(0, y), new Point(width, y));
            }

            double circleRadius = Math.Min(cellWidth, cellHeight) * CircleRatio;

            // 交点位置 (3x3)
            double offset = circleRadius * 1.2;

            for (int r = 0; r <= 2; r++)
            {
                for (int c = 0; c <= 2; c++)
                {
                    double x = c * cellWidth;
                    double y = r * cellHeight;

                    int number = r * 3 + c + 1;

                    // 画圆圈
                    dc.DrawEllipse(null, pen, new Point(x, y), circleRadius, circleRadius);
                    number = number - 1;
                    // 画数字，稍微偏移
                    var fontSize = circleRadius * 1.5;
                    var formatted = new FormattedText(
                        number.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        fontSize,
                        Brushes.Black,
                        1.25
                    );

                    // 数字偏移：右下方
                    dc.DrawText(formatted, new Point(x + offset - formatted.Width / 2, y + offset - formatted.Height / 2));
                }
            }
        }
    }
}
