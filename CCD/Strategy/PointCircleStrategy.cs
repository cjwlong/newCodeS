using CCD.Controls;
using CCD.libs;
using CCD.shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Shape = CCD.shapes.Shape;

namespace CCD.Strategy
{
    public class PointCircleStrategy : IShapeStrategy
    {
        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = true;
            PointCircle shape = new PointCircle();
            shape.UpdatePoints(mousePosition);
            shape.NextStep();
            return shape;

        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            PointCircle pointCircle = (PointCircle)drawingVisual.Shape;
            pointCircle.UpdatePoints(mousePosition);
            pointCircle.NextStep();
            drawingVisual.DrawShape();
            return !pointCircle.IsFinish();
        }

        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            PointCircle pointCircle = (PointCircle)drawingVisual.Shape;
            pointCircle.UpdatePoints(mousePosition);
            drawingVisual.DrawShape();
        }
    }
}
