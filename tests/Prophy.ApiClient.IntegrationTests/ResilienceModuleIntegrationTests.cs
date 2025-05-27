using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Prophy.ApiClient.Modules;
using Polly.RateLimiting;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Prophy.ApiClient.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ResilienceModule with real HTTP scenarios.
    /// </summary>
    public class ResilienceModuleIntegrationTests : IDisposable
    {
        private readonly ILogger<ResilienceModule> _logger;
        private readonly HttpClient _httpClient;

        public ResilienceModuleIntegrationTests()
        {
            _logger = new LoggerFactory().CreateLogger<ResilienceModule>();
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task ResilienceModule_WithSuccessfulHttpRequest_CompletesSuccessfully()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromSeconds(30)
                }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act
            var response = await resilienceModule.ExecuteAsync("test-endpoint", async ct =>
            {
                // Simulate a successful HTTP request
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Success")
                };
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Success", content);
        }

        [Fact]
        public async Task ResilienceModule_WithRateLimitingEnabled_EnforcesRateLimit()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 2,
                    Window = TimeSpan.FromSeconds(5),
                    SegmentsPerWindow = 1,
                    QueueLimit = 0
                },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act - First two requests should succeed
            var response1 = await resilienceModule.ExecuteAsync("rate-limit-test", async ct =>
                new HttpResponseMessage(HttpStatusCode.OK));
            
            var response2 = await resilienceModule.ExecuteAsync("rate-limit-test", async ct =>
                new HttpResponseMessage(HttpStatusCode.OK));

            // Assert - First two requests succeed
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            // Act & Assert - Third request should be rate limited
            await Assert.ThrowsAsync<RateLimiterRejectedException>(async () =>
            {
                await resilienceModule.ExecuteAsync("rate-limit-test", async ct =>
                    new HttpResponseMessage(HttpStatusCode.OK));
            });
        }

        [Fact]
        public async Task ResilienceModule_WithCircuitBreakerEnabled_OpensOnFailures()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureRatio = 0.5,
                    MinimumThroughput = 3,
                    SamplingDuration = TimeSpan.FromSeconds(10),
                    BreakDuration = TimeSpan.FromSeconds(2)
                },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act - Generate failures to trip the circuit breaker
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await resilienceModule.ExecuteAsync("circuit-breaker-test", async ct =>
                        new HttpResponseMessage(HttpStatusCode.InternalServerError));
                }
                catch
                {
                    // Expected failures
                }
            }

            // Assert - Circuit should be open now, causing immediate failures
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            {
                await resilienceModule.ExecuteAsync("circuit-breaker-test", async ct =>
                    new HttpResponseMessage(HttpStatusCode.OK));
            });
        }

        [Fact]
        public async Task ResilienceModule_WithRetryEnabled_RetriesTransientFailures()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(100),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);
            var attemptCount = 0;

            // Act
            var response = await resilienceModule.ExecuteAsync("retry-test", async ct =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(3, attemptCount); // Initial + 2 retries
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResilienceModule_WithTimeoutEnabled_TimesOutLongOperations()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromMilliseconds(200)
                }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            {
                await resilienceModule.ExecuteAsync("timeout-test", async ct =>
                {
                    await Task.Delay(1000, ct); // Longer than timeout, use cancellation token
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });
            });
        }

        [Fact]
        public async Task ResilienceModule_WithAllPoliciesEnabled_WorksTogether()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                },
                CircuitBreaker = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureRatio = 0.8,
                    MinimumThroughput = 5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(5)
                },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(50),
                    BackoffType = DelayBackoffType.Linear,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromSeconds(5)
                }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act - Test successful operation with all policies
            var response = await resilienceModule.ExecuteAsync("combined-test", async ct =>
            {
                await Task.Delay(100, ct); // Within timeout
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResilienceModule_MetricsCollection_TracksOperations()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act - Perform successful operations
            await resilienceModule.ExecuteAsync("metrics-test", async ct =>
                new HttpResponseMessage(HttpStatusCode.OK));
            
            await resilienceModule.ExecuteAsync("metrics-test", async ct =>
                new HttpResponseMessage(HttpStatusCode.OK));

            // Try a failed operation
            try
            {
                await resilienceModule.ExecuteAsync("metrics-test", async ct =>
                    throw new HttpRequestException("Test failure"));
            }
            catch
            {
                // Expected
            }

            // Assert
            var metrics = resilienceModule.GetMetrics();
            Assert.NotEmpty(metrics);
            Assert.Contains("metrics-test.requests.success", metrics.Keys);
            Assert.Contains("metrics-test.requests.failure", metrics.Keys);
            Assert.Equal(2, metrics["metrics-test.requests.success"]);
            Assert.Equal(1, metrics["metrics-test.requests.failure"]);
        }

        [Fact]
        public void ResilienceModule_ConfigurationUpdate_UpdatesRuntimeBehavior()
        {
            // Arrange
            var initialOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var resilienceModule = new ResilienceModule(initialOptions, _logger);

            // Act - Update configuration
            var newOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(10)
                },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            resilienceModule.UpdateConfiguration(newOptions);

            // Assert
            Assert.Equal(newOptions.RateLimiting.Enabled, resilienceModule.Options.RateLimiting.Enabled);
            Assert.Equal(newOptions.RateLimiting.PermitLimit, resilienceModule.Options.RateLimiting.PermitLimit);
        }

        [Fact]
        public async Task ResilienceModule_EndpointSpecificPipelines_IsolateMetrics()
        {
            // Arrange
            var options = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions { Enabled = false },
                CircuitBreaker = new CircuitBreakerOptions { Enabled = false },
                Retry = new RetryOptions { Enabled = false },
                Timeout = new TimeoutOptions { Enabled = false }
            };

            using var resilienceModule = new ResilienceModule(options, _logger);

            // Act - Operations on different endpoints
            await resilienceModule.ExecuteAsync("endpoint-1", async ct =>
                new HttpResponseMessage(HttpStatusCode.OK));
            
            await resilienceModule.ExecuteAsync("endpoint-2", async ct =>
                new HttpResponseMessage(HttpStatusCode.OK));

            // Assert
            var metrics = resilienceModule.GetMetrics();
            Assert.Contains("endpoint-1.requests.success", metrics.Keys);
            Assert.Contains("endpoint-2.requests.success", metrics.Keys);
            Assert.Equal(1, metrics["endpoint-1.requests.success"]);
            Assert.Equal(1, metrics["endpoint-2.requests.success"]);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 