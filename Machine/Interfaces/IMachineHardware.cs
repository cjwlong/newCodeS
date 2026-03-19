using ACS.SPiiPlusNET;
using Machine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Machine.Interfaces
{
    public interface IMachineHardware
    {
        #region 轴方法
        /// <summary>
        /// 轴回零方法
        /// </summary>
        /// <returns>错误信息，无错误为空字符串</returns>
        Task<string> Home(AxisDefination axis);
        Task<string> HomeAll();
        string ToPoint(AxisDefination axis, double target, bool isAbsolute = true, bool isAsync = false); // 点对点运动
        public void MotionConfigure(Axis node, double v, double acc, double dec);
        void SetZero(AxisDefination axis);

        Task<string> RunBufferFile(string file, bool value);
        Task<string> WaitBufferFinished(bool isProgress, int time);
        string StopBufferRUN();
        string SuspendBuffer();
        string ContinueBufferRUN();

        //string Jog(AxisDefination axis, double speed, bool? direction, bool isAsync = false); // Jog运动
        //Tuple<bool, Exception> CheckLimit(AxisDefination axis, double target, bool isAbsolute = true);
        string Enable(AxisDefination axis);    // 上使能
        string Disable(AxisDefination axis);   // 断使能
        string Stop(AxisDefination axis);  // 刹车
        Tuple<AxisStatusModel, Exception> Refresh(AxisDefination axis);   // 从机床更新状态
        #endregion
        #region 机床方法
        string Connect(MachineConnectionInfo machine);
        string Disconnect(MachineConnectionInfo machine);
        bool IsConnected(MachineConnectionInfo machine);
        bool IsRtcpOn();
        string StopAll();
        string ToPoint(AxisDefination[] axis, double[] target, bool inProgress, bool isAbsolute = true, bool isAsync = false); // 点对点运动
        /// <summary>
        /// 连续运动</br>
        /// 此为NC机床专用方法，数控机床暂时忽略，实现时直接调用ToPoint去最后一个点即可
        /// </summary>
        /// <param name="axis">轴</param>
        /// <param name="target">目标点（绝对位置）</param>
        /// <param name="inProgress">是否处于加工状态</param>
        /// <param name="isAsync">是否异步</param>
        /// <returns></returns>
        string MoveContinuous(AxisDefination[] axis, List<double[]> targets, bool inProgress, bool isAsync = false);
        string Focus();
        string WaitMotionEnd(AxisDefination[] axis);

        
        /// <summary>
        /// 生成加工用的G代码，并上传至数控系统，部分机床才需要实现这一步
        /// </summary>
        /// <param name="work_points">加工点列表</param>
        /// <returns></returns>
        string PrepareForWork(List<(double[], bool)> work_points, double? DefaultRetractValue, bool need_focus, double feed, double focus_feed);
        /// <summary>
        /// 用于结束工作后的清理工作
        /// </summary>
        /// <returns></returns>
        string StopWork();
        #endregion

        #region IO控制
        string ReadIO(int port, int bit, out bool value);
        string WriteIO(int port, int bit, bool value);
        //Task<string> RunBufferFile(string file, bool value);
        #endregion
        //string SingleAxisMotion(string GCodeCommand);  //单轴运动传入G代码必须带G90或G91
        //string MutilAxisMotion(string GCodeCommand);


        Task<string> StartRunningFile(string file);

        Task StartRunSript(string sript);
    }
}
