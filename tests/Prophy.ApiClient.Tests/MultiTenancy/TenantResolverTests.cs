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
    /// Unit tests for the TenantResolver class.
    /// </summary>
    public class TenantResolverTests
    {
        private readonly Mock<ILogger<TenantResolver>> _mockLogger;
        private readonly TenantResolver _tenantResolver;

        public TenantResolverTests()
        {
            _mockLogger = TestHelpers.CreateMockLogger<TenantResolver>();
            _tenantResolver = new TenantResolver(_mockLogger.Object);
        }

        #region Header-based Resolution Tests

        [Fact]
        public void ResolveFromHeaders_WithOrganizationCodeHeader_ShouldResolveCorrectly()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "X-Organization-Code", "tenant-123" }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal("tenant-123", result);
        }

        [Fact]
        public void ResolveFromHeaders_WithOrgCodeHeader_ShouldResolveCorrectly()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "X-Org-Code", "tenant-456" }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal("tenant-456", result);
        }

        [Fact]
        public void ResolveFromHeaders_WithTenantIdHeader_ShouldResolveCorrectly()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "X-Tenant-Id", "tenant-789" }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal("tenant-789", result);
        }

        [Theory]
        [InlineData("X-Organization-Code", "org-1")]
        [InlineData("X-Org-Code", "org-2")]
        [InlineData("X-Tenant-Id", "tenant-3")]
        [InlineData("Organization-Code", "org-4")]
        [InlineData("Org-Code", "org-5")]
        public void ResolveFromHeaders_WithDifferentHeaderNames_ShouldResolveCorrectly(string headerName, string tenantValue)
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { headerName, tenantValue }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal(tenantValue, result);
        }

        [Fact]
        public void ResolveFromHeaders_WithEmptyHeaders_ShouldReturnNull()
        {
            // Arrange
            var headers = new Dictionary<string, string>();

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFromHeaders_WithNullHeaders_ShouldReturnNull()
        {
            // Act
            var result = _tenantResolver.ResolveFromHeaders(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFromHeaders_WithMultipleHeaders_ShouldReturnFirst()
        {
            // Arrange - X-Organization-Code should have priority
            var headers = new Dictionary<string, string>
            {
                { "X-Organization-Code", "first-org" },
                { "X-Tenant-Id", "second-tenant" }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal("first-org", result);
        }

        [Fact]
        public void ResolveFromHeaders_WithWhitespaceValue_ShouldReturnNull()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "X-Organization-Code", "   " }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFromHeaders_WithEmptyValue_ShouldReturnNull()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "X-Organization-Code", "" }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region URL Resolution Tests

        [Fact]
        public void ResolveFromUrl_WithSubdomain_ShouldResolveCorrectly()
        {
            // Arrange
            var uri = new Uri("https://tenant-789.api.prophy.ai/api/test");

            // Act
            var result = _tenantResolver.ResolveFromUrl(uri);

            // Assert
            Assert.Equal("tenant-789", result);
        }

        [Fact]
        public void ResolveFromUrl_WithNoSubdomain_ShouldReturnNull()
        {
            // Arrange
            var uri = new Uri("https://api.prophy.ai/api/test");

            // Act
            var result = _tenantResolver.ResolveFromUrl(uri);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("https://tenant1.api.prophy.ai/api/test", "tenant1")]
        [InlineData("https://org-123.service.com/api/test", "org-123")]
        [InlineData("https://client_456.example.org/api/test", "client_456")]
        [InlineData("https://test-tenant.localhost/api/test", "test-tenant")]
        public void ResolveFromUrl_WithDifferentSubdomains_ShouldResolveCorrectly(string url, string expectedTenant)
        {
            // Arrange
            var uri = new Uri(url);

            // Act
            var result = _tenantResolver.ResolveFromUrl(uri);

            // Assert
            Assert.Equal(expectedTenant, result);
        }

        [Fact]
        public void ResolveFromUrl_WithWwwSubdomain_ShouldReturnNull()
        {
            // Arrange
            var uri = new Uri("https://www.prophy.ai/api/test");

            // Act
            var result = _tenantResolver.ResolveFromUrl(uri);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFromUrl_WithApiSubdomain_ShouldReturnNull()
        {
            // Arrange
            var uri = new Uri("https://api.prophy.ai/api/test");

            // Act
            var result = _tenantResolver.ResolveFromUrl(uri);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFromUrl_WithNullUri_ShouldReturnNull()
        {
            // Act
            var result = _tenantResolver.ResolveFromUrl(null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Token Resolution Tests

        [Fact]
        public async Task ResolveFromTokenAsync_WithValidJwtToken_ShouldResolveCorrectly()
        {
            // Arrange
            var token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmciOiJ0ZXN0LW9yZyIsInN1YiI6IjEyMzQ1Njc4OTAiLCJuYW1lIjoiSm9obiBEb2UiLCJpYXQiOjE1MTYyMzkwMjJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var result = await _tenantResolver.ResolveFromTokenAsync(token);

            // Assert
            Assert.Equal("test-org", result);
        }

        [Fact]
        public async Task ResolveFromTokenAsync_WithNullToken_ShouldReturnNull()
        {
            // Act
            var result = await _tenantResolver.ResolveFromTokenAsync(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveFromTokenAsync_WithEmptyToken_ShouldReturnNull()
        {
            // Act
            var result = await _tenantResolver.ResolveFromTokenAsync("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveFromTokenAsync_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid-token-format";

            // Act
            var result = await _tenantResolver.ResolveFromTokenAsync(invalidToken);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Request Resolution Tests

        [Fact]
        public async Task ResolveFromRequestAsync_WithHeadersInRequest_ShouldResolveCorrectly()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            request.Headers.Add("X-Organization-Code", "request-org");

            // Act
            var result = await _tenantResolver.ResolveFromRequestAsync(request);

            // Assert
            Assert.Equal("request-org", result);
        }

        [Fact]
        public async Task ResolveFromRequestAsync_WithNullRequest_ShouldReturnNull()
        {
            // Act
            var result = await _tenantResolver.ResolveFromRequestAsync(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveFromRequestAsync_WithSubdomainInRequest_ShouldResolveCorrectly()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://subdomain-org.api.prophy.ai/test");

            // Act
            var result = await _tenantResolver.ResolveFromRequestAsync(request);

            // Assert
            Assert.Equal("subdomain-org", result);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Theory]
        [InlineData("tenant-with-special-chars-!@#$%")]
        [InlineData("tenant_with_underscores")]
        [InlineData("tenant-with-unicode-世界")]
        [InlineData("TENANT-WITH-UPPERCASE")]
        [InlineData("tenant.with.dots")]
        [InlineData("tenant:with:colons")]
        public void ResolveFromHeaders_WithSpecialCharacters_ShouldHandleCorrectly(string tenantValue)
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "X-Organization-Code", tenantValue }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal(tenantValue, result);
        }

        [Fact]
        public void ResolveFromHeaders_WithVeryLongTenantValue_ShouldHandleCorrectly()
        {
            // Arrange
            var longTenantValue = new string('A', 1000);
            var headers = new Dictionary<string, string>
            {
                { "X-Organization-Code", longTenantValue }
            };

            // Act
            var result = _tenantResolver.ResolveFromHeaders(headers);

            // Assert
            Assert.Equal(longTenantValue, result);
        }

        [Fact]
        public void GetResolutionOrder_ShouldReturnExpectedOrder()
        {
            // Act
            var order = _tenantResolver.GetResolutionOrder();

            // Assert
            Assert.NotNull(order);
            Assert.Contains("Headers", order);
            Assert.Contains("Token", order);
            Assert.Contains("URL", order);
        }

        #endregion
    }
} 