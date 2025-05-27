using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Extensions.DependencyInjection.Configuration
{
    /// <summary>
    /// Configuration options for the Prophy API client using the options pattern.
    /// </summary>
    public class ProphyApiClientOptions
    {
        /// <summary>
        /// Gets or sets the base URL for the Prophy API.
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the timeout for HTTP requests.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed requests.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level for the API client.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the user agent string for HTTP requests.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the default headers to include with all requests.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the list of allowed IP addresses for security filtering.
        /// </summary>
        public List<string> AllowedIpAddresses { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of blocked IP addresses for security filtering.
        /// </summary>
        public List<string> BlockedIpAddresses { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of allowed CIDR ranges for security filtering.
        /// </summary>
        public List<string> AllowedCidrRanges { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of blocked CIDR ranges for security filtering.
        /// </summary>
        public List<string> BlockedCidrRanges { get; set; } = new List<string>();

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BaseUrl) && 
                   !string.IsNullOrWhiteSpace(ApiKey) &&
                   Timeout > TimeSpan.Zero &&
                   MaxRetryAttempts >= 0 &&
                   RetryDelay >= TimeSpan.Zero;
        }

        /// <summary>
        /// Gets validation errors for the current configuration.
        /// </summary>
        /// <returns>A list of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("BaseUrl is required.");

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("ApiKey is required.");

            if (Timeout <= TimeSpan.Zero)
                errors.Add("Timeout must be greater than zero.");

            if (MaxRetryAttempts < 0)
                errors.Add("MaxRetryAttempts must be non-negative.");

            if (RetryDelay < TimeSpan.Zero)
                errors.Add("RetryDelay must be non-negative.");

            return errors;
        }
    }
} 