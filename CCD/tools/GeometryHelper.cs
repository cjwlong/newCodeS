using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CCD.tools
{
    static class GeometryHelper
    {

        public static void CalculateCircle(List<Point> points, out Point center, out double radius)
        {
            if (points == null || points.Count == 3)
            {
                center = new();
                radius = 0;
            }

            double x1 = points[0].X, y1 = points[0].Y, x2 = points[1].X, y2 = points[1].Y, x3 = points[2].X, y3 = points[2].Y;
            double a = x1 - x2;
            double b = y1 - y2;
            double c = x1 - x3;
            double d = y1 - y3;
            double e = ((x1 * x1 - x2 * x2) - (y2 * y2 - y1 * y1)) / 2;
            double f = ((x1 * x1 - x3 * x3) - (y3 * y3 - y1 * y1)) / 2;

            // 圆心位置 
            double temp = a * d - b * c;
            // 判断三点是否共线
            if (temp == 0)
            {
                // 共线则将第一个点 pt1 作为圆心
                center = new Point(points[0].X, points[0].Y);
                radius = 0;
            }
            else
            {
                double x = (e * d - b * f) / temp;
                double y = (a * f - e * c) / temp;
                center = new Point(x, y);
                radius = CoordinateHelper.LineDistance(center, points[0]);
            }
        }
    }
}
