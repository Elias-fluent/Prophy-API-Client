using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// OAuth 2.0 client for handling various OAuth flows.
    /// </summary>
    public class OAuthClient : IOAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuthClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the OAuthClient class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making OAuth requests.</param>
        /// <param name="logger">The logger instance for logging OAuth operations.</param>
        public OAuthClient(HttpClient httpClient, ILogger<OAuthClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public async Task<OAuthTokenResponse> GetClientCredentialsTokenAsync(
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            string? scope = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tokenEndpoint))
                throw new ArgumentException("Token endpoint cannot be null or empty.", nameof(tokenEndpoint));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

            if (string.IsNullOrEmpty(clientSecret))
                throw new ArgumentException("Client secret cannot be null or empty.", nameof(clientSecret));

            try
            {
                _logger.LogDebug("Requesting client credentials token from: {TokenEndpoint}", tokenEndpoint);

                var request = new OAuthTokenRequest
                {
                    GrantType = "client_credentials",
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = scope
                };

                return await SendTokenRequestAsync(tokenEndpoint, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get client credentials token from: {TokenEndpoint}", tokenEndpoint);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<OAuthTokenResponse> GetAuthorizationCodeTokenAsync(
            string tokenEndpoint,
            string clientId,
            string code,
            string redirectUri,
            string? clientSecret = null,
            string? codeVerifier = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tokenEndpoint))
                throw new ArgumentException("Token endpoint cannot be null or empty.", nameof(tokenEndpoint));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Authorization code cannot be null or empty.", nameof(code));

            if (string.IsNullOrEmpty(redirectUri))
                throw new ArgumentException("Redirect URI cannot be null or empty.", nameof(redirectUri));

            try
            {
                _logger.LogDebug("Requesting authorization code token from: {TokenEndpoint}", tokenEndpoint);

                var request = new OAuthTokenRequest
                {
                    GrantType = "authorization_code",
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Code = code,
                    RedirectUri = redirectUri,
                    CodeVerifier = codeVerifier
                };

                return await SendTokenRequestAsync(tokenEndpoint, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get authorization code token from: {TokenEndpoint}", tokenEndpoint);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<OAuthTokenResponse> RefreshTokenAsync(
            string tokenEndpoint,
            string clientId,
            string refreshToken,
            string? clientSecret = null,
            string? scope = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tokenEndpoint))
                throw new ArgumentException("Token endpoint cannot be null or empty.", nameof(tokenEndpoint));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty.", nameof(refreshToken));

            try
            {
                _logger.LogDebug("Refreshing token from: {TokenEndpoint}", tokenEndpoint);

                var request = new OAuthTokenRequest
                {
                    GrantType = "refresh_token",
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    RefreshToken = refreshToken,
                    Scope = scope
                };

                return await SendTokenRequestAsync(tokenEndpoint, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token from: {TokenEndpoint}", tokenEndpoint);
                throw;
            }
        }

        /// <inheritdoc />
        public string BuildAuthorizationUrl(
            string authorizationEndpoint,
            string clientId,
            string redirectUri,
            string? scope = null,
            string? state = null,
            string? codeChallenge = null,
            string codeChallengeMethod = "S256")
        {
            if (string.IsNullOrEmpty(authorizationEndpoint))
                throw new ArgumentException("Authorization endpoint cannot be null or empty.", nameof(authorizationEndpoint));

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

            if (string.IsNullOrEmpty(redirectUri))
                throw new ArgumentException("Redirect URI cannot be null or empty.", nameof(redirectUri));

            try
            {
                _logger.LogDebug("Building authorization URL for client: {ClientId}", clientId);

                var parameters = new List<string>
                {
                    $"response_type=code",
                    $"client_id={Uri.EscapeDataString(clientId)}",
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}"
                };

                if (!string.IsNullOrEmpty(scope))
                    parameters.Add($"scope={Uri.EscapeDataString(scope)}");

                if (!string.IsNullOrEmpty(state))
                    parameters.Add($"state={Uri.EscapeDataString(state)}");

                if (!string.IsNullOrEmpty(codeChallenge))
                {
                    parameters.Add($"code_challenge={Uri.EscapeDataString(codeChallenge)}");
                    parameters.Add($"code_challenge_method={Uri.EscapeDataString(codeChallengeMethod)}");
                }

                var queryString = string.Join("&", parameters);
                var separator = authorizationEndpoint.Contains("?") ? "&" : "?";
                var authUrl = $"{authorizationEndpoint}{separator}{queryString}";

                _logger.LogDebug("Built authorization URL with {ParameterCount} parameters", parameters.Count);

                return authUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build authorization URL for client: {ClientId}", clientId);
                throw;
            }
        }

        /// <summary>
        /// Sends a token request to the OAuth server.
        /// </summary>
        /// <param name="tokenEndpoint">The token endpoint URL.</param>
        /// <param name="request">The token request parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The OAuth token response.</returns>
        private async Task<OAuthTokenResponse> SendTokenRequestAsync(
            string tokenEndpoint,
            OAuthTokenRequest request,
            CancellationToken cancellationToken)
        {
            var formData = CreateFormData(request);
            var content = new FormUrlEncodedContent(formData);

            _logger.LogDebug("Sending {GrantType} token request", request.GrantType);

            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OAuth token request failed with status {StatusCode}: {ResponseContent}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"OAuth token request failed with status {response.StatusCode}: {responseContent}");
            }

            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent, _jsonOptions);
            
            if (tokenResponse == null)
                throw new InvalidOperationException("Failed to deserialize OAuth token response");

            _logger.LogDebug("Successfully received {GrantType} token with {ExpiresIn} seconds expiration", 
                request.GrantType, tokenResponse.ExpiresIn);

            return tokenResponse;
        }

        /// <summary>
        /// Creates form data from an OAuth token request.
        /// </summary>
        /// <param name="request">The token request.</param>
        /// <returns>A dictionary of form data parameters.</returns>
        private static Dictionary<string, string> CreateFormData(OAuthTokenRequest request)
        {
            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = request.GrantType,
                ["client_id"] = request.ClientId
            };

            if (!string.IsNullOrEmpty(request.ClientSecret))
                formData["client_secret"] = request.ClientSecret;

            if (!string.IsNullOrEmpty(request.Code))
                formData["code"] = request.Code;

            if (!string.IsNullOrEmpty(request.RedirectUri))
                formData["redirect_uri"] = request.RedirectUri;

            if (!string.IsNullOrEmpty(request.CodeVerifier))
                formData["code_verifier"] = request.CodeVerifier;

            if (!string.IsNullOrEmpty(request.RefreshToken))
                formData["refresh_token"] = request.RefreshToken;

            if (!string.IsNullOrEmpty(request.Scope))
                formData["scope"] = request.Scope;

            if (!string.IsNullOrEmpty(request.State))
                formData["state"] = request.State;

            return formData;
        }
    }
} 