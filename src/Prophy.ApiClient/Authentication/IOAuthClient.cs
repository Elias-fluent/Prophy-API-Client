using System;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Interface for OAuth 2.0 client operations.
    /// </summary>
    public interface IOAuthClient
    {
        /// <summary>
        /// Gets an access token using the client credentials flow.
        /// </summary>
        /// <param name="tokenEndpoint">The OAuth token endpoint URL.</param>
        /// <param name="clientId">The OAuth client ID.</param>
        /// <param name="clientSecret">The OAuth client secret.</param>
        /// <param name="scope">The optional scope for the token request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The OAuth token response.</returns>
        Task<OAuthTokenResponse> GetClientCredentialsTokenAsync(
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            string? scope = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an access token using the authorization code flow.
        /// </summary>
        /// <param name="tokenEndpoint">The OAuth token endpoint URL.</param>
        /// <param name="clientId">The OAuth client ID.</param>
        /// <param name="code">The authorization code received from the authorization server.</param>
        /// <param name="redirectUri">The redirect URI used in the authorization request.</param>
        /// <param name="clientSecret">The optional OAuth client secret (for confidential clients).</param>
        /// <param name="codeVerifier">The optional PKCE code verifier (for public clients).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The OAuth token response.</returns>
        Task<OAuthTokenResponse> GetAuthorizationCodeTokenAsync(
            string tokenEndpoint,
            string clientId,
            string code,
            string redirectUri,
            string? clientSecret = null,
            string? codeVerifier = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes an access token using a refresh token.
        /// </summary>
        /// <param name="tokenEndpoint">The OAuth token endpoint URL.</param>
        /// <param name="clientId">The OAuth client ID.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="clientSecret">The optional OAuth client secret.</param>
        /// <param name="scope">The optional scope for the token request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The OAuth token response.</returns>
        Task<OAuthTokenResponse> RefreshTokenAsync(
            string tokenEndpoint,
            string clientId,
            string refreshToken,
            string? clientSecret = null,
            string? scope = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds an authorization URL for the authorization code flow.
        /// </summary>
        /// <param name="authorizationEndpoint">The OAuth authorization endpoint URL.</param>
        /// <param name="clientId">The OAuth client ID.</param>
        /// <param name="redirectUri">The redirect URI for the authorization response.</param>
        /// <param name="scope">The optional scope for the authorization request.</param>
        /// <param name="state">The optional state parameter for CSRF protection.</param>
        /// <param name="codeChallenge">The optional PKCE code challenge.</param>
        /// <param name="codeChallengeMethod">The PKCE code challenge method (default: S256).</param>
        /// <returns>The complete authorization URL.</returns>
        string BuildAuthorizationUrl(
            string authorizationEndpoint,
            string clientId,
            string redirectUri,
            string? scope = null,
            string? state = null,
            string? codeChallenge = null,
            string codeChallengeMethod = "S256");
    }
} 