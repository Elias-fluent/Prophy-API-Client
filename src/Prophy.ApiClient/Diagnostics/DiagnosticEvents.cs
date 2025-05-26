using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Diagnostics
{
    /// <summary>
    /// Defines diagnostic events and performance monitoring for the Prophy API Client.
    /// </summary>
    public static class DiagnosticEvents
    {
        /// <summary>
        /// The activity source name for the Prophy API Client.
        /// </summary>
        public const string ActivitySourceName = "Prophy.ApiClient";

        /// <summary>
        /// The activity source for creating activities.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

        /// <summary>
        /// Event IDs for structured logging.
        /// </summary>
        public static class EventIds
        {
            // Client lifecycle events (1000-1099)
            public static readonly EventId ClientInitialized = new EventId(1000, nameof(ClientInitialized));
            public static readonly EventId ClientDisposed = new EventId(1001, nameof(ClientDisposed));

            // HTTP request events (1100-1199)
            public static readonly EventId HttpRequestStarted = new EventId(1100, nameof(HttpRequestStarted));
            public static readonly EventId HttpRequestCompleted = new EventId(1101, nameof(HttpRequestCompleted));
            public static readonly EventId HttpRequestFailed = new EventId(1102, nameof(HttpRequestFailed));
            public static readonly EventId SlowHttpRequest = new EventId(1103, nameof(SlowHttpRequest));

            // Authentication events (1200-1299)
            public static readonly EventId AuthenticationStarted = new EventId(1200, nameof(AuthenticationStarted));
            public static readonly EventId AuthenticationCompleted = new EventId(1201, nameof(AuthenticationCompleted));
            public static readonly EventId AuthenticationFailed = new EventId(1202, nameof(AuthenticationFailed));
            public static readonly EventId JwtTokenGenerated = new EventId(1203, nameof(JwtTokenGenerated));

            // Serialization events (1300-1399)
            public static readonly EventId SerializationStarted = new EventId(1300, nameof(SerializationStarted));
            public static readonly EventId SerializationCompleted = new EventId(1301, nameof(SerializationCompleted));
            public static readonly EventId SerializationFailed = new EventId(1302, nameof(SerializationFailed));
            public static readonly EventId DeserializationStarted = new EventId(1303, nameof(DeserializationStarted));
            public static readonly EventId DeserializationCompleted = new EventId(1304, nameof(DeserializationCompleted));
            public static readonly EventId DeserializationFailed = new EventId(1305, nameof(DeserializationFailed));

            // API operation events (1400-1499)
            public static readonly EventId ManuscriptUploadStarted = new EventId(1400, nameof(ManuscriptUploadStarted));
            public static readonly EventId ManuscriptUploadCompleted = new EventId(1401, nameof(ManuscriptUploadCompleted));
            public static readonly EventId ManuscriptUploadFailed = new EventId(1402, nameof(ManuscriptUploadFailed));
            public static readonly EventId WebhookProcessingStarted = new EventId(1403, nameof(WebhookProcessingStarted));
            public static readonly EventId WebhookProcessingCompleted = new EventId(1404, nameof(WebhookProcessingCompleted));
            public static readonly EventId WebhookProcessingFailed = new EventId(1405, nameof(WebhookProcessingFailed));

            // Performance events (1500-1599)
            public static readonly EventId PerformanceMetrics = new EventId(1500, nameof(PerformanceMetrics));
            public static readonly EventId MemoryUsage = new EventId(1501, nameof(MemoryUsage));
            public static readonly EventId CacheHit = new EventId(1502, nameof(CacheHit));
            public static readonly EventId CacheMiss = new EventId(1503, nameof(CacheMiss));
        }

        /// <summary>
        /// Log scopes for structured logging.
        /// </summary>
        public static class Scopes
        {
            public static IDisposable HttpRequest(ILogger logger, string method, string uri, string requestId)
            {
                return logger.BeginScope(new Dictionary<string, object>
                {
                    ["HttpMethod"] = method,
                    ["RequestUri"] = uri,
                    ["RequestId"] = requestId
                });
            }

            public static IDisposable ApiOperation(ILogger logger, string operation, string? organizationCode = null)
            {
                var scope = new Dictionary<string, object>
                {
                    ["Operation"] = operation
                };

                if (!string.IsNullOrEmpty(organizationCode))
                {
                    scope["OrganizationCode"] = organizationCode;
                }

                return logger.BeginScope(scope);
            }

            public static IDisposable Serialization(ILogger logger, string operation, Type type)
            {
                return logger.BeginScope(new Dictionary<string, object>
                {
                    ["SerializationOperation"] = operation,
                    ["TargetType"] = type.Name
                });
            }
        }

        /// <summary>
        /// Activity tags for distributed tracing.
        /// </summary>
        public static class ActivityTags
        {
            public const string HttpMethod = "http.method";
            public const string HttpUrl = "http.url";
            public const string HttpStatusCode = "http.status_code";
            public const string HttpRequestSize = "http.request.size";
            public const string HttpResponseSize = "http.response.size";
            public const string OrganizationCode = "prophy.organization_code";
            public const string ApiOperation = "prophy.api.operation";
            public const string ManuscriptId = "prophy.manuscript.id";
            public const string WebhookEventType = "prophy.webhook.event_type";
            public const string ErrorType = "error.type";
            public const string ErrorMessage = "error.message";
        }

        /// <summary>
        /// Performance metrics collection.
        /// </summary>
        public static class Metrics
        {
            private static readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();

            public static void RecordHttpRequestDuration(string method, string endpoint, TimeSpan duration, bool success)
            {
                var key = $"http.request.duration.{method.ToLowerInvariant()}.{(success ? "success" : "failure")}";
                RecordDuration(key, duration);
            }

            public static void RecordSerializationDuration(string operation, Type type, TimeSpan duration)
            {
                var key = $"serialization.{operation.ToLowerInvariant()}.{type.Name.ToLowerInvariant()}";
                RecordDuration(key, duration);
            }

            public static void RecordApiOperationDuration(string operation, TimeSpan duration, bool success)
            {
                var key = $"api.operation.{operation.ToLowerInvariant()}.{(success ? "success" : "failure")}";
                RecordDuration(key, duration);
            }

            public static void IncrementCounter(string name)
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.Increment();
                }
                else
                {
                    _counters[name] = new PerformanceCounter { Count = 1 };
                }
            }

            public static void RecordValue(string name, double value)
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.RecordValue(value);
                }
                else
                {
                    _counters[name] = new PerformanceCounter();
                    _counters[name].RecordValue(value);
                }
            }

            private static void RecordDuration(string key, TimeSpan duration)
            {
                RecordValue(key, duration.TotalMilliseconds);
            }

            public static Dictionary<string, PerformanceCounter> GetCounters()
            {
                return new Dictionary<string, PerformanceCounter>(_counters);
            }

            public static void Reset()
            {
                _counters.Clear();
            }
        }

        /// <summary>
        /// Performance counter for tracking metrics.
        /// </summary>
        public class PerformanceCounter
        {
            private readonly object _lock = new object();
            private double _sum;
            private double _min = double.MaxValue;
            private double _max = double.MinValue;
            private bool _hasValues;

            public long Count { get; set; }
            public double Average => Count > 0 ? _sum / Count : 0;
            public double Min => _hasValues ? _min : 0;
            public double Max => _hasValues ? _max : 0;
            public double Sum => _sum;

            public void Increment()
            {
                lock (_lock)
                {
                    Count++;
                }
            }

            public void RecordValue(double value)
            {
                lock (_lock)
                {
                    Count++;
                    _sum += value;
                    if (!_hasValues || value < _min) _min = value;
                    if (!_hasValues || value > _max) _max = value;
                    _hasValues = true;
                }
            }

            public override string ToString()
            {
                return $"Count: {Count}, Avg: {Average:F2}, Min: {Min:F2}, Max: {Max:F2}";
            }
        }
    }

    /// <summary>
    /// Extension methods for logging diagnostic events.
    /// </summary>
    public static class LoggerExtensions
    {
        public static void LogClientInitialized(this ILogger logger, string organizationCode, string baseUrl)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.ClientInitialized,
                "Prophy API Client initialized for organization {OrganizationCode} with base URL {BaseUrl}",
                organizationCode, baseUrl);
        }

        public static void LogClientDisposed(this ILogger logger)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.ClientDisposed,
                "Prophy API Client disposed");
        }

        public static void LogHttpRequestStarted(this ILogger logger, string method, string uri, string requestId)
        {
            logger.LogDebug(DiagnosticEvents.EventIds.HttpRequestStarted,
                "HTTP {Method} request started to {Uri} [RequestId: {RequestId}]",
                method, uri, requestId);
        }

        public static void LogHttpRequestCompleted(this ILogger logger, string method, string uri, string requestId, 
            int statusCode, TimeSpan elapsed)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.HttpRequestCompleted,
                "HTTP {Method} request to {Uri} completed with status {StatusCode} in {ElapsedMs:F1}ms [RequestId: {RequestId}]",
                method, uri, statusCode, elapsed.TotalMilliseconds, requestId);
        }

        public static void LogHttpRequestFailed(this ILogger logger, Exception exception, string method, string uri, 
            string requestId, TimeSpan elapsed)
        {
            logger.LogError(DiagnosticEvents.EventIds.HttpRequestFailed, exception,
                "HTTP {Method} request to {Uri} failed after {ElapsedMs:F1}ms [RequestId: {RequestId}]",
                method, uri, elapsed.TotalMilliseconds, requestId);
        }

        public static void LogSlowHttpRequest(this ILogger logger, string method, string uri, string requestId, 
            TimeSpan elapsed, double threshold)
        {
            logger.LogWarning(DiagnosticEvents.EventIds.SlowHttpRequest,
                "Slow HTTP {Method} request to {Uri} detected: {ElapsedMs:F1}ms (threshold: {ThresholdMs:F1}ms) [RequestId: {RequestId}]",
                method, uri, elapsed.TotalMilliseconds, threshold, requestId);
        }

        public static void LogManuscriptUploadStarted(this ILogger logger, string fileName, long fileSize)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.ManuscriptUploadStarted,
                "Manuscript upload started for file {FileName} ({FileSize} bytes)",
                fileName, fileSize);
        }

        public static void LogManuscriptUploadCompleted(this ILogger logger, string fileName, string manuscriptId, TimeSpan elapsed)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.ManuscriptUploadCompleted,
                "Manuscript upload completed for file {FileName} with ID {ManuscriptId} in {ElapsedMs:F1}ms",
                fileName, manuscriptId, elapsed.TotalMilliseconds);
        }

        public static void LogManuscriptUploadFailed(this ILogger logger, Exception exception, string fileName, TimeSpan elapsed)
        {
            logger.LogError(DiagnosticEvents.EventIds.ManuscriptUploadFailed, exception,
                "Manuscript upload failed for file {FileName} after {ElapsedMs:F1}ms",
                fileName, elapsed.TotalMilliseconds);
        }

        public static void LogWebhookProcessingStarted(this ILogger logger, string eventType, string webhookId)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.WebhookProcessingStarted,
                "Webhook processing started for event type {EventType} [WebhookId: {WebhookId}]",
                eventType, webhookId);
        }

        public static void LogWebhookProcessingCompleted(this ILogger logger, string eventType, string webhookId, TimeSpan elapsed)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.WebhookProcessingCompleted,
                "Webhook processing completed for event type {EventType} in {ElapsedMs:F1}ms [WebhookId: {WebhookId}]",
                eventType, elapsed.TotalMilliseconds, webhookId);
        }

        public static void LogWebhookProcessingFailed(this ILogger logger, Exception exception, string eventType, 
            string webhookId, TimeSpan elapsed)
        {
            logger.LogError(DiagnosticEvents.EventIds.WebhookProcessingFailed, exception,
                "Webhook processing failed for event type {EventType} after {ElapsedMs:F1}ms [WebhookId: {WebhookId}]",
                eventType, elapsed.TotalMilliseconds, webhookId);
        }

        public static void LogPerformanceMetrics(this ILogger logger, Dictionary<string, object> metrics)
        {
            logger.LogInformation(DiagnosticEvents.EventIds.PerformanceMetrics,
                "Performance metrics: {Metrics}", metrics);
        }
    }
} 