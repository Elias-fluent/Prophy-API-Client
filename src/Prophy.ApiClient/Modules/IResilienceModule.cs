using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Interface for the resilience module that manages rate limiting, circuit breaker, and other resilience patterns.
    /// </summary>
    public interface IResilienceModule
    {
        /// <summary>
        /// Gets the current resilience configuration options.
        /// </summary>
        ResilienceOptions Options { get; }

        /// <summary>
        /// Gets the global resilience pipeline that combines all enabled strategies.
        /// </summary>
        ResiliencePipeline<HttpResponseMessage> GlobalPipeline { get; }

        /// <summary>
        /// Creates a resilience pipeline for a specific endpoint with custom configuration.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint for logging and metrics.</param>
        /// <param name="customOptions">Custom resilience options for this endpoint. If null, uses global options.</param>
        /// <returns>A resilience pipeline configured for the specific endpoint.</returns>
        ResiliencePipeline<HttpResponseMessage> CreateEndpointPipeline(string endpointName, ResilienceOptions? customOptions = null);

        /// <summary>
        /// Executes an HTTP operation through the global resilience pipeline.
        /// </summary>
        /// <param name="operation">The HTTP operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> ExecuteAsync(Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an HTTP operation through a specific endpoint pipeline.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint.</param>
        /// <param name="operation">The HTTP operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> ExecuteAsync(string endpointName, Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the resilience configuration at runtime.
        /// </summary>
        /// <param name="newOptions">The new resilience options to apply.</param>
        void UpdateConfiguration(ResilienceOptions newOptions);

        /// <summary>
        /// Gets resilience metrics and statistics.
        /// </summary>
        /// <returns>A dictionary containing resilience metrics.</returns>
        System.Collections.Generic.IDictionary<string, object> GetMetrics();

        /// <summary>
        /// Resets all resilience state (circuit breakers, rate limiters, etc.).
        /// </summary>
        void Reset();
    }
} 