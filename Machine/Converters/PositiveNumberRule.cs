using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Machine.Converters
{
    public class PositiveNumberRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string str && double.TryParse(str, out double number))
            {
                if (number > 0)
                    return ValidationResult.ValidResult;
            }

            return new ValidationResult(false, "请输入大于 0 的数值");
        }
    }
}
