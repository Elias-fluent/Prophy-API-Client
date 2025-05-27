using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Tenant-aware API key authenticator that automatically uses the current organization context.
    /// </summary>
    public class TenantAwareApiKeyAuthenticator : IApiKeyAuthenticator
    {
        private readonly IOrganizationContextProvider _contextProvider;
        private readonly ITenantConfigurationProvider _configurationProvider;
        private readonly ILogger<TenantAwareApiKeyAuthenticator> _logger;

        /// <summary>
        /// Initializes a new instance of the TenantAwareApiKeyAuthenticator class.
        /// </summary>
        /// <param name="contextProvider">The organization context provider.</param>
        /// <param name="configurationProvider">The tenant configuration provider.</param>
        /// <param name="logger">The logger instance.</param>
        public TenantAwareApiKeyAuthenticator(
            IOrganizationContextProvider contextProvider,
            ITenantConfigurationProvider configurationProvider,
            ILogger<TenantAwareApiKeyAuthenticator> logger)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public string? ApiKey
        {
            get
            {
                var context = _contextProvider.GetCurrentContext();
                return context?.ApiKey;
            }
        }

        /// <inheritdoc />
        public string? OrganizationCode
        {
            get
            {
                var context = _contextProvider.GetCurrentContext();
                return context?.OrganizationCode;
            }
        }

        /// <inheritdoc />
        public void SetApiKey(string apiKey)
        {
            // For tenant-aware authenticator, API keys are managed through the context provider
            // This method is kept for interface compatibility but logs a warning
            _logger.LogWarning("SetApiKey called on TenantAwareApiKeyAuthenticator. Use OrganizationContext.WithApiKey() instead.");
        }

        /// <inheritdoc />
        public void SetOrganizationCode(string organizationCode)
        {
            // For tenant-aware authenticator, organization codes are managed through the context provider
            // This method is kept for interface compatibility but logs a warning
            _logger.LogWarning("SetOrganizationCode called on TenantAwareApiKeyAuthenticator. Use IOrganizationContextProvider.SetCurrentContext() instead.");
        }

        /// <inheritdoc />
        public void ClearApiKey()
        {
            // For tenant-aware authenticator, clearing is done through the context provider
            _contextProvider.ClearCurrentContext();
            _logger.LogDebug("Cleared tenant context");
        }

        /// <inheritdoc />
        public void AuthenticateRequest(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var context = _contextProvider.GetCurrentContext();
            if (context == null)
            {
                _logger.LogWarning("No tenant context available for authentication");
                throw new InvalidOperationException("No tenant context available. Set the organization context first.");
            }

            if (string.IsNullOrEmpty(context.ApiKey))
            {
                _logger.LogError("No API key available in tenant context for organization: {OrganizationCode}", context.OrganizationCode);
                throw new InvalidOperationException($"No API key configured for organization: {context.OrganizationCode}");
            }

            // Add the X-ApiKey header as required by the Prophy API
            request.Headers.Remove("X-ApiKey");
            request.Headers.Add("X-ApiKey", context.ApiKey);

            // Add organization code header
            request.Headers.Remove("X-Organization-Code");
            request.Headers.Add("X-Organization-Code", context.OrganizationCode);

            _logger.LogDebug("Added authentication headers for organization: {OrganizationCode}", context.OrganizationCode);
        }
    }
} 