using CCD.Controls;
using CCD.shapes;
using CCD.tools;
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
    public class LineStrategy : IShapeStrategy
    {
        public void UpdateShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            Line line = (Line)drawingVisual.Shape;
            line.EndPoint.SetPixPoint = mousePosition;
            drawingVisual.DrawShape();
        }

        public Shape CreateShape(Point mousePosition, out bool isDrawing)
        {
            isDrawing = true;
            return new Line
            {
                StartPoint = new() { SetPixPoint = mousePosition },
                EndPoint = new() { SetPixPoint = mousePosition },
                MidPoint = new() { SetPixPoint = mousePosition }
            };
        }

        public bool FinishShape(ImgDrawingVisual drawingVisual, Point mousePosition)
        {
            Line line = (Line)drawingVisual.Shape;
            line.EndPoint = new() { SetPixPoint = mousePosition };
            Vector vector = line.EndPoint.MacPoint - line.StartPoint.MacPoint;
            var mid_mac = line.StartPoint.MacPoint + vector / 2;
            line.MidPoint = new()
            {
                SetPixPoint =
                    CoordinateHelper.Instance.ConvertToPix(
                        CoordinateHelper.Instance.ConvertToRealByAbsolute(CoordinateHelper.Instance.MachinePoint, mid_mac))
            };
            drawingVisual.DrawShape();
            return false;
        }
    }
}
