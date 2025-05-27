using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Provides comprehensive input validation and sanitization for security purposes.
    /// </summary>
    public static class InputValidator
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AlphanumericRegex = new Regex(
            @"^[a-zA-Z0-9]+$",
            RegexOptions.Compiled);

        private static readonly Regex SafeStringRegex = new Regex(
            @"^[a-zA-Z0-9\s\-_.@]+$",
            RegexOptions.Compiled);

        private static readonly Regex OrganizationCodeRegex = new Regex(
            @"^[a-zA-Z0-9\-_]{2,50}$",
            RegexOptions.Compiled);

        private static readonly Regex ApiKeyRegex = new Regex(
            @"^[a-zA-Z0-9\-_]{20,128}$",
            RegexOptions.Compiled);

        private static readonly HashSet<string> DangerousPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "<script", "</script>", "javascript:", "vbscript:", "onload=", "onerror=",
            "onclick=", "onmouseover=", "onfocus=", "onblur=", "onchange=", "onsubmit=",
            "eval(", "expression(", "url(", "import(", "document.cookie", "document.write",
            "window.location", "document.location", "innerhtml", "outerhtml",
            "exec(", "system(", "shell_exec(", "passthru(", "file_get_contents(",
            "file_put_contents(", "fopen(", "fwrite(", "include(", "require(",
            "drop table", "delete from", "insert into", "update set", "union select",
            "select * from", "or 1=1", "and 1=1", "' or '", "\" or \"", "; --", "/*", "*/",
            "../", "..\\", "%2e%2e", "%2f", "%5c", "file://", "data:",
            "base64,", "charset=", "content-type:", "x-forwarded", "x-real-ip"
        };

        /// <summary>
        /// Validates and sanitizes an email address.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>A validation result containing the sanitized email and any errors.</returns>
        public static ValidationResult<string> ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ValidationResult<string>.Failure("Email address is required.");
            }

            var sanitized = SanitizeInput(email.Trim());
            
            if (sanitized.Length > 254) // RFC 5321 limit
            {
                return ValidationResult<string>.Failure("Email address is too long (maximum 254 characters).");
            }

            if (!EmailRegex.IsMatch(sanitized))
            {
                return ValidationResult<string>.Failure("Email address format is invalid.");
            }

            return ValidationResult<string>.Success(sanitized);
        }

        /// <summary>
        /// Validates an organization code.
        /// </summary>
        /// <param name="organizationCode">The organization code to validate.</param>
        /// <returns>A validation result containing the sanitized organization code and any errors.</returns>
        public static ValidationResult<string> ValidateOrganizationCode(string? organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
            {
                return ValidationResult<string>.Failure("Organization code is required.");
            }

            var sanitized = SanitizeInput(organizationCode.Trim());

            if (!OrganizationCodeRegex.IsMatch(sanitized))
            {
                return ValidationResult<string>.Failure("Organization code must be 2-50 characters and contain only letters, numbers, hyphens, and underscores.");
            }

            return ValidationResult<string>.Success(sanitized);
        }

        /// <summary>
        /// Validates an API key format.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>A validation result containing validation status and any errors.</returns>
        public static ValidationResult<string> ValidateApiKey(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ValidationResult<string>.Failure("API key is required.");
            }

            var sanitized = SanitizeInput(apiKey.Trim());

            if (!ApiKeyRegex.IsMatch(sanitized))
            {
                return ValidationResult<string>.Failure("API key format is invalid. Must be 20-128 characters containing only letters, numbers, hyphens, and underscores.");
            }

            return ValidationResult<string>.Success(sanitized);
        }

        /// <summary>
        /// Validates a URL for security and format.
        /// </summary>
        /// <param name="url">The URL to validate.</param>
        /// <param name="allowedSchemes">The allowed URL schemes (default: http, https).</param>
        /// <returns>A validation result containing the validated URL and any errors.</returns>
        public static ValidationResult<Uri> ValidateUrl(string? url, string[]? allowedSchemes = null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return ValidationResult<Uri>.Failure("URL is required.");
            }

            allowedSchemes ??= new[] { "http", "https" };

            var trimmed = url.Trim();

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return ValidationResult<Uri>.Failure("URL format is invalid.");
            }

            if (!allowedSchemes.Contains(uri.Scheme.ToLowerInvariant()))
            {
                return ValidationResult<Uri>.Failure($"URL scheme '{uri.Scheme}' is not allowed. Allowed schemes: {string.Join(", ", allowedSchemes)}");
            }

            // Check for suspicious patterns in URL (use original trimmed URL, not HTML encoded)
            if (ContainsDangerousPatterns(trimmed))
            {
                return ValidationResult<Uri>.Failure("URL contains potentially dangerous content.");
            }

            return ValidationResult<Uri>.Success(uri);
        }

        /// <summary>
        /// Validates a string for safe content (alphanumeric plus common safe characters).
        /// </summary>
        /// <param name="input">The input string to validate.</param>
        /// <param name="maxLength">The maximum allowed length (default: 1000).</param>
        /// <param name="allowEmpty">Whether to allow empty/null values (default: false).</param>
        /// <returns>A validation result containing the sanitized string and any errors.</returns>
        public static ValidationResult<string> ValidateSafeString(string? input, int maxLength = 1000, bool allowEmpty = false)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                if (allowEmpty)
                {
                    return ValidationResult<string>.Success(string.Empty);
                }
                return ValidationResult<string>.Failure("Input is required.");
            }

            var sanitized = SanitizeInput(input.Trim());

            if (sanitized.Length > maxLength)
            {
                return ValidationResult<string>.Failure($"Input is too long (maximum {maxLength} characters).");
            }

            if (!SafeStringRegex.IsMatch(sanitized))
            {
                return ValidationResult<string>.Failure("Input contains invalid characters. Only letters, numbers, spaces, hyphens, underscores, periods, and @ symbols are allowed.");
            }

            if (ContainsDangerousPatterns(sanitized))
            {
                return ValidationResult<string>.Failure("Input contains potentially dangerous content.");
            }

            return ValidationResult<string>.Success(sanitized);
        }

        /// <summary>
        /// Validates an alphanumeric string.
        /// </summary>
        /// <param name="input">The input string to validate.</param>
        /// <param name="minLength">The minimum required length (default: 1).</param>
        /// <param name="maxLength">The maximum allowed length (default: 100).</param>
        /// <returns>A validation result containing the sanitized string and any errors.</returns>
        public static ValidationResult<string> ValidateAlphanumeric(string? input, int minLength = 1, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult<string>.Failure("Input is required.");
            }

            var sanitized = SanitizeInput(input.Trim());

            if (sanitized.Length < minLength)
            {
                return ValidationResult<string>.Failure($"Input is too short (minimum {minLength} characters).");
            }

            if (sanitized.Length > maxLength)
            {
                return ValidationResult<string>.Failure($"Input is too long (maximum {maxLength} characters).");
            }

            if (!AlphanumericRegex.IsMatch(sanitized))
            {
                return ValidationResult<string>.Failure("Input must contain only letters and numbers.");
            }

            return ValidationResult<string>.Success(sanitized);
        }

        /// <summary>
        /// Validates a numeric value within specified bounds.
        /// </summary>
        /// <param name="value">The numeric value to validate.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>A validation result containing validation status and any errors.</returns>
        public static ValidationResult<int> ValidateNumericRange(int value, int min, int max)
        {
            if (value < min)
            {
                return ValidationResult<int>.Failure($"Value {value} is below the minimum allowed value of {min}.");
            }

            if (value > max)
            {
                return ValidationResult<int>.Failure($"Value {value} is above the maximum allowed value of {max}.");
            }

            return ValidationResult<int>.Success(value);
        }

        /// <summary>
        /// Sanitizes input by removing or encoding potentially dangerous characters.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <param name="htmlEncode">Whether to HTML encode the result (default: true).</param>
        /// <returns>The sanitized string.</returns>
        public static string SanitizeInput(string? input, bool htmlEncode = true)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Remove null characters and control characters (except tab, newline, carriage return)
            var chars = new List<char>();
            foreach (char c in input)
            {
                // Keep printable characters (32-126) and allowed whitespace characters
                if ((c >= 32 && c <= 126) || c == '\t' || c == '\n' || c == '\r')
                {
                    chars.Add(c);
                }
                // Skip control characters (0-31 except tab, newline, carriage return) and extended ASCII (127+)
            }
            var sanitized = new string(chars.ToArray());

            // HTML encode to prevent XSS (optional)
            if (htmlEncode)
            {
                sanitized = HttpUtility.HtmlEncode(sanitized);
            }

            // Trim whitespace
            sanitized = sanitized.Trim();

            return sanitized;
        }

        /// <summary>
        /// Checks if the input contains any dangerous patterns.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if dangerous patterns are found, false otherwise.</returns>
        public static bool ContainsDangerousPatterns(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            var lowerInput = input.ToLowerInvariant();
            return DangerousPatterns.Any(pattern => lowerInput.Contains(pattern));
        }

        /// <summary>
        /// Validates multiple inputs and returns a combined result.
        /// </summary>
        /// <param name="validations">The validation functions to execute.</param>
        /// <returns>A validation result indicating overall success or failure with all error messages.</returns>
        public static ValidationResult ValidateMultiple(params Func<ValidationResult>[] validations)
        {
            var errors = new List<string>();

            foreach (var validation in validations)
            {
                var result = validation();
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }

            return errors.Any() 
                ? ValidationResult.Failure(errors.ToArray()) 
                : ValidationResult.Success();
        }
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid { get; protected set; }

        /// <summary>
        /// Gets the validation error messages.
        /// </summary>
        public IReadOnlyList<string> Errors { get; protected set; } = new List<string>();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A successful validation result.</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        /// <param name="errors">The validation error messages.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = errors?.ToList() ?? new List<string>() 
            };
        }
    }

    /// <summary>
    /// Represents the result of a validation operation with a typed value.
    /// </summary>
    /// <typeparam name="T">The type of the validated value.</typeparam>
    public class ValidationResult<T> : ValidationResult
    {
        /// <summary>
        /// Gets the validated value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Creates a successful validation result with a value.
        /// </summary>
        /// <param name="value">The validated value.</param>
        /// <returns>A successful validation result.</returns>
        public static ValidationResult<T> Success(T value)
        {
            return new ValidationResult<T> 
            { 
                IsValid = true, 
                Value = value 
            };
        }

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        /// <param name="errors">The validation error messages.</param>
        /// <returns>A failed validation result.</returns>
        public static new ValidationResult<T> Failure(params string[] errors)
        {
            return new ValidationResult<T> 
            { 
                IsValid = false, 
                Errors = errors?.ToList() ?? new List<string>() 
            };
        }
    }
} 