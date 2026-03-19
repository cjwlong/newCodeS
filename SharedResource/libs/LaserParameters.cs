using Prism.Mvvm;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SharedResource.libs
{
    public class LaserParameters : BindableBase
    {
        private double _powerPercentage;
        [Range(0, 100, ErrorMessage = "功率百分比必须在0-100%之间")]
        public double PowerPercentage
        {
            get { return _powerPercentage; }
            set { SetProperty(ref _powerPercentage, value); }
        }

        private double _frequency;
        [Range(1, 1000, ErrorMessage = "频率必须在1-1000Hz之间")]
        public double Frequency
        {
            get { return _frequency; }
            set { SetProperty(ref _frequency, value); }
        }

        private double _divider;
        [Range(1, 16, ErrorMessage = "分份必须在1-16之间")]
        public double Divider
        {
            get { return _divider; }
            set { SetProperty(ref _divider, value); }
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
            return validationResults;
        }
    }
}