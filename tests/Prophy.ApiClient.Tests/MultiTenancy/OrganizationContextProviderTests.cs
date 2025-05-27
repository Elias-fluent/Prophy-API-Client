using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Tests.MultiTenancy
{
    public class OrganizationContextProviderTests
    {
        private readonly Mock<ILogger<OrganizationContextProvider>> _mockLogger;
        private readonly OrganizationContextProvider _provider;

        public OrganizationContextProviderTests()
        {
            _mockLogger = new Mock<ILogger<OrganizationContextProvider>>();
            _provider = new OrganizationContextProvider(_mockLogger.Object);
        }

        [Fact]
        public void GetCurrentContext_WithNoContext_ReturnsNull()
        {
            // Act
            var context = _provider.GetCurrentContext();

            // Assert
            Assert.Null(context);
        }

        [Fact]
        public void SetCurrentContext_WithValidContext_SetsContext()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Organization", "api-key", "https://api.test.com");

            // Act
            _provider.SetCurrentContext(context);
            var retrievedContext = _provider.GetCurrentContext();

            // Assert
            Assert.NotNull(retrievedContext);
            Assert.Equal("TEST", retrievedContext.OrganizationCode);
            Assert.Equal("Test Organization", retrievedContext.OrganizationName);
        }

        [Fact]
        public void SetCurrentContext_WithNull_ClearsContext()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Organization", "api-key", "https://api.test.com");
            _provider.SetCurrentContext(context);

            // Act
            _provider.SetCurrentContext(null);
            var retrievedContext = _provider.GetCurrentContext();

            // Assert
            Assert.Null(retrievedContext);
        }

        [Fact]
        public void ClearCurrentContext_RemovesContext()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Organization", "api-key", "https://api.test.com");
            _provider.SetCurrentContext(context);

            // Act
            _provider.ClearCurrentContext();
            var retrievedContext = _provider.GetCurrentContext();

            // Assert
            Assert.Null(retrievedContext);
        }

        [Fact]
        public async Task ResolveContextAsync_WithExistingContext_ReturnsContext()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Organization", "api-key", "https://api.test.com");
            _provider.SetCurrentContext(context);

            // Act
            var resolvedContext = await _provider.ResolveContextAsync("TEST");

            // Assert
            Assert.NotNull(resolvedContext);
            Assert.Equal("TEST", resolvedContext.OrganizationCode);
        }

        [Fact]
        public async Task ResolveContextAsync_WithNonExistentContext_CreatesNewContext()
        {
            // Act
            var resolvedContext = await _provider.ResolveContextAsync("NONEXISTENT");

            // Assert
            Assert.NotNull(resolvedContext);
            Assert.Equal("NONEXISTENT", resolvedContext.OrganizationCode);
            Assert.Equal("NONEXISTENT", resolvedContext.OrganizationName); // Default name is same as code
        }

        [Fact]
        public async Task ResolveContextAsync_WithNullOrganizationCode_ReturnsNull()
        {
            // Act
            var resolvedContext1 = await _provider.ResolveContextAsync(null);
            var resolvedContext2 = await _provider.ResolveContextAsync("");
            var resolvedContext3 = await _provider.ResolveContextAsync("   ");

            // Assert
            Assert.Null(resolvedContext1);
            Assert.Null(resolvedContext2);
            Assert.Null(resolvedContext3);
        }

        [Fact]
        public async Task ContextIsolation_BetweenAsyncOperations_MaintainsIndependence()
        {
            // Arrange
            var context1 = new OrganizationContext("ORG1", "Organization 1", "key1", "url1");
            var context2 = new OrganizationContext("ORG2", "Organization 2", "key2", "url2");

            // Act & Assert
            var task1 = Task.Run(async () =>
            {
                _provider.SetCurrentContext(context1);
                await Task.Delay(50); // Simulate async work
                var retrievedContext = _provider.GetCurrentContext();
                Assert.NotNull(retrievedContext);
                Assert.Equal("ORG1", retrievedContext.OrganizationCode);
            });

            var task2 = Task.Run(async () =>
            {
                await Task.Delay(25); // Start slightly later
                _provider.SetCurrentContext(context2);
                await Task.Delay(50); // Simulate async work
                var retrievedContext = _provider.GetCurrentContext();
                Assert.NotNull(retrievedContext);
                Assert.Equal("ORG2", retrievedContext.OrganizationCode);
            });

            await Task.WhenAll(task1, task2);
        }

        [Fact]
        public async Task ContextPropagation_ThroughAsyncCalls_MaintainsContext()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Organization", "api-key", "https://api.test.com");
            _provider.SetCurrentContext(context);

            // Act
            var retrievedContext = await GetContextAfterAsyncOperation();

            // Assert
            Assert.NotNull(retrievedContext);
            Assert.Equal("TEST", retrievedContext.OrganizationCode);
        }

        [Fact]
        public void ContextCaching_WithSameOrganizationCode_ReturnsCachedInstance()
        {
            // Arrange
            var context = new OrganizationContext("TEST", "Test Organization", "api-key", "https://api.test.com");
            _provider.SetCurrentContext(context);

            // Act
            var context1 = _provider.GetCurrentContext();
            var context2 = _provider.GetCurrentContext();

            // Assert
            Assert.Same(context1, context2);
        }

        [Fact]
        public void MultipleContexts_InCache_AreStoredIndependently()
        {
            // Arrange
            var context1 = new OrganizationContext("ORG1", "Organization 1", "key1", "url1");
            var context2 = new OrganizationContext("ORG2", "Organization 2", "key2", "url2");

            // Act
            _provider.SetCurrentContext(context1);
            var retrieved1 = _provider.GetCurrentContext();

            _provider.SetCurrentContext(context2);
            var retrieved2 = _provider.GetCurrentContext();

            // Assert
            Assert.Equal("ORG1", retrieved1.OrganizationCode);
            Assert.Equal("ORG2", retrieved2.OrganizationCode);
            Assert.NotSame(retrieved1, retrieved2);
        }

        [Fact]
        public void ContextUpdate_WithSameOrganizationCode_UpdatesCache()
        {
            // Arrange
            var originalContext = new OrganizationContext("TEST", "Test Organization", "old-key", "old-url");
            var updatedContext = new OrganizationContext("TEST", "Test Organization", "new-key", "new-url");

            // Act
            _provider.SetCurrentContext(originalContext);
            var retrieved1 = _provider.GetCurrentContext();

            _provider.SetCurrentContext(updatedContext);
            var retrieved2 = _provider.GetCurrentContext();

            // Assert
            Assert.Equal("old-key", retrieved1.ApiKey);
            Assert.Equal("new-key", retrieved2.ApiKey);
            Assert.NotSame(retrieved1, retrieved2);
        }

        private async Task<OrganizationContext?> GetContextAfterAsyncOperation()
        {
            await Task.Delay(10); // Simulate async work
            return _provider.GetCurrentContext();
        }
    }
} 