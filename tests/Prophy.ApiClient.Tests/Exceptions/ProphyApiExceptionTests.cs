using System;
using System.Net;
using Prophy.ApiClient.Exceptions;
using Xunit;

namespace Prophy.ApiClient.Tests.Exceptions
{
    public class ProphyApiExceptionTests
    {
        private const string TestMessage = "Test error message";
        private const string TestErrorCode = "TEST_ERROR";
        private const string TestErrorDetails = "Additional error details";
        private const string TestRequestId = "req-123456";

        [Fact]
        public void Constructor_WithMessageAndErrorCode_SetsProperties()
        {
            // Act
            var exception = new ProphyApiException(TestMessage, TestErrorCode);

            // Assert
            Assert.Equal(TestMessage, exception.Message);
            Assert.Equal(TestErrorCode, exception.ErrorCode);
            Assert.Null(exception.HttpStatusCode);
            Assert.Null(exception.ErrorDetails);
            Assert.Null(exception.RequestId);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithMessageErrorCodeAndInnerException_SetsProperties()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ProphyApiException(TestMessage, TestErrorCode, innerException);

            // Assert
            Assert.Equal(TestMessage, exception.Message);
            Assert.Equal(TestErrorCode, exception.ErrorCode);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Null(exception.HttpStatusCode);
            Assert.Null(exception.ErrorDetails);
            Assert.Null(exception.RequestId);
        }

        [Fact]
        public void Constructor_WithHttpDetails_SetsAllProperties()
        {
            // Act
            var exception = new ProphyApiException(TestMessage, TestErrorCode, HttpStatusCode.BadRequest, TestErrorDetails, TestRequestId);

            // Assert
            Assert.Equal(TestMessage, exception.Message);
            Assert.Equal(TestErrorCode, exception.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest, exception.HttpStatusCode);
            Assert.Equal(TestErrorDetails, exception.ErrorDetails);
            Assert.Equal(TestRequestId, exception.RequestId);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithHttpDetailsAndInnerException_SetsAllProperties()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ProphyApiException(TestMessage, TestErrorCode, HttpStatusCode.InternalServerError, innerException, TestErrorDetails, TestRequestId);

            // Assert
            Assert.Equal(TestMessage, exception.Message);
            Assert.Equal(TestErrorCode, exception.ErrorCode);
            Assert.Equal(HttpStatusCode.InternalServerError, exception.HttpStatusCode);
            Assert.Equal(TestErrorDetails, exception.ErrorDetails);
            Assert.Equal(TestRequestId, exception.RequestId);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_WithNullOrEmptyErrorCode_ThrowsArgumentNullException(string? errorCode)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new ProphyApiException(TestMessage, errorCode!));
            
            Assert.Equal("errorCode", exception.ParamName);
        }

        [Fact]
        public void ToString_WithMinimalInfo_ReturnsFormattedString()
        {
            // Arrange
            var exception = new ProphyApiException(TestMessage, TestErrorCode);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("ProphyApiException", result);
            Assert.Contains(TestMessage, result);
            Assert.Contains($"ErrorCode: {TestErrorCode}", result);
        }

        [Fact]
        public void ToString_WithHttpStatusCode_IncludesStatusCode()
        {
            // Arrange
            var exception = new ProphyApiException(TestMessage, TestErrorCode, HttpStatusCode.NotFound);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("HttpStatusCode: NotFound", result);
        }

        [Fact]
        public void ToString_WithRequestId_IncludesRequestId()
        {
            // Arrange
            var exception = new ProphyApiException(TestMessage, TestErrorCode, HttpStatusCode.BadRequest, null, TestRequestId);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains($"RequestId: {TestRequestId}", result);
        }

        [Fact]
        public void ToString_WithErrorDetails_IncludesDetails()
        {
            // Arrange
            var exception = new ProphyApiException(TestMessage, TestErrorCode, HttpStatusCode.BadRequest, TestErrorDetails);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains($"Details: {TestErrorDetails}", result);
        }

        [Fact]
        public void ToString_WithInnerException_IncludesInnerException()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");
            var exception = new ProphyApiException(TestMessage, TestErrorCode, innerException);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("Inner Exception:", result);
            Assert.Contains("Inner error", result);
        }

        [Fact]
        public void ToString_WithAllProperties_IncludesAllInformation()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");
            var exception = new ProphyApiException(TestMessage, TestErrorCode, HttpStatusCode.InternalServerError, innerException, TestErrorDetails, TestRequestId);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains("ProphyApiException", result);
            Assert.Contains(TestMessage, result);
            Assert.Contains($"ErrorCode: {TestErrorCode}", result);
            Assert.Contains("HttpStatusCode: InternalServerError", result);
            Assert.Contains($"RequestId: {TestRequestId}", result);
            Assert.Contains($"Details: {TestErrorDetails}", result);
            Assert.Contains("Inner Exception:", result);
        }
    }
} 