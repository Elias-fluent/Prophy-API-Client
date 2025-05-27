using System;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Responses
{
    /// <summary>
    /// Represents an OAuth 2.0 token response.
    /// </summary>
    public class OAuthTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token type (usually "Bearer").
        /// </summary>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Gets or sets the token expiration time in seconds.
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the refresh token (if applicable).
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the granted scope.
        /// </summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets the state parameter (if provided in request).
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// Gets the calculated expiration time based on ExpiresIn.
        /// </summary>
        [JsonIgnore]
        public DateTime ExpiresAt => DateTime.UtcNow.AddSeconds(ExpiresIn);

        /// <summary>
        /// Gets a value indicating whether the token is expired.
        /// </summary>
        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Gets a value indicating whether the token will expire soon (within 5 minutes).
        /// </summary>
        [JsonIgnore]
        public bool IsExpiringSoon => DateTime.UtcNow.AddMinutes(5) >= ExpiresAt;
    }
} 