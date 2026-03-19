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
    public class IntersectionLinesStrategy : IShapeStrategy
    {
        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = true;
            IntersectionLines shape = new IntersectionLines();
            shape.UpdatePoints(mousePosition);
            shape.NextStep();
            return shape;
        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            IntersectionLines shape = (IntersectionLines)drawingVisual.Shape;
            shape.UpdatePoints(mousePosition);
            shape.NextStep();
            drawingVisual.DrawShape();
            return !shape.IsFinish();
        }

        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            IntersectionLines shape = (IntersectionLines)drawingVisual.Shape;
            shape.UpdatePoints(mousePosition);
            drawingVisual.DrawShape();
        }
    }
}
