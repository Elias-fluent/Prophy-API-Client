using System;
using System.Net;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Exception thrown when API requests timeout.
    /// </summary>
    public class ApiTimeoutException : ProphyApiException
    {
        /// <summary>
        /// Gets the timeout duration that was exceeded.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Initializes a new instance of the ApiTimeoutException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="timeout">The timeout duration that was exceeded.</param>
        public ApiTimeoutException(string message, TimeSpan timeout) 
            : base(message, "REQUEST_TIMEOUT")
        {
            Timeout = timeout;
        }

        /// <summary>
        /// Initializes a new instance of the ApiTimeoutException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="timeout">The timeout duration that was exceeded.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiTimeoutException(string message, TimeSpan timeout, Exception innerException) 
            : base(message, "REQUEST_TIMEOUT", innerException)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// Initializes a new instance of the ApiTimeoutException class with HTTP details.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="timeout">The timeout duration that was exceeded.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public ApiTimeoutException(string message, TimeSpan timeout, HttpStatusCode httpStatusCode, string? errorDetails = null, string? requestId = null) 
            : base(message, "REQUEST_TIMEOUT", httpStatusCode, errorDetails, requestId)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// Returns a string representation of the exception with timeout details.
        /// </summary>
        /// <returns>A formatted string containing exception and timeout details.</returns>
        public override string ToString()
        {
            var result = base.ToString();
            result += $"\nTimeout: {Timeout.TotalSeconds:F1} seconds";
            return result;
        }
    }
} 