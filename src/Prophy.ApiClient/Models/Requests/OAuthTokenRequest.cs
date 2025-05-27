using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Represents an OAuth 2.0 token request for various grant types.
    /// </summary>
    public class OAuthTokenRequest
    {
        /// <summary>
        /// Gets or sets the OAuth grant type (e.g., "client_credentials", "authorization_code", "refresh_token").
        /// </summary>
        [Required]
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client ID for OAuth authentication.
        /// </summary>
        [Required]
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client secret for OAuth authentication.
        /// </summary>
        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the authorization code (for authorization code flow).
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the redirect URI (for authorization code flow).
        /// </summary>
        [JsonPropertyName("redirect_uri")]
        public string? RedirectUri { get; set; }

        /// <summary>
        /// Gets or sets the PKCE code verifier (for authorization code flow with PKCE).
        /// </summary>
        [JsonPropertyName("code_verifier")]
        public string? CodeVerifier { get; set; }

        /// <summary>
        /// Gets or sets the refresh token (for refresh token flow).
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the requested scope for the token.
        /// </summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets additional state parameter for security.
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }
} 