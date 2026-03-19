using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    public class ToolHeadAxisViewModel : BindableBase
    {
        string _name;
        double _position = 0;
        private double _ptpTarget = 0;
        private double _relTarget = 1;
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public double Position { get => _position; set => SetProperty(ref _position, value); }
        public double PtpTarget { get => _ptpTarget; set => SetProperty(ref _ptpTarget, value); }
        public double RelTarget { get => _relTarget; set => SetProperty(ref _relTarget, value); }
        public double RadianValue { get => Position / 180 * Math.PI; }

        public ToolHeadAxisViewModel()
        {
        }
    }
}
