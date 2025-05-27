using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Security policy that enforces request throttling and rate limiting.
    /// </summary>
    public class RequestThrottlingPolicy : ISecurityPolicy
    {
        private readonly ILogger _logger;
        private readonly RequestThrottlingOptions _options;
        private readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();

        /// <inheritdoc />
        public string Name => "Request Throttling";

        /// <inheritdoc />
        public int Priority => 90; // High priority, but lower than TLS

        /// <inheritdoc />
        public bool IsEnabled => _options.IsEnabled;

        /// <summary>
        /// Initializes a new instance of the RequestThrottlingPolicy class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The request throttling options.</param>
        public RequestThrottlingPolicy(ILogger logger, RequestThrottlingOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new RequestThrottlingOptions();
            _requestTrackers = new ConcurrentDictionary<string, RequestTracker>();

            // Set up cleanup timer to remove old entries every minute
            _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <inheritdoc />
        public Task<PolicyValidationResult> ValidateRequestAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            var violations = new List<PolicyViolation>();
            var clientKey = GetClientKey(context);
            var now = DateTimeOffset.UtcNow;

            // Get or create request tracker for this client
            var tracker = _requestTrackers.GetOrAdd(clientKey, _ => new RequestTracker());

            lock (tracker.Lock)
            {
                // Clean up old entries
                CleanupOldEntries(tracker, now);

                // Check rate limits
                CheckRateLimits(tracker, now, violations, context);

                // Record this request
                tracker.RequestTimes.Add(now);
                tracker.LastRequestTime = now;

                // Check concurrent requests
                CheckConcurrentRequests(tracker, violations, context);
            }

            var metadata = new Dictionary<string, object>
            {
                ["ClientKey"] = clientKey,
                ["RequestCount"] = tracker.RequestTimes.Count,
                ["LastRequestTime"] = tracker.LastRequestTime,
                ["ConcurrentRequests"] = tracker.ConcurrentRequests,
                ["ThrottlingStrategy"] = _options.Strategy.ToString(),
                ["Timestamp"] = now
            };

            var isValid = !violations.Any(v => v.Severity >= PolicyViolationSeverity.Error);

            if (isValid)
            {
                _logger.LogDebug("Request throttling validation passed for client {ClientKey}", clientKey);
            }
            else
            {
                _logger.LogWarning("Request throttling validation failed for client {ClientKey} with {ViolationCount} violations", 
                    clientKey, violations.Count);
            }

            return Task.FromResult(isValid 
                ? PolicyValidationResult.Success(metadata) 
                : PolicyValidationResult.Failure(violations, metadata));
        }

        /// <inheritdoc />
        public Task<PolicyValidationResult> ValidateResponseAsync(
            HttpResponseMessage response, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            var clientKey = GetClientKey(context);
            
            // Decrement concurrent request count
            if (_requestTrackers.TryGetValue(clientKey, out var tracker))
            {
                lock (tracker.Lock)
                {
                    tracker.ConcurrentRequests = Math.Max(0, tracker.ConcurrentRequests - 1);
                }
            }

            var metadata = new Dictionary<string, object>
            {
                ["ClientKey"] = clientKey,
                ["StatusCode"] = response.StatusCode,
                ["ConcurrentRequests"] = tracker?.ConcurrentRequests ?? 0,
                ["Timestamp"] = DateTimeOffset.UtcNow
            };

            _logger.LogDebug("Request completed for client {ClientKey}, concurrent requests: {ConcurrentRequests}", 
                clientKey, tracker?.ConcurrentRequests ?? 0);

            return Task.FromResult(PolicyValidationResult.Success(metadata));
        }

        /// <inheritdoc />
        public Task HandleViolationAsync(
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            var clientKey = GetClientKey(context);
            
            _logger.LogWarning("Request throttling violation for client {ClientKey}: {ViolationCode} - {Message}", 
                clientKey, violation.Code, violation.Message);

            // For critical throttling violations, we might want to take additional action
            if (violation.Severity >= PolicyViolationSeverity.Error)
            {
                _logger.LogError("Blocking request due to throttling violation for client {ClientKey}", clientKey);
                
                // In a real implementation, this might:
                // - Temporarily increase throttling for this client
                // - Add client to a temporary block list
                // - Send alerts to administrators
            }

            return Task.CompletedTask;
        }

        private string GetClientKey(SecurityContext context)
        {
            // Use a combination of IP address and organization code for client identification
            var ipAddress = context.ClientIpAddress ?? "unknown";
            var orgCode = context.OrganizationCode ?? "unknown";
            return $"{ipAddress}:{orgCode}";
        }

        private void CheckRateLimits(RequestTracker tracker, DateTimeOffset now, List<PolicyViolation> violations, SecurityContext context)
        {
            switch (_options.Strategy)
            {
                case ThrottlingStrategy.SlidingWindow:
                    CheckSlidingWindowLimits(tracker, now, violations, context);
                    break;
                case ThrottlingStrategy.FixedWindow:
                    CheckFixedWindowLimits(tracker, now, violations, context);
                    break;
                case ThrottlingStrategy.TokenBucket:
                    CheckTokenBucketLimits(tracker, now, violations, context);
                    break;
                case ThrottlingStrategy.LeakyBucket:
                    CheckLeakyBucketLimits(tracker, now, violations, context);
                    break;
                default:
                    CheckSlidingWindowLimits(tracker, now, violations, context);
                    break;
            }
        }

        private void CheckSlidingWindowLimits(RequestTracker tracker, DateTimeOffset now, List<PolicyViolation> violations, SecurityContext context)
        {
            // Check requests per minute
            var oneMinuteAgo = now.AddMinutes(-1);
            var requestsInLastMinute = tracker.RequestTimes.Count(t => t >= oneMinuteAgo);
            
            if (requestsInLastMinute >= _options.MaxRequestsPerMinute)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "RATE_LIMIT_EXCEEDED_MINUTE",
                    $"Rate limit exceeded: {requestsInLastMinute} requests in the last minute (limit: {_options.MaxRequestsPerMinute})",
                    new Dictionary<string, object>
                    {
                        ["RequestCount"] = requestsInLastMinute,
                        ["Limit"] = _options.MaxRequestsPerMinute,
                        ["TimeWindow"] = "1 minute",
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }

            // Check requests per hour
            var oneHourAgo = now.AddHours(-1);
            var requestsInLastHour = tracker.RequestTimes.Count(t => t >= oneHourAgo);
            
            if (requestsInLastHour >= _options.MaxRequestsPerHour)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "RATE_LIMIT_EXCEEDED_HOUR",
                    $"Rate limit exceeded: {requestsInLastHour} requests in the last hour (limit: {_options.MaxRequestsPerHour})",
                    new Dictionary<string, object>
                    {
                        ["RequestCount"] = requestsInLastHour,
                        ["Limit"] = _options.MaxRequestsPerHour,
                        ["TimeWindow"] = "1 hour",
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }

            // Check burst limits
            var burstWindow = now.AddSeconds(-_options.BurstWindowSeconds);
            var requestsInBurstWindow = tracker.RequestTimes.Count(t => t >= burstWindow);
            
            if (requestsInBurstWindow > _options.BurstAllowance)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Critical,
                    "BURST_LIMIT_EXCEEDED",
                    $"Burst limit exceeded: {requestsInBurstWindow} requests in {_options.BurstWindowSeconds} seconds (limit: {_options.BurstAllowance})",
                    new Dictionary<string, object>
                    {
                        ["RequestCount"] = requestsInBurstWindow,
                        ["Limit"] = _options.BurstAllowance,
                        ["TimeWindow"] = $"{_options.BurstWindowSeconds} seconds",
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }
        }

        private void CheckFixedWindowLimits(RequestTracker tracker, DateTimeOffset now, List<PolicyViolation> violations, SecurityContext context)
        {
            // For fixed window, we check against the current minute/hour boundaries
            var currentMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset);
            var currentHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

            var requestsThisMinute = tracker.RequestTimes.Count(t => t >= currentMinute);
            var requestsThisHour = tracker.RequestTimes.Count(t => t >= currentHour);

            if (requestsThisMinute > _options.MaxRequestsPerMinute)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "FIXED_WINDOW_MINUTE_EXCEEDED",
                    $"Fixed window rate limit exceeded: {requestsThisMinute} requests this minute (limit: {_options.MaxRequestsPerMinute})",
                    new Dictionary<string, object>
                    {
                        ["RequestCount"] = requestsThisMinute,
                        ["Limit"] = _options.MaxRequestsPerMinute,
                        ["WindowStart"] = currentMinute,
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }

            if (requestsThisHour > _options.MaxRequestsPerHour)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "FIXED_WINDOW_HOUR_EXCEEDED",
                    $"Fixed window rate limit exceeded: {requestsThisHour} requests this hour (limit: {_options.MaxRequestsPerHour})",
                    new Dictionary<string, object>
                    {
                        ["RequestCount"] = requestsThisHour,
                        ["Limit"] = _options.MaxRequestsPerHour,
                        ["WindowStart"] = currentHour,
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }
        }

        private void CheckTokenBucketLimits(RequestTracker tracker, DateTimeOffset now, List<PolicyViolation> violations, SecurityContext context)
        {
            // Token bucket implementation
            // Initialize bucket if not exists
            if (tracker.TokenBucket == null)
            {
                tracker.TokenBucket = new TokenBucket(_options.MaxRequestsPerMinute, _options.MaxRequestsPerMinute);
                tracker.LastTokenRefill = now;
            }

            // Refill tokens based on time elapsed
            var timeSinceLastRefill = now - tracker.LastTokenRefill;
            var tokensToAdd = (int)(timeSinceLastRefill.TotalMinutes * _options.MaxRequestsPerMinute);
            
            if (tokensToAdd > 0)
            {
                tracker.TokenBucket.AddTokens(tokensToAdd);
                tracker.LastTokenRefill = now;
            }

            // Try to consume a token
            if (!tracker.TokenBucket.TryConsumeToken())
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "TOKEN_BUCKET_EMPTY",
                    $"Token bucket empty: no tokens available for request (bucket size: {_options.MaxRequestsPerMinute})",
                    new Dictionary<string, object>
                    {
                        ["AvailableTokens"] = tracker.TokenBucket.AvailableTokens,
                        ["BucketSize"] = _options.MaxRequestsPerMinute,
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }
        }

        private void CheckLeakyBucketLimits(RequestTracker tracker, DateTimeOffset now, List<PolicyViolation> violations, SecurityContext context)
        {
            // Leaky bucket implementation
            // Initialize bucket if not exists
            if (tracker.LeakyBucket == null)
            {
                tracker.LeakyBucket = new LeakyBucket(_options.MaxRequestsPerMinute, _options.MaxRequestsPerMinute);
                tracker.LastBucketLeak = now;
            }

            // Leak tokens based on time elapsed
            var timeSinceLastLeak = now - tracker.LastBucketLeak;
            var tokensToLeak = (int)(timeSinceLastLeak.TotalMinutes * _options.MaxRequestsPerMinute);
            
            if (tokensToLeak > 0)
            {
                tracker.LeakyBucket.LeakTokens(tokensToLeak);
                tracker.LastBucketLeak = now;
            }

            // Try to add a request to the bucket
            if (!tracker.LeakyBucket.TryAddRequest())
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "LEAKY_BUCKET_FULL",
                    $"Leaky bucket full: cannot accept more requests (bucket size: {_options.MaxRequestsPerMinute})",
                    new Dictionary<string, object>
                    {
                        ["CurrentRequests"] = tracker.LeakyBucket.CurrentRequests,
                        ["BucketSize"] = _options.MaxRequestsPerMinute,
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }
        }

        private void CheckConcurrentRequests(RequestTracker tracker, List<PolicyViolation> violations, SecurityContext context)
        {
            tracker.ConcurrentRequests++;

            if (tracker.ConcurrentRequests > _options.MaxConcurrentRequests)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "CONCURRENT_LIMIT_EXCEEDED",
                    $"Concurrent request limit exceeded: {tracker.ConcurrentRequests} concurrent requests (limit: {_options.MaxConcurrentRequests})",
                    new Dictionary<string, object>
                    {
                        ["ConcurrentRequests"] = tracker.ConcurrentRequests,
                        ["Limit"] = _options.MaxConcurrentRequests,
                        ["ClientKey"] = GetClientKey(context)
                    }));
            }
        }

        private void CleanupOldEntries(RequestTracker tracker, DateTimeOffset now)
        {
            // Remove entries older than 1 hour
            var cutoff = now.AddHours(-1);
            tracker.RequestTimes.RemoveAll(t => t < cutoff);
        }

        private void CleanupOldEntries(object? state)
        {
            var now = DateTimeOffset.UtcNow;
            var keysToRemove = new List<string>();

            foreach (var kvp in _requestTrackers)
            {
                var tracker = kvp.Value;
                lock (tracker.Lock)
                {
                    CleanupOldEntries(tracker, now);
                    
                    // Remove trackers that haven't been used in the last hour
                    if (now - tracker.LastRequestTime > TimeSpan.FromHours(1))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            // Remove old trackers
            foreach (var key in keysToRemove)
            {
                _requestTrackers.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old request trackers", keysToRemove.Count);
            }
        }

        /// <summary>
        /// Disposes the policy and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    /// <summary>
    /// Tracks request information for a specific client.
    /// </summary>
    internal class RequestTracker
    {
        public List<DateTimeOffset> RequestTimes { get; } = new List<DateTimeOffset>();
        public DateTimeOffset LastRequestTime { get; set; } = DateTimeOffset.UtcNow;
        public int ConcurrentRequests { get; set; } = 0;
        public TokenBucket? TokenBucket { get; set; }
        public LeakyBucket? LeakyBucket { get; set; }
        public DateTimeOffset LastTokenRefill { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastBucketLeak { get; set; } = DateTimeOffset.UtcNow;
        public object Lock { get; } = new object();
    }

    /// <summary>
    /// Token bucket implementation for rate limiting.
    /// </summary>
    internal class TokenBucket
    {
        private readonly int _maxTokens;
        private int _availableTokens;

        public int AvailableTokens => _availableTokens;

        public TokenBucket(int maxTokens, int initialTokens)
        {
            _maxTokens = maxTokens;
            _availableTokens = Math.Min(initialTokens, maxTokens);
        }

        public void AddTokens(int tokens)
        {
            _availableTokens = Math.Min(_maxTokens, _availableTokens + tokens);
        }

        public bool TryConsumeToken()
        {
            if (_availableTokens > 0)
            {
                _availableTokens--;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Leaky bucket implementation for rate limiting.
    /// </summary>
    internal class LeakyBucket
    {
        private readonly int _maxRequests;
        private int _currentRequests;

        public int CurrentRequests => _currentRequests;

        public LeakyBucket(int maxRequests, int initialRequests = 0)
        {
            _maxRequests = maxRequests;
            _currentRequests = Math.Min(initialRequests, maxRequests);
        }

        public void LeakTokens(int tokens)
        {
            _currentRequests = Math.Max(0, _currentRequests - tokens);
        }

        public bool TryAddRequest()
        {
            if (_currentRequests < _maxRequests)
            {
                _currentRequests++;
                return true;
            }
            return false;
        }
    }
} 