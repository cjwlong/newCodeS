using CCD.shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace CCD.Controls
{
    public class ImgCanvas : Canvas
    {
        public ObservableCollection<Shape> Shapes { get; }
        private readonly Dictionary<Guid, ImgDrawingVisual> visualDictionary;

        public ImgCanvas()
        {
            Shapes = new ObservableCollection<Shape>();
            visualDictionary = new Dictionary<Guid, ImgDrawingVisual>();
        }

        public void AddVisual(ImgDrawingVisual visual)
        {
            if (visualDictionary.ContainsKey(visual.Shape.Id))
            {
                RemoveVisualChild(visualDictionary[visual.Shape.Id]);
                RemoveLogicalChild(visualDictionary[visual.Shape.Id]);
            }
            visualDictionary[visual.Shape.Id] = visual;
            //Shapes.Add(visual.Shape);
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        public void DeleteVisualById(Guid uuid)
        {
            if (visualDictionary.ContainsKey(uuid))
            {
                RemoveVisualChild(visualDictionary[uuid]);
                RemoveLogicalChild(visualDictionary[uuid]);
                visualDictionary.Remove(uuid);
            }
        }

        public void DeleteVisual(ImgDrawingVisual drawingVisual)
        {
            RemoveVisualChild(drawingVisual);
            RemoveLogicalChild(drawingVisual);
            Shapes.Remove(drawingVisual.Shape);
            visualDictionary.Remove(drawingVisual.Shape.Id);
        }

        public void ClearVisual()
        {
            foreach (KeyValuePair<Guid, ImgDrawingVisual> kvp in visualDictionary)
            {
                ImgDrawingVisual value = kvp.Value;
                RemoveVisualChild(value);
                RemoveLogicalChild(value);
            }

            if (Shapes.Count > 0)
            {
                Shapes.Clear();
            }

            visualDictionary.Clear();
        }

        public List<ImgDrawingVisual> GetAllImgDrawingVisuals()
        {
            List<ImgDrawingVisual> visualList = visualDictionary.Values.ToList();
            return visualList;
        }

        public ImgDrawingVisual GetVisualById(Guid uuid)
        {
            if (visualDictionary.ContainsKey(uuid))
            {
                return visualDictionary[uuid];
            }
            return null;
        }

        public void ClearShapeCache()
        {
            foreach (var shape in Shapes)
            {
                shape.ClearCache();
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= visualDictionary.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            int currentIndex = 0;
            foreach (Visual visual in visualDictionary.Values)
            {
                if (currentIndex == index)
                {
                    return visual;
                }
                currentIndex++;
            }

            return null;
        }

        protected override int VisualChildrenCount => visualDictionary.Count;

    }
}
