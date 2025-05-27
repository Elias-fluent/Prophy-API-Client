using System.Collections.Generic;
using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Extensions.DependencyInjection.Factories
{
    /// <summary>
    /// Factory interface for creating named Prophy API client instances for multi-tenant scenarios.
    /// </summary>
    public interface INamedProphyApiClientFactory : IProphyApiClientFactory
    {
        /// <summary>
        /// Creates a new instance of the Prophy API client using the named configuration.
        /// </summary>
        /// <param name="name">The name of the configuration to use.</param>
        /// <returns>A new <see cref="ProphyApiClient"/> instance.</returns>
        ProphyApiClient CreateNamedClient(string name);

        /// <summary>
        /// Creates a new instance of the multi-tenant Prophy API client using the named configuration.
        /// </summary>
        /// <param name="name">The name of the configuration to use.</param>
        /// <returns>A new multi-tenant <see cref="MultiTenantProphyApiClient"/> instance.</returns>
        MultiTenantProphyApiClient CreateNamedMultiTenantClient(string name);

        /// <summary>
        /// Creates a new instance of the Prophy API client for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A new <see cref="ProphyApiClient"/> instance configured for the specified tenant.</returns>
        ProphyApiClient CreateTenantClient(string tenantId);

        /// <summary>
        /// Creates a new instance of the multi-tenant Prophy API client for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A new multi-tenant <see cref="MultiTenantProphyApiClient"/> instance configured for the specified tenant.</returns>
        MultiTenantProphyApiClient CreateTenantMultiTenantClient(string tenantId);

        /// <summary>
        /// Gets all available named configurations.
        /// </summary>
        /// <returns>An enumerable of configuration names.</returns>
        IEnumerable<string> GetAvailableConfigurations();

        /// <summary>
        /// Checks if a named configuration exists.
        /// </summary>
        /// <param name="name">The name of the configuration to check.</param>
        /// <returns>True if the configuration exists; otherwise, false.</returns>
        bool HasConfiguration(string name);
    }
} 