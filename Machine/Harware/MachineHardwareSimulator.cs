using ACS.SPiiPlusNET;
using Machine.Interfaces;
using Machine.Models;
using Newtonsoft.Json.Linq;
using OperationLogManager.libs;
using Prism.Events;
using SharedResource.events.eventMeg;
using SharedResource.events.Machine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Media3D;

namespace Machine.Harware
{
    internal class MachineHardwareSimulator : IMachineHardware
    {
        IEventAggregator _ea;
        AxesCompensation Axes_Compensation;
        #region 模拟器信息
        bool IsEnmergency = false;
        double[] Positions = new double[10];    // 先搞十个轴用着
        bool[] IsMoving = new bool[10];
        bool[] Enabled = new bool[10];
        bool Connected = false;
        #endregion
        public MachineHardwareSimulator(IEventAggregator eventAggregator)
        {
            _ea = eventAggregator;
            Axes_Compensation = AxesCompensation.Instance;
            AxesCompensation.GenerateCompensationFile();
        }
        public string Connect(MachineConnectionInfo machine)
        {
            Connected = true;
            IsEnmergency = false;
            return null;
        }
        public string Disconnect(MachineConnectionInfo machine)
        {
            Connected = false;
            return null;
        }

        public string Enable(AxisDefination axis)
        {
            Enabled[axis.NodeNum] = true;
            return null;
        }

        public string Disable(AxisDefination axis)
        {
            Enabled[axis.NodeNum] = false;
            return null;
        }


        public string Home(AxisDefination axis)
        {
            double Position = Math.Round(Positions[axis.NodeNum], 6);
            if (Position == 0) return null;
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
            ToPoint(axis, 0, isAbsolute: true, isAsync: true);
            return null;
        }

        public bool IsConnected(MachineConnectionInfo machine)
        {
            return Connected;
        }



        public string Stop(AxisDefination axis)
        {
            IsEnmergency = true;
            return null;
        }

        public string StopAll()
        {
            IsEnmergency = true;
            Task.Run(async () =>
            {
                await Task.Delay(100);
                IsEnmergency = false;
                for (int i = 0; i < IsMoving.Length; i++)
                {
                    IsMoving[i] = false;
                }
            });
            return null;
        }
        //string IMachineHardware.MutilAxisMotion(string GCodeCommand)
        //{
        //    string err = null;

        //    bool isAbsolute;
        //    int axisNum;
        //    double target;
        //    double speed;

        //    var GCodeParse = Soft_CNC.ParseGCode(GCodeCommand);

        //    isAbsolute = GCodeParse.MotionMode == "G90" ? true : false;

        //    if (!GCodeParse.Item3.Any()) return "没有要运动的对象！";

        //    foreach (var (axisName, destination) in GCodeParse.Item3)
        //    {
        //        target = destination;
        //        axisNum = axisName.ToUpper() switch
        //        {
        //            "X" => 0,
        //            "Y" => 1,
        //            "Z" => 2,
        //            "A" => 3,
        //            "B" => 4,
        //            "C" => 5,
        //            _ => -1
        //        };
        //        speed = double.Parse(GCodeParse.Speed);

        //        double used_time = 0;
        //        double moveStep;

        //        if (isAbsolute) moveStep = target - Positions[axisNum];
        //        else moveStep = target;

        //        if (Math.Abs(moveStep) < speed / 10) used_time = Math.Sqrt(2 * Math.Abs(moveStep) / (5 * speed));
        //        else used_time = moveStep / speed + 10;

        //        _ea.GetEvent<SimulateEvent>().Publish(new(SimuEventEnum.MachineUsedTime, used_time));

        //        if (isAbsolute) Positions[axisNum] = target;
        //        else Positions[axisNum] += target;
        //    }
        //    return err;
        //}
        //string IMachineHardware.SingleAxisMotion(string GCodeCommand)
        //{
        //    string err = null;

        //    bool isAbsolute;
        //    int axisNum;
        //    double speed;

        //    var GCodeParse = Soft_CNC.ParseGCode(GCodeCommand);

        //    isAbsolute = GCodeParse.MotionMode == "G90" ? true : false;
        //    var (axisName, target) = GCodeParse.Item3.FirstOrDefault();
        //    axisNum = axisName.ToUpper() switch
        //    {
        //        "X" => 0,
        //        "Y" => 1,
        //        "Z" => 2,
        //        "A" => 3,
        //        "B" => 4,
        //        "C" => 5,
        //        _ => -1
        //    };
        //    speed = double.Parse(GCodeParse.Speed);

