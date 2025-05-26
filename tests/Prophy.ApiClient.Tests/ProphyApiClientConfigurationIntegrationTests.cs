using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using Prophy.ApiClient.Configuration;

namespace Prophy.ApiClient.Tests
{
    /// <summary>
    /// Integration tests for ProphyApiClient configuration support.
    /// </summary>
    public class ProphyApiClientConfigurationIntegrationTests
    {
        [Fact]
        public void Constructor_WithValidConfiguration_InitializesSuccessfully()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            // Act
            using var client = new ProphyApiClient(configuration);

            // Assert
            Assert.Equal("test-org", client.OrganizationCode);
            Assert.Equal(new Uri("https://www.prophy.ai/api/"), client.BaseUrl);
        }

        [Fact]
        public void Constructor_WithCustomBaseUrl_SetsCorrectBaseUrl()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org",
                BaseUrl = "https://custom.api.com/"
            };

            // Act
            using var client = new ProphyApiClient(configuration);

            // Assert
            Assert.Equal(new Uri("https://custom.api.com/"), client.BaseUrl);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProphyApiClient((IProphyApiClientConfiguration)null!));
        }

        [Fact]
        public void Constructor_WithInvalidConfiguration_ThrowsArgumentException()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "", // Invalid - empty
                OrganizationCode = "test-org"
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ProphyApiClient(configuration));
            Assert.Contains("Configuration is invalid", exception.Message);
            Assert.Contains("API key is required", exception.Message);
        }

        [Fact]
        public void Constructor_WithConfigurationAndCustomHttpClient_InitializesSuccessfully()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            using var httpClient = new HttpClient();

            // Act
            using var client = new ProphyApiClient(configuration, httpClient, disposeHttpClient: false);

            // Assert
            Assert.Equal("test-org", client.OrganizationCode);
        }

        [Fact]
        public void Constructor_WithConfigurationAndNullHttpClient_ThrowsArgumentNullException()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProphyApiClient(configuration, (HttpClient)null!));
        }

        [Fact]
        public void Constructor_WithConfigurationAndLogger_InitializesSuccessfully()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            var logger = new TestLogger<ProphyApiClient>();

            // Act
            using var client = new ProphyApiClient(configuration, logger);

            // Assert
            Assert.Equal("test-org", client.OrganizationCode);
            Assert.True(logger.LoggedMessages.Count > 0);
        }

        [Fact]
        public void Constructor_ConfiguresHttpClientFromConfiguration()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org",
                BaseUrl = "https://custom.api.com/",
                TimeoutSeconds = 120,
                UserAgent = "Custom-Agent/1.0"
            };

            // Act
            using var client = new ProphyApiClient(configuration);
            var httpClient = client.GetHttpClient();

            // Assert
            Assert.Equal(new Uri("https://custom.api.com/"), client.BaseUrl);
            // Note: We can't directly test HttpClient timeout and headers through the wrapper,
            // but we can verify the client was initialized with the configuration
        }

        [Fact]
        public void FluentConfigurationBuilder_Integration_WorksCorrectly()
        {
            // Arrange & Act
            var configuration = ProphyApiClientConfigurationBuilder.Create()
                .WithApiKey("fluent-api-key")
                .WithOrganizationCode("fluent-org")
                .WithBaseUrl("https://fluent.api.com/")
                .WithTimeout(180)
                .WithDetailedLogging(true)
                .Build();

            using var client = new ProphyApiClient(configuration);

            // Assert
            Assert.Equal("fluent-org", client.OrganizationCode);
            Assert.Equal(new Uri("https://fluent.api.com/"), client.BaseUrl);
        }

        [Fact]
        public void Constructor_WithMultipleValidationErrors_ThrowsArgumentExceptionWithAllErrors()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "", // Invalid
                OrganizationCode = "", // Invalid
                TimeoutSeconds = 0, // Invalid
                MaxRetryAttempts = -1 // Invalid
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ProphyApiClient(configuration));
            Assert.Contains("API key is required", exception.Message);
            Assert.Contains("Organization code is required", exception.Message);
            Assert.Contains("Timeout seconds must be greater than zero", exception.Message);
            Assert.Contains("Maximum retry attempts cannot be negative", exception.Message);
        }

        [Fact]
        public void GetAuthenticator_ReturnsConfiguredAuthenticator()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            // Act
            using var client = new ProphyApiClient(configuration);
            var authenticator = client.GetAuthenticator();

            // Assert
            Assert.NotNull(authenticator);
        }

        [Fact]
        public void GetHttpClient_ReturnsConfiguredHttpClient()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            // Act
            using var client = new ProphyApiClient(configuration);
            var httpClient = client.GetHttpClient();

            // Assert
            Assert.NotNull(httpClient);
        }

        [Fact]
        public void Manuscripts_Property_IsAccessible()
        {
            // Arrange
            var configuration = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org"
            };

            // Act
            using var client = new ProphyApiClient(configuration);
            var manuscripts = client.Manuscripts;

            // Assert
            Assert.NotNull(manuscripts);
        }
    }

    /// <summary>
    /// Test logger implementation for testing purposes.
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = new List<string>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }
} 