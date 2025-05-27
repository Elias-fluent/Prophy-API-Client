using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Interface for JWT token validation with signature verification and claims validation.
    /// </summary>
    public interface IJwtValidator
    {
        /// <summary>
        /// Validates a JWT token including signature verification and claims validation.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <param name="secretKey">The secret key used to verify the token signature.</param>
        /// <param name="options">Optional validation options.</param>
        /// <returns>A validation result indicating success or failure with details.</returns>
        JwtValidationResult ValidateToken(string token, string secretKey, JwtValidationOptions? options = null);

        /// <summary>
        /// Checks if a claims principal has all the required claims.
        /// </summary>
        /// <param name="principal">The claims principal to check.</param>
        /// <param name="requiredClaims">The collection of required claim types.</param>
        /// <returns>True if all required claims are present, false otherwise.</returns>
        bool HasRequiredClaims(ClaimsPrincipal principal, IEnumerable<string> requiredClaims);

        /// <summary>
        /// Checks if a claims principal has a specific role.
        /// </summary>
        /// <param name="principal">The claims principal to check.</param>
        /// <param name="role">The role to check for.</param>
        /// <returns>True if the principal has the role, false otherwise.</returns>
        bool HasRole(ClaimsPrincipal principal, string role);

        /// <summary>
        /// Checks if a claims principal has any of the specified roles.
        /// </summary>
        /// <param name="principal">The claims principal to check.</param>
        /// <param name="roles">The collection of roles to check for.</param>
        /// <returns>True if the principal has any of the roles, false otherwise.</returns>
        bool HasAnyRole(ClaimsPrincipal principal, IEnumerable<string> roles);

        /// <summary>
        /// Gets the value of a specific claim from a claims principal.
        /// </summary>
        /// <param name="principal">The claims principal to get the claim from.</param>
        /// <param name="claimType">The type of claim to retrieve.</param>
        /// <returns>The claim value if found, null otherwise.</returns>
        string? GetClaimValue(ClaimsPrincipal principal, string claimType);

        /// <summary>
        /// Gets all values of a specific claim type from a claims principal.
        /// </summary>
        /// <param name="principal">The claims principal to get the claims from.</param>
        /// <param name="claimType">The type of claims to retrieve.</param>
        /// <returns>A collection of claim values.</returns>
        IEnumerable<string> GetClaimValues(ClaimsPrincipal principal, string claimType);
    }
} 