using CCD.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CCD.libs
{
    internal class CameraPoint
    {
        private Point pixPoint;
        private Point camPoint;
        private Point macPoint;
        /// <summary>
        /// 像素坐标
        /// </summary>
        public Point PixPoint { get => pixPoint; private set => pixPoint = value; }
        /// <summary>
        /// 相机坐标系下的坐标
        /// </summary>
        public Point CamPoint { get => camPoint; private set => camPoint = value; }
        /// <summary>
        /// 机床坐标系下的坐标
        /// </summary>
        public Point MacPoint { get => macPoint; private set => macPoint = value; }
        public Point SetPixPoint
        {
            set
            {
                pixPoint = value;
                CamPoint = CoordinateHelper.Instance.ConvertToRealFromPix(value);
                MacPoint = CoordinateHelper.Instance.ConvertToAbsoluteFromReal(CamPoint);
            }
        }
        public Point SetMacPoint
        {
            set
            {
                macPoint = value;
                camPoint = CoordinateHelper.Instance.ConvertToReal(macPoint);
                pixPoint = CoordinateHelper.Instance.ConvertToPix(camPoint);
            }
        }
        public Point SetCamPoint { set => CamPoint = value; }
        public Point RefreshPix { set => pixPoint = value; }

        public void MachineMoved()
        {
            // 机床下的坐标不变，相机坐标系下的坐标改变
            CamPoint = CoordinateHelper.Instance.ConvertToReal(MacPoint);     // 计算相机系下的坐标
            pixPoint = CoordinateHelper.Instance.ConvertToPix(CamPoint);   // 计算像素坐标
        }
        /// <summary>
        /// 标定模式的移动，慎用
        /// </summary>
        /// <param name="vector"></param>
        public void MachineMovedInCalibration(Vector vector)
        {
            CamPoint += vector;
            pixPoint = CoordinateHelper.Instance.ConvertToPix(CamPoint);   // 计算像素坐标
        }
    }

    internal static class CameraPointExtensions
    {
        public static CameraPoint GetMidPointByMac(this CameraPoint p1, CameraPoint p2)
        {
            var vector = p1.MacPoint - p2.MacPoint;

            return new CameraPoint() { SetMacPoint = p2.MacPoint + (vector / 2) };
        }
    }
}
