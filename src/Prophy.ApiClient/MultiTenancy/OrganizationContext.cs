using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Represents the context for a specific organization/tenant, encapsulating
    /// tenant-specific data and configuration throughout the application lifecycle.
    /// </summary>
    public class OrganizationContext
    {
        /// <summary>
        /// Initializes a new instance of the OrganizationContext class.
        /// </summary>
        /// <param name="organizationCode">The unique organization code.</param>
        /// <param name="organizationName">The display name of the organization.</param>
        /// <param name="apiKey">The API key associated with this organization.</param>
        /// <param name="baseUrl">The base URL for API calls for this organization.</param>
        /// <param name="properties">Additional organization-specific properties.</param>
        /// <param name="userClaims">User claims associated with the current context.</param>
        /// <exception cref="ArgumentException">Thrown when organizationCode is null or empty.</exception>
        public OrganizationContext(
            string organizationCode,
            string? organizationName = null,
            string? apiKey = null,
            string? baseUrl = null,
            IReadOnlyDictionary<string, object>? properties = null,
            IEnumerable<Claim>? userClaims = null)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be null or empty.", nameof(organizationCode));

            OrganizationCode = organizationCode;
            OrganizationName = organizationName ?? organizationCode;
            ApiKey = apiKey;
            BaseUrl = baseUrl;
            Properties = properties?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
            UserClaims = userClaims?.ToImmutableList() ?? ImmutableList<Claim>.Empty;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the unique organization code that identifies this tenant.
        /// </summary>
        public string OrganizationCode { get; }

        /// <summary>
        /// Gets the display name of the organization.
        /// </summary>
        public string OrganizationName { get; }

        /// <summary>
        /// Gets the API key associated with this organization.
        /// </summary>
        public string? ApiKey { get; }

        /// <summary>
        /// Gets the base URL for API calls for this organization.
        /// </summary>
        public string? BaseUrl { get; }

        /// <summary>
        /// Gets additional organization-specific properties.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the user claims associated with the current context.
        /// </summary>
        public IReadOnlyList<Claim> UserClaims { get; }

        /// <summary>
        /// Gets the timestamp when this context was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Gets a property value by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <returns>The property value if found; otherwise, the default value for the type.</returns>
        public T GetProperty<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default(T);

            return Properties.TryGetValue(key, out var value) && value is T typedValue
                ? typedValue
                : default(T);
        }

        /// <summary>
        /// Checks if a property exists.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>True if the property exists; otherwise, false.</returns>
        public bool HasProperty(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && Properties.ContainsKey(key);
        }

        /// <summary>
        /// Creates a new OrganizationContext with updated properties.
        /// </summary>
        /// <param name="additionalProperties">Additional properties to merge with existing ones.</param>
        /// <returns>A new OrganizationContext instance with merged properties.</returns>
        public OrganizationContext WithProperties(IReadOnlyDictionary<string, object> additionalProperties)
        {
            if (additionalProperties == null || additionalProperties.Count == 0)
                return this;

            var mergedProperties = new Dictionary<string, object>();
            foreach (var kvp in Properties)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in additionalProperties)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }

            return new OrganizationContext(
                OrganizationCode,
                OrganizationName,
                ApiKey,
                BaseUrl,
                mergedProperties,
                UserClaims);
        }

        /// <summary>
        /// Creates a new OrganizationContext with updated user claims.
        /// </summary>
        /// <param name="userClaims">The user claims to set.</param>
        /// <returns>A new OrganizationContext instance with updated user claims.</returns>
        public OrganizationContext WithUserClaims(IEnumerable<Claim> userClaims)
        {
            return new OrganizationContext(
                OrganizationCode,
                OrganizationName,
                ApiKey,
                BaseUrl,
                Properties,
                userClaims);
        }

        /// <summary>
        /// Creates a new OrganizationContext with an updated API key.
        /// </summary>
        /// <param name="apiKey">The new API key.</param>
        /// <returns>A new OrganizationContext instance with the updated API key.</returns>
        public OrganizationContext WithApiKey(string apiKey)
        {
            return new OrganizationContext(
                OrganizationCode,
                OrganizationName,
                apiKey,
                BaseUrl,
                Properties,
                UserClaims);
        }

        /// <summary>
        /// Creates a new OrganizationContext with an updated base URL.
        /// </summary>
        /// <param name="baseUrl">The new base URL.</param>
        /// <returns>A new OrganizationContext instance with the updated base URL.</returns>
        public OrganizationContext WithBaseUrl(string baseUrl)
        {
            return new OrganizationContext(
                OrganizationCode,
                OrganizationName,
                ApiKey,
                baseUrl,
                Properties,
                UserClaims);
        }

        /// <summary>
        /// Returns a string representation of the organization context.
        /// </summary>
        /// <returns>A string containing the organization code and name.</returns>
        public override string ToString()
        {
            return $"OrganizationContext: {OrganizationCode} ({OrganizationName})";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            return obj is OrganizationContext other && 
                   OrganizationCode.Equals(other.OrganizationCode, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(OrganizationCode);
        }
    }
} 