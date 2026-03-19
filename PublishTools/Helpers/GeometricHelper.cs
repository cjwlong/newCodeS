using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace PublishTools.Helpers
{
    public class GeometricHelper
    {
        ///<summary>
        ///用来解决一些几何的计算
        ///</summary>
        public static double SegmentLength = 0.01; // 设定能打标的最短线长为0.01mm

        public static List<MarkingPoint> Arc_Paint(Point3D core, double radius, double startAngle, double endAngle, Point3D normal)
        {
            var polyLine = new MarkingLine { Positions = new List<MarkingPoint>() };
            Point3D u, v;

            // 正交基计算
            CreateOrthogonalBasis(normal, out u, out v);

            double startAngleRad = startAngle / 180.0 * Math.PI;
            double endAngleRad = endAngle / 180.0 * Math.PI;
            double arcLength = Math.Abs(radius * (endAngleRad - startAngleRad));
            int numSegments = (int)Math.Abs(arcLength / SegmentLength - 1);
            double angleIncrement = (endAngleRad - startAngleRad) / numSegments;

            // 向量缩放
            Vector3D Multiplyer(Point3D a, double scalar) => new Vector3D(a.X * scalar, a.Y * scalar, a.Z * scalar);

            // 画线段
            double currentAngle = startAngleRad;
            Point3D? lastPoint = null;

            for (int i = 0; i < numSegments; i++)
            {
                // 计算当前线段的起点和终点
                Point3D startPoint = core + Multiplyer(u, Math.Cos(currentAngle) * radius) + Multiplyer(v, Math.Sin(currentAngle) * radius);
                currentAngle += angleIncrement;
                Point3D endPoint = core + Multiplyer(u, Math.Cos(currentAngle) * radius) + Multiplyer(v, Math.Sin(currentAngle) * radius);

                // 添加点到多段线
                if (lastPoint != null && lastPoint == endPoint)
                {
                    polyLine.Positions.Add(new MarkingPoint
                    {
                        Type = MarkingPointType.Line,
                        Point = new[] { endPoint.X, endPoint.Y, endPoint.Z }
                    });
                }
                else
                {
                    polyLine.Positions.Add(new MarkingPoint
                    {
                        Type = MarkingPointType.Jump,
                        Point = new[] { startPoint.X, startPoint.Y, startPoint.Z }
                    });
                }

                lastPoint = endPoint;
            }

            return polyLine.Positions;
        }

        // 正交基计算
        private static void CreateOrthogonalBasis(Point3D normal, out Point3D u, out Point3D v)
        {
            // 选择一个不与法向量平行的向量
            Point3D arbitraryVector = new Point3D(1, 0, 0); // 可以使用 (0, 1, 0) 或其他的非平行向量
            if (Math.Abs(Dot(normal, arbitraryVector)) > 0.99)
            {
                arbitraryVector = new Point3D(0, 1, 0);
            }

            // 计算正交基
            u = Normalized(Cross(normal, arbitraryVector));
            v = Normalized(Cross(normal, u));
        }

        // 归一化
        private static Point3D Normalized(Point3D a)
        {
            double length = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            return new Point3D(a.X / length, a.Y / length, a.Z / length);
        }

        // 点积
        private static double Dot(Point3D a, Point3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        // 叉积
        private static Point3D Cross(Point3D a, Point3D b)
        {
            return new Point3D(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public static List<MarkingPoint> Ellipse_Paint(Point3D focus1, Point3D focus2, Point3D normal, double shortAxisLongAxisRatio, double startRadian, double endRadian)
        {
            // 计算长轴和短轴长度
            double distanceBetweenFoci = (focus1 - focus2).Length;
            double c = distanceBetweenFoci / 2; // 焦距的一半
            double a = c / Math.Sqrt(1 - shortAxisLongAxisRatio * shortAxisLongAxisRatio); // 长轴
            double b = shortAxisLongAxisRatio * a; // 短轴

            // 计算椭圆圆弧的长度
            double CalculateEllipseArcLength(double a, double b)
            {
                int n = 100000;
                double totalLength = 0.0;
                double step = (endRadian - startRadian) / n; // 分割间隔
                for (int i = 0; i < n; i++)
                {
                    double t1 = i * step + startRadian;
                    double t2 = (i + 1) * step + startRadian;

                    // 使用 Simpson's 1/3 Rule 计算每个小区间的弧长
                    totalLength += (ArcLengthSegment(a, b, t1) + 4 * ArcLengthSegment(a, b, (t1 + t2) / 2) + ArcLengthSegment(a, b, t2)) * step / 6;
                }
                return totalLength;
            }

            double ArcLengthSegment(double a, double b, double t)
            {
                // 计算导数
                double dx = -a * Math.Sin(t);
                double dy = b * Math.Cos(t);
                return Math.Sqrt(dx * dx + dy * dy);
            }

            double arc_perimeter = CalculateEllipseArcLength(a / 2, b / 2);

            // 根据段长获取分割数量
            int segments = (int)(arc_perimeter / SegmentLength - 1);
            double step = (endRadian - startRadian) / segments; // 将参数范围划分为 segments 段

            // 计算椭圆中心
            Point3D center = new Point3D(
                (focus1.X + focus2.X) / 2,
                (focus1.Y + focus2.Y) / 2,
                (focus1.Z + focus2.Z) / 2);

            // 计算法向量的正交基
            Vector3D uv = Vector3D.CrossProduct((Vector3D)normal, new Vector3D(1, 0, 0));
            if (uv.Length < 1e-6) // 如果法向量与x轴平行，选择y轴
            {
                uv = Vector3D.CrossProduct((Vector3D)normal, new Vector3D(0, 1, 0));
            }
            uv.Normalize();

            Vector3D wv = Vector3D.CrossProduct((Vector3D)normal, uv); // 计算法向量的另一个正交基

            // 创建点集合
            MarkingLine poly_line = new() { Positions = new() };
            // 生成椭圆的点
            for (int i = 0; i <= segments; i++)
            {
                double t = i * step;
                double x = a * Math.Cos(t + startRadian);
                double y = b * Math.Sin(t + startRadian);

                // 转换为三维坐标
                Point3D point = center + (x * uv + y * wv);
                poly_line.Positions.Add(new MarkingPoint()
                {
                    Type = MarkingPointType.Line,
                    Point = new[] { point.X, point.Y, point.Z },
                });
            }

            return poly_line.Positions;
        }

        public static Point3D? OCStoWCS(Point3D point_lwpolyline, Point3D normal)
        {
            Vector3D normalOCS = (Vector3D)normal;
            normalOCS.Normalize();

            // 计算OCS的基向量（X轴和Y轴）
            Vector3D zOCS = normalOCS; // OCS的Z轴是法向量
            Vector3D xOCS = Vector3D.CrossProduct(zOCS, new Vector3D(0, 0, 1));

            // 如果OCS的法向量与Z轴平行，选择一个默认的X轴
            if (xOCS.Length < 1e-6)
            {
                xOCS = new Vector3D(1, 0, 0);
            }
            xOCS.Normalize();

            Vector3D yOCS = Vector3D.CrossProduct(zOCS, xOCS); // 计算Y轴方向
            yOCS.Normalize();

            // 计算WCS中的坐标
            double xWCS = point_lwpolyline.X * xOCS.X + point_lwpolyline.Y * yOCS.X;
            double yWCS = point_lwpolyline.X * xOCS.Y + point_lwpolyline.Y * yOCS.Y;
            double zWCS = point_lwpolyline.X * xOCS.Z + point_lwpolyline.Y * yOCS.Z; // 如果Z轴存在，这里可以调整

            return new Point3D(xWCS, yWCS, zWCS);
        }

        /// <summary>
        /// Slerp插值函数
        /// </summary>
        /// <param name="a">向量a</param>
        /// <param name="b">向量b</param>
        /// <param name="t">倾向</param>
        /// <returns>插值向量</returns>
        public static Vector3D Slerp(Vector3D a, Vector3D b, double t)
        {
            // 确保向量是单位向量（归一化）
            a.Normalize();
            b.Normalize();

            // 计算夹角
            double dotProduct = Vector3D.DotProduct(a, b);

            // 防止浮点误差导致的问题，限制 dotProduct 在 -1 到 1 之间
            dotProduct = Math.Max(-1.0, Math.Min(1.0, dotProduct));

            // 计算插值角度
            double theta = Math.Acos(dotProduct);

            // 处理特殊情况
            if (theta == 0) return a; // 如果夹角为0，直接返回a

            // 计算slerp插值
            double sinTheta = Math.Sin(theta);
            double weightA = Math.Sin((1 - t) * theta) / sinTheta;
            double weightB = Math.Sin(t * theta) / sinTheta;

            // 返回插值后的结果
            return weightA * a + weightB * b;
        }

        // 线性插值函数
        public static Point3D Lerp(Point3D start, Point3D end, double t)
        {
            return new Point3D(
                (1 - t) * start.X + t * end.X,
                (1 - t) * start.Y + t * end.Y,
                (1 - t) * start.Z + t * end.Z
            );
        }
    }
}
