using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Xunit;

namespace Prophy.ApiClient.Tests.Authentication
{
    public class ApiKeyAuthenticatorTests
    {
        private readonly Mock<ILogger<ApiKeyAuthenticator>> _mockLogger;
        private const string TestApiKey = "test-api-key-123";
        private const string TestOrgCode = "test-org";

        public ApiKeyAuthenticatorTests()
        {
            _mockLogger = new Mock<ILogger<ApiKeyAuthenticator>>();
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Act
            var authenticator = new ApiKeyAuthenticator(TestApiKey, TestOrgCode, _mockLogger.Object);

            // Assert
            Assert.Equal(TestApiKey, authenticator.ApiKey);
            Assert.Equal(TestOrgCode, authenticator.OrganizationCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidApiKey_ThrowsArgumentException(string? invalidApiKey)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ApiKeyAuthenticator(invalidApiKey!, TestOrgCode, _mockLogger.Object));
            
            Assert.Equal("API key cannot be null or empty. (Parameter 'apiKey')", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidOrganizationCode_ThrowsArgumentException(string? invalidOrgCode)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ApiKeyAuthenticator(TestApiKey, invalidOrgCode!, _mockLogger.Object));
            
            Assert.Equal("Organization code cannot be null or empty. (Parameter 'organizationCode')", exception.Message);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new ApiKeyAuthenticator(TestApiKey, TestOrgCode, null!));
            
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void AuthenticateRequest_WithValidRequest_AddsApiKeyHeader()
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator(TestApiKey, TestOrgCode, _mockLogger.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

            // Act
            authenticator.AuthenticateRequest(request);

            // Assert
            Assert.True(request.Headers.Contains("X-ApiKey"));
            var headerValues = request.Headers.GetValues("X-ApiKey");
            Assert.Single(headerValues);
            Assert.Equal(TestApiKey, headerValues.First());
        }

        [Fact]
        public void AuthenticateRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator(TestApiKey, TestOrgCode, _mockLogger.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                authenticator.AuthenticateRequest(null!));
            
            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public void AuthenticateRequest_CalledMultipleTimes_AddsHeaderEachTime()
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator(TestApiKey, TestOrgCode, _mockLogger.Object);
            var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test1");
            var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/test2");

            // Act
            authenticator.AuthenticateRequest(request1);
            authenticator.AuthenticateRequest(request2);

            // Assert
            Assert.True(request1.Headers.Contains("X-ApiKey"));
            Assert.True(request2.Headers.Contains("X-ApiKey"));
            
            var headerValues1 = request1.Headers.GetValues("X-ApiKey");
            var headerValues2 = request2.Headers.GetValues("X-ApiKey");
            
            Assert.Single(headerValues1);
            Assert.Single(headerValues2);
            Assert.Equal(TestApiKey, headerValues1.First());
            Assert.Equal(TestApiKey, headerValues2.First());
        }
    }
} 