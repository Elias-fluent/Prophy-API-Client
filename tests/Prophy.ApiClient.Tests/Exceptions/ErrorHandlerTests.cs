using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Exceptions;
using Xunit;

namespace Prophy.ApiClient.Tests.Exceptions
{
    public class ErrorHandlerTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly HttpClient _httpClient;

        public ErrorHandlerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task HandleResponseAsync_WithSuccessStatusCode_DoesNotThrow()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            // Act & Assert
            await ErrorHandler.HandleResponseAsync(response, _mockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task HandleResponseAsync_WithUnauthorized_ThrowsAuthenticationException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"message\":\"Invalid API key\"}", Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("AUTH_ERROR", exception.ErrorCode);
            Assert.Equal(HttpStatusCode.Unauthorized, exception.HttpStatusCode);
        }

        [Fact]
        public async Task HandleResponseAsync_WithBadRequestAndValidationErrors_ThrowsValidationException()
        {
            // Arrange
            var errorResponse = new
            {
                message = "Validation failed",
                validationErrors = new[] { "Email is required", "Name is too short" }
            };
            var json = JsonSerializer.Serialize(errorResponse);
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Contains("Email is required", exception.ValidationErrors);
            Assert.Contains("Name is too short", exception.ValidationErrors);
        }

        [Fact]
        public async Task HandleResponseAsync_WithRateLimitExceeded_ThrowsRateLimitException()
        {
            // Arrange
            var errorResponse = new
            {
                message = "Rate limit exceeded",
                retryAfterSeconds = 60,
                remainingRequests = 0,
                requestLimit = 100
            };
            var json = JsonSerializer.Serialize(errorResponse);
            var response = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("RATE_LIMIT_EXCEEDED", exception.ErrorCode);
            Assert.NotNull(exception.RetryAfter);
            Assert.Equal(0, exception.RemainingRequests);
            Assert.Equal(100, exception.RequestLimit);
        }

        [Fact]
        public async Task HandleResponseAsync_WithRequestTimeout_ThrowsApiTimeoutException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent("{\"message\":\"Request timeout\"}", Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApiTimeoutException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("REQUEST_TIMEOUT", exception.ErrorCode);
            Assert.Equal(TimeSpan.FromSeconds(30), exception.Timeout);
        }

        [Fact]
        public async Task HandleResponseAsync_WithRequestIdHeader_IncludesRequestId()
        {
            // Arrange
            var requestId = "req-123456";
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            response.Headers.Add("X-Request-ID", requestId);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal(requestId, exception.RequestId);
        }

        [Fact]
        public void CreateExceptionFromResponse_WithUnknownStatusCode_ReturnsGenericException()
        {
            // Act
            var exception = ErrorHandler.CreateExceptionFromResponse(HttpStatusCode.NotImplemented, null, null, null);

            // Assert
            Assert.IsType<ProphyApiException>(exception);
            Assert.Equal("HTTP_ERROR", exception.ErrorCode);
            Assert.Equal(HttpStatusCode.NotImplemented, exception.HttpStatusCode);
        }

        [Fact]
        public void CreateExceptionFromResponse_WithForbidden_ReturnsAuthenticationException()
        {
            // Act
            var exception = ErrorHandler.CreateExceptionFromResponse(HttpStatusCode.Forbidden, null, null, null);

            // Assert
            Assert.IsType<AuthenticationException>(exception);
            Assert.Equal("AUTH_ERROR", exception.ErrorCode);
        }

        [Fact]
        public void CreateExceptionFromResponse_WithInternalServerError_ReturnsGenericException()
        {
            // Act
            var exception = ErrorHandler.CreateExceptionFromResponse(HttpStatusCode.InternalServerError, null, null, null);

            // Assert
            Assert.IsType<ProphyApiException>(exception);
            Assert.Equal("INTERNAL_SERVER_ERROR", exception.ErrorCode);
        }

        [Fact]
        public void HandleTimeoutException_CreatesApiTimeoutException()
        {
            // Arrange
            var originalException = new TaskCanceledException("Operation was canceled");
            var timeout = TimeSpan.FromSeconds(30);

            // Act
            var exception = ErrorHandler.HandleTimeoutException(originalException, timeout, _mockLogger.Object);

            // Assert
            Assert.Equal("REQUEST_TIMEOUT", exception.ErrorCode);
            Assert.Equal(timeout, exception.Timeout);
            Assert.Equal(originalException, exception.InnerException);
            Assert.Contains("30.0 seconds", exception.Message);
        }

        [Fact]
        public void HandleSerializationException_CreatesSerializationException()
        {
            // Arrange
            var originalException = new JsonException("Invalid JSON");
            var targetType = typeof(string);
            var jsonContent = "{invalid json}";

            // Act
            var exception = ErrorHandler.HandleSerializationException(originalException, targetType, jsonContent, _mockLogger.Object);

            // Assert
            Assert.Equal("SERIALIZATION_ERROR", exception.ErrorCode);
            Assert.Equal(targetType, exception.TargetType);
            Assert.Equal(jsonContent, exception.JsonContent);
            Assert.Equal(originalException, exception.InnerException);
            Assert.Contains("String", exception.Message);
        }

        [Fact]
        public void HandleSerializationException_WithNullType_HandlesGracefully()
        {
            // Arrange
            var originalException = new JsonException("Invalid JSON");

            // Act
            var exception = ErrorHandler.HandleSerializationException(originalException, null, null, _mockLogger.Object);

            // Assert
            Assert.Equal("SERIALIZATION_ERROR", exception.ErrorCode);
            Assert.Null(exception.TargetType);
            Assert.Null(exception.JsonContent);
            Assert.Contains("object", exception.Message);
        }

        [Fact]
        public async Task HandleResponseAsync_WithMalformedJson_HandlesGracefully()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{invalid json}", Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
        }

        [Fact]
        public async Task HandleResponseAsync_WithEmptyContent_HandlesGracefully()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("INTERNAL_SERVER_ERROR", exception.ErrorCode);
        }

        [Fact]
        public async Task HandleResponseAsync_LogsErrorDetails()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\":\"Test error\"}", Encoding.UTF8, "application/json")
            };

            // Act
            await Assert.ThrowsAsync<ValidationException>(() => 
                ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API request failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 