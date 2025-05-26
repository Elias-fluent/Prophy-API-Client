using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Represents the claims required for JWT-based user login to the Prophy platform.
    /// </summary>
    public class JwtLoginClaims
    {
        /// <summary>
        /// Gets or sets the subject (organization name) for the JWT token.
        /// </summary>
        [Required]
        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization code for the JWT token.
        /// </summary>
        [Required]
        [JsonPropertyName("organization")]
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the folder or manuscript context for the login.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets the origin ID for tracking purposes.
        /// </summary>
        [JsonPropertyName("originId")]
        public string? OriginId { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the user's full name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the user's role or permissions.
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the token expiration time in seconds from now.
        /// Default is 3600 seconds (1 hour).
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Expiration seconds must be greater than 0")]
        [JsonPropertyName("expirationSeconds")]
        public int ExpirationSeconds { get; set; } = 3600;

        /// <summary>
        /// Gets or sets the issuer of the JWT token.
        /// </summary>
        [JsonPropertyName("issuer")]
        public string? Issuer { get; set; } = "Prophy";

        /// <summary>
        /// Gets or sets the audience for the JWT token.
        /// </summary>
        [JsonPropertyName("audience")]
        public string? Audience { get; set; } = "Prophy";
    }
} 