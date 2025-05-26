using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Concrete implementation of IApiKeyAuthenticator that handles API key authentication for the Prophy API.
    /// </summary>
    public class ApiKeyAuthenticator : IApiKeyAuthenticator
    {
        private readonly ILogger<ApiKeyAuthenticator> _logger;

        /// <summary>
        /// Initializes a new instance of the ApiKeyAuthenticator class.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="organizationCode">The organization code associated with the API key.</param>
        /// <param name="logger">The logger instance for logging authentication operations.</param>
        /// <exception cref="ArgumentException">Thrown when apiKey or organizationCode is null or empty.</exception>
        public ApiKeyAuthenticator(string apiKey, string organizationCode, ILogger<ApiKeyAuthenticator> logger)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty.", nameof(organizationCode));

            ApiKey = apiKey;
            OrganizationCode = organizationCode;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("ApiKeyAuthenticator initialized for organization: {OrganizationCode}", organizationCode);
        }

        /// <inheritdoc />
        public string ApiKey { get; }

        /// <inheritdoc />
        public string OrganizationCode { get; }

        /// <inheritdoc />
        public void AuthenticateRequest(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Add the X-ApiKey header as required by the Prophy API
            request.Headers.Add("X-ApiKey", ApiKey);

            _logger.LogDebug("Added X-ApiKey header to request for {RequestUri}", request.RequestUri);
        }
    }
} 