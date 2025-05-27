namespace Prophy.ApiClient.Extensions.DependencyInjection.Configuration
{
    /// <summary>
    /// Configuration options for multi-tenancy support.
    /// </summary>
    public class MultiTenancyOptions
    {
        /// <summary>
        /// Gets or sets the default configuration section name for the primary API client.
        /// </summary>
        public string DefaultConfigurationSection { get; set; } = "ProphyApiClient";

        /// <summary>
        /// Gets or sets the configuration section name containing tenant-specific configurations.
        /// </summary>
        public string? TenantsConfigurationSection { get; set; } = "Tenants";

        /// <summary>
        /// Gets or sets a value indicating whether to enable automatic tenant resolution from HTTP context.
        /// </summary>
        public bool EnableAutomaticTenantResolution { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to cache tenant configurations.
        /// </summary>
        public bool EnableConfigurationCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the cache expiration time for tenant configurations in minutes.
        /// </summary>
        public int ConfigurationCacheExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets a value indicating whether to validate tenant configurations on startup.
        /// </summary>
        public bool ValidateConfigurationsOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets the fallback behavior when a tenant configuration is not found.
        /// </summary>
        public TenantFallbackBehavior FallbackBehavior { get; set; } = TenantFallbackBehavior.UseDefault;
    }

    /// <summary>
    /// Defines the behavior when a tenant configuration is not found.
    /// </summary>
    public enum TenantFallbackBehavior
    {
        /// <summary>
        /// Use the default configuration when tenant configuration is not found.
        /// </summary>
        UseDefault,

        /// <summary>
        /// Throw an exception when tenant configuration is not found.
        /// </summary>
        ThrowException,

        /// <summary>
        /// Return null when tenant configuration is not found.
        /// </summary>
        ReturnNull
    }
} 