using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;
using Prophy.ApiClient.Configuration;

namespace Prophy.ApiClient.Tests.Configuration
{
    /// <summary>
    /// Unit tests for ProphyApiClientConfigurationBuilder class.
    /// </summary>
    public class ProphyApiClientConfigurationBuilderTests
    {
        [Fact]
        public void Create_ReturnsNewBuilder()
        {
            // Act
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void CreateDefault_ReturnsBuilderWithDefaultSources()
        {
            // Act
            var builder = ProphyApiClientConfigurationBuilder.CreateDefault();

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void WithApiKey_SetsApiKey()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-api-key")
                .WithOrganizationCode("test-org")
                .Build();

            // Assert
            Assert.Equal("test-api-key", config.ApiKey);
        }

        [Fact]
        public void WithOrganizationCode_SetsOrganizationCode()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-organization")
                .Build();

            // Assert
            Assert.Equal("test-organization", config.OrganizationCode);
        }

        [Fact]
        public void WithBaseUrl_SetsBaseUrl()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithBaseUrl("https://custom.api.com/")
                .Build();

            // Assert
            Assert.Equal("https://custom.api.com/", config.BaseUrl);
        }

        [Fact]
        public void WithTimeout_SetsTimeout()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithTimeout(120)
                .Build();

