using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using CCD.shapes;

namespace CCD.Controls
{
    public class ImgDrawingVisual : DrawingVisual
    {
        public Shape Shape { get; }

        public ImgDrawingVisual(Shape shape)
        {
            Shape = shape;
            Shape.PropertyChanged += OnShapePropertyChanged;

        }

        public void DrawShape()
        {
            using DrawingContext drawingContext = RenderOpen();
            Shape.Draw(drawingContext);
        }

        public void SelectDrawShape()
        {
            using DrawingContext drawingContext = RenderOpen();
            Shape.LightDraw(drawingContext);
        }

        public void MoveReDrawShape(Vector vector)
        {
            Shape.ShapeMove(vector);
            using DrawingContext drawingContext = RenderOpen();
            Shape.ReDraw(drawingContext);
        }
        public void MoveReDrawShape(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            Shape.ShapeMove(point, dir_z, dir_x);
            using DrawingContext drawingContext = RenderOpen();
            Shape.ReDraw(drawingContext);
        }

        public void ReDrawShape()
        {
            using DrawingContext drawingContext = RenderOpen();
            Shape.ReDraw(drawingContext);
        }

        private void OnShapePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelect")
            {
                if (Shape.IsSelect)
                {
                    SelectDrawShape();
                }
                else
                {
                    DrawShape();
                }
            }
        }
    }
}
