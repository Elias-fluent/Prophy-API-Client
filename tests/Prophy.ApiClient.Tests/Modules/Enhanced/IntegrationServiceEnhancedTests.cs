using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Models.Webhooks;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.Modules.Enhanced
{
    /// <summary>
    /// Enhanced tests for integration services (Webhook and Resilience modules) using property-based testing.
    /// </summary>
    public class IntegrationServiceEnhancedTests : IDisposable
    {
        private readonly Mock<IWebhookValidator> _mockValidator;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly Mock<ILogger<WebhookModule>> _mockWebhookLogger;
        private readonly Mock<ILogger<ResilienceModule>> _mockResilienceLogger;
        private readonly WebhookModule _webhookModule;
        private ResilienceModule _resilienceModule;

        public IntegrationServiceEnhancedTests()
        {
            _mockValidator = new Mock<IWebhookValidator>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            _mockWebhookLogger = TestHelpers.CreateMockLogger<WebhookModule>();
            _mockResilienceLogger = TestHelpers.CreateMockLogger<ResilienceModule>();
            
            _webhookModule = new WebhookModule(
                _mockValidator.Object,
                _mockJsonSerializer.Object,
                _mockWebhookLogger.Object);

            var resilienceOptions = CreateTestResilienceOptions();
            _resilienceModule = new ResilienceModule(resilienceOptions, _mockResilienceLogger.Object);
        }

        private static ResilienceOptions CreateTestResilienceOptions()
        {
            return new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(1),
                    SegmentsPerWindow = 2,
                    QueueLimit = 5
                },
                CircuitBreaker = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureRatio = 0.5,
                    MinimumThroughput = 3,
                    SamplingDuration = TimeSpan.FromSeconds(5),
                    BreakDuration = TimeSpan.FromSeconds(1)
                },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(50),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false
                },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromSeconds(2)
                }
            };
        }

        #region Webhook Property-Based Tests

        [Property]
        public Property ValidateSignature_WithValidInputs_ShouldReturnConsistentResults()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (payload, signature, secret) =>
                {
                    // Arrange
                    var payloadStr = payload.Get;
                    var signatureStr = signature.Get;
                    var secretStr = secret.Get;

                    var expectedResult = payloadStr.GetHashCode() % 2 == 0; // Deterministic but varied
                    _mockValidator.Setup(v => v.ValidateSignature(payloadStr, signatureStr, secretStr))
                        .Returns(expectedResult);

                    // Act
                    var result1 = _webhookModule.ValidateSignature(payloadStr, signatureStr, secretStr);
                    var result2 = _webhookModule.ValidateSignature(payloadStr, signatureStr, secretStr);

                    // Assert - Should be consistent
                    return result1 == result2 && result1 == expectedResult;
                });
        }

        [Property]
        public Property ParsePayload_WithValidJson_ShouldNeverThrowForValidStructure()
        {
            return Prop.ForAll(
                Arb.From<WebhookEventType>(),
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (eventType, id, organization) =>
                {
                    // Arrange
                    var payload = CreateValidJsonPayload(eventType, id.Get, organization.Get);
                    var webhookPayload = CreateValidWebhookPayload(eventType, id.Get, organization.Get);

                    _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                        .Returns(webhookPayload);

                    // Act & Assert
                    try
                    {
                        var result = _webhookModule.ParsePayload(payload);
                        return result != null && result.EventType == eventType && result.Id == id.Get;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Resilience Property-Based Tests

        [Property]
        public Property ExecuteAsync_WithSuccessfulOperations_ShouldAlwaysSucceed()
        {
            return Prop.ForAll(
                Arb.From<PositiveInt>().Filter(x => x.Get <= 100), // Keep delays reasonable
                (delayMs) =>
                {
                    var delay = delayMs.Get;
                    
                    try
                    {
                        var result = _resilienceModule.ExecuteAsync(async ct =>
                        {
                            await Task.Delay(delay, ct);
                            return TestHelpers.CreateSuccessResponse();
                        }).Result;

                        return result.IsSuccessStatusCode;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property CreateEndpointPipeline_WithValidNames_ShouldCreatePipelines()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                (endpointName) =>
                {
                    // Clean the endpoint name to make it valid
                    var name = endpointName.Get
                        .Replace(" ", "-")
                        .Replace("/", "-")
                        .Replace("\\", "-");
                    
                    try
                    {
                        var pipeline = _resilienceModule.CreateEndpointPipeline(name);
                        return pipeline != null;
                    }
                    catch (ArgumentException)
                    {
                        // Invalid names are acceptable to reject
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Integration Scenarios

        [Fact]
        public async Task WebhookModule_ProcessWebhookAsync_WithValidPayload_ShouldSucceed()
        {
            // Arrange
            var payload = CreateValidJsonPayload(WebhookEventType.MarkAsProposalReferee, "test-id", "test-org");
            var signature = "valid-signature";
            var secret = "test-secret";
            var webhookPayload = CreateValidWebhookPayload();

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret)).Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload)).Returns(true);
            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload)).Returns(webhookPayload);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.SignatureValid);
            Assert.True(result.PayloadValid);
            Assert.NotNull(result.Payload);
        }

        [Fact]
        public async Task ResilienceModule_ExecuteAsync_WithSuccessfulOperation_ShouldReturnResponse()
        {
            // Arrange
            var expectedResponse = TestHelpers.CreateSuccessResponse();
            
            // Act
            var result = await _resilienceModule.ExecuteAsync(async ct =>
            {
                await Task.Delay(10, ct);
                return expectedResponse;
            });

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact]
        public async Task WebhookModule_ProcessWebhookAsync_WithInvalidSignature_ShouldReturnFailure()
        {
            // Arrange
            var payload = CreateValidJsonPayload(WebhookEventType.MarkAsProposalReferee, "test-id", "test-org");
            var signature = "invalid-signature";
            var secret = "test-secret";

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret)).Returns(false);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.False(result.Success);
            Assert.False(result.SignatureValid);
        }

        [Fact]
        public async Task ResilienceModule_ExecuteAsync_WithEndpointName_ShouldCreateEndpointPipeline()
        {
            // Arrange
            var endpointName = "test-endpoint";
            var expectedResponse = TestHelpers.CreateSuccessResponse();
            
            // Act
            var result = await _resilienceModule.ExecuteAsync(endpointName, async ct =>
            {
                await Task.Delay(10, ct);
                return expectedResponse;
            });

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.True(result.IsSuccessStatusCode);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task WebhookModule_ProcessWebhookAsync_WithMalformedJson_ShouldHandleGracefully()
        {
            // Arrange
            var payload = "{ invalid json structure }";
            var signature = "valid-signature";
            var secret = "test-secret";

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret)).Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload)).Returns(true);
            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Throws(new SerializationException("Invalid JSON", typeof(WebhookPayload)));

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.SignatureValid);
            Assert.False(result.PayloadValid);
            Assert.Null(result.Payload);
        }

        [Fact]
        public void ResilienceModule_CreateEndpointPipeline_WithNullEndpointName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _resilienceModule.CreateEndpointPipeline(null!));
        }

        [Fact]
        public void ResilienceModule_CreateEndpointPipeline_WithEmptyEndpointName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _resilienceModule.CreateEndpointPipeline(""));
        }

        [Theory]
        [InlineData(null, "signature", "secret")]
        [InlineData("", "signature", "secret")]
        [InlineData("payload", null, "secret")]
        [InlineData("payload", "", "secret")]
        [InlineData("payload", "signature", null)]
        [InlineData("payload", "signature", "")]
        public async Task WebhookModule_ProcessWebhookAsync_WithInvalidParameters_ShouldThrowArgumentException(
            string payload, string signature, string secret)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _webhookModule.ProcessWebhookAsync(payload, signature, secret));
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task IntegrationServices_ConcurrentOperations_ShouldHandleParallelism()
        {
            // Arrange
            var webhookTasks = new List<Task<WebhookProcessingResult>>();
            var resilienceTasks = new List<Task<HttpResponseMessage>>();

            // Setup webhook operations
            for (int i = 0; i < 3; i++)
            {
                var payload = CreateValidJsonPayload(WebhookEventType.MarkAsProposalReferee, $"test-id-{i}", "test-org");
                var signature = "valid-signature";
                var secret = "test-secret";

                _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret)).Returns(true);
                _mockValidator.Setup(v => v.ValidatePayloadStructure(payload)).Returns(true);
                _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                    .Returns(CreateValidWebhookPayload());

                webhookTasks.Add(_webhookModule.ProcessWebhookAsync(payload, signature, secret));
            }

            // Setup resilience operations
            for (int i = 0; i < 3; i++)
            {
                resilienceTasks.Add(_resilienceModule.ExecuteAsync(async ct =>
                {
                    await Task.Delay(50, ct);
                    return TestHelpers.CreateSuccessResponse();
                }));
            }

            // Act
            var webhookResults = await Task.WhenAll(webhookTasks);
            var resilienceResults = await Task.WhenAll(resilienceTasks);

            // Assert
            Assert.All(webhookResults, result => Assert.True(result.Success));
            Assert.All(resilienceResults, result => Assert.True(result.IsSuccessStatusCode));
        }

        #endregion

        #region Helper Methods

        private static string CreateValidJsonPayload(WebhookEventType eventType, string id = "test-id", string organization = "test-org")
        {
            return $@"{{
                ""id"": ""{id}"",
                ""event_type"": ""{eventType}"",
                ""timestamp"": ""{DateTime.UtcNow:O}"",
                ""organization"": ""{organization}"",
                ""data"": {{}}
            }}";
        }

        private static WebhookPayload CreateValidWebhookPayload(WebhookEventType eventType = WebhookEventType.MarkAsProposalReferee, string id = "test-id", string organization = "test-org")
        {
            return new WebhookPayload
            {
                Id = id,
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Organization = organization,
                Data = new Dictionary<string, object>()
            };
        }

        #endregion

        public void Dispose()
        {
            _resilienceModule?.Dispose();
        }
    }
} 