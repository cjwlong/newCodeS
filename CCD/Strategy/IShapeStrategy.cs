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
    public interface IShapeStrategy
    {
        void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition);
        Shape CreateShape(Point mousePosition, out bool isDrawing);
        bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition);
    }
}
