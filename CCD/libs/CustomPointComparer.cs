using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CCD.libs
{
    public class CustomPointComparer : IComparer<Point>
    {
        public int Compare(Point p1, Point p2)
        {
            int equalYThreshold = 50;

            if (Math.Abs(p1.Y - p2.Y) <= equalYThreshold)
            {
                if (p1.X == p2.X)
                    return 0;

                return p1.X.CompareTo(p2.X);
            }
            else
            {
                return p1.Y.CompareTo(p2.Y);
            }
        }
    }
}
