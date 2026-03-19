using Machine.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    public class IOViewModel : BindableBase
    {
        IOModel model = new();
        public int Port { get => model.Port; set => SetProperty(ref model.Port, value); }
        public int Bit { get => model.Bit; set => SetProperty(ref model.Bit, value); }
        public bool? Value { get => model.Value; set => SetProperty(ref model.Value, value); }
        public string Name { get => model.Name; set => SetProperty(ref model.Name, value); }
    }
}
