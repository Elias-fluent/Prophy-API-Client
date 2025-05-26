using System;
using System.Net.Http;
using Prophy.ApiClient.Models.Requests;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Interface for the authentication module that handles both API key and JWT authentication.
    /// </summary>
    public interface IAuthenticationModule
    {
        /// <summary>
        /// Gets the current API key being used for authentication.
        /// </summary>
        string? ApiKey { get; }

        /// <summary>
        /// Gets the current organization code.
        /// </summary>
        string? OrganizationCode { get; }

        /// <summary>
        /// Sets the API key for authentication.
        /// </summary>
        /// <param name="apiKey">The API key to use for authentication.</param>
        /// <param name="organizationCode">The organization code associated with the API key.</param>
        /// <exception cref="ArgumentException">Thrown when apiKey is null or empty.</exception>
        void SetApiKey(string apiKey, string? organizationCode = null);

        /// <summary>
        /// Adds authentication headers to an HTTP request message.
        /// </summary>
        /// <param name="request">The HTTP request message to authenticate.</param>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no API key is configured.</exception>
        void AuthenticateRequest(HttpRequestMessage request);

        /// <summary>
        /// Generates a JWT token using the provided claims and secret key.
        /// </summary>
        /// <param name="claims">The JWT claims to include in the token.</param>
        /// <param name="secretKey">The secret key used to sign the JWT token.</param>
        /// <returns>A signed JWT token string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when claims or secretKey is null.</exception>
        /// <exception cref="ArgumentException">Thrown when secretKey is empty or invalid.</exception>
        string GenerateJwtToken(JwtLoginClaims claims, string secretKey);

        /// <summary>
        /// Generates a login URL with an embedded JWT token for seamless user authentication.
        /// </summary>
        /// <param name="claims">The JWT claims to include in the token.</param>
        /// <param name="secretKey">The secret key used to sign the JWT token.</param>
        /// <param name="baseUrl">The base URL for the Prophy login endpoint. If null, uses the default.</param>
        /// <returns>A complete login URL with the JWT token embedded.</returns>
        /// <exception cref="ArgumentNullException">Thrown when claims or secretKey is null.</exception>
        /// <exception cref="ArgumentException">Thrown when secretKey is empty or invalid.</exception>
        string GenerateLoginUrl(JwtLoginClaims claims, string secretKey, string? baseUrl = null);

        /// <summary>
        /// Validates the format and basic structure of a JWT token without verifying the signature.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>True if the token has a valid format, false otherwise.</returns>
        bool IsValidJwtTokenFormat(string token);

        /// <summary>
        /// Clears the current authentication configuration.
        /// </summary>
        void ClearAuthentication();
    }
} 