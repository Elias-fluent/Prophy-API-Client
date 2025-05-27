using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Extensions.DependencyInjection.Configuration;
using Prophy.ApiClient.Extensions.DependencyInjection.Factories;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Serialization;
using Prophy.ApiClient.Diagnostics;
using Prophy.ApiClient.MultiTenancy;

namespace Prophy.ApiClient.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> to register Prophy API client services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Prophy API client services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration to use for the API client.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddProphyApiClient(this IServiceCollection services, IProphyApiClientConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return services.AddProphyApiClient(options =>
            {
                options.BaseUrl = configuration.BaseUrl;
                options.ApiKey = configuration.ApiKey;
                options.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
                options.MaxRetryAttempts = configuration.MaxRetryAttempts;
                options.RetryDelay = TimeSpan.FromMilliseconds(configuration.RetryDelayMilliseconds);
                options.EnableLogging = configuration.EnableDetailedLogging;
                options.UserAgent = configuration.UserAgent;
                // Note: Other properties like DefaultHeaders, IP filtering are not part of the base interface
                // They can be configured directly in the options pattern
            });
        }

        /// <summary>
        /// Adds the Prophy API client services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">An action to configure the API client options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddProphyApiClient(this IServiceCollection services, Action<ProphyApiClientOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure options
            services.Configure(configureOptions);

            // Register core services
            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Adds the Prophy API client services to the specified <see cref="IServiceCollection"/> using configuration from the specified section.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">The name of the configuration section containing the API client settings. Defaults to "ProphyApiClient".</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddProphyApiClient(this IServiceCollection services, IConfiguration configuration, string sectionName = "ProphyApiClient")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Bind configuration
            services.Configure<ProphyApiClientOptions>(configuration.GetSection(sectionName));

            // Register core services
            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Adds the Prophy API client services with multi-tenancy support to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">The name of the configuration section containing the API client settings. Defaults to "ProphyApiClient".</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddProphyApiClientWithMultiTenancy(this IServiceCollection services, IConfiguration configuration, string sectionName = "ProphyApiClient")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Add base client services
            services.AddProphyApiClient(configuration, sectionName);

            // Register multi-tenancy services
            RegisterMultiTenancyServices(services);

            return services;
        }

        /// <summary>
        /// Adds the Prophy API client services with multi-tenancy support to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">An action to configure the API client options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddProphyApiClientWithMultiTenancy(this IServiceCollection services, Action<ProphyApiClientOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Add base client services
            services.AddProphyApiClient(configureOptions);

            // Register multi-tenancy services
            RegisterMultiTenancyServices(services);

            return services;
        }

        /// <summary>
        /// Adds a named Prophy API client configuration for multi-tenant scenarios.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configureOptions">An action to configure the API client options for this tenant.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddNamedProphyApiClient(this IServiceCollection services, string name, Action<ProphyApiClientOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure named options
            services.Configure<ProphyApiClientOptions>(name, configureOptions);

            // Register core services if not already registered
            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Adds a named Prophy API client configuration using configuration from the specified section.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">The name of the configuration section containing the API client settings.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddNamedProphyApiClient(this IServiceCollection services, string name, IConfiguration configuration, string sectionName)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or empty.", nameof(sectionName));

            // Bind named configuration
            services.Configure<ProphyApiClientOptions>(name, configuration.GetSection(sectionName));

            // Register core services if not already registered
            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Adds multiple tenant configurations from a configuration section containing tenant-specific settings.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="tenantsSection">The name of the configuration section containing tenant configurations. Defaults to "Tenants".</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMultiTenantProphyApiClients(this IServiceCollection services, IConfiguration configuration, string tenantsSection = "Tenants")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var tenantsConfig = configuration.GetSection(tenantsSection);
            if (!tenantsConfig.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{tenantsSection}' not found.");
            }

            // Register each tenant configuration
            foreach (var tenantSection in tenantsConfig.GetChildren())
            {
                var tenantName = tenantSection.Key;
                services.Configure<ProphyApiClientOptions>(tenantName, tenantSection);
            }

            // Register core services
            RegisterCoreServices(services);

            // Register multi-tenancy services
            RegisterMultiTenancyServices(services);

            return services;
        }

        /// <summary>
        /// Adds the Prophy API client services with advanced multi-tenancy support including named configurations and tenant resolution.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configureMultiTenancy">An action to configure multi-tenancy options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddAdvancedMultiTenantProphyApiClient(this IServiceCollection services, IConfiguration configuration, Action<MultiTenancyOptions>? configureMultiTenancy = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Configure multi-tenancy options
            var multiTenancyOptions = new MultiTenancyOptions();
            configureMultiTenancy?.Invoke(multiTenancyOptions);

            // Add default configuration
            services.AddProphyApiClient(configuration, multiTenancyOptions.DefaultConfigurationSection);

            // Add tenant-specific configurations if specified
            if (!string.IsNullOrWhiteSpace(multiTenancyOptions.TenantsConfigurationSection))
            {
                services.AddMultiTenantProphyApiClients(configuration, multiTenancyOptions.TenantsConfigurationSection);
            }

            // Register multi-tenancy services
            RegisterMultiTenancyServices(services);

            // Register enhanced factory
            services.TryAddSingleton<INamedProphyApiClientFactory, NamedProphyApiClientFactory>();

            return services;
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            // Register HTTP client wrapper
            services.TryAddSingleton<IHttpClientWrapper, HttpClientWrapper>();

            // Register authentication services
            services.TryAddSingleton<IApiKeyAuthenticator, ApiKeyAuthenticator>();

            // Register serialization services
            services.TryAddSingleton<IJsonSerializer>(provider => SerializationFactory.CreateJsonSerializer());

            // Note: Diagnostics are handled via static methods in DiagnosticEvents class

            // Register configuration wrapper
            services.TryAddSingleton<IProphyApiClientConfiguration>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ProphyApiClientOptions>>().Value;
                return new OptionsBasedConfiguration(options);
            });

            // Register client factory
            services.TryAddSingleton<IProphyApiClientFactory, ProphyApiClientFactory>();

            // Register the main API client as scoped to allow for proper disposal
            services.TryAddScoped<ProphyApiClient>(provider =>
            {
                var factory = provider.GetRequiredService<IProphyApiClientFactory>();
                return factory.CreateClient();
            });
        }

        private static void RegisterMultiTenancyServices(IServiceCollection services)
        {
            // Register organization context provider
            services.TryAddSingleton<IOrganizationContextProvider, OrganizationContextProvider>();

            // Register tenant configuration provider
            services.TryAddSingleton<ITenantConfigurationProvider, TenantConfigurationProvider>();

            // Register tenant resolver
            services.TryAddSingleton<ITenantResolver, TenantResolver>();

            // Register tenant resolution service
            services.TryAddSingleton<TenantResolutionService>();

            // Replace HTTP client wrapper with tenant-aware version
            services.Replace(ServiceDescriptor.Singleton<IHttpClientWrapper, TenantAwareHttpClientWrapper>());

            // Replace authenticator with tenant-aware version
            services.Replace(ServiceDescriptor.Singleton<IApiKeyAuthenticator, TenantAwareApiKeyAuthenticator>());

            // Register the multi-tenant API client separately
            services.TryAddScoped<MultiTenantProphyApiClient>(provider =>
            {
                var factory = provider.GetRequiredService<IProphyApiClientFactory>();
                return factory.CreateMultiTenantClient();
            });
        }
    }
} 