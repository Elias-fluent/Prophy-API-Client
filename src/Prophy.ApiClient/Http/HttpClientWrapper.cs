using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Prophy.ApiClient.Diagnostics;
using Prophy.ApiClient.Modules;

namespace Prophy.ApiClient.Http
{
    /// <summary>
    /// Concrete implementation of IHttpClientWrapper that provides HTTP operations with resilience patterns and logging.
    /// </summary>
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientWrapper> _logger;
        private readonly IResilienceModule? _resilienceModule;

        /// <summary>
        /// Initializes a new instance of the HttpClientWrapper class.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use for HTTP operations.</param>
        /// <param name="logger">The logger instance for logging HTTP operations.</param>
        public HttpClientWrapper(HttpClient httpClient, ILogger<HttpClientWrapper> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resilienceModule = null; // No resilience module - direct HTTP calls
        }

        /// <summary>
        /// Initializes a new instance of the HttpClientWrapper class with resilience support.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use for HTTP operations.</param>
        /// <param name="logger">The logger instance for logging HTTP operations.</param>
        /// <param name="resilienceModule">The resilience module for rate limiting, circuit breaker, and retry policies.</param>
        public HttpClientWrapper(HttpClient httpClient, ILogger<HttpClientWrapper> logger, IResilienceModule resilienceModule)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resilienceModule = resilienceModule ?? throw new ArgumentNullException(nameof(resilienceModule));
        }

        /// <summary>
        /// Executes an HTTP operation with resilience patterns if available, otherwise executes directly.
        /// </summary>
        /// <param name="method">The HTTP method for logging and endpoint identification.</param>
        /// <param name="requestUri">The request URI for endpoint identification.</param>
        /// <param name="operation">The HTTP operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> ExecuteWithResilienceAsync(string method, string requestUri, Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken cancellationToken)
        {
            if (_resilienceModule != null)
            {
                // Use endpoint-specific pipeline based on the request URI
                var endpointName = GetEndpointName(method, requestUri);
                return await _resilienceModule.ExecuteAsync(endpointName, operation, cancellationToken);
            }
            else
            {
                // Execute directly without resilience patterns
                return await operation(cancellationToken);
            }
        }

        /// <summary>
        /// Gets a normalized endpoint name for resilience pipeline identification.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <returns>A normalized endpoint name.</returns>
        private static string GetEndpointName(string method, string requestUri)
        {
            // Extract the path from the URI and normalize it for endpoint identification
            var uri = requestUri.StartsWith("http") ? new Uri(requestUri) : new Uri($"https://example.com{requestUri}");
            var path = uri.AbsolutePath.Trim('/');
            
            // Replace dynamic segments with placeholders for consistent endpoint naming
            // e.g., /api/external/proposal/123 -> /api/external/proposal/{id}
            path = System.Text.RegularExpressions.Regex.Replace(path, @"\d+", "{id}");
            
            return $"{method.ToUpperInvariant()}:{path}";
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending GET request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("GET", requestUri, async (ct) =>
            {
                var response = await _httpClient.GetAsync(requestUri, ct);
                LogResponse(response, "GET", requestUri);
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending GET request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("GET", requestUri?.ToString() ?? "", async (ct) =>
            {
                var response = await _httpClient.GetAsync(requestUri, ct);
                LogResponse(response, "GET", requestUri?.ToString());
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending POST request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("POST", requestUri, async (ct) =>
            {
                var response = await _httpClient.PostAsync(requestUri, content, ct);
                LogResponse(response, "POST", requestUri);
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending POST request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("POST", requestUri?.ToString() ?? "", async (ct) =>
            {
                var response = await _httpClient.PostAsync(requestUri, content, ct);
                LogResponse(response, "POST", requestUri?.ToString());
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending PUT request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("PUT", requestUri, async (ct) =>
            {
                var response = await _httpClient.PutAsync(requestUri, content, ct);
                LogResponse(response, "PUT", requestUri);
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending PUT request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("PUT", requestUri?.ToString() ?? "", async (ct) =>
            {
                var response = await _httpClient.PutAsync(requestUri, content, ct);
                LogResponse(response, "PUT", requestUri?.ToString());
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending DELETE request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("DELETE", requestUri, async (ct) =>
            {
                var response = await _httpClient.DeleteAsync(requestUri, ct);
                LogResponse(response, "DELETE", requestUri);
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending DELETE request to: {RequestUri}", requestUri);
            
            return await ExecuteWithResilienceAsync("DELETE", requestUri?.ToString() ?? "", async (ct) =>
            {
                var response = await _httpClient.DeleteAsync(requestUri, ct);
                LogResponse(response, "DELETE", requestUri?.ToString());
                return response;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending {Method} request to: {RequestUri}", request.Method, request.RequestUri);
            
            return await ExecuteWithResilienceAsync(request.Method.ToString(), request.RequestUri?.ToString() ?? "", async (ct) =>
            {
                var response = await _httpClient.SendAsync(request, ct);
                LogResponse(response, request.Method.ToString(), request.RequestUri?.ToString());
                return response;
            }, cancellationToken);
        }

        private void LogResponse(HttpResponseMessage response, string method, string? requestUri)
        {
            var statusCode = (int)response.StatusCode;
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("HTTP {Method} request to {RequestUri} completed successfully. Status: {StatusCode}", 
                    method, requestUri, response.StatusCode);
            }
            else
            {
                _logger.LogWarning("HTTP {Method} request to {RequestUri} failed. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    method, requestUri, response.StatusCode, response.ReasonPhrase);
            }

            // Record metrics
            DiagnosticEvents.Metrics.IncrementCounter($"http.requests.{method.ToLowerInvariant()}.{statusCode}");
            DiagnosticEvents.Metrics.IncrementCounter($"http.requests.{method.ToLowerInvariant()}.{(response.IsSuccessStatusCode ? "success" : "failure")}");
        }


    }
} 