using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Http
{
    /// <summary>
    /// HTTP message handler that provides comprehensive logging of HTTP requests and responses
    /// with sensitive data redaction and performance monitoring.
    /// </summary>
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;
        private readonly LoggingOptions _options;
        private static readonly HashSet<string> SensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "X-ApiKey",
            "X-API-Key",
            "Cookie",
            "Set-Cookie",
            "Proxy-Authorization"
        };

        /// <summary>
        /// Initializes a new instance of the LoggingHandler class.
        /// </summary>
        /// <param name="logger">The logger instance for logging HTTP operations.</param>
        /// <param name="options">Configuration options for logging behavior.</param>
        public LoggingHandler(ILogger<LoggingHandler> logger, LoggingOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new LoggingOptions();
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Log request
                await LogRequestAsync(request, requestId);

                // Send request
                var response = await base.SendAsync(request, cancellationToken);

                stopwatch.Stop();

                // Log response
                await LogResponseAsync(response, requestId, stopwatch.Elapsed);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogRequestException(ex, requestId, stopwatch.Elapsed);
                throw;
            }
        }

        private async Task LogRequestAsync(HttpRequestMessage request, string requestId)
        {
            if (!_logger.IsEnabled(LogLevel.Debug) && !_options.LogRequests)
                return;

            var logLevel = _options.LogRequests ? LogLevel.Information : LogLevel.Debug;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["HttpMethod"] = request.Method.ToString(),
                ["RequestUri"] = request.RequestUri?.ToString() ?? "Unknown"
            });

            _logger.Log(logLevel, "HTTP {Method} request to {Uri} [RequestId: {RequestId}]",
                request.Method, request.RequestUri, requestId);

            // Log headers if enabled
            if (_options.LogHeaders && _logger.IsEnabled(LogLevel.Debug))
            {
                var headers = GetSafeHeaders(request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()));
                if (headers.Any())
                {
                    _logger.LogDebug("Request headers [RequestId: {RequestId}]: {Headers}", requestId, headers);
                }
            }

            // Log request body if enabled and present
            if (_options.LogRequestBody && request.Content != null && _logger.IsEnabled(LogLevel.Debug))
            {
                var body = await GetSafeRequestBodyAsync(request.Content);
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogDebug("Request body [RequestId: {RequestId}]: {Body}", requestId, body);
                }
            }
        }

        private async Task LogResponseAsync(HttpResponseMessage response, string requestId, TimeSpan elapsed)
        {
            if (!_logger.IsEnabled(LogLevel.Debug) && !_options.LogResponses)
                return;

            var logLevel = response.IsSuccessStatusCode 
                ? (_options.LogResponses ? LogLevel.Information : LogLevel.Debug)
                : LogLevel.Warning;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["StatusCode"] = (int)response.StatusCode,
                ["ElapsedMs"] = elapsed.TotalMilliseconds
            });

            _logger.Log(logLevel, "HTTP response {StatusCode} {ReasonPhrase} in {ElapsedMs:F1}ms [RequestId: {RequestId}]",
                (int)response.StatusCode, response.ReasonPhrase, elapsed.TotalMilliseconds, requestId);

            // Log response headers if enabled
            if (_options.LogHeaders && _logger.IsEnabled(LogLevel.Debug))
            {
                var headers = GetSafeHeaders(response.Headers.Concat(response.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()));
                if (headers.Any())
                {
                    _logger.LogDebug("Response headers [RequestId: {RequestId}]: {Headers}", requestId, headers);
                }
            }

            // Log response body if enabled
            if (_options.LogResponseBody && response.Content != null && _logger.IsEnabled(LogLevel.Debug))
            {
                var body = await GetSafeResponseBodyAsync(response.Content);
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogDebug("Response body [RequestId: {RequestId}]: {Body}", requestId, body);
                }
            }

            // Log performance metrics
            if (_options.LogPerformanceMetrics)
            {
                LogPerformanceMetrics(requestId, elapsed, response);
            }
        }

        private void LogRequestException(Exception exception, string requestId, TimeSpan elapsed)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["ElapsedMs"] = elapsed.TotalMilliseconds
            });

            _logger.LogError(exception, "HTTP request failed after {ElapsedMs:F1}ms [RequestId: {RequestId}]",
                elapsed.TotalMilliseconds, requestId);
        }

        private void LogPerformanceMetrics(string requestId, TimeSpan elapsed, HttpResponseMessage response)
        {
            var metrics = new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["ElapsedMs"] = elapsed.TotalMilliseconds,
                ["StatusCode"] = (int)response.StatusCode,
                ["ContentLength"] = response.Content?.Headers?.ContentLength ?? 0
            };

            if (elapsed.TotalMilliseconds > _options.SlowRequestThresholdMs)
            {
                _logger.LogWarning("Slow HTTP request detected [RequestId: {RequestId}]: {ElapsedMs:F1}ms", 
                    requestId, elapsed.TotalMilliseconds);
            }

            _logger.LogDebug("HTTP request metrics [RequestId: {RequestId}]: {Metrics}", requestId, metrics);
        }

        private static Dictionary<string, string> GetSafeHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            var safeHeaders = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                var value = SensitiveHeaders.Contains(header.Key) 
                    ? "[REDACTED]" 
                    : string.Join(", ", header.Value);
                
                safeHeaders[header.Key] = value;
            }

            return safeHeaders;
        }

        private async Task<string> GetSafeRequestBodyAsync(HttpContent content)
        {
            try
            {
                // Don't log binary content
                var contentType = content.Headers?.ContentType?.MediaType;
                if (IsBinaryContent(contentType))
                {
                    return $"[BINARY CONTENT: {contentType}]";
                }

                // Limit body size for logging
                var body = await content.ReadAsStringAsync();
                if (body.Length > _options.MaxBodyLogLength)
                {
                    return body.Substring(0, _options.MaxBodyLogLength) + "... [TRUNCATED]";
                }

                return RedactSensitiveData(body);
            }
            catch (Exception ex)
            {
                return $"[ERROR READING BODY: {ex.Message}]";
            }
        }

        private async Task<string> GetSafeResponseBodyAsync(HttpContent content)
        {
            try
            {
                // Don't log binary content
                var contentType = content.Headers?.ContentType?.MediaType;
                if (IsBinaryContent(contentType))
                {
                    return $"[BINARY CONTENT: {contentType}]";
                }

                // Create a copy of the content to avoid consuming the original stream
                var buffer = await content.ReadAsByteArrayAsync();
                var body = Encoding.UTF8.GetString(buffer);

                if (body.Length > _options.MaxBodyLogLength)
                {
                    return body.Substring(0, _options.MaxBodyLogLength) + "... [TRUNCATED]";
                }

                return RedactSensitiveData(body);
            }
            catch (Exception ex)
            {
                return $"[ERROR READING BODY: {ex.Message}]";
            }
        }

        private static bool IsBinaryContent(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var binaryTypes = new[]
            {
                "application/octet-stream",
                "application/pdf",
                "application/zip",
                "image/",
                "video/",
                "audio/"
            };

            return binaryTypes.Any(type => contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
        }

        private static string RedactSensitiveData(string content)
        {
            // Redact common sensitive patterns
            var patterns = new[]
            {
                (@"""api_?key""\s*:\s*""[^""]+""", @"""api_key"": ""[REDACTED]"""),
                (@"""password""\s*:\s*""[^""]+""", @"""password"": ""[REDACTED]"""),
                (@"""token""\s*:\s*""[^""]+""", @"""token"": ""[REDACTED]"""),
                (@"""secret""\s*:\s*""[^""]+""", @"""secret"": ""[REDACTED]""")
            };

            foreach (var (pattern, replacement) in patterns)
            {
                content = System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return content;
        }
    }

    /// <summary>
    /// Configuration options for HTTP logging behavior.
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Gets or sets whether to log HTTP requests at Information level.
        /// Default is false (logs at Debug level only).
        /// </summary>
        public bool LogRequests { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log HTTP responses at Information level.
        /// Default is false (logs at Debug level only).
        /// </summary>
        public bool LogResponses { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log HTTP headers.
        /// Default is true.
        /// </summary>
        public bool LogHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log request bodies.
        /// Default is false.
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log response bodies.
        /// Default is false.
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log performance metrics.
        /// Default is true.
        /// </summary>
        public bool LogPerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum length of request/response bodies to log.
        /// Default is 4096 characters.
        /// </summary>
        public int MaxBodyLogLength { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the threshold in milliseconds for considering a request as slow.
        /// Default is 5000ms (5 seconds).
        /// </summary>
        public double SlowRequestThresholdMs { get; set; } = 5000;
    }
} 