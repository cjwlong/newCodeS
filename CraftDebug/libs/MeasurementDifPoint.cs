using Newtonsoft.Json.Linq;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftDebug.libs
{
    public class MeasurementDifPoint : BindableBase
    {
        public int Index { get; set; }

        private double? _pointA;
        public double? PointA
        {
            get { return _pointA; }
            set
            {
                SetProperty(ref _pointA, value);
                Change_DifferenceValue();
                Change_Difference_AverageValue();
            }
        }

        private double? _pointB;
        public double? PointB
        {
            get { return _pointB; }
            set
            {
                SetProperty(ref _pointB, value);
                Change_DifferenceValue();
                Change_Difference_AverageValue();
            }
        }

        private double? _difference;
        public double? Difference
        {
            get { return _difference; }
            set
            {
                SetProperty(ref _difference, value);
            }
        }

        private double? _difference_Average;
        public double? Difference_Average
        {
            get { return _difference_Average; }
            set
            {
                SetProperty(ref _difference_Average, value);
            }
        }

        private void Change_DifferenceValue()
        {
            if (PointA != null && PointB != null)
            {
                double val = Math.Round(PointB.Value - PointA.Value, 4);

                if (val == double.MaxValue || val == double.MinValue)
                    return;
                Difference = val;
            }
        }

        private void Change_Difference_AverageValue()
        {
            if (PointA != null && PointB != null)
            {
                double val = Math.Round((PointB.Value - PointA.Value) / 3, 5);

                if (val == double.MaxValue || val == double.MinValue)
                    return;
                Difference_Average = val;
            }
        }
    }
}
