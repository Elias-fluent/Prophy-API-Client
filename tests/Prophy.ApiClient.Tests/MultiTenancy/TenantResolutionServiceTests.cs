using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.MultiTenancy;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.MultiTenancy
{
    /// <summary>
    /// Unit tests for the TenantResolutionService class.
    /// </summary>
    public class TenantResolutionServiceTests
    {
        private readonly Mock<ITenantResolver> _mockTenantResolver;
        private readonly Mock<IOrganizationContextProvider> _mockContextProvider;
        private readonly Mock<ILogger<TenantResolutionService>> _mockLogger;
        private readonly TenantResolutionService _tenantResolutionService;

        public TenantResolutionServiceTests()
        {
            _mockTenantResolver = new Mock<ITenantResolver>();
            _mockContextProvider = new Mock<IOrganizationContextProvider>();
            _mockLogger = TestHelpers.CreateMockLogger<TenantResolutionService>();
            
            _tenantResolutionService = new TenantResolutionService(
                _mockTenantResolver.Object,
                _mockContextProvider.Object,
                _mockLogger.Object);
        }

        #region ResolveAndSetContextAsync Tests

        [Fact]
        public async Task ResolveAndSetContextAsync_WithValidRequest_ShouldResolveAndSetContext()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var expectedOrganization = "test-org";
            var mockContext = new Mock<OrganizationContext>();

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(expectedOrganization);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync(expectedOrganization))
                .ReturnsAsync(mockContext.Object);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Equal(mockContext.Object, result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(expectedOrganization), Times.Once);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithNullRequest_ShouldReturnNull()
        {
            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(null);

            // Assert
            Assert.Null(result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(It.IsAny<HttpRequestMessage>()), Times.Never);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithUnresolvableRequest_ShouldReturnNull()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Null(result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithEmptyOrganization_ShouldReturnNull()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(string.Empty);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Null(result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithWhitespaceOrganization_ShouldReturnNull()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync("   ");

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Null(result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ResolveAndSetContextAsync_WhenResolverThrows_ShouldHandleGracefully()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var expectedException = new InvalidOperationException("Resolver error");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Null(result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WhenContextProviderThrows_ShouldHandleGracefully()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var expectedOrganization = "test-org";
            var expectedException = new InvalidOperationException("Context provider error");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(expectedOrganization);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync(expectedOrganization))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Null(result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(expectedOrganization), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("org-1")]
        [InlineData("tenant-123")]
        [InlineData("client_456")]
        [InlineData("UPPERCASE-ORG")]
        [InlineData("org-with-special-chars-!@#")]
        public async Task ResolveAndSetContextAsync_WithDifferentOrganizations_ShouldHandleCorrectly(string organizationCode)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var mockContext = new Mock<OrganizationContext>();

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(organizationCode);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync(organizationCode))
                .ReturnsAsync(mockContext.Object);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Equal(mockContext.Object, result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(organizationCode), Times.Once);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithMultipleRequests_ShouldHandleEachIndependently()
        {
            // Arrange
            var request1 = new HttpRequestMessage(HttpMethod.Get, "https://org1.api.prophy.ai/test");
            var request2 = new HttpRequestMessage(HttpMethod.Get, "https://org2.api.prophy.ai/test");
            var request3 = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

            var mockContext1 = new Mock<OrganizationContext>();
            var mockContext2 = new Mock<OrganizationContext>();

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request1))
                .ReturnsAsync("org1");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request2))
                .ReturnsAsync("org2");

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request3))
                .ReturnsAsync((string?)null);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync("org1"))
                .ReturnsAsync(mockContext1.Object);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync("org2"))
                .ReturnsAsync(mockContext2.Object);

            // Act
            var result1 = await _tenantResolutionService.ResolveAndSetContextAsync(request1);
            var result2 = await _tenantResolutionService.ResolveAndSetContextAsync(request2);
            var result3 = await _tenantResolutionService.ResolveAndSetContextAsync(request3);

            // Assert
            Assert.Equal(mockContext1.Object, result1);
            Assert.Equal(mockContext2.Object, result2);
            Assert.Null(result3);

            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request1), Times.Once);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request2), Times.Once);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request3), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync("org1"), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync("org2"), Times.Once);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithConcurrentRequests_ShouldHandleConcurrency()
        {
            // Arrange
            var tasks = new List<Task<OrganizationContext?>>();
            var numberOfConcurrentRequests = 10;

            for (int i = 0; i < numberOfConcurrentRequests; i++)
            {
                var orgCode = $"org-{i}";
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://{orgCode}.api.prophy.ai/test");
                var mockContext = new Mock<OrganizationContext>();

                _mockTenantResolver
                    .Setup(x => x.ResolveFromRequestAsync(request))
                    .ReturnsAsync(orgCode);

                _mockContextProvider
                    .Setup(x => x.ResolveContextAsync(orgCode))
                    .ReturnsAsync(mockContext.Object);

                tasks.Add(_tenantResolutionService.ResolveAndSetContextAsync(request));
            }

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.NotNull(result));
            Assert.Equal(numberOfConcurrentRequests, results.Length);

            for (int i = 0; i < numberOfConcurrentRequests; i++)
            {
                _mockContextProvider.Verify(x => x.ResolveContextAsync($"org-{i}"), Times.Once);
            }
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task ResolveAndSetContextAsync_WithManySequentialCalls_ShouldPerformWell()
        {
            // Arrange
            var numberOfCalls = 100;
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var mockContext = new Mock<OrganizationContext>();

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync("test-org");

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync("test-org"))
                .ReturnsAsync(mockContext.Object);

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < numberOfCalls; i++)
            {
                var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);
                Assert.NotNull(result);
            }
            var endTime = DateTime.UtcNow;

            // Assert
            var totalTime = endTime - startTime;
            Assert.True(totalTime.TotalSeconds < 5, $"Performance test took too long: {totalTime.TotalSeconds} seconds");
            _mockContextProvider.Verify(x => x.ResolveContextAsync("test-org"), Times.Exactly(numberOfCalls));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ResolveAndSetContextAsync_WithVeryLongOrganizationCode_ShouldHandleCorrectly()
        {
            // Arrange
            var longOrgCode = new string('a', 1000); // Very long organization code
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var mockContext = new Mock<OrganizationContext>();

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(longOrgCode);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync(longOrgCode))
                .ReturnsAsync(mockContext.Object);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Equal(mockContext.Object, result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(longOrgCode), Times.Once);
        }

        [Fact]
        public async Task ResolveAndSetContextAsync_WithSpecialCharactersInOrganization_ShouldHandleCorrectly()
        {
            // Arrange
            var specialOrgCode = "org-with-special-chars-!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            var mockContext = new Mock<OrganizationContext>();

            _mockTenantResolver
                .Setup(x => x.ResolveFromRequestAsync(request))
                .ReturnsAsync(specialOrgCode);

            _mockContextProvider
                .Setup(x => x.ResolveContextAsync(specialOrgCode))
                .ReturnsAsync(mockContext.Object);

            // Act
            var result = await _tenantResolutionService.ResolveAndSetContextAsync(request);

            // Assert
            Assert.Equal(mockContext.Object, result);
            _mockTenantResolver.Verify(x => x.ResolveFromRequestAsync(request), Times.Once);
            _mockContextProvider.Verify(x => x.ResolveContextAsync(specialOrgCode), Times.Once);
        }

        #endregion
    }
} 