using CCD.Controls;
using CCD.shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CCD.Strategy
{
    public class LocationPointStrategy : IShapeStrategy
    {
        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = false;
            return new LocationPoint
            {
                Point = new() { SetPixPoint = mousePosition }
            };
        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            throw new NotImplementedException();
        }

        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            drawingVisual.DrawShape();
        }
    }
}
