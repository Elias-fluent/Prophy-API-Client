using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.MultiTenancy;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.MultiTenancy
{
    /// <summary>
    /// Unit tests for the TenantConfigurationProvider class.
    /// </summary>
    public class TenantConfigurationProviderTests
    {
        private readonly Mock<IOrganizationContextProvider> _mockContextProvider;
        private readonly Mock<IProphyApiClientConfiguration> _mockDefaultConfiguration;
        private readonly Mock<ILogger<TenantConfigurationProvider>> _mockLogger;
        private readonly TenantConfigurationProvider _provider;

        public TenantConfigurationProviderTests()
        {
            _mockContextProvider = new Mock<IOrganizationContextProvider>();
            _mockDefaultConfiguration = new Mock<IProphyApiClientConfiguration>();
            _mockLogger = TestHelpers.CreateMockLogger<TenantConfigurationProvider>();

            // Setup default configuration
            _mockDefaultConfiguration.Setup(x => x.ApiKey).Returns("default-api-key");
            _mockDefaultConfiguration.Setup(x => x.BaseUrl).Returns("https://api.prophy.ai");
            _mockDefaultConfiguration.Setup(x => x.TimeoutSeconds).Returns(30);
            _mockDefaultConfiguration.Setup(x => x.MaxRetryAttempts).Returns(3);
            _mockDefaultConfiguration.Setup(x => x.RetryDelayMilliseconds).Returns(1000);
            _mockDefaultConfiguration.Setup(x => x.EnableDetailedLogging).Returns(false);
            _mockDefaultConfiguration.Setup(x => x.MaxFileSize).Returns(10485760L); // 10MB
            _mockDefaultConfiguration.Setup(x => x.ValidateSslCertificates).Returns(true);
            _mockDefaultConfiguration.Setup(x => x.UserAgent).Returns("Prophy-ApiClient/1.0");
            _mockDefaultConfiguration.Setup(x => x.IsValid).Returns(true);

            _provider = new TenantConfigurationProvider(
                _mockContextProvider.Object,
                _mockDefaultConfiguration.Object,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            var provider = new TenantConfigurationProvider(
                _mockContextProvider.Object,
                _mockDefaultConfiguration.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void Constructor_WithNullContextProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TenantConfigurationProvider(
                    null!,
                    _mockDefaultConfiguration.Object,
                    _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullDefaultConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TenantConfigurationProvider(
                    _mockContextProvider.Object,
                    null!,
                    _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TenantConfigurationProvider(
                    _mockContextProvider.Object,
                    _mockDefaultConfiguration.Object,
                    null!));
        }

        #endregion

        #region GetConfigurationAsync Tests

        [Fact]
        public async Task GetConfigurationAsync_WithNoCurrentContext_ShouldReturnDefaultConfiguration()
        {
            // Arrange
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns((OrganizationContext?)null);

            // Act
            var result = await _provider.GetConfigurationAsync();

            // Assert
            Assert.Equal(_mockDefaultConfiguration.Object, result);
            _mockContextProvider.Verify(x => x.GetCurrentContext(), Times.Once);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithCurrentContext_ShouldReturnTenantConfiguration()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns(context);
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result = await _provider.GetConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("tenant-api-key", result.ApiKey);
            Assert.Equal("https://tenant.api.prophy.ai", result.BaseUrl);
            Assert.Equal(organizationCode, result.OrganizationCode);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithOrganizationCode_ShouldReturnTenantConfiguration()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result = await _provider.GetConfigurationAsync(organizationCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("tenant-api-key", result.ApiKey);
            Assert.Equal("https://tenant.api.prophy.ai", result.BaseUrl);
            Assert.Equal(organizationCode, result.OrganizationCode);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithNullOrganizationCode_ShouldReturnDefaultConfiguration()
        {
            // Act
            var result1 = await _provider.GetConfigurationAsync((string)null!);
            var result2 = await _provider.GetConfigurationAsync(string.Empty);
            var result3 = await _provider.GetConfigurationAsync("   ");

            // Assert
            Assert.Equal(_mockDefaultConfiguration.Object, result1);
            Assert.Equal(_mockDefaultConfiguration.Object, result2);
            Assert.Equal(_mockDefaultConfiguration.Object, result3);
        }

        [Fact]
        public async Task GetConfigurationAsync_ShouldCacheConfiguration()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result1 = await _provider.GetConfigurationAsync(organizationCode);
            var result2 = await _provider.GetConfigurationAsync(organizationCode);

            // Assert
            Assert.Same(result1, result2);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
        }

        [Fact]
        public async Task GetConfigurationAsync_WhenExceptionThrown_ShouldReturnDefaultConfiguration()
        {
            // Arrange
            var organizationCode = "test-org";
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _provider.GetConfigurationAsync(organizationCode);

            // Assert
            Assert.Equal(_mockDefaultConfiguration.Object, result);
        }

        #endregion

        #region API Key Tests

        [Fact]
        public async Task GetApiKeyAsync_WithCurrentContext_ShouldReturnTenantApiKey()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns(context);
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result = await _provider.GetApiKeyAsync();

            // Assert
            Assert.Equal("tenant-api-key", result);
        }

        [Fact]
        public async Task GetApiKeyAsync_WithOrganizationCode_ShouldReturnTenantApiKey()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result = await _provider.GetApiKeyAsync(organizationCode);

            // Assert
            Assert.Equal("tenant-api-key", result);
        }

        [Fact]
        public async Task GetApiKeyAsync_WithNoContext_ShouldReturnDefaultApiKey()
        {
            // Arrange
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns((OrganizationContext?)null);

            // Act
            var result = await _provider.GetApiKeyAsync();

            // Assert
            Assert.Equal("default-api-key", result);
        }

        #endregion

        #region Base URL Tests

        [Fact]
        public async Task GetBaseUrlAsync_WithCurrentContext_ShouldReturnTenantBaseUrl()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns(context);
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result = await _provider.GetBaseUrlAsync();

            // Assert
            Assert.Equal("https://tenant.api.prophy.ai", result);
        }

        [Fact]
        public async Task GetBaseUrlAsync_WithOrganizationCode_ShouldReturnTenantBaseUrl()
        {
            // Arrange
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization", "tenant-api-key", "https://tenant.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result = await _provider.GetBaseUrlAsync(organizationCode);

            // Assert
            Assert.Equal("https://tenant.api.prophy.ai", result);
        }

        [Fact]
        public async Task GetBaseUrlAsync_WithNoContext_ShouldReturnDefaultBaseUrl()
        {
            // Arrange
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns((OrganizationContext?)null);

            // Act
            var result = await _provider.GetBaseUrlAsync();

            // Assert
            Assert.Equal("https://api.prophy.ai", result);
        }

        #endregion

        #region Set Configuration Tests

        [Fact]
        public async Task SetConfigurationAsync_WithValidParameters_ShouldUpdateConfiguration()
        {
            // Arrange
            var organizationCode = "test-org";
            var newConfiguration = new Mock<IProphyApiClientConfiguration>();
            newConfiguration.Setup(x => x.ApiKey).Returns("new-api-key");

            // Act
            await _provider.SetConfigurationAsync(organizationCode, newConfiguration.Object);

            // Verify configuration is cached
            var result = await _provider.GetConfigurationAsync(organizationCode);

            // Assert
            Assert.Equal(newConfiguration.Object, result);
        }

        [Fact]
        public async Task SetConfigurationAsync_WithNullOrganizationCode_ShouldThrowArgumentException()
        {
            // Arrange
            var configuration = new Mock<IProphyApiClientConfiguration>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetConfigurationAsync(null!, configuration.Object));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetConfigurationAsync(string.Empty, configuration.Object));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetConfigurationAsync("   ", configuration.Object));
        }

        [Fact]
        public async Task SetConfigurationAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _provider.SetConfigurationAsync("test-org", null!));
        }

        #endregion

        #region Set API Key Tests

        [Fact]
        public async Task SetApiKeyAsync_WithValidParameters_ShouldUpdateApiKey()
        {
            // Arrange
            var organizationCode = "test-org";
            var newApiKey = "new-api-key";
            var originalContext = new OrganizationContext(organizationCode, "Test Organization", "old-api-key", "https://tenant.api.prophy.ai");
            var updatedContext = originalContext.WithApiKey(newApiKey);

            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(originalContext);

            // Act
            await _provider.SetApiKeyAsync(organizationCode, newApiKey);

            // Assert
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
            _mockContextProvider.Verify(x => x.SetCurrentContext(It.Is<OrganizationContext>(c => c.ApiKey == newApiKey)), Times.Once);
        }

        [Fact]
        public async Task SetApiKeyAsync_WithNullOrganizationCode_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetApiKeyAsync(null!, "api-key"));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetApiKeyAsync(string.Empty, "api-key"));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetApiKeyAsync("   ", "api-key"));
        }

        [Fact]
        public async Task SetApiKeyAsync_WithNonExistentOrganization_ShouldNotUpdateContext()
        {
            // Arrange
            var organizationCode = "non-existent-org";
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync((OrganizationContext?)null);

            // Act
            await _provider.SetApiKeyAsync(organizationCode, "new-api-key");

            // Assert
            _mockContextProvider.Verify(x => x.SetCurrentContext(It.IsAny<OrganizationContext>()), Times.Never);
        }

        #endregion

        #region Set Base URL Tests

        [Fact]
        public async Task SetBaseUrlAsync_WithValidParameters_ShouldUpdateBaseUrl()
        {
            // Arrange
            var organizationCode = "test-org";
            var newBaseUrl = "https://new.api.prophy.ai";
            var originalContext = new OrganizationContext(organizationCode, "Test Organization", "api-key", "https://old.api.prophy.ai");

            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(originalContext);

            // Act
            await _provider.SetBaseUrlAsync(organizationCode, newBaseUrl);

            // Assert
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
            _mockContextProvider.Verify(x => x.SetCurrentContext(It.Is<OrganizationContext>(c => c.BaseUrl == newBaseUrl)), Times.Once);
        }

        [Fact]
        public async Task SetBaseUrlAsync_WithNullOrganizationCode_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetBaseUrlAsync(null!, "https://api.prophy.ai"));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetBaseUrlAsync(string.Empty, "https://api.prophy.ai"));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.SetBaseUrlAsync("   ", "https://api.prophy.ai"));
        }

        #endregion

        #region Cache Management Tests

        [Theory]
        [InlineData("org-1")]
        [InlineData("tenant-123")]
        [InlineData("CLIENT_456")]
        [InlineData("org-with-special-chars")]
        public async Task ConfigurationCaching_WithDifferentOrganizationCodes_ShouldWorkCorrectly(string organizationCode)
        {
            // Arrange
            var context = new OrganizationContext(organizationCode, $"Organization {organizationCode}", $"api-key-{organizationCode}", $"https://{organizationCode}.api.prophy.ai");
            
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            var result1 = await _provider.GetConfigurationAsync(organizationCode);
            var result2 = await _provider.GetConfigurationAsync(organizationCode);

            // Assert
            Assert.Same(result1, result2);
            Assert.Equal($"api-key-{organizationCode}", result1.ApiKey);
            Assert.Equal($"https://{organizationCode}.api.prophy.ai", result1.BaseUrl);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
        }

        [Fact]
        public async Task CacheInvalidation_AfterSetApiKey_ShouldRefreshConfiguration()
        {
            // Arrange
            var organizationCode = "test-org";
            var originalContext = new OrganizationContext(organizationCode, "Test Organization", "old-api-key", "https://tenant.api.prophy.ai");
            var updatedContext = originalContext.WithApiKey("new-api-key");

            _mockContextProvider.SetupSequence(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(originalContext)
                .ReturnsAsync(updatedContext);

            // Act
            var config1 = await _provider.GetConfigurationAsync(organizationCode);
            await _provider.SetApiKeyAsync(organizationCode, "new-api-key");
            var config2 = await _provider.GetConfigurationAsync(organizationCode);

            // Assert
            Assert.NotSame(config1, config2);
            Assert.Equal("old-api-key", config1.ApiKey);
            Assert.Equal("new-api-key", config2.ApiKey);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task FullWorkflow_CreateUpdateAndRetrieveConfiguration_ShouldWorkCorrectly()
        {
            // Arrange
            var organizationCode = "integration-test-org";
            var originalContext = new OrganizationContext(organizationCode, "Integration Test Organization", "original-api-key", "https://original.api.prophy.ai");
            var updatedContext = originalContext.WithApiKey("updated-api-key").WithBaseUrl("https://updated.api.prophy.ai");

            _mockContextProvider.SetupSequence(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(originalContext)
                .ReturnsAsync(updatedContext)
                .ReturnsAsync(updatedContext);

            // Act & Assert
            // 1. Get initial configuration
            var config1 = await _provider.GetConfigurationAsync(organizationCode);
            Assert.Equal("original-api-key", config1.ApiKey);
            Assert.Equal("https://original.api.prophy.ai", config1.BaseUrl);

            // 2. Update API key
            await _provider.SetApiKeyAsync(organizationCode, "updated-api-key");

            // 3. Update base URL
            await _provider.SetBaseUrlAsync(organizationCode, "https://updated.api.prophy.ai");

            // 4. Get updated configuration
            var config2 = await _provider.GetConfigurationAsync(organizationCode);
            Assert.Equal("updated-api-key", config2.ApiKey);
            Assert.Equal("https://updated.api.prophy.ai", config2.BaseUrl);

            // Verify all interactions
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Exactly(3));
            _mockContextProvider.Verify(x => x.SetCurrentContext(It.IsAny<OrganizationContext>()), Times.Exactly(2));
        }

        #endregion
    }
} 