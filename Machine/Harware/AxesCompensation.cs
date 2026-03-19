using OperationLogManager.libs;
using Prism.Mvvm;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Machine.Harware
{
    public class AxesCompensation : BindableBase
    {
        static string filePath = $"{ConfigStore.StoreDir}/AxesCompensation.json";
        //static MachineStatusViewModel machineStatusViewModel;
        private bool _needCompensation = false;

        public bool NeedCompensation { get => _needCompensation; set => SetProperty(ref _needCompensation, value); }
        public static AxesCompensation Instance = new();
        // 轴补偿类
        private AxesCompensation()
        {
        }
        class AxisCompensation
        {
            public string AxisName { get; set; }
            public List<CompensationInfo> CompensationInfoList { get; set; }
        }

        // 补偿信息类
        class CompensationInfo
        {
            public double InterpolationPoint { get; set; }
            public double? PositiveError { get; set; }
            public double? NegativeError { get; set; }
        }
        public double? GetCompensation(string axisName, double target, bool direction)
        {
            try
            {
                if (!NeedCompensation) return null;
                if (!File.Exists(filePath)) return null;
                var jsonString = File.ReadAllText(filePath);
                var axesCompensation = JsonSerializer.Deserialize<List<AxisCompensation>>(jsonString);
                var axisCompensation = axesCompensation.FirstOrDefault(axis => axis.AxisName == axisName).CompensationInfoList;
                //会出现null异常(文件没有对应的轴时）
                string ForOrBackward = direction ? "正向" : "反向";

                var sameCompensation = axisCompensation.FirstOrDefault(comp => comp.InterpolationPoint == target); //找相同点

                if (sameCompensation != null)
                {
                    if (direction && sameCompensation.PositiveError.HasValue) //正向有值
                    {
                        LoggingService.Instance.LogInfo($"需运动补偿的轴:{axisName}\n运动目标:{target}\n运动方向:{ForOrBackward}\n运动补偿量:{sameCompensation.PositiveError.Value}");
                        return sameCompensation.PositiveError.Value;
                    }
                    else if (!direction && sameCompensation.NegativeError.HasValue) //负向有值
                    {
                        LoggingService.Instance.LogInfo($"需运动补偿的轴:{axisName}\n运动目标:{target}\n运动方向:{ForOrBackward}\n运动补偿量:{sameCompensation.NegativeError.Value}");
                        return sameCompensation.NegativeError.Value;
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo($"需运动补偿的轴:{axisName}\n运动目标:{target}\n运动方向:{ForOrBackward}\n运动补偿量:无" );
                        return null;
                    }
                }
                CompensationInfo lowerCompensation = new();
                CompensationInfo upperCompensation = new();
                // 找到target两侧的已知误差数据
                if (direction) //分方向查找，增加鲁棒性，可跳过邻近空值的点
                {
                    lowerCompensation = axisCompensation
                        .LastOrDefault(comp => comp.InterpolationPoint < target && comp.PositiveError.HasValue);
                    upperCompensation = axisCompensation
                        .FirstOrDefault(comp => comp.InterpolationPoint > target && comp.PositiveError.HasValue);
                }
                else
                {
                    lowerCompensation = axisCompensation
                        .LastOrDefault(comp => comp.InterpolationPoint < target && comp.NegativeError.HasValue);
                    upperCompensation = axisCompensation
                        .FirstOrDefault(comp => comp.InterpolationPoint > target && comp.NegativeError.HasValue);
                }
                if (lowerCompensation == null || upperCompensation == null)
                {
                    LoggingService.Instance.LogError("补偿异常", new ArgumentException($"找不到{axisName}轴目标值{target}两侧的补偿数据，请检查文件！"));
                    return null;  //两侧插值点没有误差数据
                }
                // 使用线性插值计算target对应的误差数据
                double error = LinearInterpolation(lowerCompensation, upperCompensation, target, direction);

                LoggingService.Instance.LogInfo($"需运动补偿的轴:{axisName}\n运动目标:{target}\n运动方向:{ForOrBackward}\n运动补偿量:{Math.Round(error, 6)}");

                return Math.Round(error, 6);
            }
            catch
            {
                LoggingService.Instance.LogError("补偿异常", new ArgumentException($"获取补偿值异常，请检查文件！" ));
                return null;
            }
        }
        private static double LinearInterpolation(CompensationInfo lower, CompensationInfo upper, double target, bool direction)
        {
            // 计算插值系数
            double t = (target - lower.InterpolationPoint) / (upper.InterpolationPoint - lower.InterpolationPoint);

            // 根据direction选择正向或负向误差
            double lowerError = direction ? lower.PositiveError.Value : lower.NegativeError.Value;
            double upperError = direction ? upper.PositiveError.Value : upper.NegativeError.Value;

            // 线性插值计算
            double interpolatedError = lowerError + t * (upperError - lowerError);

            return interpolatedError;
        }
        public static void GenerateCompensationFile()
        {
            if (File.Exists(filePath)) { return; }
            // 创建一个包含所有轴和补偿信息的列表
            var axesCompensation = new List<AxisCompensation>();

            // 定义轴的名称
            string[] axisNames = { "X", "Y", "Z", "A", "B", "C" };

            foreach (var axisName in axisNames)
            {
                // 创建一个包含补偿信息的列表
                var compensationInfoList = new List<CompensationInfo>();

                // 插值点从-100到100，以10为增量
                for (int interpolationPoint = -100; interpolationPoint <= 100; interpolationPoint += 10)
                {
                    // 创建一个新的补偿信息对象
                    var compensationInfo = new CompensationInfo
                    {
                        InterpolationPoint = interpolationPoint,
                        PositiveError = default,  // 默认正向运动误差为null
                        NegativeError = default   // 默认负向运动误差为null
                    };

                    // 将补偿信息对象添加到列表中
                    compensationInfoList.Add(compensationInfo);
                }

                // 创建一个新的轴补偿对象
                var axisCompensation = new AxisCompensation
                {
                    AxisName = axisName,
                    CompensationInfoList = compensationInfoList
                };

                // 将轴补偿对象添加到列表中
                axesCompensation.Add(axisCompensation);
            }

            // 将列表序列化为JSON字符串
            string jsonString = JsonSerializer.Serialize(axesCompensation, new JsonSerializerOptions { WriteIndented = true });

            // 将JSON字符串写入文件
            File.WriteAllText(filePath, jsonString);
        }
    }
}
