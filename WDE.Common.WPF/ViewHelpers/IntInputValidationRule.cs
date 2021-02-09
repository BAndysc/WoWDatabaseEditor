using System;
using System.Globalization;
using System.Windows.Controls;

namespace WDE.Common.WPF.ViewHelpers
{
    public class IntInputValidationRule: ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            bool isValid = true;
            if (value is string)
            {
                var strVal = (string)value;
                var temp = 0;
                isValid = Int32.TryParse(strVal, out temp);
            }
            else
                isValid = false;
            return new ValidationResult(isValid, "Violated int only input rule for input field!");
        }
    }
}
