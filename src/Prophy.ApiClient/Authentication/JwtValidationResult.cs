using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Represents the result of JWT token validation.
    /// </summary>
    public class JwtValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the JwtValidationResult class.
        /// </summary>
        /// <param name="isValid">Whether the validation was successful.</param>
        /// <param name="errorMessage">The error message if validation failed.</param>
        /// <param name="principal">The claims principal if validation succeeded.</param>
        /// <param name="token">The validated JWT token if validation succeeded.</param>
        private JwtValidationResult(bool isValid, string? errorMessage, ClaimsPrincipal? principal, JwtSecurityToken? token)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Principal = principal;
            Token = token;
        }

        /// <summary>
        /// Gets a value indicating whether the JWT token validation was successful.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the claims principal if validation succeeded.
        /// </summary>
        public ClaimsPrincipal? Principal { get; }

        /// <summary>
        /// Gets the validated JWT token if validation succeeded.
        /// </summary>
        public JwtSecurityToken? Token { get; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <param name="principal">The validated claims principal.</param>
        /// <param name="token">The validated JWT token.</param>
        /// <returns>A successful JwtValidationResult.</returns>
        public static JwtValidationResult Success(ClaimsPrincipal? principal, JwtSecurityToken token)
        {
            return new JwtValidationResult(true, null, principal, token);
        }

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why validation failed.</param>
        /// <returns>A failed JwtValidationResult.</returns>
        public static JwtValidationResult Failed(string errorMessage)
        {
            return new JwtValidationResult(false, errorMessage, null, null);
        }

        /// <summary>
        /// Gets the subject claim value from the validated token.
        /// </summary>
        /// <returns>The subject claim value, or null if not present or validation failed.</returns>
        public string? GetSubject()
        {
            return Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                   Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Gets the email claim value from the validated token.
        /// </summary>
        /// <returns>The email claim value, or null if not present or validation failed.</returns>
        public string? GetEmail()
        {
            return Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
                   Principal?.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Gets the organization claim value from the validated token.
        /// </summary>
        /// <returns>The organization claim value, or null if not present or validation failed.</returns>
        public string? GetOrganization()
        {
            return Principal?.FindFirst("organization")?.Value;
        }

        /// <summary>
        /// Gets the role claim value from the validated token.
        /// </summary>
        /// <returns>The role claim value, or null if not present or validation failed.</returns>
        public string? GetRole()
        {
            return Principal?.FindFirst(ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Gets a specific claim value from the validated token.
        /// </summary>
        /// <param name="claimType">The type of claim to retrieve.</param>
        /// <returns>The claim value, or null if not present or validation failed.</returns>
        public string? GetClaim(string claimType)
        {
            return Principal?.FindFirst(claimType)?.Value;
        }

        /// <summary>
        /// Checks if the validated token has a specific role.
        /// </summary>
        /// <param name="role">The role to check for.</param>
        /// <returns>True if the token has the role, false otherwise.</returns>
        public bool HasRole(string role)
        {
            return IsValid && Principal?.IsInRole(role) == true;
        }

        /// <summary>
        /// Checks if the validated token has a specific claim.
        /// </summary>
        /// <param name="claimType">The type of claim to check for.</param>
        /// <returns>True if the token has the claim, false otherwise.</returns>
        public bool HasClaim(string claimType)
        {
            return IsValid && Principal?.HasClaim(claimType, null) == true;
        }

        /// <summary>
        /// Checks if the validated token has a specific claim with a specific value.
        /// </summary>
        /// <param name="claimType">The type of claim to check for.</param>
        /// <param name="claimValue">The value of the claim to check for.</param>
        /// <returns>True if the token has the claim with the specified value, false otherwise.</returns>
        public bool HasClaim(string claimType, string claimValue)
        {
            return IsValid && Principal?.HasClaim(claimType, claimValue) == true;
        }
    }
} 