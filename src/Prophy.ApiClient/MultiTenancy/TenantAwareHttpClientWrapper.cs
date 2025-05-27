using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Modules;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Tenant-aware HTTP client wrapper that automatically resolves tenant context
    /// and applies tenant-specific authentication and configuration.
    /// </summary>
    public class TenantAwareHttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TenantAwareHttpClientWrapper> _logger;
        private readonly IResilienceModule _resilienceModule;
        private readonly TenantResolutionService _tenantResolutionService;
        private readonly ITenantConfigurationProvider _configurationProvider;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TenantAwareHttpClientWrapper class.
        /// </summary>
        /// <param name="httpClient">The underlying HTTP client.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="resilienceModule">The resilience module for retry and circuit breaker policies.</param>
        /// <param name="tenantResolutionService">The tenant resolution service.</param>
        /// <param name="configurationProvider">The tenant configuration provider.</param>
        public TenantAwareHttpClientWrapper(
            HttpClient httpClient,
            ILogger<TenantAwareHttpClientWrapper> logger,
            IResilienceModule resilienceModule,
            TenantResolutionService tenantResolutionService,
            ITenantConfigurationProvider configurationProvider)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resilienceModule = resilienceModule ?? throw new ArgumentNullException(nameof(resilienceModule));
            _tenantResolutionService = tenantResolutionService ?? throw new ArgumentNullException(nameof(tenantResolutionService));
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        }

        /// <inheritdoc />
        public Uri? BaseAddress 
        { 
            get => _httpClient.BaseAddress; 
            set => _httpClient.BaseAddress = value; 
        }

        /// <inheritdoc />
        public TimeSpan Timeout 
        { 
            get => _httpClient.Timeout; 
            set => _httpClient.Timeout = value; 
        }

        /// <inheritdoc />
        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ThrowIfDisposed();

            try
            {
                // Resolve tenant context from the request
                var context = await _tenantResolutionService.ResolveAndSetContextAsync(request);
                
                // Apply tenant-specific configuration and authentication
                await ApplyTenantConfigurationAsync(request, context);

                // Execute the request with resilience policies
                return await _resilienceModule.ExecuteAsync(async (ct) =>
                {
                    _logger.LogDebug("Sending HTTP request: {Method} {Uri} for tenant: {OrganizationCode}", 
                        request.Method, request.RequestUri, context?.OrganizationCode ?? "unknown");
                    
                    var response = await _httpClient.SendAsync(request, ct);
                    
                    _logger.LogDebug("Received HTTP response: {StatusCode} for tenant: {OrganizationCode}", 
                        response.StatusCode, context?.OrganizationCode ?? "unknown");
                    
                    return response;
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send HTTP request: {Method} {Uri}", request.Method, request.RequestUri);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Applies tenant-specific configuration and authentication to the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request to configure.</param>
        /// <param name="context">The organization context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ApplyTenantConfigurationAsync(HttpRequestMessage request, OrganizationContext? context)
        {
            if (context == null)
            {
                _logger.LogWarning("No tenant context available for request: {Method} {Uri}", request.Method, request.RequestUri);
                return;
            }

            try
            {
                // Get tenant-specific configuration
                var config = await _configurationProvider.GetConfigurationAsync();
                
                // Apply tenant-specific base URL if different
                if (!string.IsNullOrWhiteSpace(config.BaseUrl) && 
                    request.RequestUri != null && 
                    !request.RequestUri.IsAbsoluteUri)
                {
                    var baseUri = new Uri(config.BaseUrl);
                    if (_httpClient.BaseAddress != baseUri)
                    {
                        _httpClient.BaseAddress = baseUri;
                        _logger.LogDebug("Updated base URL to: {BaseUrl} for tenant: {OrganizationCode}", 
                            config.BaseUrl, context.OrganizationCode);
                    }
                }

                // Apply tenant-specific API key authentication
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    request.Headers.Remove("X-ApiKey");
                    request.Headers.Add("X-ApiKey", config.ApiKey);
                    _logger.LogDebug("Applied tenant-specific API key for: {OrganizationCode}", context.OrganizationCode);
                }

                // Add organization code header
                request.Headers.Remove("X-Organization-Code");
                request.Headers.Add("X-Organization-Code", context.OrganizationCode);

                // Apply any additional tenant-specific headers from context properties
                foreach (var property in context.Properties)
                {
                    if (property.Key.StartsWith("Header.", StringComparison.OrdinalIgnoreCase) && 
                        property.Value is string headerValue)
                    {
                        var headerName = property.Key.Substring(7); // Remove "Header." prefix
                        request.Headers.Remove(headerName);
                        request.Headers.Add(headerName, headerValue);
                        _logger.LogTrace("Applied custom header {HeaderName} for tenant: {OrganizationCode}", 
                            headerName, context.OrganizationCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply tenant configuration for: {OrganizationCode}", context.OrganizationCode);
                throw;
            }
        }

        /// <summary>
        /// Throws an ObjectDisposedException if this instance has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TenantAwareHttpClientWrapper));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the wrapper and releases all resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Note: We don't dispose the underlying HttpClient as it may be shared
                // The caller is responsible for disposing the HttpClient
                _disposed = true;
                _logger.LogDebug("TenantAwareHttpClientWrapper disposed");
            }
        }
    }
} 