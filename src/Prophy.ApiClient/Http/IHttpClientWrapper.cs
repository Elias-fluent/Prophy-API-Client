using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prophy.ApiClient.Http
{
    /// <summary>
    /// Provides an abstraction over HttpClient for making HTTP requests to the Prophy API.
    /// </summary>
    public interface IHttpClientWrapper
    {
        /// <summary>
        /// Sends a GET request to the specified URI.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a GET request to the specified URI.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a POST request to the specified URI with the given content.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a POST request to the specified URI with the given content.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a PUT request to the specified URI with the given content.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a PUT request to the specified URI with the given content.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a DELETE request to the specified URI.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a DELETE request to the specified URI.
        /// </summary>
        /// <param name="requestUri">The URI to send the request to.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    }
} 