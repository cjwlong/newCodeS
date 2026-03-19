using CCD.Controls;
using CCD.shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CCD.Strategy
{
    public class CircleStrategy : IShapeStrategy
    {
        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            Circle circle = (Circle)drawingVisual.Shape;
            double radius = Math.Sqrt(Math.Pow(mousePosition.X - circle.Center.PixPoint.X, 2) + Math.Pow(mousePosition.Y - circle.Center.PixPoint.Y, 2));
            circle.Radius = radius;
            drawingVisual.DrawShape();
        }

        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = true;
            return new Circle
            {
                Center = new() { SetPixPoint = mousePosition },
                Radius = 0
            };
        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            Circle circle = (Circle)drawingVisual.Shape;
            circle.Radius = (mousePosition - circle.Center.PixPoint).Length;
            drawingVisual.DrawShape();
            return false;
        }
    }
}