            // Assert
            Assert.Equal(120, config.TimeoutSeconds);
        }

        [Fact]
        public void WithRetryPolicy_SetsRetryConfiguration()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithRetryPolicy(5, 2000)
                .Build();

            // Assert
            Assert.Equal(5, config.MaxRetryAttempts);
            Assert.Equal(2000, config.RetryDelayMilliseconds);
        }

        [Fact]
        public void WithRetryPolicy_DefaultDelay_SetsRetryConfiguration()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithRetryPolicy(3)
                .Build();

            // Assert
            Assert.Equal(3, config.MaxRetryAttempts);
            Assert.Equal(1000, config.RetryDelayMilliseconds);
        }

        [Fact]
        public void WithDetailedLogging_EnablesLogging()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithDetailedLogging(true)
                .Build();

            // Assert
            Assert.True(config.EnableDetailedLogging);
        }

        [Fact]
        public void WithDetailedLogging_DefaultTrue_EnablesLogging()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithDetailedLogging()
                .Build();

            // Assert
            Assert.True(config.EnableDetailedLogging);
        }

        [Fact]
        public void WithMaxFileSize_SetsMaxFileSize()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithMaxFileSize(100 * 1024 * 1024)
                .Build();

            // Assert
            Assert.Equal(100 * 1024 * 1024, config.MaxFileSize);
        }

        [Fact]
        public void WithSslValidation_SetsSslValidation()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithSslValidation(false)
                .Build();

            // Assert
            Assert.False(config.ValidateSslCertificates);
        }

        [Fact]
        public void WithSslValidation_DefaultTrue_EnablesSslValidation()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithSslValidation()
                .Build();

            // Assert
            Assert.True(config.ValidateSslCertificates);
        }

        [Fact]
        public void WithUserAgent_SetsUserAgent()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithUserAgent("Custom-Agent/2.0")
                .Build();

            // Assert
            Assert.Equal("Custom-Agent/2.0", config.UserAgent);
        }

        [Fact]
        public void FluentApi_AllowsMethodChaining()
        {
            // Arrange & Act
            var config = ProphyApiClientConfigurationBuilder.Create()
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .WithBaseUrl("https://custom.api.com/")
                .WithTimeout(120)
                .WithRetryPolicy(5, 2000)
                .WithDetailedLogging(true)
                .WithMaxFileSize(100 * 1024 * 1024)
                .WithSslValidation(false)
                .WithUserAgent("Custom-Agent/2.0")
                .Build();

            // Assert
            Assert.Equal("test-key", config.ApiKey);
            Assert.Equal("test-org", config.OrganizationCode);
            Assert.Equal("https://custom.api.com/", config.BaseUrl);
            Assert.Equal(120, config.TimeoutSeconds);
            Assert.Equal(5, config.MaxRetryAttempts);
            Assert.Equal(2000, config.RetryDelayMilliseconds);
            Assert.True(config.EnableDetailedLogging);
            Assert.Equal(100 * 1024 * 1024, config.MaxFileSize);
            Assert.False(config.ValidateSslCertificates);
            Assert.Equal("Custom-Agent/2.0", config.UserAgent);
        }

        [Fact]
        public void AddConfiguration_AcceptsCustomConfiguration()
        {
            // Arrange
            var customConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ProphyApiClient:ApiKey", "config-api-key"),
                    new KeyValuePair<string, string?>("ProphyApiClient:OrganizationCode", "config-org")
                })
                .Build();

            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .AddConfiguration(customConfig)
                .Build();

            // Assert
            Assert.Equal("config-api-key", config.ApiKey);
            Assert.Equal("config-org", config.OrganizationCode);
        }

        [Fact]
        public void InCodeValues_TakePrecedenceOverConfiguration()
        {
            // Arrange
            var customConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ProphyApiClient:ApiKey", "config-api-key"),
                    new KeyValuePair<string, string?>("ProphyApiClient:OrganizationCode", "config-org")
                })
                .Build();

            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .AddConfiguration(customConfig)
                .WithApiKey("code-api-key")
                .WithOrganizationCode("code-org")
                .Build();

            // Assert
            Assert.Equal("code-api-key", config.ApiKey);
            Assert.Equal("code-org", config.OrganizationCode);
        }

        [Fact]
        public void ConfigurationValues_UsedWhenInCodeValuesNotSet()
        {
            // Arrange
            var customConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ProphyApiClient:ApiKey", "config-api-key"),
                    new KeyValuePair<string, string?>("ProphyApiClient:OrganizationCode", "config-org"),
                    new KeyValuePair<string, string?>("ProphyApiClient:TimeoutSeconds", "180"),
                    new KeyValuePair<string, string?>("ProphyApiClient:EnableDetailedLogging", "true")
                })
                .Build();

            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .AddConfiguration(customConfig)
                .WithApiKey("code-api-key") // Override API key in code
                .Build();

            // Assert
            Assert.Equal("code-api-key", config.ApiKey); // From code
            Assert.Equal("config-org", config.OrganizationCode); // From configuration
            Assert.Equal(180, config.TimeoutSeconds); // From configuration
            Assert.True(config.EnableDetailedLogging); // From configuration
        }

        [Fact]
        public void Build_UsesDefaultValues_WhenNoConfigurationProvided()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .WithApiKey("test-key")
                .WithOrganizationCode("test-org")
                .Build();

            // Assert
            Assert.Equal(ProphyApiClientConfiguration.DefaultBaseUrl, config.BaseUrl);
            Assert.Equal(ProphyApiClientConfiguration.DefaultTimeoutSeconds, config.TimeoutSeconds);
            Assert.Equal(ProphyApiClientConfiguration.DefaultMaxRetryAttempts, config.MaxRetryAttempts);
            Assert.Equal(ProphyApiClientConfiguration.DefaultRetryDelayMilliseconds, config.RetryDelayMilliseconds);
            Assert.Equal(ProphyApiClientConfiguration.DefaultMaxFileSize, config.MaxFileSize);
            Assert.Equal(ProphyApiClientConfiguration.DefaultUserAgent, config.UserAgent);
            Assert.False(config.EnableDetailedLogging);
            Assert.True(config.ValidateSslCertificates);
        }

        [Fact]
        public void AddEnvironmentVariables_WithCustomPrefix()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var result = builder.AddEnvironmentVariables("CUSTOM_");

            // Assert
            Assert.Same(builder, result); // Should return same instance for fluent API
        }

        [Fact]
        public void AddJsonFile_WithCustomPath()
        {
            // Arrange
            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var result = builder.AddJsonFile("custom-config.json", optional: true);

            // Assert
            Assert.Same(builder, result); // Should return same instance for fluent API
        }

        [Fact]
        public void ConfigurationFromMultipleSources_MergesCorrectly()
        {
            // Arrange
            var config1 = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ProphyApiClient:ApiKey", "source1-key"),
                    new KeyValuePair<string, string?>("ProphyApiClient:TimeoutSeconds", "120")
                })
                .Build();

            var config2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ProphyApiClient:OrganizationCode", "source2-org"),
                    new KeyValuePair<string, string?>("ProphyApiClient:EnableDetailedLogging", "true")
                })
                .Build();

            var builder = ProphyApiClientConfigurationBuilder.Create();

            // Act
            var config = builder
                .AddConfiguration(config1)
                .AddConfiguration(config2)
                .Build();

            // Assert
            Assert.Equal("source1-key", config.ApiKey);
            Assert.Equal("source2-org", config.OrganizationCode);
            Assert.Equal(120, config.TimeoutSeconds);
            Assert.True(config.EnableDetailedLogging);
        }
    }
} 