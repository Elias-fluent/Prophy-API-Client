using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Modules;
using Polly.RateLimiting;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Prophy.ApiClient.Tests.Modules
{
    /// <summary>
    /// Unit tests for the ResilienceModule class.
    /// </summary>
    public class ResilienceModuleTests : IDisposable
    {
        private readonly Mock<ILogger<ResilienceModule>> _mockLogger;
        private readonly ResilienceOptions _defaultOptions;
        private ResilienceModule _resilienceModule;

        public ResilienceModuleTests()
        {
            _mockLogger = new Mock<ILogger<ResilienceModule>>();
            _defaultOptions = CreateDefaultOptions();
            _resilienceModule = new ResilienceModule(_defaultOptions, _mockLogger.Object);
        }

        private static ResilienceOptions CreateDefaultOptions()
        {
            return new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 5, // Low limit for testing
                    Window = TimeSpan.FromSeconds(1),
                    SegmentsPerWindow = 2,
                    QueueLimit = 2
                },
                CircuitBreaker = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureRatio = 0.5,
                    MinimumThroughput = 3,
                    SamplingDuration = TimeSpan.FromSeconds(5),
                    BreakDuration = TimeSpan.FromSeconds(2)
                },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(100),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false // Disable jitter for predictable tests
                },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromSeconds(1)
                }
            };
        }

        [Fact]
        public void Constructor_WithValidOptions_InitializesCorrectly()
        {
            // Arrange & Act
            var module = new ResilienceModule(_defaultOptions, _mockLogger.Object);

            // Assert
            Assert.NotNull(module.Options);
            Assert.NotNull(module.GlobalPipeline);
            Assert.Equal(_defaultOptions.Enabled, module.Options.Enabled);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ResilienceModule(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ResilienceModule(_defaultOptions, null!));
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperation_ReturnsResponse()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Act
            var result = await _resilienceModule.ExecuteAsync(async ct =>
            {
                await Task.Delay(10, ct);
                return expectedResponse;
            });

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task ExecuteAsync_WithEndpointName_CreatesEndpointPipeline()
        {
            // Arrange
            var endpointName = "test-endpoint";
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Act
            var result = await _resilienceModule.ExecuteAsync(endpointName, async ct =>
            {
                await Task.Delay(10, ct);
                return expectedResponse;
            });

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public void CreateEndpointPipeline_WithValidEndpointName_ReturnsPipeline()
        {
            // Arrange
            var endpointName = "test-endpoint";

            // Act
            var pipeline = _resilienceModule.CreateEndpointPipeline(endpointName);

            // Assert
            Assert.NotNull(pipeline);
        }

        [Fact]
        public void CreateEndpointPipeline_WithNullEndpointName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _resilienceModule.CreateEndpointPipeline(null!));
        }

        [Fact]
        public void CreateEndpointPipeline_WithEmptyEndpointName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _resilienceModule.CreateEndpointPipeline(""));
        }

        [Fact]
        public void CreateEndpointPipeline_WithCustomOptions_UseCustomOptions()
        {
            // Arrange
            var endpointName = "custom-endpoint";
            var customOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            // Act
            var pipeline = _resilienceModule.CreateEndpointPipeline(endpointName, customOptions);

            // Assert
            Assert.NotNull(pipeline);
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeoutOperation_ThrowsTimeoutException()
        {
            // Arrange
            var timeoutOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromMilliseconds(100)
                }
            };

            using var timeoutModule = new ResilienceModule(timeoutOptions, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            {
                await timeoutModule.ExecuteAsync(async ct =>
                {
                    await Task.Delay(500, ct); // Longer than timeout
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });
            });
        }

        [Fact]
        public async Task ExecuteAsync_WithRetryableFailure_RetriesOperation()
        {
            // Arrange
            var retryOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(10),
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var retryModule = new ResilienceModule(retryOptions, _mockLogger.Object);
            var attemptCount = 0;

            // Act
            var result = await retryModule.ExecuteAsync(async ct =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(3, attemptCount); // Initial attempt + 2 retries
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public void UpdateConfiguration_WithNewOptions_UpdatesConfiguration()
        {
            // Arrange
            var newOptions = new ResilienceOptions
            {
                Enabled = false,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            // Act
            _resilienceModule.UpdateConfiguration(newOptions);

            // Assert
            Assert.Equal(newOptions.Enabled, _resilienceModule.Options.Enabled);
            Assert.Equal(newOptions.RateLimiting.Enabled, _resilienceModule.Options.RateLimiting.Enabled);
        }

        [Fact]
        public void UpdateConfiguration_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _resilienceModule.UpdateConfiguration(null!));
        }

        [Fact]
        public async Task GetMetrics_AfterOperations_ReturnsMetrics()
        {
            // Arrange
            await _resilienceModule.ExecuteAsync(async ct =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Act
            var metrics = _resilienceModule.GetMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.Count > 0);
            Assert.Contains("global.requests.success", metrics.Keys);
        }

        [Fact]
        public void Reset_ClearsAllState()
        {
            // Arrange
            _resilienceModule.CreateEndpointPipeline("test-endpoint");
            
            // Act
            _resilienceModule.Reset();
            var metrics = _resilienceModule.GetMetrics();

            // Assert
            Assert.Empty(metrics);
        }

        [Fact]
        public void ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _resilienceModule.ExecuteAsync(null!);
            });
        }

        [Fact]
        public void ExecuteAsync_WithEndpointAndNullOperation_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _resilienceModule.ExecuteAsync("test-endpoint", null!);
            });
        }

        [Fact]
        public async Task ExecuteAsync_WithHttpRequestException_HandledByRetryPolicy()
        {
            // Arrange
            var retryOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromMilliseconds(10),
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var retryModule = new ResilienceModule(retryOptions, _mockLogger.Object);
            var attemptCount = 0;

            // Act
            var result = await retryModule.ExecuteAsync(async ct =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new HttpRequestException("Network error");
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(2, attemptCount); // Initial attempt + 1 retry
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task ExecuteAsync_WithTaskCanceledException_HandledByRetryPolicy()
        {
            // Arrange
            var retryOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromMilliseconds(10),
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var retryModule = new ResilienceModule(retryOptions, _mockLogger.Object);
            var attemptCount = 0;

            // Act
            var result = await retryModule.ExecuteAsync(async ct =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new TaskCanceledException("Request canceled");
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(2, attemptCount); // Initial attempt + 1 retry
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task ExecuteAsync_WithTransientHttpFailure_HandledByRetryPolicy()
        {
            // Arrange
            var retryOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromMilliseconds(10),
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var retryModule = new ResilienceModule(retryOptions, _mockLogger.Object);
            var attemptCount = 0;

            // Act
            var result = await retryModule.ExecuteAsync(async ct =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(2, attemptCount); // Initial attempt + 1 retry
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public void Dispose_DisposesResourcesCorrectly()
        {
            // Arrange
            var module = new ResilienceModule(_defaultOptions, _mockLogger.Object);

            // Act
            module.Dispose();

            // Assert - Should not throw when accessing disposed module
            Assert.Throws<ObjectDisposedException>(() => module.GetMetrics());
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var module = new ResilienceModule(_defaultOptions, _mockLogger.Object);

            // Act & Assert
            module.Dispose();
            module.Dispose(); // Should not throw
        }

        [Fact]
        public async Task ExecuteAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var module = new ResilienceModule(_defaultOptions, _mockLogger.Object);
            module.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await module.ExecuteAsync(async ct => new HttpResponseMessage(HttpStatusCode.OK));
            });
        }

        public void Dispose()
        {
            _resilienceModule?.Dispose();
        }
    }
} 