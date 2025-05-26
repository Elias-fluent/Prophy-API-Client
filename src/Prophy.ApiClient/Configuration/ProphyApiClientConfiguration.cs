using System;
using System.Collections.Generic;
using System.Linq;

namespace Prophy.ApiClient.Configuration
{
    /// <summary>
    /// Default implementation of IProphyApiClientConfiguration with sensible defaults.
    /// </summary>
    public class ProphyApiClientConfiguration : IProphyApiClientConfiguration
    {
        /// <summary>
        /// Default base URL for the Prophy API.
        /// </summary>
        public const string DefaultBaseUrl = "https://www.prophy.ai/api/";

        /// <summary>
        /// Default timeout in seconds.
        /// </summary>
        public const int DefaultTimeoutSeconds = 300; // 5 minutes

        /// <summary>
        /// Default maximum retry attempts.
        /// </summary>
        public const int DefaultMaxRetryAttempts = 3;

        /// <summary>
        /// Default retry delay in milliseconds.
        /// </summary>
        public const int DefaultRetryDelayMilliseconds = 1000; // 1 second

        /// <summary>
        /// Default maximum file size (50MB).
        /// </summary>
        public const long DefaultMaxFileSize = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Default user agent string.
        /// </summary>
        public const string DefaultUserAgent = "Prophy-ApiClient/1.0.0";

        /// <inheritdoc />
        public string? ApiKey { get; set; }

        /// <inheritdoc />
        public string? OrganizationCode { get; set; }

        /// <inheritdoc />
        public string? BaseUrl { get; set; } = DefaultBaseUrl;

        /// <inheritdoc />
        public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

        /// <inheritdoc />
        public int MaxRetryAttempts { get; set; } = DefaultMaxRetryAttempts;

        /// <inheritdoc />
        public int RetryDelayMilliseconds { get; set; } = DefaultRetryDelayMilliseconds;

        /// <inheritdoc />
        public bool EnableDetailedLogging { get; set; } = false;

        /// <inheritdoc />
        public long MaxFileSize { get; set; } = DefaultMaxFileSize;

        /// <inheritdoc />
        public bool ValidateSslCertificates { get; set; } = true;

        /// <inheritdoc />
        public string? UserAgent { get; set; } = DefaultUserAgent;

        /// <inheritdoc />
        public bool IsValid => !Validate().Any();

        /// <inheritdoc />
        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                errors.Add("API key is required.");
            }

            if (string.IsNullOrWhiteSpace(OrganizationCode))
            {
                errors.Add("Organization code is required.");
            }

            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                errors.Add("Base URL is required.");
            }
            else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || 
                     (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errors.Add("Base URL must be a valid HTTP or HTTPS URL.");
            }

            if (TimeoutSeconds <= 0)
            {
                errors.Add("Timeout seconds must be greater than zero.");
            }

            if (MaxRetryAttempts < 0)
            {
                errors.Add("Maximum retry attempts cannot be negative.");
            }

            if (RetryDelayMilliseconds < 0)
            {
                errors.Add("Retry delay milliseconds cannot be negative.");
            }

            if (MaxFileSize <= 0)
            {
                errors.Add("Maximum file size must be greater than zero.");
            }

            return errors;
        }

        /// <summary>
        /// Creates a copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same values.</returns>
        public ProphyApiClientConfiguration Clone()
        {
            return new ProphyApiClientConfiguration
            {
                ApiKey = ApiKey,
                OrganizationCode = OrganizationCode,
                BaseUrl = BaseUrl,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetryAttempts = MaxRetryAttempts,
                RetryDelayMilliseconds = RetryDelayMilliseconds,
                EnableDetailedLogging = EnableDetailedLogging,
                MaxFileSize = MaxFileSize,
                ValidateSslCertificates = ValidateSslCertificates,
                UserAgent = UserAgent
            };
        }
    }
} 