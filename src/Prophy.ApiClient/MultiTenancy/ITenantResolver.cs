using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Defines the contract for resolving tenant information from various sources.
    /// </summary>
    public interface ITenantResolver
    {
        /// <summary>
        /// Resolves the organization code from an HTTP request message.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the organization code, or null if not found.</returns>
        Task<string?> ResolveFromRequestAsync(HttpRequestMessage request);

        /// <summary>
        /// Resolves the organization code from HTTP headers.
        /// </summary>
        /// <param name="headers">The HTTP headers collection.</param>
        /// <returns>The organization code, or null if not found.</returns>
        string? ResolveFromHeaders(IDictionary<string, string> headers);

        /// <summary>
        /// Resolves the organization code from a JWT token.
        /// </summary>
        /// <param name="token">The JWT token string.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the organization code, or null if not found.</returns>
        Task<string?> ResolveFromTokenAsync(string token);

        /// <summary>
        /// Resolves the organization code from the request URL (e.g., subdomain).
        /// </summary>
        /// <param name="requestUri">The request URI.</param>
        /// <returns>The organization code, or null if not found.</returns>
        string? ResolveFromUrl(System.Uri requestUri);

        /// <summary>
        /// Gets the priority order of resolution strategies.
        /// </summary>
        /// <returns>An ordered list of resolution strategy names.</returns>
        IReadOnlyList<string> GetResolutionOrder();
    }
} 