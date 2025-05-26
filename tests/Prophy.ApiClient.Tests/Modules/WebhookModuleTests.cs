using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Webhooks;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Modules
{
    public class WebhookModuleTests
    {
        private readonly Mock<IWebhookValidator> _mockValidator;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly Mock<ILogger<WebhookModule>> _mockLogger;
        private readonly WebhookModule _webhookModule;

        public WebhookModuleTests()
        {
            _mockValidator = new Mock<IWebhookValidator>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            _mockLogger = new Mock<ILogger<WebhookModule>>();
            
            _webhookModule = new WebhookModule(
                _mockValidator.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullValidator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new WebhookModule(null!, _mockJsonSerializer.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullJsonSerializer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new WebhookModule(_mockValidator.Object, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new WebhookModule(_mockValidator.Object, _mockJsonSerializer.Object, null!));
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithValidPayload_ReturnsSuccessResult()
        {
            // Arrange
            var payload = "{\"id\":\"test-id\",\"event_type\":\"MarkAsProposalReferee\",\"timestamp\":\"2023-01-01T00:00:00Z\",\"organization\":\"test-org\",\"data\":{}}";
            var signature = "test-signature";
            var secret = "test-secret";

            var webhookPayload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.MarkAsProposalReferee,
                Timestamp = DateTime.UtcNow,
                Organization = "test-org",
                Data = new Dictionary<string, object>()
            };

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret))
                .Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload))
                .Returns(true);
            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Returns(webhookPayload);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.SignatureValid);
            Assert.True(result.PayloadValid);
            Assert.Equal(webhookPayload, result.Payload);
            Assert.Equal(0, result.HandlersExecuted);
            Assert.Equal(0, result.HandlersFailed);
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithInvalidSignature_ReturnsFailureResult()
        {
            // Arrange
            var payload = "{\"test\":\"data\"}";
            var signature = "invalid-signature";
            var secret = "test-secret";

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret))
                .Returns(false);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.False(result.Success);
            Assert.False(result.SignatureValid);
            Assert.False(result.PayloadValid);
            Assert.Null(result.Payload);
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithInvalidPayloadStructure_ReturnsFailureResult()
        {
            // Arrange
            var payload = "{\"invalid\":\"structure\"}";
            var signature = "test-signature";
            var secret = "test-secret";

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret))
                .Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload))
                .Returns(false);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.SignatureValid);
            Assert.False(result.PayloadValid);
            Assert.Null(result.Payload);
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithMultipleSecrets_ValidatesAgainstAll()
        {
            // Arrange
            var payload = "{\"id\":\"test-id\",\"event_type\":\"MarkAsProposalReferee\",\"timestamp\":\"2023-01-01T00:00:00Z\",\"organization\":\"test-org\",\"data\":{}}";
            var signature = "test-signature";
            var secrets = new[] { "secret1", "secret2", "secret3" };

            var webhookPayload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.MarkAsProposalReferee,
                Timestamp = DateTime.UtcNow,
                Organization = "test-org",
                Data = new Dictionary<string, object>()
            };

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secrets))
                .Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload))
                .Returns(true);
            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Returns(webhookPayload);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secrets);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.SignatureValid);
            Assert.True(result.PayloadValid);
            _mockValidator.Verify(v => v.ValidateSignature(payload, signature, secrets), Times.Once);
        }

        [Fact]
        public void ParsePayload_WithValidJson_ReturnsWebhookPayload()
        {
            // Arrange
            var payload = "{\"id\":\"test-id\",\"event_type\":\"MarkAsProposalReferee\"}";
            var expectedPayload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.MarkAsProposalReferee
            };

            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Returns(expectedPayload);

            // Act
            var result = _webhookModule.ParsePayload(payload);

            // Assert
            Assert.Equal(expectedPayload, result);
        }

        [Fact]
        public void ParsePayload_WithInvalidJson_ThrowsSerializationException()
        {
            // Arrange
            var payload = "invalid json";

            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Throws(new JsonException("Invalid JSON"));

            // Act & Assert
            Assert.Throws<SerializationException>(() => _webhookModule.ParsePayload(payload));
        }

        [Fact]
        public void ParsePayload_WithNullPayload_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _webhookModule.ParsePayload(null!));
        }

        [Fact]
        public void ParsePayload_WithEmptyPayload_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _webhookModule.ParsePayload(string.Empty));
        }

        [Fact]
        public void ExtractEventData_WithValidPayload_ReturnsTypedEventData()
        {
            // Arrange
            var payload = new WebhookPayload
            {
                Data = new Dictionary<string, object>
                {
                    ["manuscript_id"] = "test-manuscript",
                    ["referee"] = new { name = "Test Referee" }
                }
            };

            var expectedEventData = new MarkAsRefereeEvent
            {
                ManuscriptId = "test-manuscript"
            };

            var dataJson = "{\"manuscript_id\":\"test-manuscript\",\"referee\":{\"name\":\"Test Referee\"}}";
            _mockJsonSerializer.Setup(s => s.Serialize(payload.Data))
                .Returns(dataJson);
            _mockJsonSerializer.Setup(s => s.Deserialize<MarkAsRefereeEvent>(dataJson))
                .Returns(expectedEventData);

            // Act
            var result = _webhookModule.ExtractEventData<MarkAsRefereeEvent>(payload);

            // Assert
            Assert.Equal(expectedEventData, result);
        }

        [Fact]
        public void ExtractEventData_WithNullPayload_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _webhookModule.ExtractEventData<MarkAsRefereeEvent>(null!));
        }

        [Fact]
        public void RegisterHandler_WithGenericHandler_AddsToHandlerCollection()
        {
            // Arrange
            var mockHandler = new Mock<IWebhookEventHandler>();
            mockHandler.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);

            // Act
            _webhookModule.RegisterHandler(mockHandler.Object);

            // Assert
            var handlers = _webhookModule.GetHandlers(WebhookEventType.MarkAsProposalReferee);
            Assert.Contains(mockHandler.Object, handlers);
        }

        [Fact]
        public void RegisterHandler_WithTypedHandler_AddsToTypedHandlerCollection()
        {
            // Arrange
            var mockHandler = new Mock<IWebhookEventHandler<MarkAsRefereeEvent>>();

            // Act
            _webhookModule.RegisterHandler(WebhookEventType.MarkAsProposalReferee, mockHandler.Object);

            // Assert - No direct way to verify typed handlers, but we can test through processing
            Assert.True(true); // This test verifies the method doesn't throw
        }

        [Fact]
        public void RegisterHandler_WithNullGenericHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _webhookModule.RegisterHandler((IWebhookEventHandler)null!));
        }

        [Fact]
        public void RegisterHandler_WithNullTypedHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _webhookModule.RegisterHandler<MarkAsRefereeEvent>(WebhookEventType.MarkAsProposalReferee, null!));
        }

        [Fact]
        public void UnregisterHandlers_RemovesAllHandlersForEventType()
        {
            // Arrange
            var mockHandler1 = new Mock<IWebhookEventHandler>();
            var mockHandler2 = new Mock<IWebhookEventHandler>();
            mockHandler1.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);
            mockHandler2.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);

            _webhookModule.RegisterHandler(mockHandler1.Object);
            _webhookModule.RegisterHandler(mockHandler2.Object);

            // Act
            _webhookModule.UnregisterHandlers(WebhookEventType.MarkAsProposalReferee);

            // Assert
            var handlers = _webhookModule.GetHandlers(WebhookEventType.MarkAsProposalReferee);
            Assert.Empty(handlers);
        }

        [Fact]
        public void UnregisterHandler_RemovesSpecificHandler()
        {
            // Arrange
            var mockHandler1 = new Mock<IWebhookEventHandler>();
            var mockHandler2 = new Mock<IWebhookEventHandler>();
            mockHandler1.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);
            mockHandler2.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);

            _webhookModule.RegisterHandler(mockHandler1.Object);
            _webhookModule.RegisterHandler(mockHandler2.Object);

            // Act
            _webhookModule.UnregisterHandler(mockHandler1.Object);

            // Assert
            var handlers = _webhookModule.GetHandlers(WebhookEventType.MarkAsProposalReferee);
            Assert.DoesNotContain(mockHandler1.Object, handlers);
            Assert.Contains(mockHandler2.Object, handlers);
        }

        [Fact]
        public void GetHandlers_WithNoRegisteredHandlers_ReturnsEmptyCollection()
        {
            // Act
            var handlers = _webhookModule.GetHandlers(WebhookEventType.MarkAsProposalReferee);

            // Assert
            Assert.Empty(handlers);
        }

        [Fact]
        public void ValidateSignature_CallsValidatorValidateSignature()
        {
            // Arrange
            var payload = "test-payload";
            var signature = "test-signature";
            var secret = "test-secret";

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret))
                .Returns(true);

            // Act
            var result = _webhookModule.ValidateSignature(payload, signature, secret);

            // Assert
            Assert.True(result);
            _mockValidator.Verify(v => v.ValidateSignature(payload, signature, secret), Times.Once);
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithRegisteredGenericHandler_ExecutesHandler()
        {
            // Arrange
            var payload = "{\"id\":\"test-id\",\"event_type\":\"MarkAsProposalReferee\",\"timestamp\":\"2023-01-01T00:00:00Z\",\"organization\":\"test-org\",\"data\":{}}";
            var signature = "test-signature";
            var secret = "test-secret";

            var webhookPayload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.MarkAsProposalReferee,
                Timestamp = DateTime.UtcNow,
                Organization = "test-org",
                Data = new Dictionary<string, object>()
            };

            var mockHandler = new Mock<IWebhookEventHandler>();
            mockHandler.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<WebhookPayload>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret))
                .Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload))
                .Returns(true);
            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Returns(webhookPayload);

            _webhookModule.RegisterHandler(mockHandler.Object);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.HandlersExecuted);
            Assert.Equal(0, result.HandlersFailed);
            mockHandler.Verify(h => h.HandleAsync(webhookPayload, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithFailingHandler_RecordsFailure()
        {
            // Arrange
            var payload = "{\"id\":\"test-id\",\"event_type\":\"MarkAsProposalReferee\",\"timestamp\":\"2023-01-01T00:00:00Z\",\"organization\":\"test-org\",\"data\":{}}";
            var signature = "test-signature";
            var secret = "test-secret";

            var webhookPayload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.MarkAsProposalReferee,
                Timestamp = DateTime.UtcNow,
                Organization = "test-org",
                Data = new Dictionary<string, object>()
            };

            var mockHandler = new Mock<IWebhookEventHandler>();
            mockHandler.Setup(h => h.EventType).Returns(WebhookEventType.MarkAsProposalReferee);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<WebhookPayload>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Handler failed"));

            _mockValidator.Setup(v => v.ValidateSignature(payload, signature, secret))
                .Returns(true);
            _mockValidator.Setup(v => v.ValidatePayloadStructure(payload))
                .Returns(true);
            _mockJsonSerializer.Setup(s => s.Deserialize<WebhookPayload>(payload))
                .Returns(webhookPayload);

            _webhookModule.RegisterHandler(mockHandler.Object);

            // Act
            var result = await _webhookModule.ProcessWebhookAsync(payload, signature, secret);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.HandlersExecuted);
            Assert.Equal(1, result.HandlersFailed);
            Assert.Single(result.Errors);
            Assert.True(result.HasErrors);
        }

        [Theory]
        [InlineData(null, "signature", "secret")]
        [InlineData("", "signature", "secret")]
        [InlineData("payload", null, "secret")]
        [InlineData("payload", "", "secret")]
        [InlineData("payload", "signature", null)]
        [InlineData("payload", "signature", "")]
        public async Task ProcessWebhookAsync_WithInvalidParameters_ThrowsArgumentException(
            string payload, string signature, string secret)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _webhookModule.ProcessWebhookAsync(payload, signature, secret));
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithMultipleSecretsNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _webhookModule.ProcessWebhookAsync("payload", "signature", (IEnumerable<string>)null!));
        }

        [Fact]
        public async Task ProcessWebhookAsync_WithEmptySecrets_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _webhookModule.ProcessWebhookAsync("payload", "signature", new string[0]));
        }

        [Fact]
        public void ExtractEventData_WithRefereeStatusUpdatedEvent_ReturnsTypedEventData()
        {
            // Arrange
            var eventData = new RefereeStatusUpdatedEvent
            {
                ManuscriptId = "manuscript-123",
                ManuscriptTitle = "Test Manuscript",
                Referee = new RefereeCandidate 
                { 
                    Id = "referee-456", 
                    Author = new Author { Name = "Dr. Smith" }
                },
                PreviousStatus = "pending",
                NewStatus = "accepted",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin@test.com",
                Reason = "Referee accepted invitation"
            };

            var payload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.RefereeStatusUpdated,
                Data = new Dictionary<string, object>
                {
                    ["manuscript_id"] = eventData.ManuscriptId,
                    ["manuscript_title"] = eventData.ManuscriptTitle,
                    ["referee"] = eventData.Referee,
                    ["previous_status"] = eventData.PreviousStatus,
                    ["new_status"] = eventData.NewStatus,
                    ["updated_at"] = eventData.UpdatedAt,
                    ["updated_by"] = eventData.UpdatedBy,
                    ["reason"] = eventData.Reason
                }
            };

            _mockJsonSerializer.Setup(s => s.Deserialize<RefereeStatusUpdatedEvent>(It.IsAny<string>()))
                .Returns(eventData);

            // Act
            var result = _webhookModule.ExtractEventData<RefereeStatusUpdatedEvent>(payload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventData.ManuscriptId, result.ManuscriptId);
            Assert.Equal(eventData.NewStatus, result.NewStatus);
        }

        [Fact]
        public void ExtractEventData_WithManuscriptUploadedEvent_ReturnsTypedEventData()
        {
            // Arrange
            var eventData = new ManuscriptUploadedEvent
            {
                ManuscriptId = "manuscript-789",
                ManuscriptTitle = "New Research Paper",
                Abstract = "This is a test abstract",
                UploadedAt = DateTime.UtcNow,
                UploadedBy = "author@test.com",
                FileName = "research-paper.pdf",
                FileSize = 1024000,
                FileType = "application/pdf",
                InitialStatus = "uploaded"
            };

            var payload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.ManuscriptUploaded,
                Data = new Dictionary<string, object>
                {
                    ["manuscript_id"] = eventData.ManuscriptId,
                    ["manuscript_title"] = eventData.ManuscriptTitle,
                    ["abstract"] = eventData.Abstract,
                    ["uploaded_at"] = eventData.UploadedAt,
                    ["uploaded_by"] = eventData.UploadedBy,
                    ["file_name"] = eventData.FileName,
                    ["file_size"] = eventData.FileSize,
                    ["file_type"] = eventData.FileType,
                    ["initial_status"] = eventData.InitialStatus
                }
            };

            _mockJsonSerializer.Setup(s => s.Deserialize<ManuscriptUploadedEvent>(It.IsAny<string>()))
                .Returns(eventData);

            // Act
            var result = _webhookModule.ExtractEventData<ManuscriptUploadedEvent>(payload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventData.ManuscriptId, result.ManuscriptId);
            Assert.Equal(eventData.FileName, result.FileName);
            Assert.Equal(eventData.FileSize, result.FileSize);
        }

        [Fact]
        public void ExtractEventData_WithRefereeRecommendationsGeneratedEvent_ReturnsTypedEventData()
        {
            // Arrange
            var eventData = new RefereeRecommendationsGeneratedEvent
            {
                ManuscriptId = "manuscript-101",
                ManuscriptTitle = "AI Research Paper",
                GeneratedAt = DateTime.UtcNow,
                RequestedBy = "editor@test.com",
                TotalRecommendations = 10,
                HighQualityCount = 7,
                ProcessingTimeMs = 5000,
                MinRelevanceScore = 0.8,
                MaxRecommendations = 15,
                ConflictOfInterestFiltering = true,
                ConflictOfInterestExcludedCount = 3,
                IsAutomatic = true
            };

            var payload = new WebhookPayload
            {
                Id = "test-id",
                EventType = WebhookEventType.RefereeRecommendationsGenerated,
                Data = new Dictionary<string, object>
                {
                    ["manuscript_id"] = eventData.ManuscriptId,
                    ["manuscript_title"] = eventData.ManuscriptTitle,
                    ["generated_at"] = eventData.GeneratedAt,
                    ["requested_by"] = eventData.RequestedBy,
                    ["total_recommendations"] = eventData.TotalRecommendations,
                    ["high_quality_count"] = eventData.HighQualityCount,
                    ["processing_time_ms"] = eventData.ProcessingTimeMs,
                    ["min_relevance_score"] = eventData.MinRelevanceScore,
                    ["max_recommendations"] = eventData.MaxRecommendations,
                    ["coi_filtering"] = eventData.ConflictOfInterestFiltering,
                    ["coi_excluded_count"] = eventData.ConflictOfInterestExcludedCount,
                    ["automatic"] = eventData.IsAutomatic
                }
            };

            _mockJsonSerializer.Setup(s => s.Deserialize<RefereeRecommendationsGeneratedEvent>(It.IsAny<string>()))
                .Returns(eventData);

            // Act
            var result = _webhookModule.ExtractEventData<RefereeRecommendationsGeneratedEvent>(payload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventData.ManuscriptId, result.ManuscriptId);
            Assert.Equal(eventData.TotalRecommendations, result.TotalRecommendations);
            Assert.Equal(eventData.HighQualityCount, result.HighQualityCount);
        }
    }
} 