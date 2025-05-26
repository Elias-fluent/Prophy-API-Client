using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Diagnostics;
using Xunit;

namespace Prophy.ApiClient.Tests.Diagnostics
{
    public class DiagnosticEventsTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public DiagnosticEventsTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void ActivitySource_HasCorrectName()
        {
            // Assert
            Assert.Equal("Prophy.ApiClient", DiagnosticEvents.ActivitySourceName);
            Assert.Equal("Prophy.ApiClient", DiagnosticEvents.ActivitySource.Name);
        }

        [Fact]
        public void EventIds_HaveUniqueValues()
        {
            // Arrange
            var eventIds = new[]
            {
                DiagnosticEvents.EventIds.ClientInitialized,
                DiagnosticEvents.EventIds.ClientDisposed,
                DiagnosticEvents.EventIds.HttpRequestStarted,
                DiagnosticEvents.EventIds.HttpRequestCompleted,
                DiagnosticEvents.EventIds.HttpRequestFailed,
                DiagnosticEvents.EventIds.SlowHttpRequest,
                DiagnosticEvents.EventIds.AuthenticationStarted,
                DiagnosticEvents.EventIds.AuthenticationCompleted,
                DiagnosticEvents.EventIds.AuthenticationFailed,
                DiagnosticEvents.EventIds.JwtTokenGenerated,
                DiagnosticEvents.EventIds.SerializationStarted,
                DiagnosticEvents.EventIds.SerializationCompleted,
                DiagnosticEvents.EventIds.SerializationFailed,
                DiagnosticEvents.EventIds.DeserializationStarted,
                DiagnosticEvents.EventIds.DeserializationCompleted,
                DiagnosticEvents.EventIds.DeserializationFailed,
                DiagnosticEvents.EventIds.ManuscriptUploadStarted,
                DiagnosticEvents.EventIds.ManuscriptUploadCompleted,
                DiagnosticEvents.EventIds.ManuscriptUploadFailed,
                DiagnosticEvents.EventIds.WebhookProcessingStarted,
                DiagnosticEvents.EventIds.WebhookProcessingCompleted,
                DiagnosticEvents.EventIds.WebhookProcessingFailed,
                DiagnosticEvents.EventIds.PerformanceMetrics,
                DiagnosticEvents.EventIds.MemoryUsage,
                DiagnosticEvents.EventIds.CacheHit,
                DiagnosticEvents.EventIds.CacheMiss
            };

            var uniqueIds = new HashSet<int>();

            // Act & Assert
            foreach (var eventId in eventIds)
            {
                Assert.True(uniqueIds.Add(eventId.Id), $"Duplicate event ID found: {eventId.Id} ({eventId.Name})");
            }
        }

