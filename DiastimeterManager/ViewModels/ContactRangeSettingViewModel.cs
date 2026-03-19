using DiastimeterManager.libs;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using RJCP.IO.Ports;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.events.Machine;
using SharedResource.events.RangeFinder;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace DiastimeterManager.ViewModels
{
    internal class ContactRangeSettingViewModel : BindableBase
    {
        public ContactRangeSettingViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            LGQuick = containerProvider.Resolve<LGQuick>();
            globalMachineState = containerProvider.Resolve<GlobalMachineState>();

            BaudRates = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200 };
            SelectedBaudRate = 115200;

            eventAggregator.GetEvent<ContactRangefinderConnectEvent>().Subscribe(() =>
            {
                    ExecuteConnect();
            });

            RefreshCOM();
        }

        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;
        private GlobalMachineState globalMachineState;

        private System.Timers.Timer _timer;
        private int _limitAlerted = 0; // 0 = not alerted, 1 = alerted
        public ObservableCollection<int> BaudRates { get; }

        private ObservableCollection<string> _availablePorts;
        public ObservableCollection<string> AvailablePorts
        {
            get => _availablePorts;
            set => SetProperty(ref _availablePorts, value);
        }

        private bool _StartListen = false;
        public bool StartListen
        {
            get => _StartListen;
            set
            {
                SetProperty(ref _StartListen, value);
            }
        }

        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                SetProperty(ref _selectedPort, value);
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        private int _selectedBaudRate;
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        private LGQuick _lGQuick;
        public LGQuick LGQuick
        {
            get { return _lGQuick; }
            set
            {
                SetProperty(ref _lGQuick, value);
            }
        }

        private double _limitValue;
        public double LimitValue
        {
            get => _limitValue;
            set
            {
                SetProperty(ref _limitValue, value);
            }
        }

        private DelegateCommand<object> _changeListenStatusCommand;
        public DelegateCommand<object> ChangeListenStatusCommand => _changeListenStatusCommand ??
            (_changeListenStatusCommand = new DelegateCommand<object>((r) =>
            {
                if (r != null)
                {
                    if ((bool)r)
                    {
                        Interlocked.Exchange(ref _limitAlerted, 0);
                        _timer = new System.Timers.Timer(100);
                        _timer.Elapsed += ListenValueChange;
                        _timer.AutoReset = true;
                        _timer.Start();
                        StartListen = true;
                    }
                    else
                    {
                        StartListen = false;
                        _timer?.Stop(); 
                        _timer.Elapsed -= ListenValueChange; 
                        _timer?.Dispose(); 
                        _timer = null;
                    }
                }
            }));

        private void ListenValueChange(object sender, ElapsedEventArgs e)
        {
            if (LimitValue == 0) return;

            if (!LGQuick.ListenerValue(LimitValue))
            {
                globalMachineState.LimitSafe = true;
                return;
            }

            if (Interlocked.Exchange(ref _limitAlerted, 1) != 0) return;

            // 立即停止定时器，防止更多 Elapsed 事件再进来
            try
            {
                _timer?.Stop();
                _timer.Elapsed -= ListenValueChange;
            }
            catch { }

            globalMachineState.LimitSafe = false;
            eventAggregator.GetEvent<KillAllAxisEvent>().Publish();

            Application.Current.Dispatcher.Invoke(() =>
            {
                StartListen = false;
                try { _timer?.Dispose(); _timer = null; } catch {  }
                MessageBox.Show($"接触式测距仪超出预期限位：{LimitValue}，已停止监听");
            });
        }

        private DelegateCommand _connectCommand;
        public DelegateCommand ConnectCommand =>
            _connectCommand = (_connectCommand = new DelegateCommand(ExecuteConnect));

        private DelegateCommand _refreshPortConnectCommand;
        public DelegateCommand RefreshPortConnectCommand => _refreshPortConnectCommand ??
            (_refreshPortConnectCommand = new DelegateCommand(RefreshCOM));

        private DelegateCommand _zeroSettingCommand;
        public DelegateCommand ZeroSettingCommand => _zeroSettingCommand ??
            (_zeroSettingCommand = new DelegateCommand(() =>
            {
                //if (msg.ToString() == "0")
                //{
                //    LGQuick.SetZeroSetting(0);
                //}
                //else
                //{
                //    var value = LGQuick.LGQuickValue;
                //    LGQuick.SetZeroSetting(value);
                //}
                LGQuick.SetZeroSetting();
            }));

        public void ExecuteConnect()
        {
            Task.Run(() =>
            {
                try
                {
                    if (LGQuick.DeviceStatus == SharedResource.enums.DeviceStatus.Disconnected)
                    {
                        LGQuick.Connect(SelectedPort, SelectedBaudRate);
                    }
                    else
                    {
                        LGQuick.Disconnect();
                    }
                }
                catch
                {
                }
            });
        }

        private void RefreshCOM()
        {
            var result = SerialPortStream.GetPortNames();
            AvailablePorts = new ObservableCollection<string>(result);
            if (AvailablePorts.Count > 0)
            {
                SelectedPort = AvailablePorts[0];
                foreach (var port in AvailablePorts)
                {
                    if (LGQuick._portName == port)
                        SelectedPort = port;
                }
                foreach (var baudRate in BaudRates)
                {
                    if (LGQuick._baudRate == baudRate)
                        SelectedBaudRate = baudRate;
                }                
            }                
        }
    }
}
