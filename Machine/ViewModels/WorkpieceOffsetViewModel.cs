using Machine.Models;
using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WorkpieceOffsetViewModel : BindableBase
    {
        public WorkpieceOffsetViewModel() { }
        private WorkpieceOffsetModel _offsetModel = new();
        [JsonIgnore]
        public Action DataChanged { get; set; }
        public double X { get => _offsetModel.X; set { SetProperty(ref _offsetModel.X, value); DataChanged?.Invoke(); } }
        public double Y { get => _offsetModel.Y; set { SetProperty(ref _offsetModel.Y, value); DataChanged?.Invoke(); } }
        public double Z { get => _offsetModel.Z; set { SetProperty(ref _offsetModel.Z, value); DataChanged?.Invoke(); } }
        public double Alpha { get => _offsetModel.Alpha; set { SetProperty(ref _offsetModel.Alpha, value); DataChanged?.Invoke(); } }
        public double Beta { get => _offsetModel.Beta; set { SetProperty(ref _offsetModel.Beta, value); DataChanged?.Invoke(); } }
        public double Gamma { get => _offsetModel.Gamma; set { SetProperty(ref _offsetModel.Gamma, value); DataChanged?.Invoke(); } }
        public double[] GetOffset() =>
            new double[6] { X, Y, Z, Alpha, Beta, Gamma };
        public override string ToString()
        {
            return $"X:{X:F4} ,Y:{Y:F4} ,Z:{Z:F4} ,α:{Alpha:F4} ,β:{Beta:F4} ,γ:{Gamma:F4}";
        }
    }
}
