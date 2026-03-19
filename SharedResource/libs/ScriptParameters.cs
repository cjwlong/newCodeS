using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SharedResource.libs
{
    public class ScriptParameters : BindableBase
    {
        private int _a;
        [Range(0, 99, ErrorMessage = "轴号必须设置在0-99之间")]
        public int A
        {
            get { return _a; }
            set { SetProperty(ref _a, value); }
        }

        private int _b;
        [Range(0, 99, ErrorMessage = "轴号必须设置在0-99之间")]
        public int B
        {
            get { return _b; }
            set { SetProperty(ref _b, value); }
        }

        // 刻槽数
        private int _m_Number;
        [Range(0, int.MaxValue)]
        public int m_Number
        {
            get { return _m_Number; }
            set { SetProperty(ref _m_Number, value); ChangThem_c(); ChangThem_SlotWidth(); }
        }

        // 槽宽
        private double _m_SlotWidth;
        [Range(0, double.MaxValue)]
        public double m_SlotWidth
        {
            get { return _m_SlotWidth; }
            set { SetProperty(ref _m_SlotWidth, value); }
        }

        // 循环次数
        private double _m_Loop;
        [Range(0, double.MaxValue)]
        public double m_Loop
        {
            get { return _m_Loop; }
            set { SetProperty(ref _m_Loop, value); }
        }

        // 槽间隔
        //private double _cuttingInterval;
        //[Range(0, double.MaxValue)]
        //public double CuttingInterval
        //{
        //    get { return _cuttingInterval; }
        //    set { SetProperty(ref _cuttingInterval, value); }
        //}

        // 加工起始角度
        private double _m_StartForMachi;
        public double m_StartForMachi
        {
            get { return _m_StartForMachi; }
            set { SetProperty(ref _m_StartForMachi, value); }
        }

        // 加工终止角度
        private double _m_EndForMachi;
        public double m_EndForMachi
        {
            get { return _m_EndForMachi; }
            set { SetProperty(ref _m_EndForMachi, value); }
        }

        // 起始位置角度
        private double _m_StartForMove;
        public double m_StartForMove
        {
            get { return _m_StartForMove; }
            set { SetProperty(ref _m_StartForMove, value); ChangThem_RotationRangeOfBaxis(); }
        }

        // 终止位置角度
        private double _m_EndForMove;
        public double m_EndForMove
        {
            get { return _m_EndForMove; }
            set { SetProperty(ref _m_EndForMove, value); ChangThem_RotationRangeOfBaxis(); }
        }

        // 螺旋倾角
        private double _m_spiralDip;
        public double m_SpiralDip
        {
            get { return _m_spiralDip; }
            set { SetProperty(ref _m_spiralDip, value); ChangThem_RotationRangeOfBaxis(); }
        }

        // 间隔角度
        //private double _intervalAngle;
        //public double m_DistanceForAngle
        //{
        //    get { return _intervalAngle; }
        //    set { SetProperty(ref _intervalAngle, value);}
        //}

        // m_c
        private double _m_c;
        public double m_c
        {
            get { return _m_c; }
            set { SetProperty(ref _m_c, value); }
        }

        // b轴旋转范围
        private double _m_RotationRangeOfBaxis;
        public double m_RotationRangeOfBaxis
        {
            get { return _m_RotationRangeOfBaxis; }
            set { SetProperty(ref _m_RotationRangeOfBaxis, value); }
        }

        // 槽台比
        private double _m_SlotRatio;
        [Range(0.9, 1.1, ErrorMessage = "槽台比必须设置在0.9~1.1之间")]
        public double m_SlotRatio
        {
            get { return _m_SlotRatio; }
            set { SetProperty(ref _m_SlotRatio, value); ChangThem_SlotWidth(); }
        }

        public Dictionary<string, string> Validate()
        {
            var validationResults = new Dictionary<string, string>();
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(this, context, results, true))
            {
                foreach (var result in results)
                {
                    validationResults[result.MemberNames.First()] = result.ErrorMessage;
                }
            }

            // 自定义验证
            if (m_StartForMove - m_StartForMachi <= 0.05)
            {
                validationResults.Add("m_StartForMachi", "加工起始角度比起始位置角度至少大0.05");
            }
            if (m_EndForMove > m_StartForMachi - 0.05)
            {
                validationResults.Add("m_EndForMachi", "加工终止角度比起始位置角度至少小0.05");
            }
            return validationResults;
        }

        private void ChangThem_c()
        {
            if (m_Number != 0)
                m_c = 360 / m_Number;
        }

        private void ChangThem_RotationRangeOfBaxis()
        {
            if (m_SpiralDip == 0) return;

            double value = Math.Abs(m_StartForMove - m_EndForMove);
            if (value > 0)
            {
                // 将角度转换为弧度
                double radians = m_SpiralDip * Math.PI / 180.0;

                // 计算正切值
                double tanValue = Math.Tan(radians);

                // 避免除以零的情况
                if (Math.Abs(tanValue) < 1e-10)
                {
                    return;
                    //throw new ArgumentException("y值导致正切值为零，无法计算m");
                }

                m_RotationRangeOfBaxis = value / tanValue;
            }
        }

        private void ChangThem_SlotWidth()
        {
            if (m_Number != 0)
            {
                m_SlotWidth = 360 / m_Number * m_SlotRatio / (m_SlotRatio + 1);
                m_Loop = Math.Round(m_SlotWidth / (0.0687549 * 2), 0);
            }
        }
    }
}