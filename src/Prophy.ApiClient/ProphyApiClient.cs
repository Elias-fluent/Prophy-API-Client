using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient
{
    /// <summary>
    /// Main client for interacting with the Prophy API.
    /// Provides a high-level interface for all Prophy API operations.
    /// </summary>
    public class ProphyApiClient : IDisposable
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IApiKeyAuthenticator _authenticator;
        private readonly ILogger<ProphyApiClient> _logger;
        private readonly bool _disposeHttpClient;
        private bool _disposed;

        // API Modules
        private readonly Lazy<IManuscriptModule> _manuscripts;
        private readonly Lazy<ICustomFieldModule> _customFields;
        private readonly Lazy<IWebhookModule> _webhooks;
        private readonly Lazy<IJournalRecommendationModule> _journals;
        private readonly Lazy<IAuthorGroupModule> _authorGroups;
        private readonly Lazy<IResilienceModule> _resilience;

        /// <summary>
        /// Gets the base URL for the Prophy API.
        /// </summary>
        public Uri BaseUrl { get; }

        /// <summary>
        /// Gets the organization code associated with this client instance.
        /// </summary>
        public string OrganizationCode { get; private set; }

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
        /// Initializes a new instance of the ProphyApiClient class with the specified API key and organization code.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="organizationCode">The organization code associated with the API key.</param>
        /// <param name="baseUrl">The base URL for the Prophy API. Defaults to https://www.prophy.ai/api/</param>
        /// <param name="logger">Optional logger instance. If not provided, a null logger will be used.</param>
        public ProphyApiClient(string apiKey, string organizationCode, string? baseUrl = null, ILogger<ProphyApiClient>? logger = null)
            : this(apiKey, organizationCode, CreateDefaultHttpClient(baseUrl), true, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ProphyApiClient class with a configuration object.
        /// </summary>
        /// <param name="configuration">The configuration object containing all client settings.</param>
        /// <param name="logger">Optional logger instance. If not provided, a null logger will be used.</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        public ProphyApiClient(IProphyApiClientConfiguration configuration, ILogger<ProphyApiClient>? logger = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Validate configuration
            var validationErrors = configuration.Validate().ToList();
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Configuration is invalid: {string.Join(", ", validationErrors)}", nameof(configuration));
            }

            var httpClient = CreateHttpClientFromConfiguration(configuration);
            
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProphyApiClient>.Instance;
            _disposeHttpClient = true;
            OrganizationCode = configuration.OrganizationCode!;

            // Set base URL
            BaseUrl = httpClient.BaseAddress ?? new Uri(configuration.BaseUrl!);
            if (httpClient.BaseAddress == null)
            {
                httpClient.BaseAddress = BaseUrl;
            }

            // Create authenticator
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiKeyAuthenticator>.Instance;
            _authenticator = new ApiKeyAuthenticator(configuration.ApiKey!, authenticatorLogger);
            _authenticator.SetOrganizationCode(configuration.OrganizationCode!);

            // Initialize resilience module first
            _resilience = new Lazy<IResilienceModule>(() => CreateResilienceModule());

            // Create HTTP client wrapper with resilience support
            var httpClientLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HttpClientWrapper>.Instance;
            _httpClient = new HttpClientWrapper(httpClient, httpClientLogger, _resilience.Value);

            // Initialize API modules
            _manuscripts = new Lazy<IManuscriptModule>(() => CreateManuscriptModule());
            _customFields = new Lazy<ICustomFieldModule>(() => CreateCustomFieldModule());
            _webhooks = new Lazy<IWebhookModule>(() => CreateWebhookModule());
            _journals = new Lazy<IJournalRecommendationModule>(() => CreateJournalRecommendationModule());
            _authorGroups = new Lazy<IAuthorGroupModule>(() => CreateAuthorGroupModule());

            _logger.LogInformation("ProphyApiClient initialized for organization: {OrganizationCode}, Base URL: {BaseUrl}", 
                OrganizationCode, BaseUrl);
        }

        /// <summary>
        /// Initializes a new instance of the ProphyApiClient class with a configuration object and custom HttpClient.
        /// </summary>
        /// <param name="configuration">The configuration object containing all client settings.</param>
        /// <param name="httpClient">The HttpClient instance to use for HTTP operations.</param>
        /// <param name="disposeHttpClient">Whether to dispose the HttpClient when this instance is disposed.</param>
        /// <param name="logger">Optional logger instance. If not provided, a null logger will be used.</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration or httpClient is null.</exception>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        public ProphyApiClient(IProphyApiClientConfiguration configuration, HttpClient httpClient, bool disposeHttpClient = false, ILogger<ProphyApiClient>? logger = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            // Validate configuration
            var validationErrors = configuration.Validate().ToList();
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Configuration is invalid: {string.Join(", ", validationErrors)}", nameof(configuration));
            }

            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProphyApiClient>.Instance;
            _disposeHttpClient = disposeHttpClient;
            OrganizationCode = configuration.OrganizationCode!;

            // Configure HttpClient from configuration
            ConfigureHttpClientFromConfiguration(httpClient, configuration);

            // Set base URL
            BaseUrl = httpClient.BaseAddress ?? new Uri(configuration.BaseUrl!);
            if (httpClient.BaseAddress == null)
            {
                httpClient.BaseAddress = BaseUrl;
            }

            // Create authenticator
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiKeyAuthenticator>.Instance;
            _authenticator = new ApiKeyAuthenticator(configuration.ApiKey!, authenticatorLogger);
            _authenticator.SetOrganizationCode(configuration.OrganizationCode!);

            // Initialize resilience module first
            _resilience = new Lazy<IResilienceModule>(() => CreateResilienceModule());

            // Create HTTP client wrapper with resilience support
            var httpClientLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HttpClientWrapper>.Instance;
            _httpClient = new HttpClientWrapper(httpClient, httpClientLogger, _resilience.Value);

            // Initialize API modules
            _manuscripts = new Lazy<IManuscriptModule>(() => CreateManuscriptModule());
            _customFields = new Lazy<ICustomFieldModule>(() => CreateCustomFieldModule());
            _webhooks = new Lazy<IWebhookModule>(() => CreateWebhookModule());
            _journals = new Lazy<IJournalRecommendationModule>(() => CreateJournalRecommendationModule());
            _authorGroups = new Lazy<IAuthorGroupModule>(() => CreateAuthorGroupModule());

            _logger.LogInformation("ProphyApiClient initialized for organization: {OrganizationCode}, Base URL: {BaseUrl}", 
                OrganizationCode, BaseUrl);
        }

        /// <summary>
        /// Initializes a new instance of the ProphyApiClient class with a custom HttpClient.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="organizationCode">The organization code associated with the API key.</param>
        /// <param name="httpClient">The HttpClient instance to use for HTTP operations.</param>
        /// <param name="disposeHttpClient">Whether to dispose the HttpClient when this instance is disposed.</param>
        /// <param name="logger">Optional logger instance. If not provided, a null logger will be used.</param>
        public ProphyApiClient(string apiKey, string organizationCode, HttpClient httpClient, bool disposeHttpClient = false, ILogger<ProphyApiClient>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty.", nameof(organizationCode));

            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProphyApiClient>.Instance;
            _disposeHttpClient = disposeHttpClient;
            OrganizationCode = organizationCode;

            // Set base URL
            BaseUrl = httpClient.BaseAddress ?? new Uri("https://www.prophy.ai/api/");
            if (httpClient.BaseAddress == null)
            {
                httpClient.BaseAddress = BaseUrl;
            }

            // Create authenticator
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiKeyAuthenticator>.Instance;
            _authenticator = new ApiKeyAuthenticator(apiKey, authenticatorLogger);
            _authenticator.SetOrganizationCode(organizationCode);

            // Initialize resilience module first
            _resilience = new Lazy<IResilienceModule>(() => CreateResilienceModule());

            // Create HTTP client wrapper with resilience support
            var httpClientLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HttpClientWrapper>.Instance;
            _httpClient = new HttpClientWrapper(httpClient, httpClientLogger, _resilience.Value);

            // Initialize API modules
            _manuscripts = new Lazy<IManuscriptModule>(() => CreateManuscriptModule());
            _customFields = new Lazy<ICustomFieldModule>(() => CreateCustomFieldModule());
            _webhooks = new Lazy<IWebhookModule>(() => CreateWebhookModule());
            _journals = new Lazy<IJournalRecommendationModule>(() => CreateJournalRecommendationModule());
            _authorGroups = new Lazy<IAuthorGroupModule>(() => CreateAuthorGroupModule());

            _logger.LogInformation("ProphyApiClient initialized for organization: {OrganizationCode}, Base URL: {BaseUrl}", 
                organizationCode, BaseUrl);
        }

        /// <summary>
        /// Creates a default HttpClient with standard configuration for the Prophy API.
        /// </summary>
        /// <param name="baseUrl">The base URL for the API.</param>
        /// <returns>A configured HttpClient instance.</returns>
        private static HttpClient CreateDefaultHttpClient(string? baseUrl = null)
        {
            var httpClient = new HttpClient();
            
            // Set base address
            var apiBaseUrl = !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl : "https://www.prophy.ai/api/";
            httpClient.BaseAddress = new Uri(apiBaseUrl);
            
            // Set default timeout
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            // Set default headers
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Prophy-ApiClient/1.0.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            return httpClient;
        }

        /// <summary>
        /// Creates an HttpClient configured from the provided configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use for HttpClient setup.</param>
        /// <returns>A configured HttpClient instance.</returns>
        private static HttpClient CreateHttpClientFromConfiguration(IProphyApiClientConfiguration configuration)
        {
            var httpClient = new HttpClient();
            ConfigureHttpClientFromConfiguration(httpClient, configuration);
            return httpClient;
        }

        /// <summary>
        /// Configures an existing HttpClient from the provided configuration.
        /// </summary>
        /// <param name="httpClient">The HttpClient to configure.</param>
        /// <param name="configuration">The configuration to apply.</param>
        private static void ConfigureHttpClientFromConfiguration(HttpClient httpClient, IProphyApiClientConfiguration configuration)
        {
            // Set base address
            httpClient.BaseAddress = new Uri(configuration.BaseUrl!);
            
            // Set timeout
            httpClient.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
            
            // Set default headers
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", configuration.UserAgent ?? ProphyApiClientConfiguration.DefaultUserAgent);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Gets the HTTP client wrapper for making raw HTTP requests.
        /// This is primarily intended for advanced scenarios and testing.
        /// </summary>
        /// <returns>The HTTP client wrapper instance.</returns>
        public IHttpClientWrapper GetHttpClient()
        {
            ThrowIfDisposed();
            return _httpClient;
        }

        /// <summary>
        /// Gets the authenticator instance for this client.
        /// This is primarily intended for advanced scenarios and testing.
        /// </summary>
        /// <returns>The API key authenticator instance.</returns>
        public IApiKeyAuthenticator GetAuthenticator()
        {
            ThrowIfDisposed();
            return _authenticator;
        }

        /// <summary>
        /// Creates a new instance of the manuscript module.
        /// </summary>
        /// <returns>A configured manuscript module instance.</returns>
        private IManuscriptModule CreateManuscriptModule()
        {
            var formDataBuilderLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<MultipartFormDataBuilder>.Instance;
            var formDataBuilder = new MultipartFormDataBuilder(formDataBuilderLogger);
            
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            
            var manuscriptLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ManuscriptModule>.Instance;
            
            return new ManuscriptModule(_httpClient, _authenticator, formDataBuilder, jsonSerializer, manuscriptLogger);
        }

        /// <summary>
        /// Creates a new instance of the custom field module.
        /// </summary>
        /// <returns>A configured custom field module instance.</returns>
        private ICustomFieldModule CreateCustomFieldModule()
        {
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            var customFieldLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomFieldModule>.Instance;
            
            return new CustomFieldModule(_httpClient, _authenticator, jsonSerializer, customFieldLogger);
        }

        /// <summary>
        /// Creates a new instance of the webhook module.
        /// </summary>
        /// <returns>A configured webhook module instance.</returns>
        private IWebhookModule CreateWebhookModule()
        {
            var validatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<WebhookValidator>.Instance;
            var validator = new WebhookValidator(validatorLogger);
            
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            var webhookLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<WebhookModule>.Instance;
            
            return new WebhookModule(validator, jsonSerializer, webhookLogger);
        }

        /// <summary>
        /// Creates a new instance of the journal recommendation module.
        /// </summary>
        /// <returns>A configured journal recommendation module instance.</returns>
        private IJournalRecommendationModule CreateJournalRecommendationModule()
        {
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            var journalLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<JournalRecommendationModule>.Instance;
            
            return new JournalRecommendationModule(_httpClient, _authenticator, jsonSerializer, journalLogger);
        }

        /// <summary>
        /// Creates a new instance of the author group module.
        /// </summary>
        /// <returns>A configured author group module instance.</returns>
        private IAuthorGroupModule CreateAuthorGroupModule()
        {
            var jsonSerializer = SerializationFactory.CreateJsonSerializer();
            var authorGroupLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthorGroupModule>.Instance;
            
            return new AuthorGroupModule(_httpClient, _authenticator, jsonSerializer, authorGroupLogger);
        }

        /// <summary>
        /// Creates a new instance of the resilience module.
        /// </summary>
        /// <returns>A configured resilience module instance.</returns>
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProphyApiClient));
        }

        /// <summary>
        /// Releases all resources used by the ProphyApiClient.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the ProphyApiClient and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_disposeHttpClient && _httpClient is HttpClientWrapper wrapper)
                {
                    // Note: HttpClientWrapper doesn't implement IDisposable directly,
                    // but we should dispose the underlying HttpClient if we own it
                    // This will be handled when we implement proper HttpClient lifecycle management
                }

                _logger.LogDebug("ProphyApiClient disposed");
                _disposed = true;
            }
        }
    }
} 