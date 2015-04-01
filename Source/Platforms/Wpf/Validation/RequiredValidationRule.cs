using System;
using System.Globalization;
using System.Windows.Controls;

namespace Exceptionless.Validation {
    public class RequiredValidationRule : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (value == null)
                return new ValidationResult(false, "Value can not be null.");

            if (value is string && String.IsNullOrEmpty(value as string))
                return new ValidationResult(false, "Value can not be empty.");

            return new ValidationResult(true, null);
        }
    }
}