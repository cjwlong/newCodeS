using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    public enum MarkingPathType
    {
        Fill = 0,   // 填充
        Contour = 1,// 轮廓
    }
    public enum MarkingPointType
    {
        Line = 0,
        Jump = 1,
        Dot = 2,
    }
    public enum MarkingPolyType
    {
        MarkLine = 0,
        MarkDot = 1,
    }
    /// <summary>
    /// 激光标记的位置点
    /// </summary>
    public class MarkingPoint
    {
        public double[] Point;
        public MarkingPointType Type;
    }
    /// <summary>
    /// 一条线（可能由Dot组成）
    /// </summary>
    public class MarkingLine
    {
        public List<MarkingPoint> Positions = new();
        public MarkingPolyType Type;
    }
    /// <summary>
    /// 一组线，可以为轮廓或填充
    /// </summary>
    public class MarkingFace
    {
        public List<MarkingLine> Data = new();
        public MarkingPathType Type;
    }
}
