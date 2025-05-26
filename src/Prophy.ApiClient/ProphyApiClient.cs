using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Http;

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

        /// <summary>
        /// Gets the base URL for the Prophy API.
        /// </summary>
        public Uri BaseUrl { get; }

        /// <summary>
        /// Gets the organization code associated with this client instance.
        /// </summary>
        public string OrganizationCode => _authenticator.OrganizationCode;

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

            // Set base URL
            BaseUrl = httpClient.BaseAddress ?? new Uri("https://www.prophy.ai/api/");
            if (httpClient.BaseAddress == null)
            {
                httpClient.BaseAddress = BaseUrl;
            }

            // Create authenticator
            var authenticatorLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiKeyAuthenticator>.Instance;
            _authenticator = new ApiKeyAuthenticator(apiKey, organizationCode, authenticatorLogger);

            // Create HTTP client wrapper
            var httpClientLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HttpClientWrapper>.Instance;
            _httpClient = new HttpClientWrapper(httpClient, httpClientLogger);

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