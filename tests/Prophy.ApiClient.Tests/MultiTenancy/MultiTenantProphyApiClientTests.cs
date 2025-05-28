using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.MultiTenancy;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.MultiTenancy
{
    /// <summary>
    /// Unit tests for the MultiTenantProphyApiClient class.
    /// </summary>
    public class MultiTenantProphyApiClientTests : IDisposable
    {
        private readonly Mock<IOrganizationContextProvider> _mockContextProvider;
        private readonly Mock<ITenantConfigurationProvider> _mockConfigurationProvider;
        private readonly Mock<ITenantResolver> _mockTenantResolver;
        private readonly Mock<ILogger<MultiTenantProphyApiClient>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly MultiTenantProphyApiClient _client;

        public MultiTenantProphyApiClientTests()
        {
            _mockContextProvider = new Mock<IOrganizationContextProvider>();
            _mockConfigurationProvider = new Mock<ITenantConfigurationProvider>();
            _mockTenantResolver = new Mock<ITenantResolver>();
            _mockLogger = TestHelpers.CreateMockLogger<MultiTenantProphyApiClient>();
            _httpClient = new HttpClient();

            _client = new MultiTenantProphyApiClient(
                _mockContextProvider.Object,
                _mockConfigurationProvider.Object,
                _mockTenantResolver.Object,
                _httpClient,
                disposeHttpClient: true,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            using var client = new MultiTenantProphyApiClient(
                _mockContextProvider.Object,
                _mockConfigurationProvider.Object,
                _mockTenantResolver.Object,
                _httpClient);

            // Assert
            Assert.NotNull(client);
            Assert.NotNull(client.Manuscripts);
            Assert.NotNull(client.CustomFields);
            Assert.NotNull(client.Webhooks);
            Assert.NotNull(client.Journals);
            Assert.NotNull(client.AuthorGroups);
            Assert.NotNull(client.Resilience);
        }

        [Fact]
        public void Constructor_WithNullContextProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MultiTenantProphyApiClient(
                    null!,
                    _mockConfigurationProvider.Object,
                    _mockTenantResolver.Object,
                    _httpClient));
        }

        [Fact]
        public void Constructor_WithNullConfigurationProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MultiTenantProphyApiClient(
                    _mockContextProvider.Object,
                    null!,
                    _mockTenantResolver.Object,
                    _httpClient));
        }

        [Fact]
        public void Constructor_WithNullTenantResolver_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MultiTenantProphyApiClient(
                    _mockContextProvider.Object,
                    _mockConfigurationProvider.Object,
                    null!,
                    _httpClient));
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MultiTenantProphyApiClient(
                    _mockContextProvider.Object,
                    _mockConfigurationProvider.Object,
                    _mockTenantResolver.Object,
                    null!));
        }

        #endregion

        #region Context Management Tests

        [Fact]
        public void CurrentContext_ShouldReturnContextFromProvider()
        {
            // Arrange
            var expectedContext = new OrganizationContext("test-org", "Test Organization");
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns(expectedContext);

            // Act
            var result = _client.CurrentContext;

            // Assert
            Assert.Equal(expectedContext, result);
            _mockContextProvider.Verify(x => x.GetCurrentContext(), Times.Once);
        }

        [Fact]
        public void SetContext_WithValidContext_ShouldSetContextInProvider()
        {
            // Arrange
            var context = new OrganizationContext("test-org", "Test Organization");

            // Act
            _client.SetContext(context);

            // Assert
            _mockContextProvider.Verify(x => x.SetCurrentContext(context), Times.Once);
        }

        [Fact]
        public void SetContext_WithNullContext_ShouldSetNullInProvider()
        {
            // Act
            _client.SetContext(null);

            // Assert
            _mockContextProvider.Verify(x => x.SetCurrentContext(null), Times.Once);
        }

        [Fact]
        public async Task SetContextAsync_WithValidOrganizationCode_ShouldResolveAndSetContext()
        {
            // Arrange
            var organizationCode = "test-org";
            var expectedContext = new OrganizationContext(organizationCode, "Test Organization");

            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(expectedContext);

            // Act
            await _client.SetContextAsync(organizationCode);

            // Assert
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
            _mockContextProvider.Verify(x => x.SetCurrentContext(expectedContext), Times.Once);
        }

        [Fact]
        public async Task SetContextAsync_WithNullOrganizationCode_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _client.SetContextAsync(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => _client.SetContextAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _client.SetContextAsync("   "));
        }

        [Fact]
        public async Task SetContextAsync_WithUnresolvableOrganizationCode_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var organizationCode = "non-existent-org";
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync((OrganizationContext?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _client.SetContextAsync(organizationCode));
            
            Assert.Contains(organizationCode, exception.Message);
        }

        [Fact]
        public void ClearContext_ShouldClearContextInProvider()
        {
            // Act
            _client.ClearContext();

            // Assert
            _mockContextProvider.Verify(x => x.ClearCurrentContext(), Times.Once);
        }

        [Fact]
        public async Task ResolveContextAsync_WithValidRequest_ShouldResolveAndSetContext()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var organizationCode = "test-org";
            var expectedContext = new OrganizationContext(organizationCode, "Test Organization");

            _mockTenantResolver.Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(organizationCode);
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(expectedContext);

            // Act
            var result = await _client.ResolveContextAsync(request);

            // Assert
            Assert.Equal(expectedContext, result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
        }

        #endregion

        #region Module Access Tests

        [Fact]
        public void Manuscripts_ShouldReturnManuscriptModule()
        {
            // Act
            var module = _client.Manuscripts;

            // Assert
            Assert.NotNull(module);
            Assert.IsAssignableFrom<IManuscriptModule>(module);
        }

        [Fact]
        public void CustomFields_ShouldReturnCustomFieldModule()
        {
            // Act
            var module = _client.CustomFields;

            // Assert
            Assert.NotNull(module);
            Assert.IsAssignableFrom<ICustomFieldModule>(module);
        }

        [Fact]
        public void Webhooks_ShouldReturnWebhookModule()
        {
            // Act
            var module = _client.Webhooks;

            // Assert
            Assert.NotNull(module);
            Assert.IsAssignableFrom<IWebhookModule>(module);
        }

        [Fact]
        public void Journals_ShouldReturnJournalRecommendationModule()
        {
            // Act
            var module = _client.Journals;

            // Assert
            Assert.NotNull(module);
            Assert.IsAssignableFrom<IJournalRecommendationModule>(module);
        }

        [Fact]
        public void AuthorGroups_ShouldReturnAuthorGroupModule()
        {
            // Act
            var module = _client.AuthorGroups;

            // Assert
            Assert.NotNull(module);
            Assert.IsAssignableFrom<IAuthorGroupModule>(module);
        }

        [Fact]
        public void Resilience_ShouldReturnResilienceModule()
        {
            // Act
            var module = _client.Resilience;

            // Assert
            Assert.NotNull(module);
            Assert.IsAssignableFrom<IResilienceModule>(module);
        }

        [Fact]
        public void ModuleAccess_ShouldReturnSameInstanceOnMultipleCalls()
        {
            // Act
            var manuscripts1 = _client.Manuscripts;
            var manuscripts2 = _client.Manuscripts;
            var customFields1 = _client.CustomFields;
            var customFields2 = _client.CustomFields;

            // Assert
            Assert.Same(manuscripts1, manuscripts2);
            Assert.Same(customFields1, customFields2);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public async Task GetConfigurationAsync_ShouldReturnConfigurationFromProvider()
        {
            // Arrange
            var mockConfiguration = new Mock<IProphyApiClientConfiguration>();
            _mockConfigurationProvider.Setup(x => x.GetConfigurationAsync())
                .ReturnsAsync(mockConfiguration.Object);

            // Act
            var result = await _client.GetConfigurationAsync();

            // Assert
            Assert.Equal(mockConfiguration.Object, result);
            _mockConfigurationProvider.Verify(x => x.GetConfigurationAsync(), Times.Once);
        }

        #endregion

        #region HTTP Client Tests

        [Fact]
        public void GetHttpClient_ShouldReturnHttpClientWrapper()
        {
            // Act
            var httpClient = _client.GetHttpClient();

            // Assert
            Assert.NotNull(httpClient);
            Assert.IsAssignableFrom<IHttpClientWrapper>(httpClient);
        }

        [Fact]
        public void GetHttpClient_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _client.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _client.GetHttpClient());
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task FullWorkflow_ResolveSetAndClearContext_ShouldWorkCorrectly()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://test-org.api.prophy.ai/test");
            var organizationCode = "test-org";
            var context = new OrganizationContext(organizationCode, "Test Organization");

            _mockTenantResolver.Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(organizationCode);
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);
            _mockContextProvider.Setup(x => x.GetCurrentContext())
                .Returns(context);

            // Act & Assert
            // 1. Resolve context
            var resolvedContext = await _client.ResolveContextAsync(request);
            Assert.Equal(context, resolvedContext);

            // 2. Check current context
            var currentContext = _client.CurrentContext;
            Assert.Equal(context, currentContext);

            // 3. Clear context
            _client.ClearContext();

            // Verify all interactions
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
            _mockContextProvider.Verify(x => x.SetCurrentContext(context), Times.Once);
            _mockContextProvider.Verify(x => x.GetCurrentContext(), Times.Once);
            _mockContextProvider.Verify(x => x.ClearCurrentContext(), Times.Once);
        }

        [Theory]
        [InlineData("org-1")]
        [InlineData("tenant-123")]
        [InlineData("client_456")]
        [InlineData("UPPERCASE-ORG")]
        public async Task SetContextAsync_WithDifferentOrganizationCodes_ShouldHandleCorrectly(string organizationCode)
        {
            // Arrange
            var context = new OrganizationContext(organizationCode, $"Organization {organizationCode}");
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(context);

            // Act
            await _client.SetContextAsync(organizationCode);

            // Assert
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
            _mockContextProvider.Verify(x => x.SetCurrentContext(context), Times.Once);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldDisposeResourcesCorrectly()
        {
            // Act
            _client.Dispose();

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Act
            _client.Dispose();
            _client.Dispose();
            _client.Dispose();

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public void AccessAfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _client.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _client.GetHttpClient());
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ResolveContextAsync_WhenResolverThrows_ShouldReturnNull()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            _mockTenantResolver.Setup(x => x.ResolveFromRequestAsync(request))
                .ThrowsAsync(new InvalidOperationException("Resolver error"));

            // Act
            var result = await _client.ResolveContextAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetContextAsync_WhenProviderThrows_ShouldPropagateException()
        {
            // Arrange
            var organizationCode = "test-org";
            _mockContextProvider.Setup(x => x.ResolveContextAsync(organizationCode))
                .ThrowsAsync(new InvalidOperationException("Provider error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _client.SetContextAsync(organizationCode));
        }

        [Fact]
        public async Task GetConfigurationAsync_WhenProviderThrows_ShouldPropagateException()
        {
            // Arrange
            _mockConfigurationProvider.Setup(x => x.GetConfigurationAsync())
                .ThrowsAsync(new InvalidOperationException("Configuration error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _client.GetConfigurationAsync());
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            _httpClient?.Dispose();
        }
    }
} 