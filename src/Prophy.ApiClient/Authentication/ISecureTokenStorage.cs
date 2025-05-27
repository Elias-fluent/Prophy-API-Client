using System;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Interface for secure OAuth token storage with encryption and expiration handling.
    /// </summary>
    public interface ISecureTokenStorage
    {
        /// <summary>
        /// Stores an OAuth token securely with encryption.
        /// </summary>
        /// <param name="key">The unique key to identify the token.</param>
        /// <param name="token">The OAuth token to store.</param>
        void StoreToken(string key, OAuthTokenResponse token);

        /// <summary>
        /// Retrieves an OAuth token from secure storage.
        /// </summary>
        /// <param name="key">The unique key to identify the token.</param>
        /// <returns>The OAuth token if found and not expired, null otherwise.</returns>
        OAuthTokenResponse? GetToken(string key);

        /// <summary>
        /// Checks if a stored token is valid (exists and not expired).
        /// </summary>
        /// <param name="key">The unique key to identify the token.</param>
        /// <returns>True if the token is valid, false otherwise.</returns>
        bool IsTokenValid(string key);

        /// <summary>
        /// Checks if a stored token is expiring soon within the specified threshold.
        /// </summary>
        /// <param name="key">The unique key to identify the token.</param>
        /// <param name="threshold">The time threshold to check for expiration.</param>
        /// <returns>True if the token is expiring soon, false otherwise.</returns>
        bool IsTokenExpiringSoon(string key, TimeSpan threshold);

        /// <summary>
        /// Removes a token from secure storage.
        /// </summary>
        /// <param name="key">The unique key to identify the token.</param>
        void RemoveToken(string key);

        /// <summary>
        /// Clears all tokens from secure storage.
        /// </summary>
        void ClearAllTokens();

        /// <summary>
        /// Removes all expired tokens from storage.
        /// </summary>
        void CleanupExpiredTokens();
    }
} 