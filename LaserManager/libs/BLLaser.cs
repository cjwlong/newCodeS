using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Mvvm;
using ServiceManager;
using SharedResource.enums;
using SharedResource.events;
using SharedResource.libs;
using ServiceManager;
using SharedResource.tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace LaserManager.libs
{
    public class BLLaser : BindableBase
    {
        public BLLaser()
        {
            LaserHeadTempTimer = new System.Timers.Timer
            {
                Interval = 2000,
                AutoReset = true,
                Enabled = true,
            };

            LoadConfigForFile();
        }

        private string LaserFile_path = Path.Combine(ConfigStore.StoreDir, "Laser.json");
        private System.Timers.Timer LaserHeadTempTimer;
        private Stopwatch stopWatch = new Stopwatch();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _updateTask;

        TcpClient tcpClient;
        TcpClient cmdTcpClient;

        NetworkStream networkStream;
        NetworkStream cmdNetworkStream;
        //private TcpClient powerFactorClient;
        //private NetworkStream powerFactorStream;
        private bool _isRunning = false;
        public string error = "None";

        private string _iP;


       private ConcurrentQueue<string> queueError = new ConcurrentQueue<string>();
        public string IP
        {
            get { return _iP; }
            set 
            { 
                SetProperty(ref _iP, value); 
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set 
            { 
                SetProperty(ref _port, value);
            }
        }
        private bool isConnected = false;
        public bool IsConnected
        {
            get => isConnected;
            set => SetProperty(ref isConnected, value);
        }

        private bool _connecting = false;
        public bool Connecting
        {
            get => _connecting;
            set => SetProperty(ref _connecting, value);
        }

        private bool _isLaserOning = false;
        private bool _isLaserOn = false;
        public bool IsLaserOn
        {
            get => _isLaserOn;
            set
            {
                if (_isLaserOn == value)
                    return;
                SetProperty(ref _isLaserOn, value);
            }
        }

        private string powerFactor = "100";
        public string PowerFactor
        {
            get
            {
                return powerFactor;
            }
            set
            {
                if (value.Equals(powerFactor))
                {
                    return;
                }                
                SetProperty(ref powerFactor, value);
            }
        }

        private string baseFreq = "399";
        public string BaseFreq
        {
            get
            {
                return baseFreq;
            }
            set
            {                
                if (value.Equals(baseFreq))
                {
                    return;
                }
                value = value.Split('k')[0].ToString();
                SetProperty(ref baseFreq, value);
                ActualFrequency = double.Parse(BaseFreq) / double.Parse(Divider);
            }
        }

        private string _divider = "1";
        public string Divider
        {
            get => _divider;
            set
            {
                
                if (value.Equals(_divider))
                {
                    return;
                }
                SetProperty(ref _divider, value);
                ActualFrequency = double.Parse(BaseFreq) / double.Parse(Divider);
            }
        }

        private string _burstNum = "1";
        public string BurstNum
        {
            get => _burstNum;
            set
            {
                if (value.Equals(_burstNum))
                {
                    return;
                }
                SetProperty(ref _burstNum, value);
            }
        }

        private string _setPowerFactor = "1";
        public string SetPowerFactor
        {
            get => _setPowerFactor;
            set
            {
                if (value.Equals(_setPowerFactor))
                {
                    return;
                }
                SetProperty(ref _setPowerFactor, value);
            }
        }

        private string _laserHead = "24.2";
        public string LaserHead
        {
            get => _laserHead;
            set => SetProperty(ref _laserHead, value);
        }

        //private string _intPower = "40.64";
        //public string IntPower
        //{
        //    get => _intPower;
        //    set => SetProperty(ref _intPower, value);
        //}

        private string _laserPower = "15.79";
        public string LaserPower
        {
            get => _laserPower;
            set => SetProperty(ref _laserPower, value);
        }

        // 实际重频
        private double _actualFrequency;
        public double ActualFrequency
        {
            get => _actualFrequency;
            set => SetProperty(ref _actualFrequency, value);
        }

        private string laser_Head_Temp = "0";
        public string Laser_Head_Temp
        {
            get
            {
                return laser_Head_Temp;
            }
            set
            {
                if (value.Contains("C"))
                {
                    value = value.Replace("C", "");
                }
                SetProperty(ref laser_Head_Temp, value);
            }
        }

        private string _GET_EXT_TRIG_ = "GATED";
        public string EXT_TRIG_MOD
        {
            get => _GET_EXT_TRIG_;
            set
            {
                //StopLaserUpdate();
                //if (value == "GATED")
                //    SendMsg(BLLaserCommands.GATE);
                //else if (value == "TOD")
                //    SendMsg(BLLaserCommands.TOD);
                SetProperty(ref _GET_EXT_TRIG_, value);
                //StartOrRestartLaserUpdate();
            }
        }

        // 内控
        private bool _Internal_Trigger_state;
        public bool Internal_Trigger_state
        {
            get
            {
                return _Internal_Trigger_state;
            }
            set { SetProperty(ref _Internal_Trigger_state, value); }
        }

        // 外控
        private bool _External_Trigger_state;
        public bool External_Trigger_state
        {
            get
            {
                return _External_Trigger_state;
            }
            set { SetProperty(ref _External_Trigger_state, value); }
        }

        private string displayText = "未连接";
        public string DisplayText
        {
            get => displayText;
            set => SetProperty(ref displayText, value);
        }

        public string Connect()
        {
            if (IsConnected) return null;

            try
            {
                Connecting = true;

                DisposeAllClients();

                tcpClient = new TcpClient();
                tcpClient.Connect(IP, Port);
                networkStream = tcpClient.GetStream();

                cmdTcpClient = new TcpClient();
                cmdTcpClient.Connect(IP, Port);
                cmdNetworkStream = cmdTcpClient.GetStream();

                //powerFactorClient = new TcpClient();
                //powerFactorClient.Connect(IP, Port);
                //powerFactorStream = powerFactorClient.GetStream();

                IsConnected = true;

                SendMsg(BLLaserCommands.GATE + "\r\n");

                LaserHeadTempTimer.Elapsed += (sender, e) => LaserTempMonitor();
                _cancellationTokenSource = new CancellationTokenSource();
                _updateTask = Task.Run(() => UpdateLaserStatus(_cancellationTokenSource.Token));
                //StartOrRestartLaserUpdate();

                LoggingService.Instance.LogInfo("激光器连接成功");
                return null;
            }
            catch (Exception e)
            {
                LoggingService.Instance.LogError("激光器连接失败", e);
                IsConnected = false;
                return e.Message;
            }
            finally
            {
                if (!tcpClient.Connected)
                { 
                    if(!IsConnected)
                    {
                        GlobalCollectionService<ErrorType>.Instance.Insert((int)LaserErrorType.ConError, ErrorType.LaserError);
                    }
                }
                Connecting = false;
            }
        }

        public string DisConnect()
        {
            if (!IsConnected) return "";

            if (IsLaserOn)
            {
                IsConnected = true;
                var re = MessageBox.Show("激光器未关闭，是否断开连接", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Hand);
                if (re == MessageBoxResult.OK)
                {
                    LoggingService.Instance.LogWarning("断开激光器");
                }else if (re == MessageBoxResult.Cancel){
                    return "";
                }
            }

            try
            {
                Connecting = true;

                LaserHeadTempTimer.Elapsed -= (sender, e) => LaserTempMonitor();
                _cancellationTokenSource?.Cancel();

                networkStream?.Close();
                tcpClient?.Close();

                cmdNetworkStream?.Close();
                cmdTcpClient?.Close();

                //powerFactorStream?.Close();
                //powerFactorClient?.Close();

                IsConnected = false;
                
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("断开激光器失败", ex);
                IsConnected = true;
                return ex.Message;
            }
            finally
            {
                Connecting = false;
            }
        }

        public void OpenLaser()
        {
            if (!IsConnected) return;

            if (!IsLaserOn)
            {
                LoggingService.Instance.LogInfo("激光器开光");
                SendMsg(BLLaserCommands.StartLaser);
                _isLaserOning = true;
            }
            else {
                LoggingService.Instance.LogInfo("激光器关光");
                SendMsg(BLLaserCommands.StopLaser);
                _isLaserOning = false;
            }
        }

        public void SetEmission()
        {
            if (!IsConnected) return;

            if (!Internal_Trigger_state)
                SendMsg(BLLaserCommands.EmissionOn);
            else
                SendMsg(BLLaserCommands.EmissionOff);
        }

        public void SetEXT_TRIG_MOD(string value)
        {
            if (!IsConnected) return;

            //StopLaserUpdate();
            if (value == "GATED")
            {
                SendMsg(BLLaserCommands.GATE);
                LoggingService.Instance.LogInfo("切换为GATED模式");
            }
                
            else if (value == "TOD")
            {
                SendMsg(BLLaserCommands.TOD);
                LoggingService.Instance.LogInfo("切换为TOD模式");
            }
                
            //StartOrRestartLaserUpdate();
        }

        public void SetTRIG_EN()
        {
            if (!IsConnected) return;

            if (!External_Trigger_state)
                SendMsg(BLLaserCommands.ExtTrigOn);
            else
                SendMsg(BLLaserCommands.ExtTrigOff);
        }

        public string SendMsg(string msg)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(msg + "\r\n");
                cmdNetworkStream.Write(buffer, 0, buffer.Length);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        //private string SendPowerFactorCmd(string msg)
        //{
        //    try
        //    {
        //        byte[] buffer = Encoding.ASCII.GetBytes(msg + "\r\n");
        //        powerFactorStream.Write(buffer, 0, buffer.Length);
        //        return null;
        //    }
        //    catch (Exception e)
        //    {
        //        return e.Message;
        //    }
        //}

        private string GetLaserStatus()
        {
            string sendResult = SendStatusMsg();
            if (sendResult != null)
            {
                return "激光器状态读取失败！";
            }

            string acceptMsg = GetStatusReturn();

            // 将接受的数据记录下来
            //Task.Run(async () =>
            //{
            //    try
            //    {
            //        string logPath = Path.Combine(ConfigStore.StoreDir, "LaserStatus.log");
            //        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} -> {acceptMsg}{Environment.NewLine}");
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine("写入日志失败：" + ex.Message);
            //    }
            //});

            string[] lines = acceptMsg.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var statusDir = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    statusDir[parts[0].Trim()] = parts[1].Trim();
                }
            }

            string result = "None";
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                if (statusDir.ContainsKey("Info"))
                    DisplayText = LaserInfoTransfrom(statusDir["Info"].ToString());
                
                if (statusDir.ContainsKey("OutputPower"))
                    LaserPower = statusDir["OutputPower"].ToString();

                if (statusDir.ContainsKey("PowerFactor"))
                    PowerFactor = statusDir["PowerFactor"].ToString();

                if (statusDir.ContainsKey("LaserFrequency"))
                    BaseFreq = statusDir["LaserFrequency"].ToString();

                if (statusDir.ContainsKey("Busrt_Number"))
                    BurstNum = statusDir["Busrt_Number"].ToString();

                if (statusDir.ContainsKey("OutputDivider"))
                    Divider = statusDir["OutputDivider"].ToString();

                if (statusDir.ContainsKey("Laser_Head_TEMP"))
                    LaserHead = statusDir["Laser_Head_TEMP"].ToString();

                if (statusDir.ContainsKey("LaserOn"))
                    IsLaserOn = statusDir["LaserOn"].ToString() == "1" ? true : false;

                if (statusDir.ContainsKey("EXT_TRIG_MOD"))
                    EXT_TRIG_MOD = statusDir["EXT_TRIG_MOD"];

                if (statusDir.ContainsKey("Internal_Trigger_state"))
                {
                    Internal_Trigger_state = statusDir["Internal_Trigger_state"].ToString() == "1" ? true : false;
                    //External_Trigger_state = statusDir["Internal_Trigger_state"].ToString() == "1" ? false : true;
                }


                if (statusDir.ContainsKey("External_Trigger_state"))
                {
                    //Internal_Trigger_state = statusDir["External_Trigger_state"].ToString() == "1" ? true : false;
                    External_Trigger_state = statusDir["External_Trigger_state"].ToString() == "1" ? true : false;
                }

                if (statusDir.ContainsKey("Error"))
                {
                    string err = statusDir["Error"];
                    if (string.IsNullOrWhiteSpace(err) || err == "None")
                        result = "None";
                    else result = err;
                }
            }));

            return result;
        }

        private string LaserInfoTransfrom(string info)
        {
            if (info.Contains("Not"))
            {
                if (_isLaserOning)
                {
                    return "激光器即将启动完成...";
                }
                return "激光器未启动";

            }
            else if (info.Contains("starting"))
            {
                return "激光器正在启动，请等待...";
            }
            else if (info.Contains("turned"))
            {
                return "激光器启动完成！";
            }
            else if (info.Contains("off"))
            {
                return "激光器正在关闭...";
            }
            else
            {
                return "未知状态:" + info;
            }
        }

        private string SendStatusMsg()
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(BLLaserCommands.GetLaserStatus + "\r\n");
                networkStream.Write(buffer, 0, buffer.Length);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private string GetStatusReturn()
        {
            try
            {
                
                GlobalCollectionService<ErrorType>.Instance.Remove((int)LaserErrorType.StatusReturnError, ErrorType.LaserError);
                byte[] buffer = new byte[1024];
                int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            catch (Exception e)
            {
                GlobalCollectionService<ErrorType>.Instance.Insert((int)LaserErrorType.StatusReturnError, ErrorType.LaserError);
                return e.Message;
            }
        }

        private void LaserTempMonitor()
        {
            if (!(double.Parse(Laser_Head_Temp) >= 35))
            {
                return;
            }

            double MarkRealTime = (stopWatch.ElapsedMilliseconds) / 1000.0;

            if (MarkRealTime <= 30)
            {
                return;
            }

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                System.Windows.MessageBox.Show("激光器温度过高，请检查水冷是否正常工作！");
                
            }));

            stopWatch.Restart();
        }

        private async void UpdateLaserStatus(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    error = GetLaserStatus();
                    if(!string.IsNullOrEmpty(error)&& error != "None")  
                    {
                        GlobalCollectionService<ErrorType>.Instance.Insert((int)LaserErrorType.LaserStatusError, ErrorType.LaserError);
                    }
                    else
                    {
                        GlobalCollectionService<ErrorType>.Instance.Remove((int)LaserErrorType.LaserStatusError, ErrorType.LaserError);
                    }
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"在获取激光状态循环中操作被取消，已执行时间：{stopwatch.ElapsedMilliseconds} 毫秒");
                    }
                    finally
                    {
                        stopwatch.Stop();
                        Debug.WriteLine($"获取激光状态循环，本次延迟实际耗时：{stopwatch.ElapsedMilliseconds} 毫秒");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"获取激光状态时发生异常：{ex.Message}");
                }
            }
        }

        // 启动或重新启动任务
        public async Task StartOrRestartLaserUpdate()
        {
            StopLaserUpdate();

            _cancellationTokenSource = new CancellationTokenSource();

            await Task.Delay(2000);

            // 启动新任务
            _updateTask = Task.Run(async () => {
                UpdateLaserStatus(_cancellationTokenSource.Token);
            });
            _isRunning = true;
            Debug.WriteLine("激光状态更新任务已启动");
        }

        // 停止任务
        public void StopLaserUpdate()
        {
            if (!_isRunning || _updateTask == null || _updateTask.IsCompleted)
            {
                return;
            }

            try
            {
                _cancellationTokenSource?.Cancel();

                if (!_updateTask.Wait(100))
                {
                    Debug.WriteLine("激光状态更新任务未能及时停止");
                }
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine($"停止激光状态更新任务时发生异常: {ex.InnerException.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _isRunning = false;
                Debug.WriteLine("激光状态更新任务已停止");
            }
        }

        public async Task ChangLaserValue(List<string> lis)
        {
            if (!string.IsNullOrWhiteSpace(lis[0]) && !(int.Parse(BaseFreq) == int.Parse(lis[0])))
            {
                LoggingService.Instance.LogInfo($"激光器频率：{BaseFreq}-->{lis[0]}");
                SendMsg(BLLaserCommands.LaserFrequencyConfig + lis[0] + "kHz");
                await Task.Delay(1000);
            }
              
            if (!string.IsNullOrWhiteSpace(lis[1]))
                if (!(int.Parse(Divider.Split('.')[0]) == int.Parse(lis[1])) && lis[1] != "")
                {
                    LoggingService.Instance.LogInfo($"AOM 分频数：{Divider}-->{lis[1]}");
                    SendMsg(BLLaserCommands.OutputDividerConfig + lis[1]);
                    await Task.Delay(1000);
                }

            if (!string.IsNullOrWhiteSpace(lis[2])  && !BurstNum.Equals(lis[2]))
            {
                LoggingService.Instance.LogInfo($"Burst 脉冲数：{BurstNum}-->{lis[2]}");
                SendMsg(BLLaserCommands.BurstNumberConfig + lis[2]);
                await Task.Delay(1000);
            }

            if (!string.IsNullOrWhiteSpace(lis[3]) && !PowerFactor.Equals(lis[3]))
            {
                LoggingService.Instance.LogInfo($"激光器目标功率：{PowerFactor}-->{lis[3]}");
                //SendPowerFactorCmd(BLLaserCommands.LaserPowerConfig + lis[3]);
                SendMsg(BLLaserCommands.LaserPowerConfig + lis[3]);
            }
        }

        private void DisposeAllClients()
        {
            try
            {
                networkStream?.Close();
                tcpClient?.Close();
                tcpClient = null;

                cmdNetworkStream?.Close();
                cmdTcpClient?.Close();
                cmdTcpClient = null;

                //powerFactorStream?.Close();
                //powerFactorClient?.Close();
                //powerFactorClient = null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("释放旧连接失败", ex);
            }
        }

        public void SaveConfig2File()
        {
            try
            {
                JObject json = new JObject
                {
                    { nameof(IP), IP},
                    { nameof(Port), Port},
                };

                ConfigStore.CheckStoreFloder();
                File.WriteAllText(LaserFile_path, json.ToString());
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("激光器配置保存失败！", ex);
            }
        }

        public void LoadConfigForFile()
        {
            try
            {
                if (File.Exists(LaserFile_path))
                {
                    string info = File.ReadAllText(LaserFile_path);
                    JObject json = JObject.Parse(info);

                    string ip = json[nameof(IP)]?.ToString();
                    int? port = json[nameof(Port)]?.ToObject<int>();

                    if (!string.IsNullOrWhiteSpace(ip) && port.HasValue)
                    {
                        IP = ip;
                        Port = port.Value;
                    }
                    else LoggingService.Instance.LogWarning("激光器配置信息缺失，初始化连接信息失败！");
                }
                else
                {
                    LoggingService.Instance.LogWarning("未找到激光器配置文件，初始化连接信息失败！");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("初始化激光器连接信息失败！", ex);
            }
        }

    }
}
