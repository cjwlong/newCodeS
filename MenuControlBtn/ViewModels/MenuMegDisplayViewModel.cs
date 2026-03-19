using Machine.Interfaces;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace MenuControl.ViewModels
{
    public class MenuMegDisplayViewModel : BindableBase
    {
        public MenuMegDisplayViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            machine = containerProvider.Resolve<IMachine>();

            _dispatcher = Application.Current.Dispatcher;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => CurrentTime = DateTime.Now;
            _timer.Start();
        }

        private IContainerProvider containerProvider;
        public IMachine machine { get; set; }

        private readonly DispatcherTimer _timer;
        private readonly Dispatcher _dispatcher;
        private bool _isDisposed;

        private DateTime _currentTime;
        public DateTime CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }
    }
}
