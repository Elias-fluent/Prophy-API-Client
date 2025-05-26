using System;
using System.Net;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Exception thrown when API rate limits are exceeded.
    /// </summary>
    public class RateLimitException : ProphyApiException
    {
        /// <summary>
        /// Gets the time when the rate limit will reset.
        /// </summary>
        public DateTimeOffset? RetryAfter { get; }

        /// <summary>
        /// Gets the remaining requests allowed in the current window.
        /// </summary>
        public int? RemainingRequests { get; }

        /// <summary>
        /// Gets the total requests allowed in the current window.
        /// </summary>
        public int? RequestLimit { get; }

        /// <summary>
        /// Initializes a new instance of the RateLimitException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfter">The time when the rate limit will reset.</param>
        public RateLimitException(string message, DateTimeOffset? retryAfter = null) 
            : base(message, "RATE_LIMIT_EXCEEDED")
        {
            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Initializes a new instance of the RateLimitException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfter">The time when the rate limit will reset.</param>
        /// <param name="innerException">The inner exception.</param>
        public RateLimitException(string message, DateTimeOffset? retryAfter, Exception innerException) 
            : base(message, "RATE_LIMIT_EXCEEDED", innerException)
        {
            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Initializes a new instance of the RateLimitException class with detailed rate limit information.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfter">The time when the rate limit will reset.</param>
        /// <param name="remainingRequests">The remaining requests allowed in the current window.</param>
        /// <param name="requestLimit">The total requests allowed in the current window.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public RateLimitException(string message, DateTimeOffset? retryAfter, int? remainingRequests, int? requestLimit, HttpStatusCode httpStatusCode, string? errorDetails = null, string? requestId = null) 
            : base(message, "RATE_LIMIT_EXCEEDED", httpStatusCode, errorDetails, requestId)
        {
            RetryAfter = retryAfter;
            RemainingRequests = remainingRequests;
            RequestLimit = requestLimit;
        }

        /// <summary>
        /// Returns a string representation of the exception with rate limit details.
        /// </summary>
        /// <returns>A formatted string containing exception and rate limit details.</returns>
        public override string ToString()
        {
            var result = base.ToString();
            
            if (RetryAfter.HasValue)
                result += $"\nRetry After: {RetryAfter.Value:yyyy-MM-dd HH:mm:ss UTC}";
            
            if (RemainingRequests.HasValue && RequestLimit.HasValue)
                result += $"\nRate Limit: {RemainingRequests}/{RequestLimit} requests remaining";
            
            return result;
        }
    }
} 