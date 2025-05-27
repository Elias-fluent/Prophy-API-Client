using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Configuration;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Provides tenant-specific configuration management with caching and fallback support.
    /// </summary>
    public class TenantConfigurationProvider : ITenantConfigurationProvider
    {
        private readonly IOrganizationContextProvider _contextProvider;
        private readonly IProphyApiClientConfiguration _defaultConfiguration;
        private readonly ConcurrentDictionary<string, IProphyApiClientConfiguration> _configurationCache;
        private readonly ILogger<TenantConfigurationProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the TenantConfigurationProvider class.
        /// </summary>
        /// <param name="contextProvider">The organization context provider.</param>
        /// <param name="defaultConfiguration">The default configuration to use as fallback.</param>
        /// <param name="logger">The logger instance.</param>
        public TenantConfigurationProvider(
            IOrganizationContextProvider contextProvider,
            IProphyApiClientConfiguration defaultConfiguration,
            ILogger<TenantConfigurationProvider> logger)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _defaultConfiguration = defaultConfiguration ?? throw new ArgumentNullException(nameof(defaultConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationCache = new ConcurrentDictionary<string, IProphyApiClientConfiguration>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the configuration for the current tenant context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific configuration.</returns>
        public async Task<IProphyApiClientConfiguration> GetConfigurationAsync()
        {
            var context = _contextProvider.GetCurrentContext();
            if (context == null)
            {
                _logger.LogDebug("No organization context found, returning default configuration");
                return _defaultConfiguration;
            }

            return await GetConfigurationAsync(context.OrganizationCode);
        }

        /// <summary>
        /// Gets the configuration for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific configuration.</returns>
        public async Task<IProphyApiClientConfiguration> GetConfigurationAsync(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
            {
                _logger.LogWarning("Organization code is null or empty, returning default configuration");
                return _defaultConfiguration;
            }

            try
            {
                // Check cache first
                if (_configurationCache.TryGetValue(organizationCode, out var cachedConfig))
                {
                    _logger.LogDebug("Retrieved cached configuration for organization: {OrganizationCode}", organizationCode);
                    return cachedConfig;
                }

                // Create tenant-specific configuration
                var tenantConfig = await CreateTenantConfigurationAsync(organizationCode);
                
                // Cache the configuration
                _configurationCache.TryAdd(organizationCode, tenantConfig);
                
                _logger.LogInformation("Created and cached configuration for organization: {OrganizationCode}", organizationCode);
                return tenantConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get configuration for organization: {OrganizationCode}, falling back to default", organizationCode);
                return _defaultConfiguration;
            }
        }

        /// <summary>
        /// Invalidates the cached configuration for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task InvalidateConfigurationAsync(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                return Task.CompletedTask;

            _configurationCache.TryRemove(organizationCode, out _);
            _logger.LogInformation("Invalidated cached configuration for organization: {OrganizationCode}", organizationCode);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the API key for the current tenant context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific API key.</returns>
        public async Task<string?> GetApiKeyAsync()
        {
            var config = await GetConfigurationAsync();
            return config.ApiKey;
        }

        /// <summary>
        /// Gets the API key for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific API key.</returns>
        public async Task<string?> GetApiKeyAsync(string organizationCode)
        {
            var config = await GetConfigurationAsync(organizationCode);
            return config.ApiKey;
        }

        /// <summary>
        /// Gets the base URL for the current tenant context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific base URL.</returns>
        public async Task<string?> GetBaseUrlAsync()
        {
            var config = await GetConfigurationAsync();
            return config.BaseUrl;
        }

        /// <summary>
        /// Gets the base URL for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific base URL.</returns>
        public async Task<string?> GetBaseUrlAsync(string organizationCode)
        {
            var config = await GetConfigurationAsync(organizationCode);
            return config.BaseUrl;
        }

        /// <summary>
        /// Sets the configuration for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <param name="configuration">The configuration to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SetConfigurationAsync(string organizationCode, IProphyApiClientConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty", nameof(organizationCode));
            
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _configurationCache.AddOrUpdate(organizationCode, configuration, (key, oldValue) => configuration);
            _logger.LogInformation("Updated configuration for organization: {OrganizationCode}", organizationCode);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the API key for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <param name="apiKey">The API key to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SetApiKeyAsync(string organizationCode, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty", nameof(organizationCode));

            var context = await _contextProvider.ResolveContextAsync(organizationCode);
            if (context != null)
            {
                var updatedContext = context.WithApiKey(apiKey);
                _contextProvider.SetCurrentContext(updatedContext);
                
                // Invalidate cached configuration to force refresh
                await InvalidateConfigurationAsync(organizationCode);
                
                _logger.LogInformation("Updated API key for organization: {OrganizationCode}", organizationCode);
            }
        }

        /// <summary>
        /// Sets the base URL for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <param name="baseUrl">The base URL to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SetBaseUrlAsync(string organizationCode, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty", nameof(organizationCode));

            var context = await _contextProvider.ResolveContextAsync(organizationCode);
            if (context != null)
            {
                var updatedContext = context.WithBaseUrl(baseUrl);
                _contextProvider.SetCurrentContext(updatedContext);
                
                // Invalidate cached configuration to force refresh
                await InvalidateConfigurationAsync(organizationCode);
                
                _logger.LogInformation("Updated base URL for organization: {OrganizationCode}", organizationCode);
            }
        }

        /// <summary>
        /// Clears all cached configurations.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ClearCacheAsync()
        {
            _configurationCache.Clear();
            _logger.LogInformation("Cleared all cached tenant configurations");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a tenant-specific configuration by merging organization context with default configuration.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific configuration.</returns>
        private async Task<IProphyApiClientConfiguration> CreateTenantConfigurationAsync(string organizationCode)
        {
            var context = await _contextProvider.ResolveContextAsync(organizationCode);
            if (context == null)
            {
                _logger.LogWarning("Could not resolve context for organization: {OrganizationCode}, using default configuration", organizationCode);
                return _defaultConfiguration;
            }

            // Create a tenant-specific configuration wrapper
            return new TenantAwareConfiguration(_defaultConfiguration, context, _logger);
        }
    }

    /// <summary>
    /// A configuration wrapper that provides tenant-specific values while falling back to default configuration.
    /// </summary>
    internal class TenantAwareConfiguration : IProphyApiClientConfiguration
    {
        private readonly IProphyApiClientConfiguration _defaultConfiguration;
        private readonly OrganizationContext _organizationContext;
        private readonly ILogger _logger;

        public TenantAwareConfiguration(
            IProphyApiClientConfiguration defaultConfiguration,
            OrganizationContext organizationContext,
            ILogger logger)
        {
            _defaultConfiguration = defaultConfiguration ?? throw new ArgumentNullException(nameof(defaultConfiguration));
            _organizationContext = organizationContext ?? throw new ArgumentNullException(nameof(organizationContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the API key, preferring the tenant-specific key if available.
        /// </summary>
        public string? ApiKey => !string.IsNullOrWhiteSpace(_organizationContext.ApiKey) 
            ? _organizationContext.ApiKey 
            : _defaultConfiguration.ApiKey;

        /// <summary>
        /// Gets the base URL, preferring the tenant-specific URL if available.
        /// </summary>
        public string? BaseUrl => !string.IsNullOrWhiteSpace(_organizationContext.BaseUrl) 
            ? _organizationContext.BaseUrl 
            : _defaultConfiguration.BaseUrl;

        /// <summary>
        /// Gets the organization code from the tenant context.
        /// </summary>
        public string? OrganizationCode => _organizationContext.OrganizationCode;

        /// <summary>
        /// Gets the HTTP client timeout in seconds from the default configuration.
        /// </summary>
        public int TimeoutSeconds => _defaultConfiguration.TimeoutSeconds;

        /// <summary>
        /// Gets the maximum number of retry attempts from the default configuration.
        /// </summary>
        public int MaxRetryAttempts => _defaultConfiguration.MaxRetryAttempts;

        /// <summary>
        /// Gets the base delay in milliseconds for exponential backoff retry policy from the default configuration.
        /// </summary>
        public int RetryDelayMilliseconds => _defaultConfiguration.RetryDelayMilliseconds;

        /// <summary>
        /// Gets whether to enable detailed logging from the default configuration.
        /// </summary>
        public bool EnableDetailedLogging => _defaultConfiguration.EnableDetailedLogging;

        /// <summary>
        /// Gets the maximum file size in bytes for manuscript uploads from the default configuration.
        /// </summary>
        public long MaxFileSize => _defaultConfiguration.MaxFileSize;

        /// <summary>
        /// Gets whether to validate SSL certificates from the default configuration.
        /// </summary>
        public bool ValidateSslCertificates => _defaultConfiguration.ValidateSslCertificates;

        /// <summary>
        /// Gets the user agent string from the default configuration.
        /// </summary>
        public string? UserAgent => _defaultConfiguration.UserAgent;

        /// <summary>
        /// Gets whether the configuration is valid and complete.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey) && 
                               !string.IsNullOrWhiteSpace(BaseUrl) && 
                               !string.IsNullOrWhiteSpace(OrganizationCode) &&
                               _defaultConfiguration.IsValid;

        /// <summary>
        /// Validates the configuration and returns any validation errors.
        /// </summary>
        /// <returns>A collection of validation error messages, or empty if valid.</returns>
        public System.Collections.Generic.IEnumerable<string> Validate()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("API key is required");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("Base URL is required");

            if (string.IsNullOrWhiteSpace(OrganizationCode))
                errors.Add("Organization code is required");

            // Include validation errors from the default configuration
            errors.AddRange(_defaultConfiguration.Validate());

            return errors;
        }
    }
} 