using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Models.Requests;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Implementation of IAuthenticationModule that handles both API key and JWT authentication.
    /// </summary>
    public class AuthenticationModule : IAuthenticationModule
    {
        private readonly IApiKeyAuthenticator _apiKeyAuthenticator;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthenticationModule> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationModule class.
        /// </summary>
        /// <param name="apiKeyAuthenticator">The API key authenticator instance.</param>
        /// <param name="jwtTokenGenerator">The JWT token generator instance.</param>
        /// <param name="logger">The logger instance for logging authentication operations.</param>
        public AuthenticationModule(
            IApiKeyAuthenticator apiKeyAuthenticator,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<AuthenticationModule> logger)
        {
            _apiKeyAuthenticator = apiKeyAuthenticator ?? throw new ArgumentNullException(nameof(apiKeyAuthenticator));
            _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public string? ApiKey => _apiKeyAuthenticator.ApiKey;

        /// <inheritdoc />
        public string? OrganizationCode { get; private set; }

        /// <inheritdoc />
        public void SetApiKey(string apiKey, string? organizationCode = null)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            try
            {
                _logger.LogDebug("Setting API key for organization: {Organization}", organizationCode ?? "default");
                
                _apiKeyAuthenticator.SetApiKey(apiKey);
                OrganizationCode = organizationCode;

                _logger.LogInformation("API key configured successfully for organization: {Organization}", 
                    organizationCode ?? "default");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set API key for organization: {Organization}", organizationCode);
                throw;
            }
        }

        /// <inheritdoc />
        public void AuthenticateRequest(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("No API key configured. Call SetApiKey() first.");

            try
            {
                _logger.LogDebug("Authenticating request to: {RequestUri}", request.RequestUri);
                
                _apiKeyAuthenticator.AuthenticateRequest(request);

                _logger.LogDebug("Request authenticated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate request to: {RequestUri}", request.RequestUri);
                throw;
            }
        }

        /// <inheritdoc />
        public string GenerateJwtToken(JwtLoginClaims claims, string secretKey)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));

            try
            {
                _logger.LogDebug("Generating JWT token for subject: {Subject}, organization: {Organization}", 
                    claims.Subject, claims.Organization);

                var token = _jwtTokenGenerator.GenerateToken(claims, secretKey);

                _logger.LogInformation("JWT token generated successfully for subject: {Subject}", claims.Subject);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate JWT token for subject: {Subject}", claims.Subject);
                throw;
            }
        }

        /// <inheritdoc />
        public string GenerateLoginUrl(JwtLoginClaims claims, string secretKey, string? baseUrl = null)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));

            try
            {
                _logger.LogDebug("Generating login URL for subject: {Subject}, organization: {Organization}", 
                    claims.Subject, claims.Organization);

                var loginUrl = _jwtTokenGenerator.GenerateLoginUrl(claims, secretKey, baseUrl);

                _logger.LogInformation("Login URL generated successfully for subject: {Subject}", claims.Subject);

                return loginUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate login URL for subject: {Subject}", claims.Subject);
                throw;
            }
        }

        /// <inheritdoc />
        public bool IsValidJwtTokenFormat(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var isValid = _jwtTokenGenerator.IsValidTokenFormat(token);
                
                _logger.LogDebug("JWT token format validation result: {IsValid}", isValid);
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "JWT token format validation failed");
                return false;
            }
        }

        /// <inheritdoc />
        public void ClearAuthentication()
        {
            try
            {
                _logger.LogDebug("Clearing authentication configuration");
                
                _apiKeyAuthenticator.ClearApiKey();
                OrganizationCode = null;

                _logger.LogInformation("Authentication configuration cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear authentication configuration");
                throw;
            }
        }
    }
} 