using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Http;
using Xunit;

namespace Prophy.ApiClient.Tests
{
    public class ProphyApiClientTests : IDisposable
    {
        private readonly Mock<ILogger<ProphyApiClient>> _mockLogger;
        private const string TestApiKey = "test-api-key-123";
        private const string TestOrgCode = "test-org";
        private const string TestBaseUrl = "https://api.test.com/";

        public ProphyApiClientTests()
        {
            _mockLogger = new Mock<ILogger<ProphyApiClient>>();
        }

        [Fact]
        public void Constructor_WithApiKeyAndOrgCode_InitializesCorrectly()
        {
            // Act
            using var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);

            // Assert
            Assert.Equal(TestOrgCode, client.OrganizationCode);
            Assert.Equal(new Uri("https://www.prophy.ai/api/"), client.BaseUrl);
        }

        [Fact]
        public void Constructor_WithCustomBaseUrl_SetsCorrectBaseUrl()
        {
            // Act
            using var client = new ProphyApiClient(TestApiKey, TestOrgCode, TestBaseUrl, _mockLogger.Object);

            // Assert
            Assert.Equal(TestOrgCode, client.OrganizationCode);
            Assert.Equal(new Uri(TestBaseUrl), client.BaseUrl);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidApiKey_ThrowsArgumentException(string? invalidApiKey)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ProphyApiClient(invalidApiKey!, TestOrgCode, logger: _mockLogger.Object));
            
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
                new ProphyApiClient(TestApiKey, invalidOrgCode!, logger: _mockLogger.Object));
            
            Assert.Equal("Organization code cannot be null or empty. (Parameter 'organizationCode')", exception.Message);
        }

        [Fact]
        public void Constructor_WithCustomHttpClient_UsesProvidedClient()
        {
            // Arrange
            var customHttpClient = new HttpClient();
            customHttpClient.BaseAddress = new Uri(TestBaseUrl);

            // Act
            using var client = new ProphyApiClient(TestApiKey, TestOrgCode, customHttpClient, false, _mockLogger.Object);

            // Assert
            Assert.Equal(TestOrgCode, client.OrganizationCode);
            Assert.Equal(new Uri(TestBaseUrl), client.BaseUrl);
            
            // Cleanup
            customHttpClient.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            using var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: null);
            
            Assert.Equal(TestOrgCode, client.OrganizationCode);
        }

        [Fact]
        public void GetHttpClient_ReturnsHttpClientWrapper()
        {
            // Arrange
            using var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);

            // Act
            var httpClient = client.GetHttpClient();

            // Assert
            Assert.NotNull(httpClient);
            Assert.IsAssignableFrom<IHttpClientWrapper>(httpClient);
        }

        [Fact]
        public void GetAuthenticator_ReturnsApiKeyAuthenticator()
        {
            // Arrange
            using var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);

            // Act
            var authenticator = client.GetAuthenticator();

            // Assert
            Assert.NotNull(authenticator);
            Assert.IsAssignableFrom<IApiKeyAuthenticator>(authenticator);
            Assert.Equal(TestApiKey, authenticator.ApiKey);
            Assert.Equal(TestOrgCode, authenticator.OrganizationCode);
        }

        [Fact]
        public void GetHttpClient_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);
            client.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => client.GetHttpClient());
        }

        [Fact]
        public void GetAuthenticator_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);
            client.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => client.GetAuthenticator());
        }

        [Fact]
        public void OrganizationCode_AfterDispose_DoesNotThrow()
        {
            // Arrange
            var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);
            client.Dispose();

            // Act & Assert - Should not throw since it's just a property access
            var orgCode = client.OrganizationCode;
            Assert.Equal(TestOrgCode, orgCode);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var client = new ProphyApiClient(TestApiKey, TestOrgCode, logger: _mockLogger.Object);

            // Act & Assert - Should not throw
            client.Dispose();
            client.Dispose();
            client.Dispose();
        }

        public void Dispose()
        {
            // Cleanup any resources if needed
        }
    }
} 