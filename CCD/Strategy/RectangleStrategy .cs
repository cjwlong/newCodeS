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
    public class RectangleStrategy : IShapeStrategy
    {
        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            Rectangle rectangle = (Rectangle)drawingVisual.Shape;
            rectangle.EndPoint = mousePosition;
            drawingVisual.DrawShape();
        }

        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = true;
            return new Rectangle
            {
                StartPoint = mousePosition,
                EndPoint = mousePosition
            };
        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            Rectangle rectangle = (Rectangle)drawingVisual.Shape;
            rectangle.EndPoint = mousePosition;
            drawingVisual.DrawShape();
            return false;
        }
    }
}
