using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Machine.Interfaces
{
    // 外部调用机床使用的接口
    public interface IMachine
    {
        DeviceStatus DeviceStatus { get; }
        bool? IsMoving { get; }
        bool RtcpOn { get; }
        bool Connect();
        bool Disconnect();

        void StopAll();
        /// <summary>
        /// 点到点运动
        /// </summary>
        bool ToPoint(double[] target, bool inPogress, bool isAbsolute = true, bool isAsync = true);
        string MutilAxisMotion(string GCodeCommand);
        bool ToVector(Point3D target, Vector3D normal, Vector3D direction_x, int tool, out double theta, bool inProgress, bool isAsync = true);
        string Focus();
        /// <summary>
        /// 运动到指定模型坐标位置
        /// </summary>
        /// <param name="target">目标工作点</param>
        /// <param name="t_z">工作点法向量（工作坐标系z轴方向）</param>
        /// <param name="t_x">工作坐标系x轴方向</param>
        /// <param name="theta"></param>
        /// <param name="retract_distance">退刀距离，默认不退刀</param>
        bool ToModelVector(Point3D target, Vector3D t_z, Vector3D t_x, out double theta, bool inProgress, double retract_distance = 0);
        double[] GetPosition();
        /// <summary>
        /// 获取当前刀尖位置
        /// </summary>
        /// <returns></returns>
        public double[] GetToolHeadPosition();
        double[] FromVector(Point3D target, Vector3D z_vector, Vector3D x_vector, int tool, out double theta);
        Tuple<Point3D, Vector3D, Vector3D> Model2Workbench(Point3D target, Vector3D t_z, Vector3D t_x);
        bool CheckLimit(double[] target, bool isAbsolute = true);
        /// <summary>
        /// 测试是否超出软限位
        /// </summary>
        /// <param name="target">目标点</param>
        /// <param name="normal">法向量</param>
        /// <returns>false: 超出软限位</returns>
        bool? CheckLimit(Point3D target, Vector3D normal, Vector3D direction_x);
        /// <summary>
        /// 生成加工用的G代码，并上传至数控系统，部分机床才需要实现这一步
        /// </summary>
        /// <param name="work_points">加工点列表</param>
        /// <returns></returns>
        string PrepareForWork(List<(Point3D, Vector3D, Vector3D, bool)> work_pointsList, bool need_focus);
        /// <summary>
        /// 用于工作运行完或终止运行时的操作
        /// </summary>
        /// <returns></returns>
        string StopWork();

        string Pause();
        string ReStart();
        //string GetGcode();
    }
}
