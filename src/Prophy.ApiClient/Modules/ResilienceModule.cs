using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Implementation of the resilience module that manages rate limiting, circuit breaker, and other resilience patterns.
    /// </summary>
    public class ResilienceModule : IResilienceModule, IDisposable
    {
        private readonly ILogger<ResilienceModule> _logger;
        private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _endpointPipelines;
        private readonly ConcurrentDictionary<string, object> _metrics;
        private ResilienceOptions _options;
        private ResiliencePipeline<HttpResponseMessage> _globalPipeline;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ResilienceModule class.
        /// </summary>
        /// <param name="options">The resilience configuration options.</param>
        /// <param name="logger">The logger instance.</param>
        public ResilienceModule(ResilienceOptions options, ILogger<ResilienceModule> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpointPipelines = new ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>>();
            _metrics = new ConcurrentDictionary<string, object>();
            
            _globalPipeline = BuildPipeline("global", _options);
            
            _logger.LogInformation("ResilienceModule initialized with global pipeline");
        }

        /// <inheritdoc />
        public ResilienceOptions Options => _options;

        /// <inheritdoc />
        public ResiliencePipeline<HttpResponseMessage> GlobalPipeline => _globalPipeline;

        /// <inheritdoc />
        public ResiliencePipeline<HttpResponseMessage> CreateEndpointPipeline(string endpointName, ResilienceOptions? customOptions = null)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty.", nameof(endpointName));

            var options = customOptions ?? _options;
            var pipeline = _endpointPipelines.GetOrAdd(endpointName, _ => BuildPipeline(endpointName, options));
            
            _logger.LogDebug("Created or retrieved pipeline for endpoint: {EndpointName}", endpointName);
            return pipeline;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> ExecuteAsync(Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            ThrowIfDisposed();

            try
            {
                var result = await _globalPipeline.ExecuteAsync(async (ct) => await operation(ct), cancellationToken);
                IncrementMetric("global.requests.success");
                return result;
            }
            catch (Exception ex)
            {
                IncrementMetric("global.requests.failure");
                _logger.LogWarning(ex, "Global resilience pipeline execution failed");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> ExecuteAsync(string endpointName, Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty.", nameof(endpointName));
            
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            ThrowIfDisposed();

            var pipeline = CreateEndpointPipeline(endpointName);
            
            try
            {
                var result = await pipeline.ExecuteAsync(async (ct) => await operation(ct), cancellationToken);
                IncrementMetric($"{endpointName}.requests.success");
                return result;
            }
            catch (Exception ex)
            {
                IncrementMetric($"{endpointName}.requests.failure");
                _logger.LogWarning(ex, "Endpoint resilience pipeline execution failed for: {EndpointName}", endpointName);
                throw;
            }
        }

        /// <inheritdoc />
        public void UpdateConfiguration(ResilienceOptions newOptions)
        {
            if (newOptions == null)
                throw new ArgumentNullException(nameof(newOptions));

            ThrowIfDisposed();

            _options = newOptions;
            _globalPipeline = BuildPipeline("global", _options);
            _endpointPipelines.Clear(); // Clear cached pipelines to force rebuild with new options
            
            _logger.LogInformation("Resilience configuration updated");
        }

        /// <inheritdoc />
        public IDictionary<string, object> GetMetrics()
        {
            ThrowIfDisposed();
            return new Dictionary<string, object>(_metrics);
        }

        /// <inheritdoc />
        public void Reset()
        {
            ThrowIfDisposed();
            
            _endpointPipelines.Clear();
            _metrics.Clear();
            _globalPipeline = BuildPipeline("global", _options);
            
            _logger.LogInformation("Resilience module state reset");
        }

        private ResiliencePipeline<HttpResponseMessage> BuildPipeline(string pipelineName, ResilienceOptions options)
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            // Add timeout strategy first (outermost)
            if (options.Timeout.Enabled)
            {
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = options.Timeout.Timeout,
                    OnTimeout = args =>
                    {
                        _logger.LogWarning("Timeout occurred for pipeline: {PipelineName}, Timeout: {Timeout}ms", 
                            pipelineName, args.Timeout.TotalMilliseconds);
                        IncrementMetric($"{pipelineName}.timeout");
                        return default;
                    }
                });
            }

            // Add rate limiting strategy
            if (options.RateLimiting.Enabled)
            {
                var rateLimitOptions = new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = options.RateLimiting.PermitLimit,
                    Window = options.RateLimiting.Window,
                    SegmentsPerWindow = options.RateLimiting.SegmentsPerWindow,
                    QueueLimit = options.RateLimiting.QueueLimit,
                    AutoReplenishment = true // AutoReplenishment is boolean in Polly 8.x
                };

                // Create a single rate limiter instance to be shared
                var rateLimiter = new SlidingWindowRateLimiter(rateLimitOptions);

                builder.AddRateLimiter(new RateLimiterStrategyOptions
                {
                    RateLimiter = args => rateLimiter.AcquireAsync(permitCount: 1, cancellationToken: args.Context.CancellationToken),
                    OnRejected = args =>
                    {
                        _logger.LogWarning("Rate limit exceeded for pipeline: {PipelineName}", pipelineName);
                        IncrementMetric($"{pipelineName}.rate_limit.rejected");
                        return default;
                    }
                });
            }

            // Add circuit breaker strategy
            if (options.CircuitBreaker.Enabled)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = options.CircuitBreaker.FailureRatio,
                    MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                    SamplingDuration = options.CircuitBreaker.SamplingDuration,
                    BreakDuration = options.CircuitBreaker.BreakDuration,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response => IsTransientHttpFailure(response)),
                    OnOpened = args =>
                    {
                        _logger.LogWarning("Circuit breaker opened for pipeline: {PipelineName}", pipelineName);
                        IncrementMetric($"{pipelineName}.circuit_breaker.opened");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Circuit breaker closed for pipeline: {PipelineName}", pipelineName);
                        IncrementMetric($"{pipelineName}.circuit_breaker.closed");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("Circuit breaker half-opened for pipeline: {PipelineName}", pipelineName);
                        IncrementMetric($"{pipelineName}.circuit_breaker.half_opened");
                        return default;
                    }
                });
            }

            // Add retry strategy (innermost)
            if (options.Retry.Enabled)
            {
                var retryOptions = new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = options.Retry.MaxRetryAttempts,
                    Delay = options.Retry.Delay,
                    BackoffType = ConvertBackoffType(options.Retry.BackoffType),
                    UseJitter = options.Retry.UseJitter,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response => IsTransientHttpFailure(response)),
                    OnRetry = args =>
                    {
                        _logger.LogWarning("Retrying request for pipeline: {PipelineName}, Attempt: {Attempt}, Delay: {Delay}ms", 
                            pipelineName, args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds);
                        IncrementMetric($"{pipelineName}.retry.attempt");
                        return default;
                    }
                };

                builder.AddRetry(retryOptions);
            }

            var pipeline = builder.Build();
            _logger.LogDebug("Built resilience pipeline for: {PipelineName}", pipelineName);
            
            return pipeline;
        }

        private static bool IsTransientHttpFailure(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            
            // Consider 5xx server errors as transient
            if (statusCode >= 500)
                return true;
                
            // Consider specific 4xx errors as transient
            return response.StatusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == 429; // Too Many Requests
        }

        private static Polly.DelayBackoffType ConvertBackoffType(DelayBackoffType backoffType)
        {
            return backoffType switch
            {
                DelayBackoffType.Constant => Polly.DelayBackoffType.Constant,
                DelayBackoffType.Linear => Polly.DelayBackoffType.Linear,
                DelayBackoffType.Exponential => Polly.DelayBackoffType.Exponential,
                _ => Polly.DelayBackoffType.Exponential
            };
        }

        private void IncrementMetric(string metricName)
        {
            _metrics.AddOrUpdate(metricName, 1, (key, value) => (int)value + 1);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResilienceModule));
        }

        /// <summary>
        /// Disposes the resilience module and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resilience module and cleans up resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _endpointPipelines.Clear();
                _metrics.Clear();
                _disposed = true;
                
                _logger.LogDebug("ResilienceModule disposed");
            }
        }
    }
} 