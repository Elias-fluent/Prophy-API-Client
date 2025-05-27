using System;
using System.Collections.Generic;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Configuration options for JWT token validation.
    /// </summary>
    public class JwtValidationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to validate the token issuer.
        /// Default is false.
        /// </summary>
        public bool ValidateIssuer { get; set; } = false;

        /// <summary>
        /// Gets or sets the valid issuer for token validation.
        /// Only used if ValidateIssuer is true.
        /// </summary>
        public string? ValidIssuer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the token audience.
        /// Default is false.
        /// </summary>
        public bool ValidateAudience { get; set; } = false;

        /// <summary>
        /// Gets or sets the valid audience for token validation.
        /// Only used if ValidateAudience is true.
        /// </summary>
        public string? ValidAudience { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the token lifetime.
        /// Default is true.
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Gets or sets the clock skew allowance in seconds for token expiration validation.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int ClockSkewSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets a value indicating whether the token must have an expiration time.
        /// Default is true.
        /// </summary>
        public bool RequireExpirationTime { get; set; } = true;

        /// <summary>
        /// Gets or sets the collection of required claim types that must be present in the token.
        /// </summary>
        public IEnumerable<string>? RequiredClaims { get; set; }

        /// <summary>
        /// Gets or sets the required organization value for the "organization" claim.
        /// If specified, the token must contain an "organization" claim with this exact value.
        /// </summary>
        public string? RequiredOrganization { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of required claim values.
        /// The token must contain claims with the specified types and exact values.
        /// </summary>
        public Dictionary<string, string>? RequiredClaimValues { get; set; }

        /// <summary>
        /// Creates a new instance with default validation settings.
        /// </summary>
        /// <returns>A new JwtValidationOptions instance with default settings.</returns>
        public static JwtValidationOptions Default()
        {
            return new JwtValidationOptions();
        }

        /// <summary>
        /// Creates a new instance with strict validation settings.
        /// </summary>
        /// <param name="validIssuer">The required issuer.</param>
        /// <param name="validAudience">The required audience.</param>
        /// <returns>A new JwtValidationOptions instance with strict validation.</returns>
        public static JwtValidationOptions Strict(string validIssuer, string validAudience)
        {
            return new JwtValidationOptions
            {
                ValidateIssuer = true,
                ValidIssuer = validIssuer,
                ValidateAudience = true,
                ValidAudience = validAudience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkewSeconds = 60 // Reduced clock skew for strict validation
            };
        }

        /// <summary>
        /// Creates a new instance configured for Prophy API validation.
        /// </summary>
        /// <param name="organizationCode">The required organization code.</param>
        /// <returns>A new JwtValidationOptions instance configured for Prophy API.</returns>
        public static JwtValidationOptions ForProphy(string organizationCode)
        {
            return new JwtValidationOptions
            {
                ValidateIssuer = true,
                ValidIssuer = "Prophy",
                ValidateAudience = true,
                ValidAudience = "Prophy",
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequiredOrganization = organizationCode,
                RequiredClaims = new[] { "sub", "organization", "email" }
            };
        }
    }
} 