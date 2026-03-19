using Machine.Models;
using Newtonsoft.Json;
using OperationLogManager.libs;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    public class SensorOffsetViewModel : BindableBase
    {
        [JsonIgnore]
        public bool ConfigChanged = false;
        SensorOffsetModel _model = new();
        public string Name { get => _model.Name; set { SetProperty(ref _model.Name, value); } }
        public double OffsetX
        {
            get => _model.OffsetX;
            set
            {
                if (_model.OffsetX != value)
                {
                    var oldOffsetX = _model.OffsetX;
                    _model.OffsetX = value;
                    LoggingService.Instance.LogInfo($"{Name} OffsetX:  {oldOffsetX} ---> {_model.OffsetX} ,change {_model.OffsetX - oldOffsetX}");
                    //OnPropertyChanged(nameof(AxisOffset));
                    RaisePropertyChanged(nameof(OffsetX));
                    ConfigChanged = true;
                }
            }
        }
        public double OffsetY
        {
            get => _model.OffsetY; set
            {
                if (_model.OffsetY != value)
                {
                    var oldOffsetY = _model.OffsetY;
                    _model.OffsetY = value;
                    LoggingService.Instance.LogInfo($"{Name}  OffsetY: {oldOffsetY} ---> {_model.OffsetY} ,change {_model.OffsetY - oldOffsetY}");
                    //OnPropertyChanged(nameof(AxisOffset));
                    RaisePropertyChanged(nameof(OffsetY));
                    ConfigChanged = true;
                }
            }
        }
        public double OffsetZ
        {
            get => _model.OffsetZ; set
            {
                if (_model.OffsetZ != value)
                {
                    var oldOffsetZ = _model.OffsetZ;
                    _model.OffsetZ = value;
                    LoggingService.Instance.LogInfo($"{Name} OffsetX:  OffsetZ: {oldOffsetZ} ---> {_model.OffsetZ} ,change {_model.OffsetZ - oldOffsetZ}" );
                    //OnPropertyChanged(nameof(AxisOffset));
                    RaisePropertyChanged(nameof(OffsetZ));
                    ConfigChanged = true;
                }
            }
        }
        [JsonIgnore]
        public bool IsChecked { get => _model.IsChecked; set => SetProperty(ref _model.IsChecked, value); }
        [JsonIgnore]
        public double[] GetArray { get => new[] { OffsetX, OffsetY, OffsetZ }; }
        public static double[] operator -(SensorOffsetViewModel a, SensorOffsetViewModel b) =>
            new[] { a.OffsetX - b.OffsetX, a.OffsetY - b.OffsetY, a.OffsetZ - b.OffsetZ };
        public SensorOffsetViewModel()
        {

        }
        public override string ToString()
        {
            return $"Name: {Name},  OffsetX: {OffsetX}，OffsetY: {OffsetY}，OffsetZ: {OffsetZ}";
        }
    }
}
