using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Tests.MultiTenancy
{
    public class TenantAwareApiKeyAuthenticatorTests
    {
        private readonly Mock<IOrganizationContextProvider> _mockContextProvider;
        private readonly Mock<ITenantConfigurationProvider> _mockConfigurationProvider;
        private readonly Mock<ILogger<TenantAwareApiKeyAuthenticator>> _mockLogger;
        private readonly TenantAwareApiKeyAuthenticator _authenticator;

        public TenantAwareApiKeyAuthenticatorTests()
        {
            _mockContextProvider = new Mock<IOrganizationContextProvider>();
            _mockConfigurationProvider = new Mock<ITenantConfigurationProvider>();
            _mockLogger = new Mock<ILogger<TenantAwareApiKeyAuthenticator>>();
            
            _authenticator = new TenantAwareApiKeyAuthenticator(
                _mockContextProvider.Object,
                _mockConfigurationProvider.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void ApiKey_WithCurrentContext_ReturnsContextApiKey()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Org", "test-api-key", "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);

            // Act
            var apiKey = _authenticator.ApiKey;

            // Assert
            Assert.Equal("test-api-key", apiKey);
        }

        [Fact]
        public void ApiKey_WithNoContext_ReturnsNull()
        {
            // Arrange
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns((OrganizationContext?)null);

            // Act
            var apiKey = _authenticator.ApiKey;

            // Assert
            Assert.Null(apiKey);
        }

        [Fact]
        public void OrganizationCode_WithCurrentContext_ReturnsContextOrganizationCode()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", "api-key", "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);

            // Act
            var organizationCode = _authenticator.OrganizationCode;

            // Assert
            Assert.Equal("TEST_ORG", organizationCode);
        }

        [Fact]
        public void OrganizationCode_WithNoContext_ReturnsNull()
        {
            // Arrange
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns((OrganizationContext?)null);

            // Act
            var organizationCode = _authenticator.OrganizationCode;

            // Assert
            Assert.Null(organizationCode);
        }

        [Fact]
        public void SetApiKey_LogsWarning()
        {
            // Act
            _authenticator.SetApiKey("new-api-key");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SetApiKey called on TenantAwareApiKeyAuthenticator")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SetOrganizationCode_LogsWarning()
        {
            // Act
            _authenticator.SetOrganizationCode("NEW_ORG");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SetOrganizationCode called on TenantAwareApiKeyAuthenticator")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void ClearApiKey_ClearsCurrentContext()
        {
            // Act
            _authenticator.ClearApiKey();

            // Assert
            _mockContextProvider.Verify(x => x.ClearCurrentContext(), Times.Once);
        }

        [Fact]
        public void AuthenticateRequest_WithValidContext_AddsHeaders()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", "test-api-key", "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");

            // Act
            _authenticator.AuthenticateRequest(request);

            // Assert
            Assert.True(request.Headers.Contains("X-ApiKey"));
            Assert.True(request.Headers.Contains("X-Organization-Code"));
            Assert.Equal("test-api-key", request.Headers.GetValues("X-ApiKey").First());
            Assert.Equal("TEST_ORG", request.Headers.GetValues("X-Organization-Code").First());
        }

        [Fact]
        public void AuthenticateRequest_WithNoContext_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns((OrganizationContext?)null);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _authenticator.AuthenticateRequest(request));
            Assert.Contains("No tenant context available", exception.Message);
        }

        [Fact]
        public void AuthenticateRequest_WithNullApiKey_ThrowsInvalidOperationException()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", null, "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _authenticator.AuthenticateRequest(request));
            Assert.Contains("No API key configured for organization: TEST_ORG", exception.Message);
        }

        [Fact]
        public void AuthenticateRequest_WithEmptyApiKey_ThrowsInvalidOperationException()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", "", "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _authenticator.AuthenticateRequest(request));
            Assert.Contains("No API key configured for organization: TEST_ORG", exception.Message);
        }

        [Fact]
        public void AuthenticateRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _authenticator.AuthenticateRequest(null!));
        }

        [Fact]
        public void AuthenticateRequest_ReplacesExistingHeaders()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", "new-api-key", "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");
            request.Headers.Add("X-ApiKey", "old-api-key");
            request.Headers.Add("X-Organization-Code", "OLD_ORG");

            // Act
            _authenticator.AuthenticateRequest(request);

            // Assert
            Assert.Equal("new-api-key", request.Headers.GetValues("X-ApiKey").Single());
            Assert.Equal("TEST_ORG", request.Headers.GetValues("X-Organization-Code").Single());
        }

        [Fact]
        public void AuthenticateRequest_LogsDebugMessage()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", "test-api-key", "https://api.test.com");
            _mockContextProvider.Setup(x => x.GetCurrentContext()).Returns(context);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");

            // Act
            _authenticator.AuthenticateRequest(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Added authentication headers for organization: TEST_ORG")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
} 