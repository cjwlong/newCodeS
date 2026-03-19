using LaserManager.libs;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.libs;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LaserManager.ViewModels
{
    public class LaserSettingViewModel : BindableBase
    {
        public LaserSettingViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            BLLaser = containerProvider.Resolve<BLLaser>();
            globalMachineState = containerProvider.Resolve<GlobalMachineState>();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<SetLaserParamEvent>().Subscribe((r) =>
            {
                if (!BLLaser.IsConnected)
                {
                    MessageBox.Show("配置失败！激光器未连接！");
                    throw new Exception("配置失败！激光器未连接！");
                }
                else
                {
                    BLLaser.ChangLaserValue(r);
                }                
            });

            eventAggregator.GetEvent<SaveSettingEvent>().Subscribe((msg) =>
            {
                if (msg.ToString() == "Laser")
                {
                    BLLaser.SaveConfig2File();
                }
            });

            eventAggregator.GetEvent<BLLaserConnectEvent>().Subscribe(async () =>
            {
                await Task.Run(() => LaserConnect());
            });

            timer = new System.Timers.Timer(1000);
            timer.AutoReset = true;
            timer.Elapsed += (obj, e) =>
            {
                if (BLLaser.error != "None") globalMachineState.LaserOk = false;
                else globalMachineState.LaserOk = true;
            };
        }

        IContainerProvider containerProvider;        
        IEventAggregator eventAggregator;
        GlobalMachineState globalMachineState;
        System.Timers.Timer timer;

        private BLLaser _blLaser;
        public BLLaser BLLaser
        {
            get => _blLaser;
            set => SetProperty(ref _blLaser, value);
        }

        private string _SET_BaseFreq;
        public string SET_BaseFreq
        {
            get => _SET_BaseFreq;
            set
            {
                SetProperty(ref _SET_BaseFreq, value);
            }
        }

        private string _SET_Divider;
        public string SET_Divider
        {
            get => _SET_Divider;
            set => SetProperty(ref _SET_Divider, value);
        }

        private string _SET_BurstNum;
        public string SET_BurstNum
        {
            get => _SET_BurstNum;
            set => SetProperty(ref _SET_BurstNum, value);
        }

        private string _SET_PowerFactor;
        public string SET_PowerFactor
        {
            get => _SET_PowerFactor;
            set => SetProperty(ref _SET_PowerFactor, value);
        }

        private DelegateCommand _laserConnectCommand;
        public DelegateCommand LaserConnectCommand => _laserConnectCommand ??
            (_laserConnectCommand = new DelegateCommand(async () => {
            await Task.Run(() => LaserConnect());
            }));

        private DelegateCommand<string> _applyCommand;
        public DelegateCommand<string> ApplyCommand =>
            _applyCommand ?? (_applyCommand = new DelegateCommand<string>((bg) =>
            {
                try
                {
                    List<string> strings = new List<string>() { " ", " ", " ", " " };
                    if (!string.IsNullOrWhiteSpace(bg))
                    {
                        switch (bg)
                        {
                            case "SET_BaseFreq":
                                strings[0] = SET_BaseFreq;
                                break;
                            case "SET_Divider":
                                strings[1] = SET_Divider;
                                break;
                            case "SET_BurstNum":
                                strings[2] = SET_BurstNum;
                                break;
                            case "SET_PowerFactor":
                                strings[3] = SET_PowerFactor;
                                BLLaser.SetPowerFactor = SET_PowerFactor;
                                break;
                            default:
                                break;
                        }
                    }
                    BLLaser.ChangLaserValue(strings);
                    strings.Clear();
                }
                catch (Exception ex)
                {

                }                

                //if (bg.CommitEdit())
                //{
                //    List<string> strings = new List<string>() { SET_BaseFreq, SET_Divider, SET_BurstNum, SET_PowerFactor };
                //    BLLaser.ChangLaserValue(strings);
                //    Debug.WriteLine("激光器数据已更改");
                //    strings.Clear();
                //}
                //else
                //{
                //    Debug.WriteLine("激光器数据更改失败");
                //}
            }));

        private DelegateCommand _openLaserCommand;
        public DelegateCommand OpenLaserCommand => _openLaserCommand ??
            (_openLaserCommand = new DelegateCommand(() => {
                    BLLaser.OpenLaser();
            }));

        private DelegateCommand<string> _selectModeCommand;
        public DelegateCommand<string> SelectModeCommand => _selectModeCommand ??
            (_selectModeCommand = new DelegateCommand<string>((r) =>
            {
                if (string.IsNullOrEmpty(r.ToString())) return;
                else if (r.ToString() == "GATED") BLLaser.SetEXT_TRIG_MOD("GATED");
                else BLLaser.SetEXT_TRIG_MOD("TOD");
            }));

        private DelegateCommand _exTrigOnAndOffCommand;
        public DelegateCommand ExTrigOnAndOffCommand => _exTrigOnAndOffCommand ??
            (_exTrigOnAndOffCommand = new DelegateCommand(() =>
            {
                BLLaser.SetTRIG_EN();
            }));

        private DelegateCommand _emissionOnAndOffCommand;
        public DelegateCommand EmissionOnAndOffCommand => _emissionOnAndOffCommand ??
            (_emissionOnAndOffCommand = new DelegateCommand(() =>
            {
                BLLaser.SetEmission();
            }));

        private async void LaserConnect()
        {
            if (!BLLaser.IsConnected)
            {
                //BLLaser.DisplayText = "激光器连接中...";
                try
                {
                    globalMachineState.LaserOk = true;
                    await Task.Run(() => BLLaser.Connect());
                    if (timer == null) { 
                        timer = new System.Timers.Timer(1000);
                        timer.AutoReset = true;
                        timer.Elapsed += (obj, e)=>
                        {
                            if (BLLaser.error != "None") globalMachineState.LaserOk = false;
                            else globalMachineState.LaserOk = true;
                        };
                    }
                    timer?.Start();
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowDialog($"激光器连接失败：{ex.Message}");
                    return;
                }
                finally
                {
                    if (BLLaser.IsConnected) timer?.Start();
                    else timer?.Stop();
                }
                //if (BLLaser.IsConnected)
                //{
                //    LoggingService.Instance.LogInfo("激光器已连接");
                //}
                //else
                //{
                //    Application.Current.Dispatcher.Invoke(() =>
                //    {
                //        BLLaser.DisplayText = "激光器未连接";
                //        MessageBox.Show("激光器连接失败\n" + msg);
                //        LoggingService.Instance.LogError("激光器连接失败", new Exception(msg));
                //    });                    
                //}
            }
            else if (BLLaser.IsConnected)
            {
                //BLLaser.DisplayText = "激光器断开中...";
                try
                {
                    await Task.Run(() => BLLaser.DisConnect());
                    timer?.Dispose();
                    timer = null;
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowDialog($"激光器断开失败：{ex.Message}");
                }
                finally
                {
                    globalMachineState.LaserOk = true;
                    if (BLLaser.IsConnected) timer?.Start();
                    else timer?.Stop();
                }
                //if (!BLLaser.IsConnected)
                //{
                //    BLLaser.DisplayText = "激光器未连接";
                //    LoggingService.Instance.LogInfo("激光器已断开");
                //}
                //else
                //{
                //    Application.Current.Dispatcher.Invoke(() =>
                //    {
                //        MessageBox.Show("激光器断开失败\n" + msg);
                //        LoggingService.Instance.LogError("激光器断开失败", new Exception(msg));
                //    });                    
                //}
            }
        }
    }
}
