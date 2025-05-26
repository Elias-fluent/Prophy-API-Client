using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Http;
using Xunit;

namespace Prophy.ApiClient.Tests.Http
{
    public class LoggingHandlerTests : IDisposable
    {
        private readonly Mock<ILogger<LoggingHandler>> _mockLogger;
        private readonly TestHttpMessageHandler _testHandler;
        private readonly LoggingHandler _loggingHandler;
        private readonly HttpClient _httpClient;

        public LoggingHandlerTests()
        {
            _mockLogger = new Mock<ILogger<LoggingHandler>>();
            _testHandler = new TestHttpMessageHandler();
            _loggingHandler = new LoggingHandler(_mockLogger.Object)
            {
                InnerHandler = _testHandler
            };
            _httpClient = new HttpClient(_loggingHandler);
        }

        [Fact]
        public async Task SendAsync_LogsRequestAndResponse_WhenSuccessful()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Success response")
            };
            _testHandler.SetResponse(expectedResponse);

            // Act
            var response = await _httpClient.GetAsync("https://api.example.com/test");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            
            // Verify request logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GET request to https://api.example.com/test")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            // Verify response logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP response 200")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_LogsWarning_WhenRequestFails()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad request"),
                ReasonPhrase = "Bad Request"
            };
            _testHandler.SetResponse(expectedResponse);

            // Act
            var response = await _httpClient.GetAsync("https://api.example.com/test");

            // Assert
            Assert.False(response.IsSuccessStatusCode);
            
            // Verify warning logging for failed response
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP response 400")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_LogsException_WhenRequestThrows()
        {
            // Arrange
            _testHandler.SetException(new HttpRequestException("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => 
                _httpClient.GetAsync("https://api.example.com/test"));

            // Verify exception logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP request failed")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_RedactsSensitiveHeaders_InLogs()
        {
            // Arrange
            var options = new LoggingOptions { LogHeaders = true };
            var loggingHandler = new LoggingHandler(_mockLogger.Object, options)
            {
                InnerHandler = _testHandler
            };
            var client = new HttpClient(loggingHandler);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
            request.Headers.Add("Authorization", "Bearer secret-token");
            request.Headers.Add("X-ApiKey", "secret-api-key");
            request.Headers.Add("User-Agent", "TestClient/1.0");

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _testHandler.SetResponse(expectedResponse);

            // Enable debug logging
            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

            // Act
            await client.SendAsync(request);

            // Assert
            // Verify sensitive headers are redacted
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[REDACTED]") && 
                                                  !v.ToString().Contains("secret-token") && 
                                                  !v.ToString().Contains("secret-api-key")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            // Verify non-sensitive headers are not redacted
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TestClient/1.0")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_RedactsSensitiveDataInRequestBody_WhenEnabled()
        {
            // Arrange
            var options = new LoggingOptions { LogRequestBody = true };
            var loggingHandler = new LoggingHandler(_mockLogger.Object, options)
            {
                InnerHandler = _testHandler
            };
            var client = new HttpClient(loggingHandler);

            var requestBody = @"{
                ""username"": ""testuser"",
                ""password"": ""secret123"",
                ""api_key"": ""secret-key"",
                ""data"": ""normal data""
            }";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/login")
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _testHandler.SetResponse(expectedResponse);

            // Enable debug logging
            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

            // Act
            await client.SendAsync(request);

            // Assert
            // Verify sensitive data is redacted
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[REDACTED]") && 
                                                  !v.ToString().Contains("secret123") && 
                                                  !v.ToString().Contains("secret-key")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            // Verify non-sensitive data is preserved
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("normal data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_TruncatesLargeRequestBody_WhenExceedsMaxLength()
        {
            // Arrange
            var options = new LoggingOptions { LogRequestBody = true, MaxBodyLogLength = 50 };
            var loggingHandler = new LoggingHandler(_mockLogger.Object, options)
            {
                InnerHandler = _testHandler
            };
            var client = new HttpClient(loggingHandler);

            var largeBody = new string('A', 100); // 100 characters
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/test")
            {
                Content = new StringContent(largeBody, Encoding.UTF8, "text/plain")
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _testHandler.SetResponse(expectedResponse);

            // Enable debug logging
            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

            // Act
            await client.SendAsync(request);

            // Assert
            // Verify body is truncated
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[TRUNCATED]")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_LogsSlowRequest_WhenExceedsThreshold()
        {
            // Arrange
            var options = new LoggingOptions { SlowRequestThresholdMs = 100 };
            var loggingHandler = new LoggingHandler(_mockLogger.Object, options)
            {
                InnerHandler = _testHandler
            };
            var client = new HttpClient(loggingHandler);

            // Simulate slow response
            _testHandler.SetDelay(TimeSpan.FromMilliseconds(200));
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _testHandler.SetResponse(expectedResponse);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            // Verify slow request warning
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Slow HTTP request detected")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendAsync_HandlesBinaryContent_WithoutLoggingBody()
        {
            // Arrange
            var options = new LoggingOptions { LogRequestBody = true };
            var loggingHandler = new LoggingHandler(_mockLogger.Object, options)
            {
                InnerHandler = _testHandler
            };
            var client = new HttpClient(loggingHandler);

            var binaryData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/upload")
            {
                Content = new ByteArrayContent(binaryData)
            };
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _testHandler.SetResponse(expectedResponse);

            // Enable debug logging
            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

            // Act
            await client.SendAsync(request);

            // Assert
            // Verify binary content is identified and not logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[BINARY CONTENT: image/png]")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void LoggingOptions_HasCorrectDefaults()
        {
            // Arrange & Act
            var options = new LoggingOptions();

            // Assert
            Assert.False(options.LogRequests);
            Assert.False(options.LogResponses);
            Assert.True(options.LogHeaders);
            Assert.False(options.LogRequestBody);
            Assert.False(options.LogResponseBody);
            Assert.True(options.LogPerformanceMetrics);
            Assert.Equal(4096, options.MaxBodyLogLength);
            Assert.Equal(5000, options.SlowRequestThresholdMs);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _loggingHandler?.Dispose();
            _testHandler?.Dispose();
        }
    }

    /// <summary>
    /// Test HTTP message handler for unit testing.
    /// </summary>
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage? _response;
        private Exception? _exception;
        private TimeSpan _delay = TimeSpan.Zero;

        public void SetResponse(HttpResponseMessage response)
        {
            _response = response;
            _exception = null;
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
            _response = null;
        }

        public void SetDelay(TimeSpan delay)
        {
            _delay = delay;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_exception != null)
            {
                throw _exception;
            }

            return _response ?? new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _response?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 