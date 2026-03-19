using ACS.SPiiPlusNET;
using IronPython.Compiler.Ast;
using Machine.Enums;
using Machine.Interfaces;
using Machine.Models;
using OperationLogManager.libs;
using Prism.Services.Dialogs;
using SharedResource.libs;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using System.Diagnostics;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;
using Prism.Mvvm;
using Prism.Ioc;
using Prism.Events;
using SharedResource.events;
using System.Windows;
using Microsoft.Scripting.Hosting;
using SharedResource.enums;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace Machine.Harware
{
    public class MachineHardwareAcs : BindableBase, IMachineHardware
    {
        bool IsListen = false;
        //bool IsProcessing = false;

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;
        readonly string home_path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "home");
        private readonly string HOME_FilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "home", "homeAll.txt");

        public Api Acs { get; private set; }
        AxesCompensation Axes_Compensation;

        private GlobalProcessStatus _globalProcessStatus;
        public GlobalProcessStatus GlobalProcessStatus
        {
            get { return _globalProcessStatus; }
            set
            {
                SetProperty(ref _globalProcessStatus, value);
            }
        }

        // 限位Mask
        private const SafetyControlMasks LL = SafetyControlMasks.ACSC_SAFETY_LL;
        private const SafetyControlMasks SLL = SafetyControlMasks.ACSC_SAFETY_SLL;
        private const SafetyControlMasks RL = SafetyControlMasks.ACSC_SAFETY_RL;
        private const SafetyControlMasks SRL = SafetyControlMasks.ACSC_SAFETY_SRL;
        private Axis GetAxis(int node)
            => (Axis)node;

        public MachineHardwareAcs(IContainerProvider provider)
        {
            this.containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            GlobalProcessStatus = containerProvider.Resolve<GlobalProcessStatus>();

            //eventAggregator.GetEvent<Cmd_StartProcessEvent>().Subscribe(async (meg) =>
            //{
            //    if (File.Exists(meg.Item2))
            //    {
            //        Acs.OpenMessageBuffer(1024);
            //        await Task.Delay(1500);
            //        await RunBufferFile(meg.Item2, true);
            //        WaitBufferFinished();
            //    }
            //}, ThreadOption.BackgroundThread);

            eventAggregator.GetEvent<Cmd_PauseProcessEvent>().Subscribe(() =>
            {
                if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
                {
                    LoggingService.Instance.LogInfo($"暂停执行加工工艺");
                    SuspendBuffer();
                }
                    
            }, ThreadOption.BackgroundThread);

            //eventAggregator.GetEvent<Cmd_StopProcessEvent>().Subscribe(() =>  k卡顿
            //{
            //    if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
            //    {
            //        LoggingService.Instance.LogInfo($"停止执行加工工艺");
            //        StopBufferRUN();
            //    }

            //}, ThreadOption.BackgroundThread);

            //eventAggregator.GetEvent<Cmd_StopProcessEvent>().Subscribe(() =>
            //{
            //    Task.Run(() =>
            //    {
            //        try
            //        {
            //            if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
            //            {
            //                LoggingService.Instance.LogInfo("停止执行加工工艺");
            //                StopBufferRUN();
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            LoggingService.Instance.LogError("StopBufferRUN error: " + ex.Message);
            //        }
            //    });
            //}, ThreadOption.BackgroundThread);
            eventAggregator.GetEvent<Cmd_StopProcessEvent>().Subscribe(async request =>
            {
                try
                {
                    if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
                    {
                        LoggingService.Instance.LogInfo("停止执行加工工艺"); // 这里可以直接 await，而不是 Task.Run 
                        if(await Task.Run(() => StopBufferRUN()== ProcessError.OK.ToString()))
                        {
                            request.Completion.SetResult(true); // ✔ 通知发布者成功
                        }
                        else
                        {
                            request.Completion.SetResult(false); // ❌ 通知发布者失败 
                        }
                    }
                    

                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("StopBufferRUN error: " + ex.Message);
                    request.Completion.SetResult(false); // ❌ 通知发布者失败 
                }
            }, ThreadOption.BackgroundThread);






            eventAggregator.GetEvent<Cmd_ContinueProcessEvent>().Subscribe(() =>
            {
                LoggingService.Instance.LogInfo($"继续执行加工工艺");
                ContinueBufferRUN();
            }, ThreadOption.BackgroundThread);

            try
            {
                Acs = new Api();
                Axes_Compensation = AxesCompensation.Instance;
                AxesCompensation.GenerateCompensationFile();
            }
            catch (Exception ex)
            {
                Acs = null;
            }
        }


      
        public string Disable(AxisDefination axis)
        {
            try
            {
                Acs.Disable(GetAxis(axis.NodeNum));
                return null;
            }
            catch (ACSException ex)
            {
                //PinningInfo.Show($"使能切换失败\n{ex.Message}");
                //return false;
                return $"使能切换失败\n{ex.Message}";
            }
        }

        public string Enable(AxisDefination axis)
        {
            try
            {
                Acs.Enable(GetAxis(axis.NodeNum));
                return null;
            }
            catch (ACSException ex)
            {
                //PinningInfo.Show($"使能切换失败\n{ex.Message}");
                //return false;
                return $"使能切换失败\n错误代码：{ex.ErrorCode}\n{ex.Message}";
            }
        }

        #region Buffer相关
        // Buffer号
        private const ProgramBuffer BUFFER = ProgramBuffer.ACSC_BUFFER_9;

        public async Task<string> RunBufferFile(string file, bool value)
        {
            try
            {
                ProgramStates pstate = Acs.GetProgramState(BUFFER);
                if ((pstate & ProgramStates.ACSC_PST_RUN) == ProgramStates.ACSC_PST_RUN)
                {
                    LoggingService.Instance.LogError("Buffer运行失败,Buffer正在运行！");
                    return "Buffer正在运行";
                }
                if (value)
                {
                    IsListen = true;
                    Task.Run(()=> AcceptBufferMeg());
                    LoggingService.Instance.LogInfo("开始听");
                    if (!file.ToLower().Contains("home"))
                    {
                        GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Processing;
                    }
                    eventAggregator.GetEvent<StartProcessEvent>().Publish();
                }

                var f = new StreamReader(File.OpenRead(file));
                string buffer = f.ReadToEnd();
                Acs.ClearBuffer(BUFFER, 0, 1000);
                Acs.LoadBuffer(BUFFER, buffer);
                Acs.CompileBuffer(BUFFER);
                if(!value)
                {
                    Acs.RunBuffer(BUFFER, null);// home 
                }
                else
                {
                    Acs.RunBufferAsync(BUFFER, null);// Execute
                }
                   
                return null;
            }
            catch (ACSException ex)
            {
                //   PinningInfo.Show($"Buffer运行失败\n{ex.Message}"); return null;
                if (value)
                {
                    IsListen = false;
                    GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Finished;
                    eventAggregator.GetEvent<FinishedProcessEvent>().Publish(false);
                }
                
                LoggingService.Instance.LogError("Buffer运行失败！", ex);
                return $"Buffer运行失败\n{ex.Message}";                
            }
        }
        /// <summary>
        /// 等待Buufer运行完，
        /// </summary>
        /// <param name="WaitTime">最大等待时间（秒）</param>
        /// <returns>错误信息</returns>
        public async Task<string> WaitBufferFinished(bool isProgress, int WaitTime = 90)
        {
            try
            {
                //GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Processing;
                while (true)
                {
                    ProgramStates pstate = Acs.GetProgramState(BUFFER);
                    if ((pstate & ProgramStates.ACSC_PST_RUN) == ProgramStates.ACSC_PST_RUN)
                    {
                        await Task.Delay(500); // 减少 CPU 占用，可调整间隔
                        continue;
                    }
                    if (isProgress)
                    {
                        GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Finished;
                        eventAggregator.GetEvent<FinishedProcessEvent>().Publish(true);
                    }                    
                    break;
                }
                return ProcessError.OK.ToString();
            }
            catch (ACSException ex)
            {
                GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Finished;
                eventAggregator.GetEvent<FinishedProcessEvent>().Publish(false);
                return $"等待Buffer运行失败\n{ex.Message}";
            }
            finally
            {
                //stopwatch.Stop(); // 停止计时器
                Acs.CloseMessageBuffer();
                IsListen = false;
            }
        }

        public string WaitHomeFinished(int WaitTime = 1000)
        {
            Stopwatch stopwatch = Stopwatch.StartNew(); // 启动精确计时器
            try
            {
                while (true)
                {
                    // 检查超时（使用 Stopwatch 代替循环计数）
                    if (stopwatch.Elapsed.TotalSeconds > WaitTime)
                        return "Buffer运行超时";

                    ProgramStates pstate = Acs.GetProgramState(BUFFER);
                    if ((pstate & ProgramStates.ACSC_PST_RUN) == ProgramStates.ACSC_PST_RUN)
                    {
                        //Thread.Sleep(10); // 减少 CPU 占用，可调整间隔
                        continue;
                    }
                    break;
                }
                return null;
            }
            catch (ACSException ex)
            {
                return $"等待Buffer运行失败\n{ex.Message}";
            }
        }

        /// <summary>
        /// 停止缓存区执行
        /// </summary>
        /// <returns></returns>
        public string StopBufferRUN()
        {

            //G_ProcessStatus temp = GlobalProcessStatus.ProcessStatus;  卡顿问题
            //try
            //{
            //    if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing ||
            //        GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Pause)
            //    {
            //        Acs.StopBuffer(BUFFER);
            //        Acs.CloseMessageBuffer();
            //        //Acs.KillAll();
            //    }
            //    GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Stopped;
            //    //eventAggregator.GetEvent<StopProcessEvent>().Publish();
            //    return null;
            //}
            //catch (Exception ex)
            //{
            //    Acs.KillAll();
            //    GlobalProcessStatus.ProcessStatus = temp;
            //    LoggingService.Instance.LogError("停止加工失败！", ex);
            //    LoggingService.Instance.LogInfo("停止加工失败，所有轴已急停！");
            //    return $"停止Buffer运行失败\n{ex.Message}";
            //}
            G_ProcessStatus oldState = GlobalProcessStatus.ProcessStatus;

            try
            {
                if (oldState == G_ProcessStatus.Processing ||
                    oldState == G_ProcessStatus.Pause)
                {
                    // Step1: StopBuffer 超时保护
                    bool stopped = TryStopBufferWithTimeout(2000);

                    // Step2: CloseMessageBuffer（不放在超时逻辑内）
                    try
                    {
                        Acs.CloseMessageBuffer();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"CloseMessageBuffer 失败：{ex.Message}");
                    }

                    // 如果 StopBuffer 超时，TryStopBuffer 已经 KillAll，这里不需要再次 KillAll
                    if (!stopped)
                    {
                        LoggingService.Instance.LogWarning("StopBuffer 未成功，KillAll 已执行");
                    }
                }
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Stopped;
                    // 更新 UI
                });
              
                //Application.Current.Dispatcher.Invoke(() =>
                //{
                //    eventAggregator.GetEvent<Cmd_StopProcessEvent>().Publish();
                //});

                return ProcessError.OK.ToString();
            }
            catch (Exception ex)
            {
                try { Acs.KillAll(); } catch { 
                
                }

                GlobalProcessStatus.ProcessStatus = oldState;

                LoggingService.Instance.LogError("停止加工失败！", ex);
                return $"停止Buffer运行失败\n{ex.Message}";
            }

        }

        bool TryStopBufferWithTimeout(int timeoutMs)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    Acs.StopBuffer(BUFFER);
                    return true;
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"StopBuffer 失败: {ex.Message}");
                    return false;
                }
            });

            // 超时处理
            if (!task.Wait(timeoutMs))
            {
                LoggingService.Instance.LogWarning($"StopBuffer 超时（>{timeoutMs} ms），执行 KillAll");
                try { Acs.KillAll(); } catch { }
                return false;
            }

            return task.Result;
        }

        /// <summary>
        /// 暂停buffer
        /// </summary>
        /// <returns></returns>
        public string SuspendBuffer()
        {
            try
            {
                if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Processing)
                    Acs.SuspendBuffer(BUFFER);
                    //Acs.KillAll();
                GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Pause;
                eventAggregator.GetEvent<PauseProcessEvent>().Publish();
                return null;
            }
            catch (Exception ex)
            {
                GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Pause;
                LoggingService.Instance.LogError("暂停加工失败！", ex);
                return $"暂停Buffer运行失败\n{ex.Message}";
            }
        }

        /// <summary>
        /// 继续执行缓存区
        /// </summary>
        /// <returns></returns>
        public string ContinueBufferRUN()
        {
            try
            {
                if (GlobalProcessStatus.ProcessStatus == G_ProcessStatus.Pause)
                    Acs.RunBuffer(BUFFER, null);
                GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Processing;
                eventAggregator.GetEvent<ContinueProcessEvent>().Publish();
                return null;
            }
            catch (Exception ex)
            {
                GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Pause;
                LoggingService.Instance.LogError("继续加工失败！", ex);
                return $"继续执行失败\n{ex.Message}";
            }
        }

        private async Task<string> AcceptBufferMeg()
        {
            try
            {
                string old_meg = "0";
                while (true)
                {
                    string meg = Acs.GetSingleMessage(3600000);

                    if (old_meg != meg)
                    {
                        old_meg = meg;
                        eventAggregator.GetEvent<ProgressMegevent>().Publish(meg);
                    }
                    await Task.Delay(500);
                    if (!IsListen) return null;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("进度更新失败！", ex);
                return ex.Message;
            }
        }
        #endregion
        /// <summary>
        /// 每次运动前配置参数, 加速度为速度十倍,加加速度为一百倍(经验值).
        /// </summary>
        /// <param name="v">运动速度</param>
        private void MotionConfigure(Axis node, double v, bool flag = true)
        {
            Acs.SetVelocity(node, v);              // 设置速度
            //Acs.SetAcceleration(node, v * (flag ? 10 : 5));       // 设置加速度
            //Acs.SetDeceleration(node, v * (flag ? 10 : 5));       // 减速度
            //Acs.SetJerk(node, v * (flag ? 100 : 50));            // 加加速度
            Acs.SetKillDeceleration(node, 10000); // 急停减速度
        }

        public void MotionConfigure(Axis node, double v, double acc, double dec)
        {
            Acs.SetVelocity(node, v);              // 设置速度
            Acs.SetAcceleration(node, acc);       // 设置加速度
            Acs.SetDeceleration(node, dec);       // 减速度
            Acs.SetJerk(node, 10000);            // 加加速度
            Acs.SetKillDeceleration(node, 10000 ); // 急停减速度
        }

        public async Task<string> Home(AxisDefination axis)
        {
            try
            {
                double Position = Math.Round(Acs.GetRPosition(GetAxis(axis.NodeNum)), 6);
                bool isForward = Position < 0 ? true : false;
                switch (axis.Name)  //对各轴的运动目标值进行补偿
                {
                    case "X":
                        compensation_X = Axes_Compensation.GetCompensation(axis.Name, 0, isForward); break;
                    case "Y":
                        compensation_Y = Axes_Compensation.GetCompensation(axis.Name, 0, isForward); break;
                    case "Z":
                        compensation_Z = Axes_Compensation.GetCompensation(axis.Name, 0, isForward); break;
                    case "A":
                        compensation_A = Axes_Compensation.GetCompensation(axis.Name, 0, isForward); break;
                    case "B":
                        compensation_B = Axes_Compensation.GetCompensation(axis.Name, 0, isForward); break;
                    case "C":
                        compensation_C = Axes_Compensation.GetCompensation(axis.Name, 0, isForward); break;
                    default: break;
                }
                await RunBufferFile(home_path + $"/Home{axis.Name}.txt", false);
                return WaitHomeFinished();
            }
            catch (ACSException ex)
            {
                //PinningInfo.ShowDialog($"{Name}轴回零失败:\n" + ex.Message);
                return $"{axis.Name}轴回零失败:\n{ex.Message}\n错误代码：{ex.ErrorCode}";
            }
        }

        public async Task<string> HomeAll()
        {
            try
            {
               string  runfileMsg= await RunBufferFile(HOME_FilePath, false);
               WaitBufferFinished(false);
                if(runfileMsg!=null)
                {
                    return runfileMsg;
                }
               return null;
            }
            catch (Exception ex)
            {
                return $"回零失败：{ex}";
            }
        }

        public string Stop(AxisDefination axis)
        {
            try
            {
                Acs.Kill(GetAxis(axis.NodeNum));
                return null;
            }
            catch (ACSException ex)
            {
                return $"刹车失败\n错误代码：{ex.ErrorCode}\n{ex.Message}";
            }
        }

        // 双Sigmoid函数，平滑处理脉冲信号
        static double DoubleSigmoid(double t, double t0, double t1, double k1, double k2)
        {
            // Sigmoid上升部分
            double sigmoidUp = 1 / (1 + Math.Exp(-k1 * (t - t0)));

            // Sigmoid下降部分
            double sigmoidDown = 1 / (1 + Math.Exp(-k2 * (t - t1)));

            // 返回平滑的S型函数值
            return 2 * (sigmoidUp - sigmoidDown) - 0.8d;
        }

        string IMachineHardware.MoveContinuous(AxisDefination[] axis, List<double[]> targets, bool inProgress, bool isAsync)
        {
            var machine_axes = axis.Select(x => GetAxis(x.NodeNum)).ToList();
            machine_axes.Insert(machine_axes.Count, Axis.ACSC_NONE);

            Task task = new(() =>
            {
                //double[] t = Enumerable.Range(0, targets.Count).Select(x => (double)x).ToArray();   // 构造时间轴
                //var dimensions = Enumerable.Range(0, targets[0].Length)
                //                           .Select(dim => targets.Select(x => x[dim]).ToArray())
                //                           .ToArray();
                //var splines = dimensions.Select(dim => CubicSpline.InterpolateNatural(t, dim)).ToArray();

                List<double> temp_previous_target = new();
                foreach (var ax in axis)
                {
                    temp_previous_target.Add(Acs.GetRPosition(GetAxis(ax.NodeNum)));
                    MotionConfigure(GetAxis(ax.NodeNum), ax.Speed, false);
                }
                double[] time_coefficient =
                    Enumerable.Range(0, targets.Count)
                        .Select(x => DoubleSigmoid(x, 0, targets.Count - 1, 0.001, 0.001))
                        .Select(x => 1 / x)
                        .ToArray(); ;
                //Acs.MultiPointM(MotionFlags.ACSC_NONE, machine_axes.ToArray(), 0);
                //Acs.MultiPointM(MotionFlags.ACSC_AMF_VELOCITY, machine_axes.ToArray(), 0);
                //Acs.SplineM(MotionFlags.ACSC_NONE, machine_axes.ToArray(), 30);
                //Acs.SplineM(MotionFlags.ACSC_AMF_VARTIME, machine_axes.ToArray(), 20);  // 可变时间的插值运动
                //Acs.SplineM(MotionFlags.ACSC_AMF_CUBIC | MotionFlags.ACSC_AMF_VARTIME, machine_axes.ToArray(), 0);
                //Acs.SplineM(MotionFlags.ACSC_AMF_CUBIC, machine_axes.ToArray(), 20);
                Acs.ExtendedSegmentedMotionExt(MotionFlags.ACSC_NONE, machine_axes.ToArray(), targets.First(), 0, 0, 0, 0, 0, 0, 0, 0, 0, null);
                targets.RemoveAt(0);    // 第一个点是当前坐标，好像没啥用

                double[] previous_target = temp_previous_target.ToArray();
                foreach (var i in Enumerable.Range(0, targets.Count))
                {
                    var target = targets[i];
                    if (Math.Sqrt(previous_target.Zip(target, (a, b) => Math.Pow(a - b, 2)).Sum()) > 1)
                    {
                        Acs.EndSequenceM(machine_axes.ToArray());
                        throw new Exception("插补错误");
                    }
                    double time_interval = 20;
                    double temp_time = previous_target.Zip(target, (a, b) => Math.Abs(a - b)).Zip(axis, (distance, ax) => distance / ax.Speed).Max() * 1000; // 找出最久运动时间
                    if (previous_target != null)
                        time_interval = temp_time;
                    time_interval *= time_coefficient[i];

                    // 计算斜率
                    //var slopes = new double[axis.Length];
                    //if (i + 1 < targets.Count) // 最后一个点速度为0
                    //    slopes = target.Zip(previous_target, (tar, pre) => tar - pre).Zip(axis, (movement, ax) => movement / time_interval).ToArray();
                    //if (slopes.Sum() < 10)
                    //    slopes = Enumerable.Repeat(10d, axis.Length).ToArray();

                    bool writed = false;
                    while (!writed)
                    {
                        try
                        {
                            //Acs.AddPointM(machine_axes.ToArray(), target);
                            //Acs.ExtAddPointM(machine_axes.ToArray(), target, time_interval);    // 可变时间的插值运动
                            //Acs.AddPVPointM(machine_axes.ToArray(), target, target);
                            //Acs.AddPVTPointM(machine_axes.ToArray(), target, slopes, time_interval);
                            Acs.Line(machine_axes.ToArray(), target);
                            writed = true;
                        }
                        catch
                        {
                            Thread.Sleep(20);
                        }
                    }
                    previous_target = target;

                }
                Acs.EndSequenceM(machine_axes.ToArray());   // 结束运动
            });
            task.Start();
            if (!isAsync)
                task.Wait();
            return null;
        }

        public string ExecuteMotion(bool isAbsolute, int axisNum, double target, double speed)
        {
            try
            {
                double Position = Acs.GetTargetPosition(GetAxis(axisNum));
                double LeftSoftLimit = (double)Acs.ReadVariable($"SLLIMIT{axisNum}");
                double RightSoftLimit = (double)Acs.ReadVariable($"SRLIMIT{axisNum}");

                double movement = target;
                if (!isAbsolute) movement = Position + target;

                if (movement > RightSoftLimit || movement < LeftSoftLimit)
                    return "超出软限位范围，运动取消";

                MotionConfigure(GetAxis(axisNum), speed);

                if (isAbsolute)
                    Acs.ToPointAsync(MotionFlags.ACSC_NONE, GetAxis(axisNum), target);
                else
                    Acs.ToPointAsync(MotionFlags.ACSC_AMF_RELATIVE, GetAxis(axisNum), target);

                if (Math.Abs(Acs.GetFPosition(GetAxis(axisNum)) - Acs.GetRPosition(GetAxis(axisNum))) > 2e-3)
                    return $"{axisNum}轴位置异常";

                return null;
            }
            catch (ACSException ex)
            {
                return $"运动失败\n错误代码：{ex.ErrorCode}\n{ex.Message}";
            }
        }

        public string WaitMotionEnd(AxisDefination[] axis)
        {
            try
            {
                if (axis.Length == 1)
                    Acs.WaitMotionEnd(GetAxis(axis[0].NodeNum), 100_000);
                // 等待所有轴停止
                else
                {
                    for (int i = 0; i < axis.Length; i++)
                        Acs.WaitMotionEnd(GetAxis(axis[i].NodeNum), 100_000);
                }                
                return null;
            }
            catch (ACSException ex)
            {
                return ex.Message;
            }
        }

        double? compensation_X = default;
        double? compensation_Y = default;
        double? compensation_Z = default;
        double? compensation_A = default;
        double? compensation_B = default;
        double? compensation_C = default;
        public string ToPoint(AxisDefination axis, double target, bool isAbsolute = true, bool isAsync = false)
        {  //绝对运动传进来的target是运动量，相对运动传进来的target是框里的值
            try
            {
                // 获取状态
                double Position = Math.Round(Acs.GetRPosition(GetAxis(axis.NodeNum)), 6);

                switch (axis.Name)  //下一次移动之前先减掉补偿值认为和显示值相同
                {
                    case "X": if (compensation_X.HasValue) Position -= compensation_X.Value; break;
                    case "Y": if (compensation_Y.HasValue) Position -= compensation_Y.Value; break;
                    case "Z": if (compensation_Z.HasValue) Position -= compensation_Z.Value; break;
                    case "A": if (compensation_A.HasValue) Position -= compensation_A.Value; break;
                    case "B": if (compensation_B.HasValue) Position -= compensation_B.Value; break;
                    case "C": if (compensation_C.HasValue) Position -= compensation_C.Value; break;
                    default: break;
                }

                double LeftSoftLimit = (double)Acs.ReadVariable($"SLLIMIT{axis.NodeNum}");
                double RightSoftLimit = (double)Acs.ReadVariable($"SRLIMIT{axis.NodeNum}");
                if (axis.MinSoftLimit != null)
                    LeftSoftLimit = Math.Max(LeftSoftLimit, axis.MinSoftLimit.Value);
                if (axis.MaxSoftLimit != null)
                    RightSoftLimit = Math.Min(RightSoftLimit, axis.MaxSoftLimit.Value);
                // 转绝对坐标
                double movement = target;
                if (!isAbsolute) movement = Math.Round((Position + target), 6); //movement是机床最绝对的运动目标
                // 检查软限位
                if (movement > RightSoftLimit || movement < LeftSoftLimit)
                    return $"超出软限位范围，运动取消\n运动目标:{movement}\n软限位:（{LeftSoftLimit}，{RightSoftLimit}）";

                bool isForward;
                if (movement > Position) isForward = true;
                else if (movement < Position) isForward = false;
                else return null;

                switch (axis.Name)  //对各轴的运动目标值进行补偿
                {
                    case "X":
                        compensation_X = Axes_Compensation.GetCompensation(axis.Name, movement, isForward);
                        if (compensation_X.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis.Name}轴补偿前机床绝对运动目标:{movement}, 补偿后运动目标:{movement + compensation_X.Value}");
                            movement += compensation_X.Value;
                        }
                        break;
                    case "Y":
                        compensation_Y = Axes_Compensation.GetCompensation(axis.Name, movement, isForward);
                        if (compensation_Y.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis.Name}轴补偿前机床绝对运动目标:{movement}, 补偿后运动目标:{movement + compensation_Y.Value}");
                            movement += compensation_Y.Value;
                        }
                        break;
                    case "Z":
                        compensation_Z = Axes_Compensation.GetCompensation(axis.Name, movement, isForward);
                        if (compensation_Z.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis.Name}轴补偿前机床绝对运动目标:{movement}, 补偿后运动目标:{movement + compensation_Z.Value}");
                            movement += compensation_Z.Value;
                        }
                        break;
                    case "A":
                        compensation_A = Axes_Compensation.GetCompensation(axis.Name, movement, isForward);
                        if (compensation_A.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis.Name}轴补偿前机床绝对运动目标:{movement}, 补偿后运动目标:{movement + compensation_A.Value}");
                            movement += compensation_A.Value;
                        }
                        break;
                    case "B":
                        compensation_B = Axes_Compensation.GetCompensation(axis.Name, movement, isForward);
                        if (compensation_B.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis.Name}轴补偿前机床绝对运动目标:{movement}, 补偿后运动目标:{movement + compensation_B.Value}");
                            movement += compensation_B.Value;
                        }
                        break;
                    case "C":
                        compensation_C = Axes_Compensation.GetCompensation(axis.Name, movement, isForward);
                        if (compensation_C.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis.Name}轴补偿前机床绝对运动目标:{movement}, 补偿后运动目标:{movement + compensation_C.Value}");
                            movement += compensation_C.Value;
                        }
                        break;
                    default: break;
                }
                // 设置速度
                //MotionConfigure(GetAxis(axis.NodeNum), axis.Speed);
                MotionConfigure(GetAxis(axis.NodeNum), axis.Speed, axis.acceleratedSpeed, axis.decelerationSpeed);
                // 运动

                //if (isAbsolute)【走不到这里】
                //    Acs.ToPointAsync(MotionFlags.ACSC_NONE, GetAxis(axis.NodeNum), target);
                //else
                //    Acs.ToPointAsync(MotionFlags.ACSC_AMF_RELATIVE, GetAxis(axis.NodeNum), target);【相对运动形式】

                Acs.ToPointAsync(MotionFlags.ACSC_NONE, GetAxis(axis.NodeNum), movement); //【绝对运动形式】

                if (!isAsync)   // 同步需要等待
                    Acs.WaitMotionEnd(GetAxis(axis.NodeNum), 100);

                if (Math.Abs(Acs.GetFPosition(GetAxis(axis.NodeNum)) - Acs.GetRPosition(GetAxis(axis.NodeNum))) > 2e-3)
                    return $"{axis.Name}轴位置异常";

                return null;
            }
            catch (ACSException ex)
            {
                return $"运动失败\n错误代码：{ex.ErrorCode}\n{ex.Message}";
            }
        }

        public string ToPoint(AxisDefination[] axis, double[] target, bool inProgress, bool isAbsolute = true, bool isAsync = false)
        {
            if (axis.Length != target.Length)
                return "运动终止：轴数不匹配";
            try
            {
                // 所有轴开始运动
                for (int i = 0; i < axis.Length; i++)
                    ToPoint(axis[i], target[i], isAbsolute, isAsync: true);
                if (!isAsync)
                    WaitMotionEnd(axis);
                return null;
            }
            catch (ACSException e)
            {
                return e.Message;
            }
        }
        //public Tuple<bool, Exception> CheckLimit(AxisDefination axis, double target, bool isAbsolute = true)
        //{
        //    try
        //    {
        //        double Position = Acs.GetFPosition(GetAxis(axis.NodeNum));
        //        double LeftSoftLimit = (double)Acs.ReadVariable($"SLLIMIT{axis.NodeNum}");
        //        double RightSoftLimit = (double)Acs.ReadVariable($"SRLIMIT{axis.NodeNum}");
        //        // 转绝对坐标
        //        double movement = target;
        //        if (!isAbsolute) movement = Position + target;
        //        // 验证限位
        //        if (movement < RightSoftLimit && movement > LeftSoftLimit)
        //            return new(true, null);
        //        return new(false, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new(false, ex);
        //    }
        //}
        public Tuple<AxisStatusModel, Exception> Refresh(AxisDefination axis)
        {            AxisStatusModel data = new();
            try
            {
                // 位置
                data.Position = Math.Round(Acs.GetRPosition(GetAxis(axis.NodeNum)), 6);
                switch (axis.Name)  //各轴读数补偿（显示值）
                {
                    case "X": if (compensation_X.HasValue) data.Position -= compensation_X.Value; break;
                    case "Y": if (compensation_Y.HasValue) data.Position -= compensation_Y.Value; break;
                    case "Z": if (compensation_Z.HasValue) data.Position -= compensation_Z.Value; break;
                    case "A": if (compensation_A.HasValue) data.Position -= compensation_A.Value; break;
                    case "B": if (compensation_B.HasValue) data.Position -= compensation_B.Value; break;
                    case "C": if (compensation_C.HasValue) data.Position -= compensation_C.Value; break;
                    default: break;
                }
                data.PositionError = data.Position - Acs.GetFPosition(GetAxis(axis.NodeNum));

                // 使能
                MotorStates status = Acs.GetMotorState(GetAxis(axis.NodeNum));
                data.Enabled = (status & MotorStates.ACSC_MST_ENABLE) == MotorStates.ACSC_MST_ENABLE;
                data.IsMoving = (status & MotorStates.ACSC_MST_MOVE) == MotorStates.ACSC_MST_MOVE;

                // 限位
                var temp = Acs.GetFault(GetAxis(axis.NodeNum));
                data.LeftLimit = (temp & SLL) == SLL ? LimitStatus.SoftLimited : LimitStatus.Normal;
                data.LeftLimit = (temp & LL) == LL ? LimitStatus.Limited : data.LeftLimit;
                data.RightLimit = (temp & SRL) == SRL ? LimitStatus.SoftLimited : LimitStatus.Normal;
                data.RightLimit = (temp & RL) == RL ? LimitStatus.Limited : data.RightLimit;

                // 软限位
                double[] LeftLimits = (double[])Acs.ReadVariable("SLLIMIT");
                double[] RightLimits = (double[])Acs.ReadVariable("SRLIMIT");
                var LeftSoftLimit = LeftLimits[axis.NodeNum];
                var RightSoftLimit = RightLimits[axis.NodeNum];
                if (axis.MinSoftLimit != null)
                    LeftSoftLimit = Math.Max(LeftSoftLimit, axis.MinSoftLimit.Value);
                if (axis.MaxSoftLimit != null)
                    RightSoftLimit = Math.Min(RightSoftLimit, axis.MaxSoftLimit.Value);
                data.LeftSoftLimit = LeftSoftLimit;
                data.RightSoftLimit = RightSoftLimit;

                return new(data, null);
            }
            catch (Exception ex)
            {
                // 能测多少测多少，测不到的返回默认值
                return new(data, ex);
            }
        }

        public bool IsConnected(MachineConnectionInfo machine)
        {
            var info = Acs.GetConnectionInfo();
            if (info.EthernetIP.ToString() == machine.Ip)
                return true;
            else
                return false;
        }
        public string Connect(MachineConnectionInfo machine)
        {
            var a = ConnectAsync(machine);
            return a.Result;
        }

        public async Task<string> ConnectAsync(MachineConnectionInfo machine)
        {
            try
            {
                var result = await TaskWithTimeout<string>.StartNewTask(() =>
                {
                    try
                    {
#if isSimulate
                    Acs.OpenCommSimulator();
#else
                        try
                        {
                            Acs.OpenCommEthernetTCP(machine.Ip, machine.Port);
#endif
                        }
                        catch (Exception)
                        {

                        }
                        
                        var info = Acs.GetConnectionInfo();
                        if (info.EthernetIP.ToString() == machine.Ip)
                            return null;
                        else
                            return $"连接错误，当前设备IP为\n{info.EthernetIP}";
                    }
                    catch (ACSException ex)
                    {
                        return $"连接失败 ACS: \n错误代码：{ex.ErrorCode}\n{ex.Message}";
                    }
                }, 2000);
                return result;
            }
            catch (Exception e)
            {
                return "连接超时,可能是因为未找到驱动程序";
            }
        }

        public string Disconnect(MachineConnectionInfo machine)
        {
            var a = DisconnectAsync(machine);
            return a.Result;
        }
        public async Task<string> DisconnectAsync(MachineConnectionInfo machine)
        {
            try
            {
                //var result = await TaskWithTimeout<string>.StartNewTask(() =>
                //{
                try
                {
                    Acs.CloseComm();
                    Acs.UnregisterEmergencyStop();
                    return null;
                }
                catch (ACSException ex)
                {
                    return $"机床断联失败\n错误代码：{ex.ErrorCode}\n,{ex.Message}";
                }
                //}, 2000);
                //return result;
            }
            catch (Exception e)
            {
                return "断开超时,可能是因为未找到驱动程序";
            }
        }
        private readonly object _acsLock = new();
        public string StopAll()
        {
            //StopAllAsync();
            //return null;
            try
            {
                ProgramStates pstate = Acs.GetProgramState(BUFFER);
                if ((pstate & ProgramStates.ACSC_PST_RUN) == ProgramStates.ACSC_PST_RUN)
                {
                    Acs.StopBuffer(BUFFER);
                    LoggingService.Instance.LogInfo("停止缓存区运行");
                }
                Acs.KillAll();
                return null;
            }
            catch (ACSException ex)
            {
                return $"刹车失败\n错误代码：{ex.ErrorCode}\n{ex.Message}";
            }
            lock (_acsLock)
            {
                try
                {
                    ProgramStates pstate;

                    // 读取状态必须 try 包裹，ACS 有时读状态会阻塞
                    try
                    {
                        pstate = Acs.GetProgramState(BUFFER);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"GetProgramState 失败：{ex.Message}");
                        pstate = ProgramStates.ACSC_PST_SUSPEND; // 无法读取时按停止处理
                    }

                    // 尝试停止缓冲区（单独 try，避免卡住 KillAll）
                    if ((pstate & ProgramStates.ACSC_PST_RUN) == ProgramStates.ACSC_PST_RUN)
                    {
                        try
                        {
                            //bool stopped = TryStopBufferWithTimeout(2000);
                            Acs.StopBuffer(BUFFER);
                            // 如果 StopBuffer 超时，TryStopBuffer 已经 KillAll，这里不需要再次 KillAll
                            //if (!stopped)
                            //{
                            //    LoggingService.Instance.LogWarning("StopBuffer 未成功，KillAll 已执行");
                            //}
                            LoggingService.Instance.LogInfo("停止缓存区运行");
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogWarning($"StopBuffer 失败：{ex.Message}");
                        }
                    }

                    // KillAll 做兜底，不管 StopBuffer 成功与否都执行
                    try
                    {
                        Acs.KillAll();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"KillAll 失败：{ex.Message}");
                    }

                    return null;
                }
                catch (ACSException ex)
                {
                    return $"刹车失败\n错误代码：{ex.ErrorCode}\n{ex.Message}";
                }
            }
        }

        public async Task<string> StopAllAsync()
        {

            try
            {

                ProgramStates pstate = Acs.GetProgramState(BUFFER);
                if ((pstate & ProgramStates.ACSC_PST_RUN) == ProgramStates.ACSC_PST_RUN)
                {
                    bool stopped = await TryStopBufferWithTimeoutAsync(100);

                    try
                    {
                        await Task.Run(() => Acs.KillAll());
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"KillAll 失败: {ex.Message}");
                    }
                }

                //if (oldState == G_ProcessStatus.Processing || oldState == G_ProcessStatus.Pause)
                //{
                //    bool stopped = await TryStopBufferWithTimeoutAsync(2000);

                //    try
                //    {
                //        await Task.Run(() => Acs.KillAll());
                //    }
                //    catch (Exception ex)
                //    {
                //        LoggingService.Instance.LogWarning($"KillAll 失败: {ex.Message}");
                //    }
                //}

                GlobalProcessStatus.ProcessStatus = G_ProcessStatus.Stopped;
                return null;
            }
            catch (ACSException ex)
            {
                LoggingService.Instance.LogError(ex.ToString());
                return $"急停失败\n{ex.ErrorCode}\n{ex.Message}";
            }
        }
        private async Task<bool> TryStopBufferWithTimeoutAsync(int timeoutMs)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    Acs.StopBuffer(BUFFER);  // 同步调用，放后台线程
                    return true;
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"StopBuffer 失败: {ex.Message}");
                    return false;
                }
            });

            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed != task)
            {
                LoggingService.Instance.LogWarning($"StopBuffer 超时（>{timeoutMs}ms），执行 KillAll");
                await Task.Run(() => Acs.KillAll());
                return false;
            }

            return task.Result;
        }
        public string ReadIO(int port, int bit, out bool value)
        {
            value = false;
            try
            {
                value = Acs.GetOutput(port, bit) == 1;
                return null;
            }
            catch (ACSException ex)
            {
                return ex.Message;
            }
        }

        public string WriteIO(int port, int bit, bool value)
        {
            try
            {
                Acs.SetOutput(port, bit, value ? 1 : 0);
                return null;
            }
            catch (ACSException ex)
            {
                return $"ex.Message\n错误代码：{ex.ErrorCode}";
            }
        }

        string IMachineHardware.StopWork()
        {
            try
            {
                Acs.StopBuffer(BUFFER);
                Acs.KillAll();
            }
            catch (Exception ex)
            {
                return $"机床停车失败\n{ex.Message}";
            }
            return null;
        }

        bool IMachineHardware.IsRtcpOn() => false;

        //private readonly ProgramBuffer bufNum = ProgramBuffer.ACSC_BUFFER_9;
        string IMachineHardware.Focus() => null;

        string IMachineHardware.PrepareForWork(List<(double[], bool)> work_points, double? DefaultRetractValue, bool need_focus, double feed, double focus_feed)
        {
            return null;
        }

        public string Jog(AxisDefination axis, double speed, bool? direction, bool isAsync = false)
        {
            var node = GetAxis(axis.NodeNum);
            try
            {
                if (direction == null || !direction.HasValue)
                {
                    Acs.Kill(node);
                    LoggingService.Instance.LogInfo("JogStop");
                    return null;
                }
                double safeThreshold = 1;
                speed = direction.Value ? speed : -speed;

                double LeftSoftLimit = (double)Acs.ReadVariable($"SLLIMIT{axis.NodeNum}");
                double RightSoftLimit = (double)Acs.ReadVariable($"SRLIMIT{axis.NodeNum}");
                if (axis.MinSoftLimit.HasValue)
                    LeftSoftLimit = Math.Max(LeftSoftLimit, axis.MinSoftLimit.Value);
                if (axis.MaxSoftLimit.HasValue)
                    RightSoftLimit = Math.Min(RightSoftLimit, axis.MaxSoftLimit.Value);

                double currentPos = Acs.GetRPosition(node);
                if ((speed > 0 && currentPos >= (RightSoftLimit - 2 * safeThreshold)) ||
               (speed < 0 && currentPos <= (LeftSoftLimit + 2 * safeThreshold)))
                    return $"Current Position {axis.Name}:{currentPos:F3} Reach To SoftLimit";

                MotionConfigure(node, Math.Abs(speed), flag: false);
                var waitBlock = Acs.JogAsync(MotionFlags.ACSC_AMF_VELOCITY, node, speed);

                // 在JogAsync后启动监控任务
                if (isAsync)
                {
                    Task.Run(() =>
                    {
                        LoggingService.Instance.LogInfo("Jog监控");

                        while (Acs.GetAxisState(node).HasFlag(AxisStates.ACSC_AST_MOVE))
                        {
                            double currentPos = Acs.GetRPosition(node);
                            if ((currentPos > (RightSoftLimit - safeThreshold) && speed > 0) || (currentPos < (LeftSoftLimit + safeThreshold)) && speed < 0)
                            {
                                Acs.Halt(node);
                                LoggingService.Instance.LogWarning($"点动触发软限位 {currentPos:F3}");
                                break;
                            }
                            Thread.Sleep(30); // 采样间隔
                        }
                        LoggingService.Instance.LogInfo("Jog监控结束");
                    });
                }
                return null;
            }
            catch (ACSException ex)
            {
                Acs.Halt(node);
                return $"Jog Failed\nError：{ex.ErrorCode}\n{ex.Message}";
            }
        }

        public async Task<string> StartRunningFile(string file)
        {
            //Task.Run(async () =>
            //{
            //    try
            //    {
            //if (File.Exists(file))
            //{
            //Acs.OpenMessageBuffer(1024);
            //Task.Delay(1500);
            //RunBufferFile(file, true);
            //return  await WaitBufferFinished(true);
            //Acs.RunBufferAsync
            //}
            //    }
            //    catch (Exception ex)
            //    {
            //        return ex.ToString();
            //    }
            //});            
            //return  ProcessError.None.ToString(); 

            //return await Task.Run(async () =>
            //{
            //    try
            //    {
            //        await Task.Delay(1500);
            //        RunBufferFile(file, true);
            //        return await WaitBufferFinished(true);
            //    }
            //    catch (Exception ex)
            //    {
            //        return ex.ToString();
            //    }
            //});
            try
            {
              
                if (!File.Exists(file))
                {
                    Acs.OpenMessageBuffer(1024);
                    Task.Delay(1500);
                    
                    RunBufferFile(file, true);
                    return await WaitBufferFinished(true);
                }
               
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return ProcessError.None.ToString();
        }


        public  Task StartRunSript(string sript)
        {

            return Task.Run(() =>
            {
                try
                {
                   
                    ExecuteBuffer(ProgramBuffer.ACSC_BUFFER_7,sript);
                }
                catch (Exception ex)
                {
                    throw;
                }
            });
        }
    


        public bool ExecuteBuffer(ProgramBuffer buffer,string script)
        {
          

            try
            {

                // 1. 清空 buffer
                Acs.ClearBuffer(buffer, 0, 1000);

                // 2. 将 script 按行写入 buffer
                // LoadBuffer 行为最稳定，自动处理回车、BOM、空行
                Acs.LoadBuffer(buffer, script);

                // 3. 编译脚本
                Acs.CompileBuffer(buffer);

                // 4. 检查编译状态
                ProgramStates state = Acs.GetProgramState(buffer);
                if ((state & ProgramStates.ACSC_PST_COMPILED) != ProgramStates.ACSC_PST_COMPILED)
                {
                    int errLine = Acs.GetLastError();
                    string errText = Acs.GetErrorString(errLine);
                    MessageBox.Show($"脚本编译失败（行 {errLine}）：{errText}");
                    return false;
                }

                // 5. 运行脚本
                Acs.RunBuffer(buffer, null);

                return true;
            }
            catch (ACSException ex)
            {
                int errLine = Acs.GetLastError();
                string errText = Acs.GetErrorString(errLine);
                MessageBox.Show($"执行失败：{errText}\n行号：{errLine}\n异常：{ex.Message}");
                return false;
            }
        }
        public void SetZero(AxisDefination axis)
        {
            // 当前只开放c轴
            if (axis.Name != "C") return;
            
            //Acs.SetFPosition(GetAxis(axis.NodeNum), Math.Round(Acs.GetRPosition(GetAxis(axis.NodeNum)), 6));
            Acs.SetFPosition(GetAxis(axis.NodeNum), 0);
            LoggingService.Instance.LogInfo("C轴置零");
        }

        //public string SaveConfig(string json_path = null)
        //{
        //    json_path ??= ConfigStore.StoreDir + "AcsConfig.json";

        //    try
        //    {
        //        // 写Json
        //        JObject json = new JObject
        //        {
        //            {nameof(Ip), Ip },
        //            {nameof(Port), Port },
        //        };
        //        JArray axes_data = new JArray();
        //        foreach (var axis in Axes)
        //        {
        //            axes_data.Add(new JObject()
        //            {
        //                {nameof(axis.Name), axis.Name },
        //                {nameof(axis.NodeNum), axis.NodeNum },
        //                {nameof(axis.Speed), axis.Speed },
        //            });
        //        }
        //        json.Add(nameof(Axes), axes_data);

        //        // 保存
        //        ConfigStore.CheckStoreFloder();
        //        File.WriteAllText(json_path, json.ToString());

        //        return null;
        //    }
        //    catch (Exception ex) { return $"配置写入失败\n{ex.Message}"; }
        //}

        //public string LoadConfig(string json_path = null)
        //{
        //    json_path ??= ConfigStore.StoreDir + "AcsConfig.json";
        //    try
        //    {
        //        string str = File.ReadAllText(json_path);
        //        JObject json = JObject.Parse(str);

        //        Disconnect();
        //        Axes = new();
        //        Axes.Clear();   // 清空现有配置
        //        Ip = (string)json[nameof(Ip)];
        //        Port = (uint)json[nameof(Port)];
        //        foreach (var axis in json[nameof(Axes)])
        //        {
        //            Axes.Add(new AxisHardwareAcs(this)
        //            {
        //                Name = (string)axis["Name"],
        //                NodeNum = (int)axis["NodeNum"],
        //                Speed = (double)axis["Speed"],
        //            });
        //        }
        //        return null;
        //    }
        //    catch (Exception ex) { return $"配置读取失败\n{ex.Message}"; }

        //}
    }
}
