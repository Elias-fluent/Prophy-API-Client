using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Configuration;

namespace Prophy.ApiClient.Extensions.DependencyInjection.Configuration
{
    /// <summary>
    /// Implementation of <see cref="IProphyApiClientConfiguration"/> that wraps <see cref="ProphyApiClientOptions"/>.
    /// </summary>
    public class OptionsBasedConfiguration : IProphyApiClientConfiguration
    {
        private readonly ProphyApiClientOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsBasedConfiguration"/> class.
        /// </summary>
        /// <param name="options">The options to wrap.</param>
        public OptionsBasedConfiguration(ProphyApiClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public string? ApiKey => _options.ApiKey;

        /// <inheritdoc />
        public string? OrganizationCode => null; // Not supported in options pattern, use multi-tenancy instead

        /// <inheritdoc />
        public string? BaseUrl => _options.BaseUrl;

        /// <inheritdoc />
        public int TimeoutSeconds => (int)_options.Timeout.TotalSeconds;

        /// <inheritdoc />
        public int MaxRetryAttempts => _options.MaxRetryAttempts;

        /// <inheritdoc />
        public int RetryDelayMilliseconds => (int)_options.RetryDelay.TotalMilliseconds;

        /// <inheritdoc />
        public bool EnableDetailedLogging => _options.EnableLogging;

        /// <inheritdoc />
        public long MaxFileSize => 10 * 1024 * 1024; // Default 10MB, could be made configurable

        /// <inheritdoc />
        public bool ValidateSslCertificates => true; // Default to true for security, could be made configurable

        /// <inheritdoc />
        public string? UserAgent => _options.UserAgent;

        /// <inheritdoc />
        public bool IsValid => _options.IsValid();

        /// <inheritdoc />
        public IEnumerable<string> Validate() => _options.GetValidationErrors();

        /// <summary>
        /// Gets the timeout for HTTP requests.
        /// </summary>
        public TimeSpan Timeout => _options.Timeout;

        /// <summary>
        /// Gets the delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay => _options.RetryDelay;

        /// <summary>
        /// Gets a value indicating whether logging is enabled.
        /// </summary>
        public bool EnableLogging => _options.EnableLogging;

        /// <summary>
        /// Gets the log level for the API client.
        /// </summary>
        public LogLevel LogLevel => _options.LogLevel;

        /// <summary>
        /// Gets the default headers to include with all requests.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders => _options.DefaultHeaders;

        /// <summary>
        /// Gets the list of allowed IP addresses for security filtering.
        /// </summary>
        public List<string> AllowedIpAddresses => _options.AllowedIpAddresses;

        /// <summary>
        /// Gets the list of blocked IP addresses for security filtering.
        /// </summary>
        public List<string> BlockedIpAddresses => _options.BlockedIpAddresses;

        /// <summary>
        /// Gets the list of allowed CIDR ranges for security filtering.
        /// </summary>
        public List<string> AllowedCidrRanges => _options.AllowedCidrRanges;

        /// <summary>
        /// Gets the list of blocked CIDR ranges for security filtering.
        /// </summary>
        public List<string> BlockedCidrRanges => _options.BlockedCidrRanges;
    }
} 