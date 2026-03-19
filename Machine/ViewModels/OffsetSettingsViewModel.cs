using Newtonsoft.Json;
using OperationLogManager.libs;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.events.Machine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    public class MechanicalStructureParameter : BindableBase
    {
        private string _name;
        private double _value;
        [JsonIgnore]
        public bool ConfigChanged = false;
        public string Name { get => _name; set { SetProperty(ref _name, value); ConfigChanged = true; } }
        public double Value
        {
            get => _value; set
            {
                if (_value != value)
                {
                    var oldvalue = _value;
                    _value = value;
                    LoggingService.Instance.LogInfo($"结构参数 {Name} :{oldvalue} ---> {_value}   变化 {_value - oldvalue}");
                    RaisePropertyChanged(nameof(_value));
                    ConfigChanged = true;
                    //SetProperty(ref _value, value);
                }
                //  SetProperty(ref AxisDefination.MaxSoftLimit, value); ConfigStoredFlag = false;
            }
        }
        public MechanicalStructureParameter(string name, double value)
        {
            Name = name;
            Value = value;
        }
        //重写Tostring方便日志输出
        public override string ToString()
        {
            return $" Name: {Name}, Value: {Value}";
        }
    }
    public class OffsetSettingsViewModel : BindableBase
    {
        //private double _xtb = 0;
        //private double _xtc = 0;
        //private double _yta = 0;
        //private double _ytc = 0;
        //private double _zta = 0;
        //private double _ztb = 0;
        //private double _ha = 0;
        //private double _hb = 0;

        [JsonIgnore]
        private bool _configChanged = false;
        [JsonIgnore]
        public bool ConfigChanged
        {
            get => _configChanged; set
            {
                SetProperty(ref _configChanged, value);
                //StaticEventAggregator.eventAggregator.GetEvent<ModelPresenterSetModelOffsetEvent>().Publish(new("", new List<double>(WorkpieceOffset.GetOffset()))); ;
            }
        }
        private WorkpieceOffsetViewModel _workpieceOffset;
        public WorkpieceOffsetViewModel WorkpieceOffset { get => _workpieceOffset; set => SetProperty(ref _workpieceOffset, value); }
        public ObservableCollection<SensorOffsetViewModel> SensorOffset { get; set; }
        //public double XTB { get => _xtb; set { SetProperty(ref _xtb, value); ConfigChanged = true; } }
        //public double XTC { get => _xtc; set { SetProperty(ref _xtc, value); ConfigChanged = true; } }
        //public double YTA { get => _yta; set { SetProperty(ref _yta, value); ConfigChanged = true; } }
        //public double YTC { get => _ytc; set { SetProperty(ref _ytc, value); ConfigChanged = true; } }
        //public double ZTA { get => _zta; set { SetProperty(ref _zta, value); ConfigChanged = true; } }
        //public double ZTB { get => _ztb; set { SetProperty(ref _ztb, value); ConfigChanged = true; } }
        //public double HA { get => _ha; set { SetProperty(ref _ha, value); ConfigChanged = true; } }
        //public double HB { get => _hb; set { SetProperty(ref _hb, value); ConfigChanged = true; } }
        public ObservableCollection<MechanicalStructureParameter> XTX { get; set; } = new();
        public SensorOffsetViewModel GetSelectedSensor()
        {
            foreach (var sensor in SensorOffset)
                if (sensor.IsChecked)
                    return sensor;
            return null;
        }
        public int GetSelectedSensorIndex()
        {
            return 1;
        }
        public double[] GetXTX()
            => XTX.Select(x => x.Value).ToArray();



        public OffsetSettingsViewModel()
        {
            SensorOffset = new();
            WorkpieceOffset = new();
        }
    }
}
