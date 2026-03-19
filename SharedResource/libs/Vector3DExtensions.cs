using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SharedResource.libs
{
    public static class Vector3DExtensions
    {
        public static Vector3D Cross(this Vector3D v1, Vector3D v2)
        {
            //return new Vector3D(
            //    v1.Y * v2.Z - v1.Z * v2.Y,
            //    v1.Z * v2.X - v1.X * v2.Z,
            //    v1.X * v2.Y - v1.Y * v2.X);
            return Vector3D.CrossProduct(v1, v2);
        }
        public static double Dot(this Vector3D v1, Vector3D v2)
        {
            //return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            return Vector3D.DotProduct(v1, v2);
        }
        /// <summary>
        /// 向量旋转
        /// </summary>
        /// <param name="v">要旋转的向量</param>
        /// <param name="angle">弧度</param>
        /// <param name="axis">旋转轴</param>
        /// <returns></returns>
        public static Vector3D Rotate(this Vector3D v, double angle, Vector3D axis)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double oneMinusCos = 1 - cos;

            axis = axis / axis.Length;  // 先归一化一下
            Matrix3D trans = new Matrix3D(
                cos + axis.X * axis.X * oneMinusCos, axis.X * axis.Y * oneMinusCos - axis.Z * sin, axis.X * axis.Z * oneMinusCos + axis.Y * sin, 0,
                axis.Y * axis.X * oneMinusCos + axis.Z * sin, cos + axis.Y * axis.Y * oneMinusCos, axis.Y * axis.Z * oneMinusCos - axis.X * sin, 0,
                axis.Z * axis.X * oneMinusCos - axis.Y * sin, axis.Z * axis.Y * oneMinusCos + axis.X * sin, cos + axis.Z * axis.Z * oneMinusCos, 0,
                0, 0, 0, 1);

            return v * trans;
        }
    }
}
