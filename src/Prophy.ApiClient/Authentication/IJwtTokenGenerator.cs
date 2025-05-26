using System;
using Prophy.ApiClient.Models.Requests;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Interface for generating JWT tokens for Prophy API authentication.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Generates a JWT token using the provided claims and secret key.
        /// </summary>
        /// <param name="claims">The JWT claims to include in the token.</param>
        /// <param name="secretKey">The secret key used to sign the JWT token.</param>
        /// <returns>A signed JWT token string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when claims or secretKey is null.</exception>
        /// <exception cref="ArgumentException">Thrown when secretKey is empty or invalid.</exception>
        string GenerateToken(JwtLoginClaims claims, string secretKey);

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
        bool IsValidTokenFormat(string token);
    }
} 