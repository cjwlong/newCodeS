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
    public class SquareContourStrategy : IShapeStrategy
    {
        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = true;
            SquareContour shape = new SquareContour();
            shape.UpdatePoints(mousePosition);
            shape.NextStep();
            return shape;
        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            SquareContour shape = (SquareContour)drawingVisual.Shape;
            shape.UpdatePoints(mousePosition);
            shape.NextStep();
            drawingVisual.DrawShape();
            return !shape.IsFinish();
        }

        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            SquareContour shape = (SquareContour)drawingVisual.Shape;
            shape.UpdatePoints(mousePosition);
            drawingVisual.DrawShape();
        }
    }
}
