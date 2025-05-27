using System;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Configuration options for resilience strategies
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public RateLimitingOptions RateLimiting { get; set; } = new RateLimitingOptions();

        /// <summary>
        /// Circuit breaker configuration
        /// </summary>
        public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions();

        /// <summary>
        /// Retry configuration
        /// </summary>
        public RetryOptions Retry { get; set; } = new RetryOptions();

        /// <summary>
        /// Timeout configuration
        /// </summary>
        public TimeoutOptions Timeout { get; set; } = new TimeoutOptions();

        /// <summary>
        /// Whether resilience strategies are enabled globally
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Rate limiting configuration options
    /// </summary>
    public class RateLimitingOptions
    {
        /// <summary>
        /// Whether rate limiting is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum number of requests per time window
        /// </summary>
        public int PermitLimit { get; set; } = 100;

        /// <summary>
        /// Time window for rate limiting
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Number of segments per window for sliding window rate limiter
        /// </summary>
        public int SegmentsPerWindow { get; set; } = 4;

        /// <summary>
        /// Queue limit for pending requests
        /// </summary>
        public int QueueLimit { get; set; } = 10;

        /// <summary>
        /// Auto-replenishment period for token bucket
        /// </summary>
        public TimeSpan? AutoReplenishment { get; set; }
    }

    /// <summary>
    /// Circuit breaker configuration options
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Whether circuit breaker is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Failure threshold ratio (0.0 to 1.0)
        /// </summary>
        public double FailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Minimum number of actions before circuit breaker can trip
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Sampling duration for failure ratio calculation
        /// </summary>
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Duration to keep circuit open before attempting recovery
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Retry configuration options
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// Whether retry is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay between retries
        /// </summary>
        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Backoff type for retry delays
        /// </summary>
        public DelayBackoffType BackoffType { get; set; } = DelayBackoffType.Exponential;

        /// <summary>
        /// Whether to use jitter in retry delays
        /// </summary>
        public bool UseJitter { get; set; } = true;
    }

    /// <summary>
    /// Timeout configuration options
    /// </summary>
    public class TimeoutOptions
    {
        /// <summary>
        /// Whether timeout is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Timeout duration
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Delay backoff types for retry strategies
    /// </summary>
    public enum DelayBackoffType
    {
        /// <summary>
        /// Constant delay between retries
        /// </summary>
        Constant,

        /// <summary>
        /// Linear increase in delay
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay
        /// </summary>
        Exponential
    }
} 