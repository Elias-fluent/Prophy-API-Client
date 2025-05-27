using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Provides tenant resolution from various sources including headers, tokens, and URLs.
    /// </summary>
    public class TenantResolver : ITenantResolver
    {
        private readonly ILogger<TenantResolver> _logger;
        private readonly IReadOnlyList<string> _resolutionOrder;

        // Common header names for organization identification
        private static readonly string[] OrganizationHeaders = {
            "X-Organization-Code",
            "X-Org-Code", 
            "X-Tenant-Id",
            "Organization-Code",
            "Org-Code"
        };

        /// <summary>
        /// Initializes a new instance of the TenantResolver class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TenantResolver(ILogger<TenantResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resolutionOrder = new[] { "Headers", "Token", "URL" };
            
            _logger.LogDebug("TenantResolver initialized with resolution order: {ResolutionOrder}", 
                string.Join(", ", _resolutionOrder));
        }

        /// <inheritdoc />
        public async Task<string?> ResolveFromRequestAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                _logger.LogWarning("Cannot resolve tenant from null request");
                return null;
            }

            _logger.LogTrace("Attempting to resolve tenant from request: {RequestUri}", request.RequestUri);

            // Try each resolution strategy in order
            foreach (var strategy in _resolutionOrder)
            {
                string? organizationCode = null;

                try
                {
                    switch (strategy)
                    {
                        case "Headers":
                            var headers = ExtractHeaders(request);
                            organizationCode = ResolveFromHeaders(headers);
                            break;

                        case "Token":
                            var authHeader = request.Headers.Authorization?.Parameter;
                            if (!string.IsNullOrEmpty(authHeader))
                            {
                                organizationCode = await ResolveFromTokenAsync(authHeader);
                            }
                            break;

                        case "URL":
                            if (request.RequestUri != null)
                            {
                                organizationCode = ResolveFromUrl(request.RequestUri);
                            }
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(organizationCode))
                    {
                        _logger.LogDebug("Resolved organization code '{OrganizationCode}' using strategy '{Strategy}'", 
                            organizationCode, strategy);
                        return organizationCode;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve tenant using strategy '{Strategy}'", strategy);
                }
            }

            _logger.LogDebug("Could not resolve organization code from request");
            return null;
        }

        /// <inheritdoc />
        public string? ResolveFromHeaders(IDictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                _logger.LogTrace("No headers provided for tenant resolution");
                return null;
            }

            // Try each known organization header
            foreach (var headerName in OrganizationHeaders)
            {
                if (headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogTrace("Found organization code '{OrganizationCode}' in header '{HeaderName}'", 
                        value, headerName);
                    return value.Trim();
                }
            }

            _logger.LogTrace("No organization code found in headers");
            return null;
        }

        /// <inheritdoc />
        public async Task<string?> ResolveFromTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogTrace("No token provided for tenant resolution");
                return null;
            }

            try
            {
                // Handle Bearer token prefix
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring(7);
                }

                var handler = new JwtSecurityTokenHandler();
                
                // Check if it's a valid JWT format
                if (!handler.CanReadToken(token))
                {
                    _logger.LogTrace("Token is not a valid JWT format");
                    return null;
                }

                var jwtToken = handler.ReadJwtToken(token);
                
                // Try common claim names for organization
                var organizationClaims = new[] { "org", "organization", "org_code", "tenant", "tenant_id" };
                
                foreach (var claimName in organizationClaims)
                {
                    var claim = jwtToken.Claims.FirstOrDefault(c => 
                        c.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase));
                    
                    if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
                    {
                        _logger.LogTrace("Found organization code '{OrganizationCode}' in JWT claim '{ClaimName}'", 
                            claim.Value, claimName);
                        return claim.Value.Trim();
                    }
                }

                _logger.LogTrace("No organization code found in JWT claims");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JWT token for tenant resolution");
                return null;
            }
        }

        /// <inheritdoc />
        public string? ResolveFromUrl(Uri requestUri)
        {
            if (requestUri == null)
            {
                _logger.LogTrace("No URI provided for tenant resolution");
                return null;
            }

            try
            {
                // Try to extract from subdomain (e.g., acme.prophy.ai -> acme)
                var host = requestUri.Host;
                if (!string.IsNullOrEmpty(host))
                {
                    var parts = host.Split('.');
                    if (parts.Length >= 3) // subdomain.domain.tld
                    {
                        var subdomain = parts[0];
                        if (!string.IsNullOrWhiteSpace(subdomain) && 
                            !subdomain.Equals("www", StringComparison.OrdinalIgnoreCase) &&
                            !subdomain.Equals("api", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogTrace("Found organization code '{OrganizationCode}' in subdomain", subdomain);
                            return subdomain.Trim();
                        }
                    }
                }

                // Try to extract from path segments (e.g., /api/v1/orgs/acme/...)
                var segments = requestUri.Segments;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    var segment = segments[i].Trim('/');
                    if (segment.Equals("orgs", StringComparison.OrdinalIgnoreCase) ||
                        segment.Equals("organizations", StringComparison.OrdinalIgnoreCase) ||
                        segment.Equals("tenants", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextSegment = segments[i + 1].Trim('/');
                        if (!string.IsNullOrWhiteSpace(nextSegment))
                        {
                            _logger.LogTrace("Found organization code '{OrganizationCode}' in URL path", nextSegment);
                            return nextSegment;
                        }
                    }
                }

                _logger.LogTrace("No organization code found in URL");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse URL for tenant resolution");
                return null;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetResolutionOrder()
        {
            return _resolutionOrder;
        }

        /// <summary>
        /// Extracts headers from an HTTP request message into a dictionary.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>A dictionary of header names and values.</returns>
        private static Dictionary<string, string> ExtractHeaders(HttpRequestMessage request)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Add request headers
            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            // Add content headers if present
            if (request.Content?.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }

            return headers;
        }
    }
} 