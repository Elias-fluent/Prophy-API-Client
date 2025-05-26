using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Utility class for handling HTTP responses and creating appropriate exceptions.
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handles an HTTP response and throws an appropriate exception if the response indicates an error.
        /// </summary>
        /// <param name="response">The HTTP response to handle.</param>
        /// <param name="logger">The logger for recording error details.</param>
        /// <returns>A task that completes when error handling is finished.</returns>
        /// <exception cref="ProphyApiException">Thrown when the response indicates an error.</exception>
        public static async Task HandleResponseAsync(HttpResponseMessage response, ILogger? logger = null)
        {
            if (response.IsSuccessStatusCode)
                return;

            var requestId = GetRequestId(response);
            var errorContent = await GetErrorContentAsync(response);
            var errorDetails = await ParseErrorDetailsAsync(errorContent);

            logger?.LogError("API request failed with status {StatusCode}. RequestId: {RequestId}, Error: {ErrorDetails}", 
                response.StatusCode, requestId, errorDetails?.Message ?? "Unknown error");

            var exception = CreateExceptionFromResponse(response.StatusCode, errorDetails, requestId, errorContent);
            throw exception;
        }

        /// <summary>
        /// Creates an appropriate exception based on the HTTP status code and error details.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="errorDetails">The parsed error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        /// <param name="rawContent">The raw error content.</param>
        /// <returns>An appropriate exception instance.</returns>
        public static ProphyApiException CreateExceptionFromResponse(HttpStatusCode statusCode, ErrorDetails? errorDetails, string? requestId, string? rawContent)
        {
            var message = errorDetails?.Message ?? GetDefaultErrorMessage(statusCode);
            var details = errorDetails?.Details ?? rawContent;

            return statusCode switch
            {
                HttpStatusCode.Unauthorized => new AuthenticationException(message, statusCode, details, requestId),
                HttpStatusCode.Forbidden => new AuthenticationException(message, statusCode, details, requestId),
                HttpStatusCode.BadRequest when errorDetails?.ValidationErrors?.Any() == true => 
                    new ValidationException(message, errorDetails.ValidationErrors, statusCode, details, requestId),
                HttpStatusCode.BadRequest => new ValidationException(message, new[] { message }, statusCode, details, requestId),
                (HttpStatusCode)429 => CreateRateLimitException(message, statusCode, errorDetails, details, requestId),
                HttpStatusCode.RequestTimeout => new ApiTimeoutException(message, TimeSpan.FromSeconds(30), statusCode, details, requestId),
                HttpStatusCode.InternalServerError => new ProphyApiException(message, "INTERNAL_SERVER_ERROR", statusCode, details, requestId),
                HttpStatusCode.BadGateway => new ProphyApiException(message, "BAD_GATEWAY", statusCode, details, requestId),
                HttpStatusCode.ServiceUnavailable => new ProphyApiException(message, "SERVICE_UNAVAILABLE", statusCode, details, requestId),
                HttpStatusCode.GatewayTimeout => new ApiTimeoutException(message, TimeSpan.FromSeconds(60), statusCode, details, requestId),
                _ => new ProphyApiException(message, "HTTP_ERROR", statusCode, details, requestId)
            };
        }

        /// <summary>
        /// Handles timeout exceptions and creates appropriate timeout exceptions.
        /// </summary>
        /// <param name="timeoutException">The original timeout exception.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="logger">The logger for recording error details.</param>
        /// <returns>An appropriate timeout exception.</returns>
        public static ApiTimeoutException HandleTimeoutException(Exception timeoutException, TimeSpan timeout, ILogger? logger = null)
        {
            var message = $"Request timed out after {timeout.TotalSeconds:F1} seconds";
            logger?.LogError(timeoutException, "API request timed out after {Timeout} seconds", timeout.TotalSeconds);
            return new ApiTimeoutException(message, timeout, timeoutException);
        }

        /// <summary>
        /// Handles serialization exceptions and creates appropriate serialization exceptions.
        /// </summary>
        /// <param name="serializationException">The original serialization exception.</param>
        /// <param name="targetType">The type being serialized/deserialized.</param>
        /// <param name="jsonContent">The JSON content that caused the error.</param>
        /// <param name="logger">The logger for recording error details.</param>
        /// <returns>An appropriate serialization exception.</returns>
        public static SerializationException HandleSerializationException(Exception serializationException, Type? targetType, string? jsonContent, ILogger? logger = null)
        {
            var message = $"Failed to serialize/deserialize {targetType?.Name ?? "object"}: {serializationException.Message}";
            logger?.LogError(serializationException, "Serialization failed for type {Type}", targetType?.FullName ?? "Unknown");
            return new SerializationException(message, targetType, jsonContent, serializationException);
        }

        private static async Task<string?> GetErrorContentAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private static Task<ErrorDetails?> ParseErrorDetailsAsync(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return Task.FromResult<ErrorDetails?>(null);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<ErrorDetails>(content, options);
                return Task.FromResult(result);
            }
            catch
            {
                return Task.FromResult<ErrorDetails?>(null);
            }
        }

        private static string? GetRequestId(HttpResponseMessage response)
        {
            response.Headers.TryGetValues("X-Request-ID", out var requestIdValues);
            return requestIdValues?.FirstOrDefault();
        }

        private static string GetDefaultErrorMessage(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => "Authentication failed. Please check your API key.",
                HttpStatusCode.Forbidden => "Access denied. You don't have permission to access this resource.",
                HttpStatusCode.BadRequest => "The request was invalid. Please check your request parameters.",
                HttpStatusCode.NotFound => "The requested resource was not found.",
                (HttpStatusCode)429 => "Rate limit exceeded. Please try again later.",
                HttpStatusCode.InternalServerError => "An internal server error occurred. Please try again later.",
                HttpStatusCode.BadGateway => "Bad gateway. The server is temporarily unavailable.",
                HttpStatusCode.ServiceUnavailable => "Service unavailable. Please try again later.",
                HttpStatusCode.GatewayTimeout => "Gateway timeout. The request took too long to process.",
                _ => $"HTTP error {(int)statusCode}: {statusCode}"
            };
        }

        private static RateLimitException CreateRateLimitException(string message, HttpStatusCode statusCode, ErrorDetails? errorDetails, string? details, string? requestId)
        {
            DateTimeOffset? retryAfter = null;
            int? remainingRequests = null;
            int? requestLimit = null;

            // Try to parse rate limit headers if available
            if (errorDetails?.RetryAfterSeconds.HasValue == true)
            {
                retryAfter = DateTimeOffset.UtcNow.AddSeconds(errorDetails.RetryAfterSeconds.Value);
            }

            if (errorDetails?.RemainingRequests.HasValue == true)
            {
                remainingRequests = errorDetails.RemainingRequests.Value;
            }

            if (errorDetails?.RequestLimit.HasValue == true)
            {
                requestLimit = errorDetails.RequestLimit.Value;
            }

            return new RateLimitException(message, retryAfter, remainingRequests, requestLimit, statusCode, details, requestId);
        }
    }

    /// <summary>
    /// Represents error details from an API response.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional error details.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        public List<string>? ValidationErrors { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds to wait before retrying.
        /// </summary>
        public int? RetryAfterSeconds { get; set; }

        /// <summary>
        /// Gets or sets the number of remaining requests in the current rate limit window.
        /// </summary>
        public int? RemainingRequests { get; set; }

        /// <summary>
        /// Gets or sets the total number of requests allowed in the current rate limit window.
        /// </summary>
        public int? RequestLimit { get; set; }
    }
} 