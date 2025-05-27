using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Extensions.DependencyInjection.Factories
{
    /// <summary>
    /// Factory interface for creating Prophy API client instances.
    /// </summary>
    public interface IProphyApiClientFactory
    {
        /// <summary>
        /// Creates a new instance of the Prophy API client using the default configuration.
        /// </summary>
        /// <returns>A new <see cref="ProphyApiClient"/> instance.</returns>
        ProphyApiClient CreateClient();

        /// <summary>
        /// Creates a new instance of the Prophy API client using the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use for the client.</param>
        /// <returns>A new <see cref="ProphyApiClient"/> instance.</returns>
        ProphyApiClient CreateClient(IProphyApiClientConfiguration configuration);

        /// <summary>
        /// Creates a new instance of the multi-tenant Prophy API client using the default configuration.
        /// </summary>
        /// <returns>A new multi-tenant <see cref="MultiTenantProphyApiClient"/> instance.</returns>
        MultiTenantProphyApiClient CreateMultiTenantClient();

        /// <summary>
        /// Creates a new instance of the multi-tenant Prophy API client using the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use for the client.</param>
        /// <returns>A new multi-tenant <see cref="MultiTenantProphyApiClient"/> instance.</returns>
        MultiTenantProphyApiClient CreateMultiTenantClient(IProphyApiClientConfiguration configuration);
    }
} 