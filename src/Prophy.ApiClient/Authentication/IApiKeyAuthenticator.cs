using System.Net.Http;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Provides authentication functionality for the Prophy API using API keys.
    /// </summary>
    public interface IApiKeyAuthenticator
    {
        /// <summary>
        /// Adds authentication headers to the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message to authenticate.</param>
        void AuthenticateRequest(HttpRequestMessage request);

        /// <summary>
        /// Gets the API key used for authentication.
        /// </summary>
        string? ApiKey { get; }

        /// <summary>
        /// Gets the organization code associated with the API key.
        /// </summary>
        string? OrganizationCode { get; }

        /// <summary>
        /// Sets the API key for authentication.
        /// </summary>
        /// <param name="apiKey">The API key to use for authentication.</param>
        void SetApiKey(string apiKey);

        /// <summary>
        /// Sets the organization code for authentication.
        /// </summary>
        /// <param name="organizationCode">The organization code to use for authentication.</param>
        void SetOrganizationCode(string organizationCode);

        /// <summary>
        /// Clears the current API key configuration.
        /// </summary>
        void ClearApiKey();
    }
} 