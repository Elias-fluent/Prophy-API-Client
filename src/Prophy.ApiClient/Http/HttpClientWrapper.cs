using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Diagnostics;
using Prophy.ApiClient.Modules;

namespace Prophy.ApiClient.Http
{
    /// <summary>
    /// Wrapper for HttpClient that provides additional functionality and resilience.
    /// </summary>
    public class HttpClientWrapper : IHttpClientWrapper, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientWrapper> _logger;
        private readonly IResilienceModule? _resilienceModule;
        private readonly bool _disposeHttpClient;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the HttpClientWrapper class with resilience support.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to wrap.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="resilienceModule">The resilience module for circuit breaker and retry patterns.</param>
        /// <param name="disposeHttpClient">Whether to dispose the HttpClient when this wrapper is disposed.</param>
        public HttpClientWrapper(HttpClient httpClient, ILogger<HttpClientWrapper> logger, IResilienceModule resilienceModule, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resilienceModule = resilienceModule ?? throw new ArgumentNullException(nameof(resilienceModule));
            _disposeHttpClient = disposeHttpClient;

            _logger.LogDebug("HttpClientWrapper initialized with resilience support");
        }

        /// <summary>
        /// Initializes a new instance of the HttpClientWrapper class without resilience support (.NET Framework 4.8 compatible).
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to wrap.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="disposeHttpClient">Whether to dispose the HttpClient when this wrapper is disposed.</param>
        public HttpClientWrapper(HttpClient httpClient, ILogger<HttpClientWrapper> logger, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resilienceModule = null;
            _disposeHttpClient = disposeHttpClient;

            _logger.LogDebug("HttpClientWrapper initialized without resilience support (.NET Framework 4.8 compatible)");
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
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending GET request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending GET request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending POST request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending POST request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending PUT request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending PUT request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending DELETE request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending DELETE request to: {RequestUri}", requestUri);
            
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            return await SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Sending HTTP {Method} request to {Uri}", request.Method, request.RequestUri);

                HttpResponseMessage response;

                // Use resilience module if available, otherwise send directly
                if (_resilienceModule != null)
                {
                    response = await _resilienceModule.ExecuteAsync(async (ct) =>
                    {
                        return await _httpClient.SendAsync(request, ct);
                    }, cancellationToken);
                }
                else
                {
                    response = await _httpClient.SendAsync(request, cancellationToken);
                }

                _logger.LogDebug("Received HTTP {StatusCode} response from {Uri}", 
                    response.StatusCode, request.RequestUri);

                return response;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("HTTP request to {Uri} was cancelled", request.RequestUri);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "HTTP request to {Uri} timed out", request.RequestUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to {Uri} failed with unexpected error", request.RequestUri);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Sending HTTP {Method} request to {Uri} with completion option {CompletionOption}", 
                    request.Method, request.RequestUri, completionOption);

                HttpResponseMessage response;

                // Use resilience module if available, otherwise send directly
                if (_resilienceModule != null)
                {
                    response = await _resilienceModule.ExecuteAsync(async (ct) =>
                    {
                        return await _httpClient.SendAsync(request, completionOption, ct);
                    }, cancellationToken);
                }
                else
                {
                    response = await _httpClient.SendAsync(request, completionOption, cancellationToken);
                }

                _logger.LogDebug("Received HTTP {StatusCode} response from {Uri}", 
                    response.StatusCode, request.RequestUri);

                return response;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("HTTP request to {Uri} was cancelled", request.RequestUri);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "HTTP request to {Uri} timed out", request.RequestUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to {Uri} failed with unexpected error", request.RequestUri);
                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HttpClientWrapper));
        }

        /// <summary>
        /// Disposes the HTTP client wrapper and optionally the underlying HttpClient.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the HTTP client wrapper and optionally the underlying HttpClient.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_disposeHttpClient)
                {
                    _httpClient?.Dispose();
                }

                _disposed = true;
                
                _logger.LogDebug("HttpClientWrapper disposed");
            }
        }
    }
} 