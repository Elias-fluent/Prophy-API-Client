using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Extensions.DependencyInjection.Configuration;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Extensions.DependencyInjection.Factories
{
    /// <summary>
    /// Factory implementation for creating Prophy API client instances.
    /// </summary>
    public class ProphyApiClientFactory : IProphyApiClientFactory
    {
        private readonly IOptions<ProphyApiClientOptions> _options;
        private readonly ILogger<ProphyApiClientFactory> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProphyApiClientFactory"/> class.
        /// </summary>
        /// <param name="options">The configured options for the API client.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        public ProphyApiClientFactory(
            IOptions<ProphyApiClientOptions> options,
            ILogger<ProphyApiClientFactory> logger,
            IServiceProvider serviceProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public ProphyApiClient CreateClient()
        {
            var configuration = new OptionsBasedConfiguration(_options.Value);
            return CreateClient(configuration);
        }

        /// <inheritdoc />
        public ProphyApiClient CreateClient(IProphyApiClientConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _logger.LogDebug("Creating new ProphyApiClient instance with configuration for organization: {OrganizationCode}", 
                configuration.OrganizationCode);

            var clientLogger = _serviceProvider.GetService(typeof(ILogger<ProphyApiClient>)) as ILogger<ProphyApiClient>;
            return new ProphyApiClient(configuration, clientLogger);
        }

        /// <inheritdoc />
        public MultiTenantProphyApiClient CreateMultiTenantClient()
        {
            var configuration = new OptionsBasedConfiguration(_options.Value);
            return CreateMultiTenantClient(configuration);
        }

        /// <inheritdoc />
        public MultiTenantProphyApiClient CreateMultiTenantClient(IProphyApiClientConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _logger.LogDebug("Creating new MultiTenantProphyApiClient instance with configuration");

            // Get multi-tenancy services from DI container
            var contextProvider = _serviceProvider.GetService(typeof(IOrganizationContextProvider)) as IOrganizationContextProvider;
            var tenantConfigProvider = _serviceProvider.GetService(typeof(ITenantConfigurationProvider)) as ITenantConfigurationProvider;
            var tenantResolver = _serviceProvider.GetService(typeof(ITenantResolver)) as ITenantResolver;
            var resolutionService = _serviceProvider.GetService(typeof(TenantResolutionService)) as TenantResolutionService;

            if (contextProvider == null || tenantConfigProvider == null || tenantResolver == null || resolutionService == null)
            {
                throw new InvalidOperationException(
                    "Multi-tenancy services are not registered. Please use AddProphyApiClientWithMultiTenancy() to register multi-tenancy support.");
            }

            var clientLogger = _serviceProvider.GetService(typeof(ILogger<MultiTenantProphyApiClient>)) as ILogger<MultiTenantProphyApiClient>;
            
            // Create HTTP client for the multi-tenant client
            var httpClient = new HttpClient();
            
            // Create the multi-tenant client
            return new MultiTenantProphyApiClient(
                contextProvider,
                tenantConfigProvider,
                tenantResolver,
                httpClient,
                true, // dispose HTTP client
                clientLogger);
        }
    }
} 