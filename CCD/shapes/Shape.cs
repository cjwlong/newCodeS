using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using CCD.tools;
using System.Windows;
using CCD.libs;
using CoordinateHelper = CCD.tools.CoordinateHelper;

namespace CCD.shapes
{
    public abstract class Shape : BindableBase
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public Pen Pen
        {
            get
            {
                return BackgroundHelper.Instance.CachedPen;
            }
        }

        public Brush LightBrush { get; set; } = Brushes.Yellow;
        private readonly Dictionary<string, object> cache;

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                SetProperty(ref _isExpanded, value);
            }

        }


        private bool _isSelect = false;
        public bool IsSelect
        {
            get { return _isSelect; }
            set
            {
                SetProperty(ref _isSelect, value);
            }
        }

        public Point MachinePoint { get; set; }
        public Shape()
        {
            Id = Guid.NewGuid();
            Pen.Freeze();
            MachinePoint = CoordinateHelper.Instance.MachinePoint;
            cache = new Dictionary<string, object>();
        }

        public void ReDraw(DrawingContext drawingContext)
        {
            if (IsSelect)
            {
                LightDraw(drawingContext);
            }
            else
            {
                Draw(drawingContext);
            }
        }

        public abstract void Draw(DrawingContext drawingContext);
        public abstract void LightDraw(DrawingContext drawingContext);

        protected Pen LightShape(Pen pen)
        {
            return BackgroundHelper.Instance.CachedLightPen;
        }

        private bool CacheKeyExists(string cacheKey)
        {
            // Check if the cache contains the key
            return cache.ContainsKey(cacheKey);
        }

        private T GetCachedValue<T>(string key)
        {
            if (cache.ContainsKey(key))
            {
                return (T)cache[key];
            }

            return default;
        }
        protected T GetCachedValueOrDefault<T>(string cacheKey, Func<T> valueDelegate)
        {
            if (CacheKeyExists(cacheKey))
            {
                return GetCachedValue<T>(cacheKey);
            }
            else
            {
                T calculatedValue = valueDelegate();
                SetCachedValue(cacheKey, calculatedValue);
                return calculatedValue;
            }
        }

        protected void SetCachedValue(string key, object value)
        {
            if (cache.ContainsKey(key))
            {
                cache[key] = value;
            }
            else
            {
                cache.Add(key, value);
            }
        }

        public void ClearCache()
        {
            cache.Clear();
        }

        private static Color Lighten(Color color, double factor)
        {
            double r = color.R + (255 - color.R) * factor;
            double g = color.G + (255 - color.G) * factor;
            double b = color.B + (255 - color.B) * factor;

            return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }

        public virtual void ShapeMove(Vector point)
        {
            return;
        }
        public virtual void ShapeMove(Point3D point, Vector3D dir_z, Vector3D dir_x)
        {
            return;
        }

        public virtual string DisplayShape()
        {
            return "我是一个图形";
        }

        public virtual Point? MoveToShape()
        {
            return null;
        }

        public virtual double[] MoveToCenter()
        {
            return null;
        }

        public virtual ShapeDto GetDto()
        {
            return null;
        }


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            // 比较 ID 属性的值
            return Id.Equals(((Shape)obj).Id);
        }

        // 重写 GetHashCode 方法
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public abstract class ShapeDto
        {
            public Guid Id { get; }
            public string Name { get; set; }

            public ShapeDto(Shape shape)
            {
                Id = shape.Id;
                Name = shape.Name;
            }

            public abstract List<netDxf.Entities.EntityObject> ToDxf();

            public abstract MeshGeometry3D ToSTL();
        }
    }
}