        [Fact]
        public void EventIds_AreInCorrectRanges()
        {
            // Assert client lifecycle events (1000-1099)
            Assert.InRange(DiagnosticEvents.EventIds.ClientInitialized.Id, 1000, 1099);
            Assert.InRange(DiagnosticEvents.EventIds.ClientDisposed.Id, 1000, 1099);

            // Assert HTTP request events (1100-1199)
            Assert.InRange(DiagnosticEvents.EventIds.HttpRequestStarted.Id, 1100, 1199);
            Assert.InRange(DiagnosticEvents.EventIds.HttpRequestCompleted.Id, 1100, 1199);
            Assert.InRange(DiagnosticEvents.EventIds.HttpRequestFailed.Id, 1100, 1199);
            Assert.InRange(DiagnosticEvents.EventIds.SlowHttpRequest.Id, 1100, 1199);

            // Assert authentication events (1200-1299)
            Assert.InRange(DiagnosticEvents.EventIds.AuthenticationStarted.Id, 1200, 1299);
            Assert.InRange(DiagnosticEvents.EventIds.AuthenticationCompleted.Id, 1200, 1299);
            Assert.InRange(DiagnosticEvents.EventIds.AuthenticationFailed.Id, 1200, 1299);
            Assert.InRange(DiagnosticEvents.EventIds.JwtTokenGenerated.Id, 1200, 1299);

            // Assert serialization events (1300-1399)
            Assert.InRange(DiagnosticEvents.EventIds.SerializationStarted.Id, 1300, 1399);
            Assert.InRange(DiagnosticEvents.EventIds.SerializationCompleted.Id, 1300, 1399);
            Assert.InRange(DiagnosticEvents.EventIds.SerializationFailed.Id, 1300, 1399);
            Assert.InRange(DiagnosticEvents.EventIds.DeserializationStarted.Id, 1300, 1399);
            Assert.InRange(DiagnosticEvents.EventIds.DeserializationCompleted.Id, 1300, 1399);
            Assert.InRange(DiagnosticEvents.EventIds.DeserializationFailed.Id, 1300, 1399);

            // Assert API operation events (1400-1499)
            Assert.InRange(DiagnosticEvents.EventIds.ManuscriptUploadStarted.Id, 1400, 1499);
            Assert.InRange(DiagnosticEvents.EventIds.ManuscriptUploadCompleted.Id, 1400, 1499);
            Assert.InRange(DiagnosticEvents.EventIds.ManuscriptUploadFailed.Id, 1400, 1499);
            Assert.InRange(DiagnosticEvents.EventIds.WebhookProcessingStarted.Id, 1400, 1499);
            Assert.InRange(DiagnosticEvents.EventIds.WebhookProcessingCompleted.Id, 1400, 1499);
            Assert.InRange(DiagnosticEvents.EventIds.WebhookProcessingFailed.Id, 1400, 1499);

            // Assert performance events (1500-1599)
            Assert.InRange(DiagnosticEvents.EventIds.PerformanceMetrics.Id, 1500, 1599);
            Assert.InRange(DiagnosticEvents.EventIds.MemoryUsage.Id, 1500, 1599);
            Assert.InRange(DiagnosticEvents.EventIds.CacheHit.Id, 1500, 1599);
            Assert.InRange(DiagnosticEvents.EventIds.CacheMiss.Id, 1500, 1599);
        }

        [Fact]
        public void Scopes_HttpRequest_CreatesCorrectScope()
        {
            // Arrange
            var method = "GET";
            var uri = "https://api.example.com/test";
            var requestId = "12345678";

            // Act
            using var scope = DiagnosticEvents.Scopes.HttpRequest(_mockLogger.Object, method, uri, requestId);

            // Assert
            _mockLogger.Verify(
                x => x.BeginScope(It.Is<Dictionary<string, object>>(d =>
                    d["HttpMethod"].ToString() == method &&
                    d["RequestUri"].ToString() == uri &&
                    d["RequestId"].ToString() == requestId)),
                Times.Once);
        }

        [Fact]
        public void Scopes_ApiOperation_CreatesCorrectScope()
        {
            // Arrange
            var operation = "ManuscriptUpload";
            var organizationCode = "test-org";

            // Act
            using var scope = DiagnosticEvents.Scopes.ApiOperation(_mockLogger.Object, operation, organizationCode);

            // Assert
            _mockLogger.Verify(
                x => x.BeginScope(It.Is<Dictionary<string, object>>(d =>
                    d["Operation"].ToString() == operation &&
                    d["OrganizationCode"].ToString() == organizationCode)),
                Times.Once);
        }

        [Fact]
        public void Scopes_ApiOperation_WithoutOrganizationCode_CreatesCorrectScope()
        {
            // Arrange
            var operation = "ManuscriptUpload";

            // Act
            using var scope = DiagnosticEvents.Scopes.ApiOperation(_mockLogger.Object, operation);

            // Assert
            _mockLogger.Verify(
                x => x.BeginScope(It.Is<Dictionary<string, object>>(d =>
                    d["Operation"].ToString() == operation &&
                    !d.ContainsKey("OrganizationCode"))),
                Times.Once);
        }

