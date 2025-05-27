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
        /// <param name="logger">The logger instance for logging authentication operations.</param>
        public ApiKeyAuthenticator(ILogger<ApiKeyAuthenticator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("ApiKeyAuthenticator initialized");
        }

        /// <summary>
        /// Initializes a new instance of the ApiKeyAuthenticator class with an API key.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="logger">The logger instance for logging authentication operations.</param>
        /// <exception cref="ArgumentException">Thrown when apiKey is null or empty.</exception>
        public ApiKeyAuthenticator(string apiKey, ILogger<ApiKeyAuthenticator> logger)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SetApiKey(apiKey);
        }

        /// <inheritdoc />
        public string? ApiKey { get; private set; }

        /// <inheritdoc />
        public string? OrganizationCode { get; private set; }

        /// <inheritdoc />
        public void SetApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            ApiKey = apiKey;
            _logger.LogDebug("API key set successfully");
        }

        /// <inheritdoc />
        public void SetOrganizationCode(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty.", nameof(organizationCode));

            OrganizationCode = organizationCode;
            _logger.LogDebug("Organization code set successfully");
        }

        /// <inheritdoc />
        public void ClearApiKey()
        {
            ApiKey = null;
            OrganizationCode = null;
            _logger.LogDebug("API key cleared");
        }

        /// <inheritdoc />
        public void AuthenticateRequest(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("No API key configured. Call SetApiKey() first.");

            // Add the X-ApiKey header as required by the Prophy API
            request.Headers.Add("X-ApiKey", ApiKey);

            _logger.LogDebug("Added X-ApiKey header to request for {RequestUri}", request.RequestUri);
        }
    }
} 