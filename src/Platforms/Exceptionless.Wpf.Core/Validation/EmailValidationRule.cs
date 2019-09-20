﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Exceptionless.Validation {
    public class EmailValidationRule : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (value == null)
                return new ValidationResult(false, "Email can not be null.");

            var email = value as string;
            if (email == null || email.Trim().Length == 0)
                return new ValidationResult(false, "Email can not be empty.");

            if (!Regex.IsMatch(email, @"^[a-zA-Z][\w\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\w\.-]*[a-zA-Z0-9]\.[a-zA-Z][a-zA-Z\.]"))
                return new ValidationResult(false, "Email is in an invalid format.");

            return new ValidationResult(true, null);
        }
    }
}