using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceMegManager.ViewModels
{
    public class ProcessProgressViewModel : BindableBase
    {
        public ProcessProgressViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<ProgressMegevent>().Subscribe((progress) =>
            {
                if (!string.IsNullOrEmpty(progress))
                {
                    if (double.TryParse(progress, out double re))
                    {
                        if (re < 0)
                        {
                            Number = re.ToString();
                        }
                        else
                        {
                            Progress = re * 100;
                        }
                    }
                }
            });
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;

        private string _number = "未加工";
        public string Number
        {
            get { return _number; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    value = "第" + value + "槽";
                    SetProperty(ref _number, value);
                }
            }
        }

        private double _progress = 0;
        public double Progress
        {
            get { return _progress; }
            set
            {
                SetProperty(ref _progress, value);
            }
        }
    }
}