        [Fact]
        public void Scopes_Serialization_CreatesCorrectScope()
        {
            // Arrange
            var operation = "Serialize";
            var type = typeof(string);

            // Act
            using var scope = DiagnosticEvents.Scopes.Serialization(_mockLogger.Object, operation, type);

            // Assert
            _mockLogger.Verify(
                x => x.BeginScope(It.Is<Dictionary<string, object>>(d =>
                    d["SerializationOperation"].ToString() == operation &&
                    d["TargetType"].ToString() == type.Name)),
                Times.Once);
        }

        [Fact]
        public void Metrics_RecordHttpRequestDuration_UpdatesCounters()
        {
            // Arrange
            DiagnosticEvents.Metrics.Reset();
            var method = "GET";
            var endpoint = "/api/test";
            var duration = TimeSpan.FromMilliseconds(150);

            // Act
            DiagnosticEvents.Metrics.RecordHttpRequestDuration(method, endpoint, duration, true);
            DiagnosticEvents.Metrics.RecordHttpRequestDuration(method, endpoint, TimeSpan.FromMilliseconds(200), false);

            // Assert
            var counters = DiagnosticEvents.Metrics.GetCounters();
            Assert.True(counters.ContainsKey("http.request.duration.get.success"));
            Assert.True(counters.ContainsKey("http.request.duration.get.failure"));
            
            var successCounter = counters["http.request.duration.get.success"];
            Assert.Equal(1, successCounter.Count);
            Assert.Equal(150, successCounter.Average);
        }

        [Fact]
        public void Metrics_RecordSerializationDuration_UpdatesCounters()
        {
            // Arrange
            DiagnosticEvents.Metrics.Reset();
            var operation = "Serialize";
            var type = typeof(string);
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            DiagnosticEvents.Metrics.RecordSerializationDuration(operation, type, duration);

            // Assert
            var counters = DiagnosticEvents.Metrics.GetCounters();
            var key = "serialization.serialize.string";
            Assert.True(counters.ContainsKey(key));
            
            var counter = counters[key];
            Assert.Equal(1, counter.Count);
            Assert.Equal(50, counter.Average);
        }

        [Fact]
        public void Metrics_RecordApiOperationDuration_UpdatesCounters()
        {
            // Arrange
            DiagnosticEvents.Metrics.Reset();
            var operation = "ManuscriptUpload";
            var duration = TimeSpan.FromMilliseconds(1000);

            // Act
            DiagnosticEvents.Metrics.RecordApiOperationDuration(operation, duration, true);

            // Assert
            var counters = DiagnosticEvents.Metrics.GetCounters();
            var key = "api.operation.manuscriptupload.success";
            Assert.True(counters.ContainsKey(key));
            
            var counter = counters[key];
            Assert.Equal(1, counter.Count);
            Assert.Equal(1000, counter.Average);
        }

        [Fact]
        public void Metrics_IncrementCounter_UpdatesCount()
        {
            // Arrange
            DiagnosticEvents.Metrics.Reset();
            var counterName = "test.counter";

            // Act
            DiagnosticEvents.Metrics.IncrementCounter(counterName);
            DiagnosticEvents.Metrics.IncrementCounter(counterName);
            DiagnosticEvents.Metrics.IncrementCounter(counterName);

            // Assert
            var counters = DiagnosticEvents.Metrics.GetCounters();
            Assert.True(counters.ContainsKey(counterName));
            
            var counter = counters[counterName];
            Assert.Equal(3, counter.Count);
        }

        [Fact]
        public void Metrics_RecordValue_UpdatesStatistics()
        {
            // Arrange
            DiagnosticEvents.Metrics.Reset();
            var counterName = "test.values";

            // Act
            DiagnosticEvents.Metrics.RecordValue(counterName, 10);
            DiagnosticEvents.Metrics.RecordValue(counterName, 20);
            DiagnosticEvents.Metrics.RecordValue(counterName, 30);

            // Assert
            var counters = DiagnosticEvents.Metrics.GetCounters();
            Assert.True(counters.ContainsKey(counterName));
            
            var counter = counters[counterName];
            Assert.Equal(3, counter.Count);
            Assert.Equal(20, counter.Average);
            Assert.Equal(10, counter.Min);
            Assert.Equal(30, counter.Max);
            Assert.Equal(60, counter.Sum);
        }

