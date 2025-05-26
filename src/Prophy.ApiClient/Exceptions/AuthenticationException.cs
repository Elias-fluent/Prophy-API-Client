using System;
using System.Net;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication fails or API key is invalid.
    /// </summary>
    public class AuthenticationException : ProphyApiException
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public AuthenticationException(string message) 
            : base(message, "AUTH_ERROR")
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public AuthenticationException(string message, Exception innerException) 
            : base(message, "AUTH_ERROR", innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with HTTP details.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public AuthenticationException(string message, HttpStatusCode httpStatusCode, string? errorDetails = null, string? requestId = null) 
            : base(message, "AUTH_ERROR", httpStatusCode, errorDetails, requestId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with HTTP details and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public AuthenticationException(string message, HttpStatusCode httpStatusCode, Exception innerException, string? errorDetails = null, string? requestId = null) 
            : base(message, "AUTH_ERROR", httpStatusCode, innerException, errorDetails, requestId)
        {
        }
    }
} 