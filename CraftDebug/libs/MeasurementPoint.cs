using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftDebug.libs
{
    public class MeasurementPoint : BindableBase
    {
        public int Index { get; set; }

        private double? _point1;
        public double? Point1
        {
            get => _point1;
            set
            {
                SetProperty(ref _point1, value);
                ChangeAverage();
            }
        }

        private double? _point2;
        public double? Point2
        {
            get => _point2;
            set
            {
                SetProperty(ref _point2, value);
                ChangeAverage();
            }
        }

        private double? _point3;
        public double? Point3
        {
            get => _point3;
            set
            {
                SetProperty(ref _point3, value);
                ChangeAverage();
            }
        }

        private double? _average;
        public double? Average
        {
            get
            {
                return _average;
            }
            set
            {
                SetProperty(ref _average, value);
            }
        }

        private void ChangeAverage()
        {
            if (Point1 != null && Point2 != null && Point3 != null)
                Average = (Point1.Value + Point2.Value + Point3.Value) / 3.0;
        }
    }

}
