using System.Threading.Tasks;
using Prophy.ApiClient.Configuration;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Defines the contract for providing tenant-specific configuration.
    /// </summary>
    public interface ITenantConfigurationProvider
    {
        /// <summary>
        /// Gets the configuration for the current tenant context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific configuration.</returns>
        Task<IProphyApiClientConfiguration> GetConfigurationAsync();

        /// <summary>
        /// Gets the configuration for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific configuration.</returns>
        Task<IProphyApiClientConfiguration> GetConfigurationAsync(string organizationCode);

        /// <summary>
        /// Gets the API key for the current tenant context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific API key.</returns>
        Task<string?> GetApiKeyAsync();

        /// <summary>
        /// Gets the API key for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific API key.</returns>
        Task<string?> GetApiKeyAsync(string organizationCode);

        /// <summary>
        /// Gets the base URL for the current tenant context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific base URL.</returns>
        Task<string?> GetBaseUrlAsync();

        /// <summary>
        /// Gets the base URL for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific base URL.</returns>
        Task<string?> GetBaseUrlAsync(string organizationCode);

        /// <summary>
        /// Sets the configuration for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <param name="configuration">The configuration to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetConfigurationAsync(string organizationCode, IProphyApiClientConfiguration configuration);

        /// <summary>
        /// Sets the API key for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <param name="apiKey">The API key to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetApiKeyAsync(string organizationCode, string apiKey);

        /// <summary>
        /// Sets the base URL for a specific organization.
        /// </summary>
        /// <param name="organizationCode">The organization code.</param>
        /// <param name="baseUrl">The base URL to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetBaseUrlAsync(string organizationCode, string baseUrl);
    }
} 