        //    double used_time = 0;
        //    double moveStep;

        //    if (isAbsolute) moveStep = target - Positions[axisNum];
        //    else moveStep = target;

        //    if (Math.Abs(moveStep) < speed / 10) used_time = Math.Sqrt(2 * Math.Abs(moveStep) / (5 * speed));
        //    else used_time = moveStep / speed + 10;

        //    _ea.GetEvent<SimulateEvent>().Publish(new(SimuEventEnum.MachineUsedTime, used_time));

        //    if (isAbsolute) Positions[axisNum] = target;
        //    else Positions[axisNum] += target;

        //    return err;
        //}

        double? compensation_X = default;
        double? compensation_Y = default;
        double? compensation_Z = default;
        double? compensation_A = default;
        double? compensation_B = default;
        double? compensation_C = default;
        public string ToPoint(AxisDefination axis, double target, bool isAbsolute = true, bool isAsync = false)
        {
            int node = axis.NodeNum;
            ///TODO
            // 忽略软限位先
            //先忽略加加速度，先实现功能
            double move_length = target;
            if (isAbsolute) // 转相对运动，求运动量
                move_length -= Positions[node];

            double used_time = 0;
            if (Math.Abs(move_length) < axis.Speed / 10)   // 移动距离过短
                used_time = Math.Sqrt(2 * Math.Abs(move_length) / (5 * axis.Speed));
            else
                used_time = move_length / axis.Speed + 10;
            _ea.GetEvent<SimulateEvent>().Publish(new(SimuEventEnum.MachineUsedTime, used_time));

            double movement = target;
            double Position = Math.Round(Positions[node], 6);
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
            bool isForward;
            if (!isAbsolute) movement = Math.Round((target + Position), 6);

            if (movement > Position) isForward = true;
            else if (movement < Position) isForward = false;
            else return null;
            if ((axis.MaxSoftLimit.HasValue && movement > axis.MaxSoftLimit.Value) || (axis.MinSoftLimit.HasValue && movement < axis.MinSoftLimit.Value))
                return "超出软限位范围，运动取消";

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
            // 移动到点
            //Positions[node] = movement; //运动过程
            Task task = new(() =>
            {
                double journey = 0;
                while (Math.Abs(journey) < Math.Abs(move_length))   // 运动量少于要动的距离
                {
                    var step = ((move_length > 0 ? 1 : -1) * 0.01 * axis.Speed);
                    journey += step;
                    if (IsEnmergency)
                        return;
                    Positions[axis.NodeNum] += step;
                    IsMoving[axis.NodeNum] = true;
                    Thread.Sleep(10);
                }
                Positions[axis.NodeNum] = movement;
                IsMoving[axis.NodeNum] = false;
            });
            task.Start();
            if (!isAsync)
                task.Wait();

            return null;
        }
        // 添加字段记录点动状态
        private readonly Dictionary<AxisDefination, (CancellationTokenSource cts, Task task)> _jogTasks = new();
        private readonly object _jogLock = new();
        private double safeThreshold = 1;
        public string Jog(AxisDefination axis, double speed, bool? direction, bool isAsync = false)
        {
            lock (_jogLock)
            {
                // 停止现有运动
                if (direction == null || !direction.HasValue)
                {
                    if (_jogTasks.TryGetValue(axis, out var job))
                    {
                        LoggingService.Instance.LogInfo("Canceling jog task");
                        job.cts.Cancel();
                        _jogTasks.Remove(axis);
                        IsMoving[axis.NodeNum] = false;
                    }
                    return null;
                }
                speed = direction.Value ? speed : -speed;
                double currentPos = Positions[axis.NodeNum];
                if ((axis.MaxSoftLimit.HasValue && currentPos > (axis.MaxSoftLimit.Value - 2 * safeThreshold) && speed > 0) ||
                                (axis.MinSoftLimit.HasValue && currentPos < (axis.MinSoftLimit.Value + 2 * safeThreshold)) && speed < 0)
                    return "Axis is at soft limit";

                //// 检查是否已在运动中
                if (_jogTasks.ContainsKey(axis))
                {
                    LoggingService.Instance.LogWarning("Axis is already moving");
                    return "Axis is already moving";
                }

                // 启动新任务
                var cts = new CancellationTokenSource();
                var token = cts.Token;

                Task task = Task.Run(() =>
                {
                    try
                    {
                        LoggingService.Instance.LogInfo("Jogging");
                        double step = speed * 0.01; // 10ms步长
                        IsMoving[axis.NodeNum] = true;

                        while (!token.IsCancellationRequested)
                        {
                            // 软限位检查
                            double newPos = Positions[axis.NodeNum] + step;
                            if ((axis.MaxSoftLimit.HasValue && newPos > axis.MaxSoftLimit.Value - safeThreshold && speed > 0) ||
                                (axis.MinSoftLimit.HasValue && newPos < axis.MinSoftLimit.Value + safeThreshold) && speed < 0)
                            {
                                //HandleSoftLimit(axis);
                                break;
                            }

                            // 更新位置
                            Positions[axis.NodeNum] = newPos;

                            // 运动间隔
                            Thread.Sleep(10); // 10ms刷新周期

                            // 急停检查
                            if (IsEnmergency)
                                throw new OperationCanceledException("Emergency stop triggered");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LoggingService.Instance.LogWarning("Jog task canceled");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Jog task error", ex);
                    }
                    finally
                    {
                        //_jogTasks.Remove(axis);
                        IsMoving[axis.NodeNum] = false;
                    }
                }, token);

                _jogTasks.Add(axis, (cts, task));

                if (!isAsync)
                    task.Wait();

                return null;
            }
        }

        private void HandleSoftLimit(AxisDefination axis)
        {
            // 软限位处理策略
            const double backStep = 0.5; // 回退量(mm)
            double safePos = Positions[axis.NodeNum] +
                            (axis.MaxSoftLimit.HasValue && Positions[axis.NodeNum] > axis.MaxSoftLimit.Value ?
                             -backStep : backStep);

            // 回退到安全位置
            ToPoint(axis, safePos, isAbsolute: true);
            LoggingService.Instance.LogWarning($"{axis.Name}轴触发软限位保护");
        }
        public Tuple<AxisStatusModel, Exception> Refresh(AxisDefination axis)
        {
            AxisStatusModel data = new();
            int ax = axis.NodeNum;

            double minlimit = double.MinValue;
            double maxlimit = double.MaxValue;

            if (axis.MinSoftLimit != null)
                minlimit = Math.Max(minlimit, axis.MinSoftLimit.Value);
            if (axis.MaxSoftLimit != null)
                maxlimit = Math.Min(maxlimit, axis.MaxSoftLimit.Value);

            data.Position = Math.Round(Positions[ax], 6);
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
            data.Enabled = Enabled[ax];
            data.IsMoving = IsMoving[ax];
            data.LeftLimit = Enums.LimitStatus.Normal;
            data.LeftSoftLimit = minlimit;
            data.RightLimit = Enums.LimitStatus.Normal;
            data.RightSoftLimit = maxlimit;

            return new(data, null);
        }
        public string ToPoint(AxisDefination[] axis, double[] target, bool inProgress, bool isAbsolute = true, bool isAsync = false)
        {
            //Thread.Sleep(300);
            List<double> axis_times = new List<double>();
            double[] movement = new double[axis.Length];    // 相对运动
            double[] Position = new double[axis.Length];
            for (int i = 0; i < axis.Length; i++)
            {
                int node = axis[i].NodeNum;
                ///TODO
                // 忽略软限位先
                //先忽略加加速度，先实现功能
                //double move_length = target[i];
                movement[i] = target[i];
                Position[i] = Math.Round(Positions[node], 6);

                switch (axis[i].Name)  //下一次移动之前先减掉补偿值认为和显示值相同
                {
                    case "X": if (compensation_X.HasValue) Position[i] -= compensation_X.Value; break;
                    case "Y": if (compensation_Y.HasValue) Position[i] -= compensation_Y.Value; break;
                    case "Z": if (compensation_Z.HasValue) Position[i] -= compensation_Z.Value; break;
                    case "A": if (compensation_A.HasValue) Position[i] -= compensation_A.Value; break;
                    case "B": if (compensation_B.HasValue) Position[i] -= compensation_B.Value; break;
                    case "C": if (compensation_C.HasValue) Position[i] -= compensation_C.Value; break;
                    default: break;
                }
                bool isForward;

                if (!isAbsolute) movement[i] = Math.Round((Position[i] + target[i]), 6);

                if (movement[i] == Position[i])
                {
                    //double used_time_before_compensation = 0;
                    //if (Math.Abs(movement[i]) < axis[i].Speed / 10)   // 移动距离过短
                    //    used_time_before_compensation = Math.Sqrt(2 * Math.Abs(movement[i]) / (5 * axis[i].Speed));
                    //else
                    //    used_time_before_compensation = movement[i] / axis[i].Speed + 10;
                    //axis_times.Add(used_time_before_compensation);
                    continue;
                }
                else if (movement[i] > Position[i]) isForward = true;
                else isForward = false;

                switch (axis[i].Name)  //对各轴的运动目标值进行补偿
                {
                    case "X":
                        compensation_X = Axes_Compensation.GetCompensation(axis[i].Name, movement[i], isForward);
                        if (compensation_X.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis[i].Name}轴补偿前机床绝对运动目标:{movement[i]}, 补偿后运动目标:{movement[i] + compensation_X.Value}");
                            movement[i] += compensation_X.Value;
                        }
                        break;
                    case "Y":
                        compensation_Y = Axes_Compensation.GetCompensation(axis[i].Name, movement[i], isForward);
                        if (compensation_Y.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis[i].Name}轴补偿前机床绝对运动目标:{movement[i]}, 补偿后运动目标:{movement[i] + compensation_Y.Value}");
                            movement[i] += compensation_Y.Value;
                        }
                        break;
                    case "Z":
                        compensation_Z = Axes_Compensation.GetCompensation(axis[i].Name, movement[i], isForward);
                        if (compensation_Z.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis[i].Name}轴补偿前机床绝对运动目标:{movement[i]}, 补偿后运动目标:{movement[i] + compensation_Z.Value}");
                            movement[i] += compensation_Z.Value;
                        }
                        break;
                    case "A":
                        compensation_A = Axes_Compensation.GetCompensation(axis[i].Name, movement[i], isForward);
                        if (compensation_A.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis[i].Name}轴补偿前机床绝对运动目标:{movement[i]}, 补偿后运动目标:{movement[i] + compensation_A.Value}");
                            movement[i] += compensation_A.Value;
                        }
                        break;
                    case "B":
                        compensation_B = Axes_Compensation.GetCompensation(axis[i].Name, movement[i], isForward);
                        if (compensation_B.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis[i].Name}轴补偿前机床绝对运动目标:{movement[i]}, 补偿后运动目标:{movement[i] + compensation_B.Value}");
                            movement[i] += compensation_B.Value;
                        }
                        break;
                    case "C":
                        compensation_C = Axes_Compensation.GetCompensation(axis[i].Name, movement[i], isForward);
                        if (compensation_C.HasValue)
                        {
                            LoggingService.Instance.LogInfo($"{axis[i].Name}轴补偿前机床绝对运动目标:{movement[i]}, 补偿后运动目标:{movement[i] + compensation_C.Value}");
                            movement[i] += compensation_C.Value;
                        }
                        break;
                    default: break;
                }

                double used_time = 0;
                if (Math.Abs(movement[i]) < axis[i].Speed / 10)   // 移动距离过短
                    used_time = Math.Sqrt(2 * Math.Abs(movement[i]) / (5 * axis[i].Speed));
                else
                    used_time = movement[i] / axis[i].Speed + 10;
                axis_times.Add(used_time);

                //// 移动到点
                //if (isAbsolute)
                //    Positions[node] = target[i];
                //else
                //    Positions[node] += target[i];
            }
            Task task = new(() =>
            {
                for (int i = 0; i < axis.Length; i++)
                {
                    if (isAbsolute)
                        movement[i] = target[i] - Position[i];
                    else
                        movement[i] = target[i];
                }
                double[] journey = new double[axis.Length];
                while (journey.Zip(movement).Any(x => Math.Abs(x.First) < Math.Abs(x.Second)))   // 运动量少于要动的距离
                {
                    for (int i = 0; i < axis.Length; i++)
                    {
                        if (Math.Abs(journey[i]) >= Math.Abs(movement[i]))
                            continue;
                        int node = axis[i].NodeNum;
                        var step = ((movement[i] > 0 ? 1 : -1) * 0.01 * axis[i].Speed);
                        journey[i] += step;
                        if (IsEnmergency)
                            return;
                        Positions[node] += step;
                        IsMoving[node] = true;
                    }
                    Thread.Sleep(10);
                }

                // 移动到点，消除误差
                for (int i = 0; i < axis.Length; i++)
                {
                    int node = axis[i].NodeNum;
                    if (isAbsolute)
                        Positions[node] = target[i];
                    else
                        Positions[node] += -journey[i] + target[i];

                    IsMoving[node] = false;
                }
            });
            task.Start();
            if (!isAsync)
                task.Wait();
            if (axis_times.Any())
                _ea.GetEvent<SimulateEvent>().Publish(new(SimuEventEnum.MachineUsedTime, axis_times.Max()));
            return null;
        }

        //public Tuple<bool, Exception> CheckLimit(AxisDefination axis, double target, bool isAbsolute = true)
        //{
        //    return new(true, null);
        //}

        public string ReadIO(int port, int bit, out bool value)
        {
            value = false;
            return null;
        }

        public string WriteIO(int port, int bit, bool value)
        {
            return null;
        }

        public  string WaitMotionEnd(AxisDefination[] axis)
        {
            return null;
        }

        string IMachineHardware.PrepareForWork(List<(double[], bool)> work_points, double? DefaultRetractValue,
            bool need_focus, double feed, double focus_feed)
        {
            return null;
        }

        string IMachineHardware.StopWork()
        {
            return null;
        }
        string IMachineHardware.Focus()
        {
            return null;
        }

        bool IMachineHardware.IsRtcpOn()
        {
            return true;
        }

        // 双Sigmoid函数，平滑处理脉冲信号
        static double DoubleSigmoid(double t, double t0, double t1, double k1, double k2)
        {
            // Sigmoid上升部分
            double sigmoidUp = 1 / (1 + Math.Exp(-k1 * (t - t0)));

            // Sigmoid下降部分
            double sigmoidDown = 1 / (1 + Math.Exp(-k2 * (t - t1)));

            // 返回平滑的S型函数值
            return 2 * (sigmoidUp - sigmoidDown) - 0.99d;
        }
        // 平滑的tanh函数
        static double SmoothTanh(double t, double t0, double k)
        {
            // 使用tanh函数控制平滑度，范围从0到1
            return (Math.Tanh(k * (t - t0)) + 1) / 2;
        }
        string IMachineHardware.MoveContinuous(AxisDefination[] axis, List<double[]> targets, bool inProgress, bool isAsync)
        {
            Task task = new(() =>
            {
                //double[] origin_signal = Enumerable.Repeat(1d, targets.Count).ToArray();    // 制造一个脉冲信号
                //double[] time_coefficient = GaussianSmoothing(origin_signal, 10);
                double[] time_coefficient =
                    Enumerable.Range(0, targets.Count)
                        //.Select(x => DoubleSigmoid(x, 0, targets.Count - 1, 0.3, 0.3))
                        .Select(X => SmoothTanh(X, 0, 1))
                        //.Select(x => 1/x)
                        .ToArray(); ;

                double[] previous_target = targets.First().ToArray();
                foreach (var target in targets)
                {
                    if (Math.Sqrt(previous_target.Zip(target, (a, b) => Math.Pow(a - b, 2)).Sum()) > 0.1)
                    {
                        throw new Exception("插补错误");
                    }
                    LoggingService.Instance.LogInfo($"{target[0]}\t{target[1]}\t{target[2]}\t{target[3]}\t{target[4]}");
                    if (Math.Sqrt(previous_target.Zip(target, (a, b) => Math.Pow(a - b, 2)).Sum()) > 1)
                    {
                        throw new Exception("插补错误");
                    }
                    foreach ((var ax, var tar) in axis.Zip(target))
                    {
                        Positions[ax.NodeNum] = tar;
                        IsMoving[ax.NodeNum] = true;
                        if (IsEnmergency)
                            return;
                    }
                    Thread.Sleep(10);
                    previous_target = target;
                }
                foreach (var ax in axis)
                {
                    IsMoving[ax.NodeNum] = false;
                }
            });
            task.Start();
            if (!isAsync)
                task.Wait();
            return null;
        }

        public string RunBufferFile(string file)
        {
            throw new NotImplementedException();
        }

        public string WaitBufferFinished(int time)
        {
            throw new NotImplementedException();
        }

        public string StopBufferRUN()
        {
            throw new NotImplementedException();
        }

        public string SuspendBuffer()
        {
            throw new NotImplementedException();
        }

        public string ContinueBufferRUN()
        {
            throw new NotImplementedException();
        }

        public Task<string> RunBufferFile(string file, bool value)
        {
            throw new NotImplementedException();
        }

        Task<string> IMachineHardware.Home(AxisDefination axis)
        {
            throw new NotImplementedException();
        }

        public void MotionConfigure(Axis node, double v, double acc, double dec)
        {
            throw new NotImplementedException();
        }

        public Task<string> HomeAll()
        {
            throw new NotImplementedException();
        }

        public Task<string> StartRunningFile(string file)
        {
            throw new NotImplementedException();
        }

        public Task  StartRunSript(string sript)
        {
            throw new NotImplementedException();
        }

        public async Task<string> WaitBufferFinished(bool isProgress, int time)
        {
            throw new NotImplementedException();
        }

        public void SetZero(AxisDefination axis)
        {
            throw new NotImplementedException();
        }
    }
}
