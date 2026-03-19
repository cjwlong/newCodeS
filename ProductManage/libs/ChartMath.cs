using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IronPython.Modules._ast;

namespace ProductManage.libs
{
    public class ChartMath
    {
        public static List<double> MergeContinuousClosePoints(
            IList<double> data,
            double threshold)
        {
            var result = new List<double>();
            if (data == null || data.Count == 0)
                return result;

            double sum = data[0];
            int count = 1;

            for (int i = 1; i < data.Count; i++)
            {
                // 和前一个点比
                if(Math.Abs(data[i] - (sum / count)) <= threshold) //(Math.Abs(data[i] - data[i - 1]) <= threshold)
                {
                    sum += data[i];
                    count++;
                }
                else
                {
                    // 当前组结束 → 取均值
                    result.Add(sum / count);

                    // 开启新组
                    sum = data[i];
                    count = 1;
                }
            }

            // 最后一组
            result.Add(sum / count);

            return result;
        }

        public static List<PointXY> MergeCloseXYPoints(
    IList<PointXY> points,
    double yThreshold)
        {
            var result = new List<PointXY>();
            if (points == null || points.Count == 0)
                return result;

            double sumX = points[0].X;
            double sumY = points[0].Y;
            int count = 1;

            for (int i = 1; i < points.Count; i++)
            {
                // 用 Y 判断是否相近（也可换成距离）
                if (Math.Abs(points[i].Y - points[i - 1].Y) <= yThreshold)
                {
                    sumX += points[i].X;
                    sumY += points[i].Y;
                    count++;
                }
                else
                {
                    // 输出当前组的均值点
                    result.Add(new PointXY(
                        sumX / count,
                        sumY / count));

                    // 新开一组
                    sumX = points[i].X;
                    sumY = points[i].Y;
                    count = 1;
                }
            }

            // 最后一组
            result.Add(new PointXY(
                sumX / count,
                sumY / count));

            return result;
        }



        static Trend GetTrend(double prevY, double currY, double eps)
        {
            if (currY > prevY + eps) return Trend.Up;
            if (currY < prevY - eps) return Trend.Down;
            return Trend.Flat;
        }

        // 1️⃣ 原始采样点  使用
//        List<PointXY> rawPoints = GetRawPoints();

//        // 2️⃣ 合并相近点
//        var merged = MergeCloseXYPoints(
//            rawPoints,
//            yThreshold: 0.03); 0.03是判断同一个点的阈值

//        // 3️⃣ 找波峰 / 波谷
//        FindPeaksAndValleys(
//            merged,
//            epsTrend: 0.01,
//            peakMinY: 1.95,
//            valleyMinY: 1.5,
//            valleyMaxY: 2.0,
//            out var peaks,
//            out var valleys);

//// 4️⃣ 输出
//foreach (var p in peaks)
//    Console.WriteLine($"波峰: X={p.X:F3}, Y={p.Y:F3}");

//foreach (var v in valleys)
//    Console.WriteLine($"波谷: X={v.X:F3}, Y={v.Y:F3}");  使用完成

        //        yMergeThreshold = 0.02 ~ 0.05   // 合并用
        //epsTrend        = 0.01          // 趋势判断
        //peakMinY        = 1.95  波峰下线
        //valleyMinY      = 1.5  波谷在 (1.5 , 2.0)
        //valleyMaxY      = 2.0   

        public static void FindPeaksAndValleys(
     IList<PointXY> points,
     double epsTrend,
     double peakMinY,
     double valleyMinY,
     double valleyMaxY,
     out List<PointXY> peaks,
     out List<PointXY> valleys)
        {
            peaks = new List<PointXY>();
            valleys = new List<PointXY>();

            if (points == null || points.Count < 3)
                return;

            Trend lastTrend = Trend.Flat;
            int flatStart = -1;

            for (int i = 1; i < points.Count; i++)
            {
                Trend currTrend = GetTrend(points[i - 1].Y, points[i].Y, epsTrend);

                // 处理平坦区
                if (currTrend == Trend.Flat)
                {
                    if (flatStart < 0)
                        flatStart = i - 1;
                    continue;
                }

                // 上升 → 下降 = 波峰
                if (lastTrend == Trend.Up && currTrend == Trend.Down)
                {
                    int idx = flatStart >= 0 ? (flatStart + i - 1) / 2 : i - 1;
                    var p = points[idx];

                    // 数值约束（你给的条件）
                    if (p.Y >= peakMinY)
                        peaks.Add(p);
                }
                // 下降 → 上升 = 波谷
                else if (lastTrend == Trend.Down && currTrend == Trend.Up)
                {
                    int idx = flatStart >= 0 ? (flatStart + i - 1) / 2 : i - 1;
                    var v = points[idx];

                    // 数值区间约束（你给的条件）
                    if (v.Y > valleyMinY && v.Y < valleyMaxY)
                        valleys.Add(v);
                }

                flatStart = -1;
                lastTrend = currTrend;
            }
        }

        #region
        // 1️⃣ 合并相近点 最新
//        var merged = MergeCloseXYPoints(rawPoints, 0.03);

//        // 2️⃣ 找波峰 / 波谷
//        FindPeaksAndValleys(
//            merged,
//            epsTrend: 0.01,
//            peakMinY: 1.95,
//            valleyMinY: 1.5,
//            valleyMaxY: 2.0,
//            out var peaks,
//            out var valleys);

//        // 3️⃣ 计算槽深
//        var grooveDepths = CalculateGrooveDepths(peaks, valleys);

//// 4️⃣ 输出
//foreach (var g in grooveDepths)
//{
//    Console.WriteLine(
//        $"槽深点: X={g.X:F3}, Depth={g.Depth:F4}");
//}

        #endregion
        public static List<GrooveDepthPoint> CalculateGrooveDepths(
            IList<PointXY> peaks,
            IList<PointXY> valleys)
        {
            var result = new List<GrooveDepthPoint>();

            if (peaks == null || valleys == null)
                return result;

            if (peaks.Count < 2 || valleys.Count == 0)
                return result;

            // 假设 peaks / valleys 都是按 X 从小到大排好序的
            foreach (var valley in valleys)
            {
                PointXY? leftPeak = null;
                PointXY? rightPeak = null;

                // 找左边最近波峰
                for (int i = peaks.Count - 1; i >= 0; i--)
                {
                    if (peaks[i].X < valley.X)
                    {
                        leftPeak = peaks[i];
                        break;
                    }
                }

                // 找右边最近波峰
                for (int i = 0; i < peaks.Count; i++)
                {
                    if (peaks[i].X > valley.X)
                    {
                        rightPeak = peaks[i];
                        break;
                    }
                }

                // 左右波峰必须都存在
                if (leftPeak == null || rightPeak == null)
                    continue;

                double leftDepth = leftPeak.Value.Y - valley.Y;
                double rightDepth = rightPeak.Value.Y - valley.Y;

                double avgDepth = (leftDepth + rightDepth) / 2.0;

                result.Add(new GrooveDepthPoint(
                    valley.X,
                    avgDepth));
            }

            return result;
        }

    }

    public struct PointXY
    {
        public double X;
        public double Y;

        public PointXY(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    enum Trend
    {
        Up,
        Down,
        Flat
    }
    public struct GrooveDepthPoint
    {
        public double X;      // 波谷 X
        public double Depth;  // 槽深（Y方向）

        public GrooveDepthPoint(double x, double depth)
        {
            X = x;
            Depth = depth;
        }
    }
}
