using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using Xunit;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Tests.MultiTenancy
{
    public class OrganizationContextTests
    {
        [Fact]
        public void Constructor_WithValidParameters_CreatesContext()
        {
            // Arrange
            var organizationCode = "TEST_ORG";
            var organizationName = "Test Organization";
            var apiKey = "test-api-key";
            var baseUrl = "https://api.test.com";
            var properties = new Dictionary<string, object> { { "key1", "value1" } };
            var claims = new List<Claim> { new Claim("role", "admin") };

            // Act
            var context = new OrganizationContext(
                organizationCode, 
                organizationName, 
                apiKey, 
                baseUrl, 
                properties, 
                claims);

            // Assert
            Assert.Equal(organizationCode, context.OrganizationCode);
            Assert.Equal(organizationName, context.OrganizationName);
            Assert.Equal(apiKey, context.ApiKey);
            Assert.Equal(baseUrl, context.BaseUrl);
            Assert.Single(context.Properties);
            Assert.Equal("value1", context.Properties["key1"]);
            Assert.Single(context.UserClaims);
            Assert.Equal("admin", context.UserClaims[0].Value);
        }

        [Fact]
        public void Constructor_WithNullOrganizationCode_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new OrganizationContext(null!, "Test", "key", "url"));
            Assert.Throws<ArgumentException>(() => new OrganizationContext("", "Test", "key", "url"));
            Assert.Throws<ArgumentException>(() => new OrganizationContext("   ", "Test", "key", "url"));
        }

        [Fact]
        public void Constructor_WithNullCollections_CreatesEmptyCollections()
        {
            // Act
            var context = new OrganizationContext("TEST", "Test", "key", "url", null, null);

            // Assert
            Assert.Empty(context.Properties);
            Assert.Empty(context.UserClaims);
        }

        [Fact]
        public void WithApiKey_ReturnsNewContextWithUpdatedApiKey()
        {
            // Arrange
            var original = new OrganizationContext("TEST", "Test", "old-key", "url");
            var newApiKey = "new-api-key";

            // Act
            var updated = original.WithApiKey(newApiKey);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Equal("old-key", original.ApiKey);
            Assert.Equal(newApiKey, updated.ApiKey);
            Assert.Equal(original.OrganizationCode, updated.OrganizationCode);
        }

        [Fact]
        public void WithBaseUrl_ReturnsNewContextWithUpdatedBaseUrl()
        {
            // Arrange
            var original = new OrganizationContext("TEST", "Test", "key", "old-url");
            var newBaseUrl = "https://new-api.test.com";

            // Act
            var updated = original.WithBaseUrl(newBaseUrl);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Equal("old-url", original.BaseUrl);
            Assert.Equal(newBaseUrl, updated.BaseUrl);
            Assert.Equal(original.OrganizationCode, updated.OrganizationCode);
        }

        [Fact]
        public void WithProperties_AddsNewProperties()
        {
            // Arrange
            var original = new OrganizationContext("TEST", "Test", "key", "url");
            var newProperties = new Dictionary<string, object> { { "newKey", "newValue" } };

            // Act
            var updated = original.WithProperties(newProperties);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Empty(original.Properties);
            Assert.Single(updated.Properties);
            Assert.Equal("newValue", updated.Properties["newKey"]);
        }

        [Fact]
        public void WithProperties_MergesExistingProperties()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "key1", "oldValue" } };
            var original = new OrganizationContext("TEST", "Test", "key", "url", properties);
            var newProperties = new Dictionary<string, object> { { "key1", "newValue" }, { "key2", "value2" } };

            // Act
            var updated = original.WithProperties(newProperties);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Equal("oldValue", original.Properties["key1"]);
            Assert.Equal("newValue", updated.Properties["key1"]);
            Assert.Equal("value2", updated.Properties["key2"]);
            Assert.Equal(2, updated.Properties.Count);
        }

        [Fact]
        public void WithUserClaims_ReplacesAllClaims()
        {
            // Arrange
            var originalClaims = new List<Claim> { new Claim("role", "admin") };
            var original = new OrganizationContext("TEST", "Test", "key", "url", null, originalClaims);
            var newClaims = new List<Claim> 
            { 
                new Claim("role", "user"), 
                new Claim("department", "IT") 
            };

            // Act
            var updated = original.WithUserClaims(newClaims);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Single(original.UserClaims);
            Assert.Equal(2, updated.UserClaims.Count);
            Assert.Equal("user", updated.UserClaims[0].Value);
            Assert.Equal("IT", updated.UserClaims[1].Value);
        }

        [Fact]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            // Arrange
            var context1 = new OrganizationContext("TEST", "Test", "key", "url");
            var context2 = new OrganizationContext("TEST", "Test", "key", "url");

            // Act & Assert
            Assert.True(context1.Equals(context2));
            // Note: == operator is not overridden, so it compares references
            Assert.False(context1 == context2); // Different instances
            Assert.True(context1 != context2);  // Different instances
        }

        [Fact]
        public void Equals_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var context1 = new OrganizationContext("TEST1", "Test", "key", "url");
            var context2 = new OrganizationContext("TEST2", "Test", "key", "url");

            // Act & Assert
            Assert.False(context1.Equals(context2));
            Assert.False(context1 == context2);
            Assert.True(context1 != context2);
        }

        [Fact]
        public void GetHashCode_WithSameValues_ReturnsSameHashCode()
        {
            // Arrange
            var context1 = new OrganizationContext("TEST", "Test", "key", "url");
            var context2 = new OrganizationContext("TEST", "Test", "key", "url");

            // Act & Assert
            Assert.Equal(context1.GetHashCode(), context2.GetHashCode());
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var context = new OrganizationContext("TEST_ORG", "Test Organization", "key", "url");

            // Act
            var result = context.ToString();

            // Assert
            Assert.Contains("TEST_ORG", result);
            Assert.Contains("Test Organization", result);
        }

        [Fact]
        public void GetProperty_WithExistingProperty_ReturnsValue()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 42 } };
            var context = new OrganizationContext("TEST", "Test", "key", "url", properties);

            // Act & Assert
            Assert.Equal("value1", context.GetProperty<string>("key1"));
            Assert.Equal(42, context.GetProperty<int>("key2"));
            Assert.Null(context.GetProperty<string>("nonexistent"));
        }

        [Fact]
        public void HasProperty_WithExistingProperty_ReturnsTrue()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "key1", "value1" } };
            var context = new OrganizationContext("TEST", "Test", "key", "url", properties);

            // Act & Assert
            Assert.True(context.HasProperty("key1"));
            Assert.False(context.HasProperty("nonexistent"));
            Assert.False(context.HasProperty(null));
            Assert.False(context.HasProperty(""));
        }

        [Fact]
        public void UserClaims_WithClaims_ReturnsCorrectClaims()
        {
            // Arrange
            var claims = new List<Claim> 
            { 
                new Claim("role", "admin"), 
                new Claim("department", "IT") 
            };
            var context = new OrganizationContext("TEST", "Test", "key", "url", null, claims);

            // Act & Assert
            Assert.Equal(2, context.UserClaims.Count);
            Assert.Equal("admin", context.UserClaims[0].Value);
            Assert.Equal("IT", context.UserClaims[1].Value);
        }

        [Fact]
        public void CreatedAt_IsSetToCurrentTime()
        {
            // Arrange
            var before = DateTimeOffset.UtcNow;

            // Act
            var context = new OrganizationContext("TEST", "Test", "key", "url");
            var after = DateTimeOffset.UtcNow;

            // Assert
            Assert.True(context.CreatedAt >= before);
            Assert.True(context.CreatedAt <= after);
        }
    }
} 