        [Fact]
        public void PerformanceCounter_Increment_UpdatesCount()
        {
            // Arrange
            var counter = new DiagnosticEvents.PerformanceCounter();

            // Act
            counter.Increment();
            counter.Increment();

            // Assert
            Assert.Equal(2, counter.Count);
            Assert.Equal(0, counter.Average);
            Assert.Equal(0, counter.Min);
            Assert.Equal(0, counter.Max);
            Assert.Equal(0, counter.Sum);
        }

        [Fact]
        public void PerformanceCounter_RecordValue_UpdatesStatistics()
        {
            // Arrange
            var counter = new DiagnosticEvents.PerformanceCounter();

            // Act
            counter.RecordValue(5);
            counter.RecordValue(15);
            counter.RecordValue(10);

            // Assert
            Assert.Equal(3, counter.Count);
            Assert.Equal(10, counter.Average);
            Assert.Equal(5, counter.Min);
            Assert.Equal(15, counter.Max);
            Assert.Equal(30, counter.Sum);
        }

        [Fact]
        public void PerformanceCounter_ToString_ReturnsFormattedString()
        {
            // Arrange
            var counter = new DiagnosticEvents.PerformanceCounter();
            counter.RecordValue(10);
            counter.RecordValue(20);

            // Act
            var result = counter.ToString();

            // Assert
            Assert.Contains("Count: 2", result);
            Assert.Contains("Avg: 15.00", result);
            Assert.Contains("Min: 10.00", result);
            Assert.Contains("Max: 20.00", result);
        }

        [Fact]
        public void LoggerExtensions_LogClientInitialized_LogsCorrectly()
        {
            // Arrange
            var organizationCode = "test-org";
            var baseUrl = "https://api.example.com";

            // Act
            _mockLogger.Object.LogClientInitialized(organizationCode, baseUrl);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    DiagnosticEvents.EventIds.ClientInitialized,
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(organizationCode) && v.ToString().Contains(baseUrl)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LoggerExtensions_LogHttpRequestCompleted_LogsCorrectly()
        {
            // Arrange
            var method = "POST";
            var uri = "https://api.example.com/test";
            var requestId = "12345678";
            var statusCode = 201;
            var elapsed = TimeSpan.FromMilliseconds(150);

            // Act
            _mockLogger.Object.LogHttpRequestCompleted(method, uri, requestId, statusCode, elapsed);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    DiagnosticEvents.EventIds.HttpRequestCompleted,
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(method) && 
                                                  v.ToString().Contains(uri) && 
                                                  v.ToString().Contains(requestId) && 
                                                  v.ToString().Contains("201") &&
                                                  v.ToString().Contains("150.0")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LoggerExtensions_LogManuscriptUploadStarted_LogsCorrectly()
        {
            // Arrange
            var fileName = "test-manuscript.pdf";
            var fileSize = 1024L;

            // Act
            _mockLogger.Object.LogManuscriptUploadStarted(fileName, fileSize);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    DiagnosticEvents.EventIds.ManuscriptUploadStarted,
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(fileName) && v.ToString().Contains("1024")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LoggerExtensions_LogWebhookProcessingFailed_LogsCorrectly()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error");
            var eventType = "MarkAsReferee";
            var webhookId = "webhook-123";
            var elapsed = TimeSpan.FromMilliseconds(500);

            // Act
            _mockLogger.Object.LogWebhookProcessingFailed(exception, eventType, webhookId, elapsed);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    DiagnosticEvents.EventIds.WebhookProcessingFailed,
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(eventType) && 
                                                  v.ToString().Contains(webhookId) && 
                                                  v.ToString().Contains("500.0")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
} 