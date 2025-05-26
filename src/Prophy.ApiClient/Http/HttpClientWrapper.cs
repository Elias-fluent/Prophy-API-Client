using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace Prophy.ApiClient.Http
{
    /// <summary>
    /// Concrete implementation of IHttpClientWrapper that provides HTTP operations with retry policies and logging.
    /// </summary>
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientWrapper> _logger;
        private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

        /// <summary>
        /// Initializes a new instance of the HttpClientWrapper class.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use for HTTP operations.</param>
        /// <param name="logger">The logger instance for logging HTTP operations.</param>
        public HttpClientWrapper(HttpClient httpClient, ILogger<HttpClientWrapper> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure retry pipeline with exponential backoff
            _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response => IsTransientHttpFailure(response)),
                    Delay = TimeSpan.FromSeconds(1),
                    MaxRetryAttempts = 3,
                    BackoffType = Polly.DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        _logger.LogWarning("Retrying HTTP request. Attempt: {Attempt}, Delay: {Delay}ms", 
                            args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds);
                        return default;
                    }
                })
                .Build();
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending GET request to: {RequestUri}", requestUri);
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
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
            
            return await _retryPipeline.ExecuteAsync(async (ct) =>
            {
                var response = await _httpClient.SendAsync(request, ct);
                LogResponse(response, request.Method.ToString(), request.RequestUri?.ToString());
                return response;
            }, cancellationToken);
        }

        private void LogResponse(HttpResponseMessage response, string method, string? requestUri)
        {
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
        }

        private static bool IsTransientHttpFailure(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            
            // Consider 5xx server errors as transient
            if (statusCode >= 500)
                return true;
                
            // Consider specific 4xx errors as transient
            return response.StatusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == 429; // Too Many Requests
        }
    }
} 