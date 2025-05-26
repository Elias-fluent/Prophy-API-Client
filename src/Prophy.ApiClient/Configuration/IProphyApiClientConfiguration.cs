using System;

namespace Prophy.ApiClient.Configuration
{
    /// <summary>
    /// Defines configuration options for the Prophy API Client.
    /// </summary>
    public interface IProphyApiClientConfiguration
    {
        /// <summary>
        /// Gets the API key for authentication.
        /// </summary>
        string? ApiKey { get; }

        /// <summary>
        /// Gets the organization code associated with the API key.
        /// </summary>
        string? OrganizationCode { get; }

        /// <summary>
        /// Gets the base URL for the Prophy API.
        /// </summary>
        string? BaseUrl { get; }

        /// <summary>
        /// Gets the HTTP client timeout in seconds.
        /// </summary>
        int TimeoutSeconds { get; }

        /// <summary>
        /// Gets the maximum number of retry attempts for failed requests.
        /// </summary>
        int MaxRetryAttempts { get; }

        /// <summary>
        /// Gets the base delay in milliseconds for exponential backoff retry policy.
        /// </summary>
        int RetryDelayMilliseconds { get; }

        /// <summary>
        /// Gets whether to enable detailed logging.
        /// </summary>
        bool EnableDetailedLogging { get; }

        /// <summary>
        /// Gets the maximum file size in bytes for manuscript uploads.
        /// </summary>
        long MaxFileSize { get; }

        /// <summary>
        /// Gets whether to validate SSL certificates.
        /// </summary>
        bool ValidateSslCertificates { get; }

        /// <summary>
        /// Gets the user agent string to use for HTTP requests.
        /// </summary>
        string? UserAgent { get; }

        /// <summary>
        /// Gets whether the configuration is valid and complete.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Validates the configuration and returns any validation errors.
        /// </summary>
        /// <returns>A collection of validation error messages, or empty if valid.</returns>
        System.Collections.Generic.IEnumerable<string> Validate();
    }
} 