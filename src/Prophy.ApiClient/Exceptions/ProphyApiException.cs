using System;
using System.Net;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Base exception class for all Prophy API related errors.
    /// </summary>
    public class ProphyApiException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Gets the HTTP status code if this exception was caused by an HTTP error.
        /// </summary>
        public HttpStatusCode? HttpStatusCode { get; }

        /// <summary>
        /// Gets additional error details from the API response.
        /// </summary>
        public string? ErrorDetails { get; }

        /// <summary>
        /// Gets the request ID for tracking purposes.
        /// </summary>
        public string? RequestId { get; }

        /// <summary>
        /// Initializes a new instance of the ProphyApiException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        public ProphyApiException(string message, string errorCode) 
            : base(message)
        {
            if (string.IsNullOrEmpty(errorCode))
                throw new ArgumentNullException(nameof(errorCode));
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the ProphyApiException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProphyApiException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            if (string.IsNullOrEmpty(errorCode))
                throw new ArgumentNullException(nameof(errorCode));
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the ProphyApiException class with HTTP details.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public ProphyApiException(string message, string errorCode, HttpStatusCode httpStatusCode, string? errorDetails = null, string? requestId = null) 
            : base(message)
        {
            if (string.IsNullOrEmpty(errorCode))
                throw new ArgumentNullException(nameof(errorCode));
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            ErrorDetails = errorDetails;
            RequestId = requestId;
        }

        /// <summary>
        /// Initializes a new instance of the ProphyApiException class with HTTP details and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public ProphyApiException(string message, string errorCode, HttpStatusCode httpStatusCode, Exception innerException, string? errorDetails = null, string? requestId = null) 
            : base(message, innerException)
        {
            if (string.IsNullOrEmpty(errorCode))
                throw new ArgumentNullException(nameof(errorCode));
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            ErrorDetails = errorDetails;
            RequestId = requestId;
        }

        /// <summary>
        /// Returns a string representation of the exception with all relevant details.
        /// </summary>
        /// <returns>A formatted string containing exception details.</returns>
        public override string ToString()
        {
            var result = $"ProphyApiException: {Message} (ErrorCode: {ErrorCode}";
            
            if (HttpStatusCode.HasValue)
                result += $", HttpStatusCode: {HttpStatusCode}";
            
            if (!string.IsNullOrEmpty(RequestId))
                result += $", RequestId: {RequestId}";
            
            result += ")";
            
            if (!string.IsNullOrEmpty(ErrorDetails))
                result += $"\nDetails: {ErrorDetails}";
            
            if (InnerException != null)
                result += $"\nInner Exception: {InnerException}";
            
            return result;
        }
    }
} 