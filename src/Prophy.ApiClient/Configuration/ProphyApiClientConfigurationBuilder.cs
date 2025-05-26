using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Prophy.ApiClient.Configuration
{
    /// <summary>
    /// Fluent builder for creating ProphyApiClient configurations from multiple sources.
    /// </summary>
    public class ProphyApiClientConfigurationBuilder
    {
        private readonly IConfigurationBuilder _configurationBuilder;
        private readonly ProphyApiClientConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the ProphyApiClientConfigurationBuilder class.
        /// </summary>
        public ProphyApiClientConfigurationBuilder()
        {
            _configurationBuilder = new ConfigurationBuilder();
            _configuration = new ProphyApiClientConfiguration();
        }

        /// <summary>
        /// Adds configuration from an appsettings.json file.
        /// </summary>
        /// <param name="filePath">The path to the JSON configuration file. Defaults to "appsettings.json".</param>
        /// <param name="optional">Whether the file is optional. Defaults to true.</param>
        /// <param name="reloadOnChange">Whether to reload configuration when the file changes. Defaults to false.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder AddJsonFile(string filePath = "appsettings.json", bool optional = true, bool reloadOnChange = false)
        {
            _configurationBuilder.AddJsonFile(filePath, optional, reloadOnChange);
            return this;
        }

        /// <summary>
        /// Adds configuration from environment variables with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix for environment variables. Defaults to "PROPHY_".</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder AddEnvironmentVariables(string prefix = "PROPHY_")
        {
            _configurationBuilder.AddEnvironmentVariables(prefix);
            return this;
        }

        /// <summary>
        /// Adds configuration from a custom IConfiguration instance.
        /// </summary>
        /// <param name="configuration">The configuration instance to add.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder AddConfiguration(IConfiguration configuration)
        {
            _configurationBuilder.AddConfiguration(configuration);
            return this;
        }

        /// <summary>
        /// Sets the API key directly in code.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithApiKey(string apiKey)
        {
            _configuration.ApiKey = apiKey;
            return this;
        }

        /// <summary>
        /// Sets the organization code directly in code.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithOrganizationCode(string organizationCode)
        {
            _configuration.OrganizationCode = organizationCode;
            return this;
        }

        /// <summary>
        /// Sets the base URL directly in code.
        /// </summary>
        /// <param name="baseUrl">The base URL for the Prophy API.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithBaseUrl(string baseUrl)
        {
            _configuration.BaseUrl = baseUrl;
            return this;
        }

        /// <summary>
        /// Sets the HTTP timeout directly in code.
        /// </summary>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithTimeout(int timeoutSeconds)
        {
            _configuration.TimeoutSeconds = timeoutSeconds;
            return this;
        }

        /// <summary>
        /// Sets the retry configuration directly in code.
        /// </summary>
        /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
        /// <param name="retryDelayMilliseconds">The base delay in milliseconds for exponential backoff.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithRetryPolicy(int maxRetryAttempts, int retryDelayMilliseconds = 1000)
        {
            _configuration.MaxRetryAttempts = maxRetryAttempts;
            _configuration.RetryDelayMilliseconds = retryDelayMilliseconds;
            return this;
        }

        /// <summary>
        /// Enables or disables detailed logging.
        /// </summary>
        /// <param name="enableDetailedLogging">Whether to enable detailed logging.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithDetailedLogging(bool enableDetailedLogging = true)
        {
            _configuration.EnableDetailedLogging = enableDetailedLogging;
            return this;
        }

        /// <summary>
        /// Sets the maximum file size for uploads.
        /// </summary>
        /// <param name="maxFileSizeBytes">The maximum file size in bytes.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithMaxFileSize(long maxFileSizeBytes)
        {
            _configuration.MaxFileSize = maxFileSizeBytes;
            return this;
        }

        /// <summary>
        /// Sets SSL certificate validation behavior.
        /// </summary>
        /// <param name="validateSslCertificates">Whether to validate SSL certificates.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithSslValidation(bool validateSslCertificates = true)
        {
            _configuration.ValidateSslCertificates = validateSslCertificates;
            return this;
        }

        /// <summary>
        /// Sets the user agent string for HTTP requests.
        /// </summary>
        /// <param name="userAgent">The user agent string.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public ProphyApiClientConfigurationBuilder WithUserAgent(string userAgent)
        {
            _configuration.UserAgent = userAgent;
            return this;
        }

        /// <summary>
        /// Builds the configuration by merging all sources with in-code values taking precedence.
        /// </summary>
        /// <returns>A configured ProphyApiClientConfiguration instance.</returns>
        public IProphyApiClientConfiguration Build()
        {
            // Build the configuration from all sources
            var builtConfiguration = _configurationBuilder.Build();

            // Create a new configuration instance
            var result = _configuration.Clone();

            // Bind configuration from external sources, but don't override in-code values
            var section = builtConfiguration.GetSection("ProphyApiClient");
            if (section.Exists())
            {
                // Only bind values that haven't been set in code
                if (string.IsNullOrEmpty(result.ApiKey))
                    result.ApiKey = section["ApiKey"];

                if (string.IsNullOrEmpty(result.OrganizationCode))
                    result.OrganizationCode = section["OrganizationCode"];

                if (result.BaseUrl == ProphyApiClientConfiguration.DefaultBaseUrl)
                    result.BaseUrl = section["BaseUrl"] ?? result.BaseUrl;

                if (result.TimeoutSeconds == ProphyApiClientConfiguration.DefaultTimeoutSeconds)
                    if (int.TryParse(section["TimeoutSeconds"], out var timeout))
                        result.TimeoutSeconds = timeout;

                if (result.MaxRetryAttempts == ProphyApiClientConfiguration.DefaultMaxRetryAttempts)
                    if (int.TryParse(section["MaxRetryAttempts"], out var maxRetry))
                        result.MaxRetryAttempts = maxRetry;

                if (result.RetryDelayMilliseconds == ProphyApiClientConfiguration.DefaultRetryDelayMilliseconds)
                    if (int.TryParse(section["RetryDelayMilliseconds"], out var retryDelay))
                        result.RetryDelayMilliseconds = retryDelay;

                if (!result.EnableDetailedLogging)
                    if (bool.TryParse(section["EnableDetailedLogging"], out var enableLogging))
                        result.EnableDetailedLogging = enableLogging;

                if (result.MaxFileSize == ProphyApiClientConfiguration.DefaultMaxFileSize)
                    if (long.TryParse(section["MaxFileSize"], out var maxFileSize))
                        result.MaxFileSize = maxFileSize;

                if (result.ValidateSslCertificates)
                    if (bool.TryParse(section["ValidateSslCertificates"], out var validateSsl))
                        result.ValidateSslCertificates = validateSsl;

                if (result.UserAgent == ProphyApiClientConfiguration.DefaultUserAgent)
                    result.UserAgent = section["UserAgent"] ?? result.UserAgent;
            }

            return result;
        }

        /// <summary>
        /// Creates a new configuration builder with default settings.
        /// </summary>
        /// <returns>A new ProphyApiClientConfigurationBuilder instance.</returns>
        public static ProphyApiClientConfigurationBuilder Create()
        {
            return new ProphyApiClientConfigurationBuilder();
        }

        /// <summary>
        /// Creates a configuration builder with common default sources (appsettings.json and environment variables).
        /// </summary>
        /// <returns>A configured ProphyApiClientConfigurationBuilder instance.</returns>
        public static ProphyApiClientConfigurationBuilder CreateDefault()
        {
            return new ProphyApiClientConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables("PROPHY_");
        }
    }
} 