using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Factory implementation for creating named Prophy API client instances for multi-tenant scenarios.
    /// </summary>
    public class NamedProphyApiClientFactory : INamedProphyApiClientFactory
    {
        private readonly IOptionsMonitor<ProphyApiClientOptions> _optionsMonitor;
        private readonly ILogger<NamedProphyApiClientFactory> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedProphyApiClientFactory"/> class.
        /// </summary>
        /// <param name="optionsMonitor">The options monitor for accessing named configurations.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        public NamedProphyApiClientFactory(
            IOptionsMonitor<ProphyApiClientOptions> optionsMonitor,
            ILogger<NamedProphyApiClientFactory> logger,
            IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public ProphyApiClient CreateClient()
        {
            var configuration = new OptionsBasedConfiguration(_optionsMonitor.CurrentValue);
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
            var configuration = new OptionsBasedConfiguration(_optionsMonitor.CurrentValue);
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

            if (contextProvider == null || tenantConfigProvider == null || tenantResolver == null)
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

        /// <inheritdoc />
        public ProphyApiClient CreateNamedClient(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            var options = _optionsMonitor.Get(name);
            var configuration = new OptionsBasedConfiguration(options);
            
            _logger.LogDebug("Creating named ProphyApiClient instance '{Name}' for organization: {OrganizationCode}", 
                name, configuration.OrganizationCode);

            return CreateClient(configuration);
        }

        /// <inheritdoc />
        public MultiTenantProphyApiClient CreateNamedMultiTenantClient(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            var options = _optionsMonitor.Get(name);
            var configuration = new OptionsBasedConfiguration(options);
            
            _logger.LogDebug("Creating named MultiTenantProphyApiClient instance '{Name}'", name);

            return CreateMultiTenantClient(configuration);
        }

        /// <inheritdoc />
        public ProphyApiClient CreateTenantClient(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            // Try to get tenant-specific configuration, fallback to default if not found
            var options = HasConfiguration(tenantId) ? _optionsMonitor.Get(tenantId) : _optionsMonitor.CurrentValue;
            var configuration = new OptionsBasedConfiguration(options);
            
            _logger.LogDebug("Creating tenant-specific ProphyApiClient instance for tenant: {TenantId}", tenantId);

            return CreateClient(configuration);
        }

        /// <inheritdoc />
        public MultiTenantProphyApiClient CreateTenantMultiTenantClient(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            // Try to get tenant-specific configuration, fallback to default if not found
            var options = HasConfiguration(tenantId) ? _optionsMonitor.Get(tenantId) : _optionsMonitor.CurrentValue;
            var configuration = new OptionsBasedConfiguration(options);
            
            _logger.LogDebug("Creating tenant-specific MultiTenantProphyApiClient instance for tenant: {TenantId}", tenantId);

            return CreateMultiTenantClient(configuration);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAvailableConfigurations()
        {
            // Note: IOptionsMonitor doesn't provide a way to enumerate all named configurations
            // This is a limitation of the Microsoft.Extensions.Options system
            // In a real implementation, you might need to track registered names separately
            _logger.LogWarning("GetAvailableConfigurations() is not fully supported due to IOptionsMonitor limitations. Consider tracking configuration names separately.");
            return Enumerable.Empty<string>();
        }

        /// <inheritdoc />
        public bool HasConfiguration(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            try
            {
                var options = _optionsMonitor.Get(name);
                // If we get here without exception, the configuration exists
                // However, this doesn't guarantee it's a valid configuration
                return options != null;
            }
            catch
            {
                return false;
            }
        }
    }
} 