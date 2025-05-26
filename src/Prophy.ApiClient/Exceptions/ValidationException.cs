using System;
using System.Collections.Generic;
using System.Net;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Exception thrown when request validation fails.
    /// </summary>
    public class ValidationException : ProphyApiException
    {
        /// <summary>
        /// Gets the validation errors that occurred.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationErrors">The list of validation errors.</param>
        public ValidationException(string message, IEnumerable<string> validationErrors) 
            : base(message, "VALIDATION_ERROR")
        {
            ValidationErrors = new List<string>(validationErrors ?? throw new ArgumentNullException(nameof(validationErrors)));
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationErrors">The list of validation errors.</param>
        /// <param name="innerException">The inner exception.</param>
        public ValidationException(string message, IEnumerable<string> validationErrors, Exception innerException) 
            : base(message, "VALIDATION_ERROR", innerException)
        {
            ValidationErrors = new List<string>(validationErrors ?? throw new ArgumentNullException(nameof(validationErrors)));
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class with HTTP details.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationErrors">The list of validation errors.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public ValidationException(string message, IEnumerable<string> validationErrors, HttpStatusCode httpStatusCode, string? errorDetails = null, string? requestId = null) 
            : base(message, "VALIDATION_ERROR", httpStatusCode, errorDetails, requestId)
        {
            ValidationErrors = new List<string>(validationErrors ?? throw new ArgumentNullException(nameof(validationErrors)));
        }

        /// <summary>
        /// Returns a string representation of the exception with validation errors.
        /// </summary>
        /// <returns>A formatted string containing exception and validation details.</returns>
        public override string ToString()
        {
            var result = base.ToString();
            
            if (ValidationErrors.Count > 0)
            {
                result += "\nValidation Errors:";
                foreach (var error in ValidationErrors)
                {
                    result += $"\n  - {error}";
                }
            }
            
            return result;
        }
    }
} 