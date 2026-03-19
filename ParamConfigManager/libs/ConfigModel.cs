using Prism.Mvvm;
using SharedResource.enums;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParamConfigManager.libs
{
    public class ConfigModel : BindableBase
    {
        private string _etchingName;
        [Required(ErrorMessage = "刻蚀名称不能为空")]
        public string EtchingName
        {
            get { return _etchingName; }
            set { SetProperty(ref _etchingName, value); }
        }

        private double _ballDiameter;
        [Range(0.1, 100, ErrorMessage = "球径尺寸必须在0.1-100mm之间")]
        public double BallDiameter
        {
            get { return _ballDiameter; }
            set { SetProperty(ref _ballDiameter, value); }
        }

        private Configfile_type _type = Configfile_type.none;
        public Configfile_type Type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        // 激光参数
        public LaserParameters LaserParameters { get; set; } = new LaserParameters();

        // 轴参数
        public AxesParameters AxesParameters { get; set; } = new AxesParameters();

        // 脚本参数
        public ScriptParameters ScriptParameters { get; set; } = new ScriptParameters();


        // 验证所有属性
        public Dictionary<string, string> Validate()
        {
            var validationResults = new Dictionary<string, string>();

            // 验证当前对象
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(this, context, results, true))
            {
                foreach (var result in results)
                {
                    validationResults[result.MemberNames.First()] = result.ErrorMessage;
                }
            }

            // 验证子对象
            MergeValidationResults(validationResults, LaserParameters.Validate());
            MergeValidationResults(validationResults, AxesParameters.Validate());
            MergeValidationResults(validationResults, ScriptParameters.Validate());

            return validationResults;
        }

        // 添加一个辅助方法来合并验证结果
        private void MergeValidationResults(Dictionary<string, string> target, Dictionary<string, string> source)
        {
            foreach (var item in source)
            {
                // 如果已有相同键的错误，保留第一个（或根据需要合并）
                if (!target.ContainsKey(item.Key))
                {
                    target.Add(item.Key, item.Value);
                }
            }
        }
    }
}
