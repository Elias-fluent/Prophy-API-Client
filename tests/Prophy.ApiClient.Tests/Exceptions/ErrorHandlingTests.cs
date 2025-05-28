using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.Exceptions
{
    /// <summary>
    /// Comprehensive tests for error handling and exception management.
    /// </summary>
    public class ErrorHandlingTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public ErrorHandlingTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        #region ProphyApiException Tests

        [Fact]
        public void ProphyApiException_WithBasicParameters_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Test error message";
            var errorCode = "TEST_ERROR";

            // Act
            var exception = new ProphyApiException(message, errorCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Null(exception.RequestId);
            Assert.Null(exception.HttpStatusCode);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void ProphyApiException_WithAllParameters_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Test error message";
            var errorCode = "TEST_ERROR";
            var statusCode = HttpStatusCode.BadRequest;
            var errorDetails = "Detailed error information";
            var requestId = "req-123";

            // Act
            var exception = new ProphyApiException(message, errorCode, statusCode, errorDetails, requestId);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(requestId, exception.RequestId);
            Assert.Equal(statusCode, exception.HttpStatusCode);
            Assert.Equal(errorDetails, exception.ErrorDetails);
        }

        [Fact]
        public void ProphyApiException_ToString_ShouldIncludeAllRelevantInformation()
        {
            // Arrange
            var exception = new ProphyApiException("Test error", "TEST_ERROR", HttpStatusCode.BadRequest, "Details", "req-123");

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("Test error", result);
            Assert.Contains("TEST_ERROR", result);
            Assert.Contains("req-123", result);
            Assert.Contains("BadRequest", result);
        }

        #endregion

        #region ValidationException Tests

        [Fact]
        public void ValidationException_WithValidationErrors_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Validation failed";
            var validationErrors = new List<string> { "Field1 is required", "Field2 is invalid" };

            // Act
            var exception = new ValidationException(message, validationErrors);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Contains("Field1 is required", exception.ValidationErrors);
            Assert.Contains("Field2 is invalid", exception.ValidationErrors);
        }

        [Fact]
        public void ValidationException_WithNullValidationErrors_ShouldThrowArgumentNullException()
        {
            // Arrange
            var message = "Validation failed";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationException(message, (IEnumerable<string>)null!));
        }

        [Fact]
        public void ValidationException_WithInnerException_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Validation failed";
            var validationErrors = new List<string> { "Field1 is required" };
            var innerException = new ArgumentException("Inner error");

            // Act
            var exception = new ValidationException(message, validationErrors, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
            Assert.Single(exception.ValidationErrors);
            Assert.Equal(innerException, exception.InnerException);
        }

        #endregion

        #region AuthenticationException Tests

        [Fact]
        public void AuthenticationException_WithBasicParameters_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Authentication failed";

            // Act
            var exception = new AuthenticationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("AUTH_ERROR", exception.ErrorCode);
        }

        [Fact]
        public void AuthenticationException_WithInnerException_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Authentication failed";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new AuthenticationException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("AUTH_ERROR", exception.ErrorCode);
            Assert.Equal(innerException, exception.InnerException);
        }

        #endregion

        #region SerializationException Tests

        [Fact]
        public void SerializationException_WithBasicParameters_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Serialization failed";

            // Act
            var exception = new SerializationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("SERIALIZATION_ERROR", exception.ErrorCode);
        }

        [Fact]
        public void SerializationException_WithInnerException_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Serialization failed";
            var targetType = typeof(string);
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new SerializationException(message, targetType, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("SERIALIZATION_ERROR", exception.ErrorCode);
            Assert.Equal(targetType, exception.TargetType);
            Assert.Equal(innerException, exception.InnerException);
        }

        #endregion

        #region ApiTimeoutException Tests

        [Fact]
        public void ApiTimeoutException_WithBasicParameters_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Request timed out";
            var timeout = TimeSpan.FromSeconds(30);

            // Act
            var exception = new ApiTimeoutException(message, timeout);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("REQUEST_TIMEOUT", exception.ErrorCode);
            Assert.Equal(timeout, exception.Timeout);
        }

        [Fact]
        public void ApiTimeoutException_WithInnerException_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Request timed out";
            var timeout = TimeSpan.FromSeconds(30);
            var innerException = new TaskCanceledException("Inner timeout");

            // Act
            var exception = new ApiTimeoutException(message, timeout, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("REQUEST_TIMEOUT", exception.ErrorCode);
            Assert.Equal(timeout, exception.Timeout);
            Assert.Equal(innerException, exception.InnerException);
        }

        #endregion

        #region RateLimitException Tests

        [Fact]
        public void RateLimitException_WithBasicParameters_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Rate limit exceeded";
            var retryAfter = DateTimeOffset.UtcNow.AddMinutes(5);

            // Act
            var exception = new RateLimitException(message, retryAfter);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("RATE_LIMIT_EXCEEDED", exception.ErrorCode);
            Assert.Equal(retryAfter, exception.RetryAfter);
        }

        [Fact]
        public void RateLimitException_WithInnerException_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var message = "Rate limit exceeded";
            var retryAfter = DateTimeOffset.UtcNow.AddMinutes(5);
            var innerException = new HttpRequestException("Inner error");

            // Act
            var exception = new RateLimitException(message, retryAfter, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("RATE_LIMIT_EXCEEDED", exception.ErrorCode);
            Assert.Equal(retryAfter, exception.RetryAfter);
            Assert.Equal(innerException, exception.InnerException);
        }

        #endregion

        #region ErrorHandler Tests

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithSuccessResponse_ShouldNotThrow()
        {
            // Arrange
            var response = TestHelpers.CreateSuccessResponse();

            // Act & Assert
            await ErrorHandler.HandleResponseAsync(response, _mockLogger.Object);
            // Should not throw any exception
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithBadRequestResponse_ShouldThrowValidationException()
        {
            // Arrange
            var errorContent = "{\"message\": \"Validation failed\", \"validationErrors\": [\"Field1 is required\"]}";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.BadRequest, errorContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains("Validation failed", exception.Message);
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithUnauthorizedResponse_ShouldThrowAuthenticationException()
        {
            // Arrange
            var errorContent = "{\"message\": \"Invalid API key\"}";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.Unauthorized, errorContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains("Invalid API key", exception.Message);
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithTimeoutResponse_ShouldThrowApiTimeoutException()
        {
            // Arrange
            var errorContent = "{\"message\": \"Request timeout\"}";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.RequestTimeout, errorContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApiTimeoutException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains("Request timeout", exception.Message);
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithTooManyRequestsResponse_ShouldThrowRateLimitException()
        {
            // Arrange
            var errorContent = "{\"message\": \"Rate limit exceeded\"}";
            var response = TestHelpers.CreateErrorResponse((HttpStatusCode)429, errorContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains("Rate limit exceeded", exception.Message);
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithInternalServerErrorResponse_ShouldThrowProphyApiException()
        {
            // Arrange
            var errorContent = "{\"message\": \"Internal server error\"}";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.InternalServerError, errorContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains("Internal server error", exception.Message);
            Assert.Equal(HttpStatusCode.InternalServerError, exception.HttpStatusCode);
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithMalformedJsonResponse_ShouldThrowValidationException()
        {
            // Arrange
            var malformedJson = "{invalid json content";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.BadRequest, malformedJson);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            // Should still create exception even with malformed JSON
            Assert.NotNull(exception);
        }

        #endregion

        #region Error Response Parsing Tests

        [Theory]
        [InlineData("{\"message\": \"Test error\"}", "Test error")]
        public async Task ErrorHandler_ParseErrorResponse_WithMessageField_ShouldExtractCorrectMessage(string jsonContent, string expectedMessage)
        {
            // Arrange
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.BadRequest, jsonContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task ErrorHandler_ParseErrorResponse_WithValidationDetails_ShouldIncludeAllErrors()
        {
            // Arrange
            var jsonContent = @"{
                ""message"": ""Validation failed"",
                ""validationErrors"": [
                    ""Field1 is required"",
                    ""Field2 must be a valid email""
                ]
            }";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.BadRequest, jsonContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Contains("Field1 is required", exception.ValidationErrors);
            Assert.Contains("Field2 must be a valid email", exception.ValidationErrors);
        }

        [Fact]
        public async Task ErrorHandler_ParseErrorResponse_WithRequestId_ShouldIncludeRequestId()
        {
            // Arrange
            var jsonContent = "{\"message\": \"Test error\"}";
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.BadRequest, jsonContent);
            response.Headers.Add("X-Request-ID", "req-12345");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Equal("req-12345", exception.RequestId);
        }

        #endregion

        #region Static Method Tests

        [Fact]
        public void ErrorHandler_CreateExceptionFromResponse_WithBadRequest_ShouldCreateValidationException()
        {
            // Arrange
            var errorDetails = new ErrorDetails { Message = "Validation failed" };

            // Act
            var exception = ErrorHandler.CreateExceptionFromResponse(HttpStatusCode.BadRequest, errorDetails, "req-123", "raw content");

            // Assert
            Assert.IsType<ValidationException>(exception);
            Assert.Equal("Validation failed", exception.Message);
            Assert.Equal("req-123", exception.RequestId);
        }

        [Fact]
        public void ErrorHandler_CreateExceptionFromResponse_WithUnauthorized_ShouldCreateAuthenticationException()
        {
            // Arrange
            var errorDetails = new ErrorDetails { Message = "Invalid API key" };

            // Act
            var exception = ErrorHandler.CreateExceptionFromResponse(HttpStatusCode.Unauthorized, errorDetails, null, null);

            // Assert
            Assert.IsType<AuthenticationException>(exception);
            Assert.Equal("Invalid API key", exception.Message);
        }

        [Fact]
        public void ErrorHandler_HandleTimeoutException_ShouldCreateApiTimeoutException()
        {
            // Arrange
            var taskCanceledException = new TaskCanceledException("Operation was cancelled");
            var timeout = TimeSpan.FromMinutes(2);

            // Act
            var exception = ErrorHandler.HandleTimeoutException(taskCanceledException, timeout, _mockLogger.Object);

            // Assert
            Assert.IsType<ApiTimeoutException>(exception);
            Assert.Equal(timeout, exception.Timeout);
            Assert.Equal(taskCanceledException, exception.InnerException);
        }

        [Fact]
        public void ErrorHandler_HandleSerializationException_ShouldCreateSerializationException()
        {
            // Arrange
            var originalException = new Exception("JSON parsing failed");
            var targetType = typeof(List<string>);
            var jsonContent = "{invalid json}";

            // Act
            var exception = ErrorHandler.HandleSerializationException(originalException, targetType, jsonContent, _mockLogger.Object);

            // Assert
            Assert.IsType<SerializationException>(exception);
            Assert.Equal(targetType, exception.TargetType);
            Assert.Equal(jsonContent, exception.JsonContent);
            Assert.Equal(originalException, exception.InnerException);
            Assert.Contains("List`1", exception.Message);
        }

        #endregion

        #region Edge Cases and Error Scenarios

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithNullResponse_ShouldThrowNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                () => ErrorHandler.HandleResponseAsync(null!, _mockLogger.Object));
        }

        [Fact]
        public async Task ErrorHandler_HandleResponseAsync_WithEmptyContent_ShouldUseDefaultErrorMessage()
        {
            // Arrange
            var response = TestHelpers.CreateErrorResponse(HttpStatusCode.BadRequest, "");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => ErrorHandler.HandleResponseAsync(response, _mockLogger.Object));
            
            Assert.Contains("request was invalid", exception.Message);
        }

        [Fact]
        public void ErrorHandler_CreateExceptionFromResponse_WithNullErrorDetails_ShouldUseDefaultMessage()
        {
            // Act
            var exception = ErrorHandler.CreateExceptionFromResponse(HttpStatusCode.BadRequest, null, null, null);

            // Assert
            Assert.Equal("The request was invalid. Please check your request parameters.", exception.Message);
        }

        #endregion
    }
} 