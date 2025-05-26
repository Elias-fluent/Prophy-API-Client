using System;
using System.Linq;
using Xunit;
using Prophy.ApiClient.Configuration;

namespace Prophy.ApiClient.Tests.Configuration
{
    /// <summary>
    /// Unit tests for ProphyApiClientConfiguration class.
    /// </summary>
    public class ProphyApiClientConfigurationTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var config = new ProphyApiClientConfiguration();

            // Assert
            Assert.Equal(ProphyApiClientConfiguration.DefaultBaseUrl, config.BaseUrl);
            Assert.Equal(ProphyApiClientConfiguration.DefaultTimeoutSeconds, config.TimeoutSeconds);
            Assert.Equal(ProphyApiClientConfiguration.DefaultMaxRetryAttempts, config.MaxRetryAttempts);
            Assert.Equal(ProphyApiClientConfiguration.DefaultRetryDelayMilliseconds, config.RetryDelayMilliseconds);
            Assert.Equal(ProphyApiClientConfiguration.DefaultMaxFileSize, config.MaxFileSize);
            Assert.Equal(ProphyApiClientConfiguration.DefaultUserAgent, config.UserAgent);
            Assert.False(config.EnableDetailedLogging);
            Assert.True(config.ValidateSslCertificates);
            Assert.Null(config.ApiKey);
            Assert.Null(config.OrganizationCode);
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenApiKeyIsNull()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                OrganizationCode = "test-org"
            };

            // Act & Assert
            Assert.False(config.IsValid);
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenOrganizationCodeIsNull()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key"
            };

            // Act & Assert
            Assert.False(config.IsValid);
        }

        [Fact]
        public void IsValid_ReturnsTrue_WhenRequiredFieldsAreSet()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org"
            };

            // Act & Assert
            Assert.True(config.IsValid);
        }

        [Fact]
        public void Validate_ReturnsApiKeyError_WhenApiKeyIsEmpty()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "",
                OrganizationCode = "test-org"
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("API key is required.", errors);
        }

        [Fact]
        public void Validate_ReturnsOrganizationCodeError_WhenOrganizationCodeIsEmpty()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = ""
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Organization code is required.", errors);
        }

        [Fact]
        public void Validate_ReturnsBaseUrlError_WhenBaseUrlIsInvalid()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                BaseUrl = "invalid-url"
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Base URL must be a valid HTTP or HTTPS URL.", errors);
        }

        [Fact]
        public void Validate_ReturnsTimeoutError_WhenTimeoutIsZero()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                TimeoutSeconds = 0
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Timeout seconds must be greater than zero.", errors);
        }

        [Fact]
        public void Validate_ReturnsRetryError_WhenMaxRetryAttemptsIsNegative()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                MaxRetryAttempts = -1
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Maximum retry attempts cannot be negative.", errors);
        }

        [Fact]
        public void Validate_ReturnsRetryDelayError_WhenRetryDelayIsNegative()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                RetryDelayMilliseconds = -1
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Retry delay milliseconds cannot be negative.", errors);
        }

        [Fact]
        public void Validate_ReturnsMaxFileSizeError_WhenMaxFileSizeIsZero()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                MaxFileSize = 0
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Maximum file size must be greater than zero.", errors);
        }

        [Fact]
        public void Validate_ReturnsNoErrors_WhenConfigurationIsValid()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org"
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_AcceptsHttpsUrl()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                BaseUrl = "https://api.example.com/"
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_AcceptsHttpUrl()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                BaseUrl = "http://localhost:8080/"
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Clone_CreatesExactCopy()
        {
            // Arrange
            var original = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                BaseUrl = "https://custom.api.com/",
                TimeoutSeconds = 120,
                MaxRetryAttempts = 5,
                RetryDelayMilliseconds = 2000,
                EnableDetailedLogging = true,
                MaxFileSize = 100 * 1024 * 1024,
                ValidateSslCertificates = false,
                UserAgent = "Custom-Agent/1.0"
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.ApiKey, clone.ApiKey);
            Assert.Equal(original.OrganizationCode, clone.OrganizationCode);
            Assert.Equal(original.BaseUrl, clone.BaseUrl);
            Assert.Equal(original.TimeoutSeconds, clone.TimeoutSeconds);
            Assert.Equal(original.MaxRetryAttempts, clone.MaxRetryAttempts);
            Assert.Equal(original.RetryDelayMilliseconds, clone.RetryDelayMilliseconds);
            Assert.Equal(original.EnableDetailedLogging, clone.EnableDetailedLogging);
            Assert.Equal(original.MaxFileSize, clone.MaxFileSize);
            Assert.Equal(original.ValidateSslCertificates, clone.ValidateSslCertificates);
            Assert.Equal(original.UserAgent, clone.UserAgent);
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org"
            };

            // Act
            var clone = original.Clone();
            clone.ApiKey = "modified-key";

            // Assert
            Assert.Equal("test-key", original.ApiKey);
            Assert.Equal("modified-key", clone.ApiKey);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_ReturnsApiKeyError_WhenApiKeyIsNullOrWhitespace(string? apiKey)
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = apiKey,
                OrganizationCode = "test-org"
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("API key is required.", errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_ReturnsOrganizationCodeError_WhenOrganizationCodeIsNullOrWhitespace(string? organizationCode)
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = organizationCode
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains("Organization code is required.", errors);
        }

        [Theory]
        [InlineData("ftp://example.com")]
        [InlineData("file:///path/to/file")]
        [InlineData("not-a-url")]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_ReturnsBaseUrlError_WhenBaseUrlIsInvalidOrNotHttpScheme(string? baseUrl)
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                BaseUrl = baseUrl
            };

            // Act
            var errors = config.Validate().ToList();

            // Assert
            Assert.Contains(errors, error => error.Contains("Base URL"));
        }
    }
} 