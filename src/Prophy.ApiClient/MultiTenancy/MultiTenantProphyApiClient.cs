using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Multi-tenant aware version of the Prophy API client that automatically resolves
    /// tenant context and uses tenant-specific configurations.
    /// </summary>
    public class MultiTenantProphyApiClient : IDisposable
    {
        private readonly IOrganizationContextProvider _contextProvider;
        private readonly ITenantConfigurationProvider _configurationProvider;
        private readonly ITenantResolver _tenantResolver;
        private readonly TenantResolutionService _tenantResolutionService;
        private readonly ILogger<MultiTenantProphyApiClient> _logger;
        private readonly IHttpClientWrapper _httpClient;
        private readonly bool _disposeHttpClient;
        private bool _disposed;

        // API Modules - these will be tenant-aware
        private readonly Lazy<IManuscriptModule> _manuscripts;
        private readonly Lazy<ICustomFieldModule> _customFields;
        private readonly Lazy<IWebhookModule> _webhooks;
        private readonly Lazy<IJournalRecommendationModule> _journals;
        private readonly Lazy<IAuthorGroupModule> _authorGroups;
        private readonly Lazy<IResilienceModule> _resilience;

        /// <summary>
        /// Gets the current organization context.
        /// </summary>
        public OrganizationContext? CurrentContext => _contextProvider.GetCurrentContext();

        /// <summary>
        /// Gets the manuscript module for manuscript operations.
        /// </summary>
        public IManuscriptModule Manuscripts => _manuscripts.Value;

        /// <summary>
        /// Gets the custom fields module for custom field operations.
        /// </summary>
        public ICustomFieldModule CustomFields => _customFields.Value;

        /// <summary>
        /// Gets the webhook module for webhook processing and event handling.
        /// </summary>
        public IWebhookModule Webhooks => _webhooks.Value;

        /// <summary>
        /// Gets the journal recommendation module for journal recommendation operations.
        /// </summary>
        public IJournalRecommendationModule Journals => _journals.Value;

        /// <summary>
        /// Gets the author groups module for author group management operations.
        /// </summary>
        public IAuthorGroupModule AuthorGroups => _authorGroups.Value;

        /// <summary>
        /// Gets the resilience module for rate limiting, circuit breaker, and retry policies.
        /// </summary>
        public IResilienceModule Resilience => _resilience.Value;

        /// <summary>
        /// Initializes a new instance of the MultiTenantProphyApiClient class.
        /// </summary>
        /// <param name="contextProvider">The organization context provider.</param>
        /// <param name="configurationProvider">The tenant configuration provider.</param>
        /// <param name="tenantResolver">The tenant resolver.</param>
        /// <param name="httpClient">The HTTP client to use.</param>
        /// <param name="disposeHttpClient">Whether to dispose the HTTP client when this instance is disposed.</param>
        /// <param name="logger">The logger instance.</param>
        public MultiTenantProphyApiClient(
            IOrganizationContextProvider contextProvider,
            ITenantConfigurationProvider configurationProvider,
            ITenantResolver tenantResolver,
            HttpClient httpClient,
            bool disposeHttpClient = false,
            ILogger<MultiTenantProphyApiClient>? logger = null)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
            _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MultiTenantProphyApiClient>.Instance;
            _disposeHttpClient = disposeHttpClient;

            // Create tenant resolution service
            var resolutionLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantResolutionService>.Instance;
            _tenantResolutionService = new TenantResolutionService(_tenantResolver, _contextProvider, resolutionLogger);

            // Initialize resilience module first
            _resilience = new Lazy<IResilienceModule>(() => CreateResilienceModule());

            // Create HTTP client wrapper with tenant-aware request handling
            var httpClientLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantAwareHttpClientWrapper>.Instance;
            _httpClient = new TenantAwareHttpClientWrapper(httpClient, httpClientLogger, _resilience.Value, 
                _tenantResolutionService, _configurationProvider);

            // Initialize API modules
            _manuscripts = new Lazy<IManuscriptModule>(() => CreateManuscriptModule());
            _customFields = new Lazy<ICustomFieldModule>(() => CreateCustomFieldModule());
            _webhooks = new Lazy<IWebhookModule>(() => CreateWebhookModule());
            _journals = new Lazy<IJournalRecommendationModule>(() => CreateJournalRecommendationModule());
            _authorGroups = new Lazy<IAuthorGroupModule>(() => CreateAuthorGroupModule());

            _logger.LogInformation("MultiTenantProphyApiClient initialized");
        }

        /// <summary>
        /// Resolves and sets the tenant context for the given HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request to resolve tenant context for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the resolved organization context.</returns>
        public async Task<OrganizationContext?> ResolveContextAsync(HttpRequestMessage request)
        {
            return await _tenantResolutionService.ResolveAndSetContextAsync(request);
        }

        /// <summary>
        /// Sets the current tenant context explicitly.
        /// </summary>
        /// <param name="context">The organization context to set.</param>
        public void SetContext(OrganizationContext? context)
        {
            _contextProvider.SetCurrentContext(context);
            _logger.LogDebug("Tenant context set to: {OrganizationCode}", context?.OrganizationCode ?? "null");
        }

        /// <summary>
        /// Sets the current tenant context by organization code.
        /// </summary>
        /// <param name="organizationCode">The organization code to set as current context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SetContextAsync(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty.", nameof(organizationCode));

            var context = await _contextProvider.ResolveContextAsync(organizationCode);
            if (context == null)
            {
                throw new InvalidOperationException($"Could not resolve context for organization: {organizationCode}");
            }

            _contextProvider.SetCurrentContext(context);
            _logger.LogDebug("Tenant context set to: {OrganizationCode}", organizationCode);
        }

        /// <summary>
        /// Clears the current tenant context.
        /// </summary>
        public void ClearContext()
        {
            _contextProvider.ClearCurrentContext();
            _logger.LogDebug("Tenant context cleared");
        }

        /// <summary>
        /// Gets the current tenant-specific configuration.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the tenant-specific configuration.</returns>
        public async Task<IProphyApiClientConfiguration> GetConfigurationAsync()
        {
            return await _configurationProvider.GetConfigurationAsync();
        }

        /// <summary>
        /// Gets the HTTP client wrapper for making requests.
        /// </summary>
        /// <returns>The HTTP client wrapper.</returns>
        public IHttpClientWrapper GetHttpClient()
        {
            ThrowIfDisposed();
            return _httpClient;
        }

        /// <summary>
        /// Creates the manuscript module with tenant-aware configuration.
        /// </summary>
        /// <returns>The manuscript module instance.</returns>
        private IManuscriptModule CreateManuscriptModule()
        {
            var formDataBuilderLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<MultipartFormDataBuilder>.Instance;
            var formDataBuilder = new MultipartFormDataBuilder(formDataBuilderLogger);
            
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantAwareApiKeyAuthenticator>.Instance;
            var authenticator = new TenantAwareApiKeyAuthenticator(_contextProvider, _configurationProvider, authenticatorLogger);
            
            var manuscriptLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ManuscriptModule>.Instance;
            
            return new ManuscriptModule(_httpClient, authenticator, formDataBuilder, jsonSerializer, manuscriptLogger);
        }

        /// <summary>
        /// Creates the custom field module with tenant-aware configuration.
        /// </summary>
        /// <returns>The custom field module instance.</returns>
        private ICustomFieldModule CreateCustomFieldModule()
        {
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantAwareApiKeyAuthenticator>.Instance;
            var authenticator = new TenantAwareApiKeyAuthenticator(_contextProvider, _configurationProvider, authenticatorLogger);
            
            var customFieldLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomFieldModule>.Instance;
            
            return new CustomFieldModule(_httpClient, authenticator, jsonSerializer, customFieldLogger);
        }

        /// <summary>
        /// Creates the webhook module with tenant-aware configuration.
        /// </summary>
        /// <returns>The webhook module instance.</returns>
        private IWebhookModule CreateWebhookModule()
        {
            var validatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<WebhookValidator>.Instance;
            var validator = new WebhookValidator(validatorLogger);
            
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            var webhookLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<WebhookModule>.Instance;
            
            return new WebhookModule(validator, jsonSerializer, webhookLogger);
        }

        /// <summary>
        /// Creates the journal recommendation module with tenant-aware configuration.
        /// </summary>
        /// <returns>The journal recommendation module instance.</returns>
        private IJournalRecommendationModule CreateJournalRecommendationModule()
        {
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantAwareApiKeyAuthenticator>.Instance;
            var authenticator = new TenantAwareApiKeyAuthenticator(_contextProvider, _configurationProvider, authenticatorLogger);
            
            var journalLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<JournalRecommendationModule>.Instance;
            
            return new JournalRecommendationModule(_httpClient, authenticator, jsonSerializer, journalLogger);
        }

        /// <summary>
        /// Creates the author group module with tenant-aware configuration.
        /// </summary>
        /// <returns>The author group module instance.</returns>
        private IAuthorGroupModule CreateAuthorGroupModule()
        {
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantAwareApiKeyAuthenticator>.Instance;
            var authenticator = new TenantAwareApiKeyAuthenticator(_contextProvider, _configurationProvider, authenticatorLogger);
            
            var authorGroupLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthorGroupModule>.Instance;
            
            return new AuthorGroupModule(_httpClient, authenticator, jsonSerializer, authorGroupLogger);
        }

        /// <summary>
        /// Creates the resilience module with tenant-aware configuration.
        /// </summary>
        /// <returns>The resilience module instance.</returns>
        private IResilienceModule CreateResilienceModule()
        {
            var resilienceOptions = new ResilienceOptions
            {
                Enabled = true,
                RateLimiting = new RateLimitingOptions
                {
                    Enabled = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 10
                },
                CircuitBreaker = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureRatio = 0.5,
                    MinimumThroughput = 10,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30)
                },
                Retry = new RetryOptions
                {
                    Enabled = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                },
                Timeout = new TimeoutOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.FromSeconds(30)
                }
            };

            var resilienceLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ResilienceModule>.Instance;
            return new ResilienceModule(resilienceOptions, resilienceLogger);
        }

        /// <summary>
        /// Throws an ObjectDisposedException if this instance has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MultiTenantProphyApiClient));
        }

        /// <summary>
        /// Disposes the client and releases all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the client and releases all resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_disposeHttpClient && _httpClient is IDisposable disposableClient)
                {
                    disposableClient.Dispose();
                }

                _disposed = true;
                _logger.LogDebug("MultiTenantProphyApiClient disposed");
            }
        }
    }
} 