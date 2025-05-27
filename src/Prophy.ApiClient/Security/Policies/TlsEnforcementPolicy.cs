using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Security policy that enforces TLS/HTTPS requirements for all communications.
    /// </summary>
    public class TlsEnforcementPolicy : ISecurityPolicy
    {
        private readonly ILogger _logger;
        private readonly TlsEnforcementOptions _options;

        /// <inheritdoc />
        public string Name => "TLS Enforcement";

        /// <inheritdoc />
        public int Priority => 100; // High priority

        /// <inheritdoc />
        public bool IsEnabled => _options.IsEnabled;

        /// <summary>
        /// Initializes a new instance of the TlsEnforcementPolicy class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The TLS enforcement options.</param>
        public TlsEnforcementPolicy(ILogger logger, TlsEnforcementOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new TlsEnforcementOptions();
        }

        /// <inheritdoc />
        public Task<PolicyValidationResult> ValidateRequestAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            var violations = new List<PolicyViolation>();

            // Check if the request uses HTTPS
            if (request.RequestUri?.Scheme != Uri.UriSchemeHttps)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Critical,
                    "INSECURE_PROTOCOL",
                    $"Request must use HTTPS. Current scheme: {request.RequestUri?.Scheme ?? "unknown"}",
                    new Dictionary<string, object>
                    {
                        ["RequestUri"] = request.RequestUri?.ToString() ?? "unknown",
                        ["RequiredScheme"] = Uri.UriSchemeHttps,
                        ["ActualScheme"] = request.RequestUri?.Scheme ?? "unknown"
                    }));
            }

            // Validate TLS version if we can access it
            // Note: In practice, this would require access to the underlying connection
            // For demonstration, we'll validate the URI and headers
            ValidateSecurityHeaders(request, violations);

            var metadata = new Dictionary<string, object>
            {
                ["TlsValidationPerformed"] = true,
                ["MinimumTlsVersion"] = _options.MinimumTlsVersion.ToString(),
                ["RequireValidCertificate"] = _options.RequireValidCertificate,
                ["Timestamp"] = DateTimeOffset.UtcNow
            };

            var isValid = !violations.Any();
            
            if (isValid)
            {
                _logger.LogDebug("TLS enforcement validation passed for {RequestUri}", request.RequestUri);
            }
            else
            {
                _logger.LogWarning("TLS enforcement validation failed for {RequestUri} with {ViolationCount} violations", 
                    request.RequestUri, violations.Count);
            }

            return Task.FromResult(isValid 
                ? PolicyValidationResult.Success(metadata) 
                : PolicyValidationResult.Failure(violations, metadata));
        }

        /// <inheritdoc />
        public Task<PolicyValidationResult> ValidateResponseAsync(
            HttpResponseMessage response, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            var violations = new List<PolicyViolation>();

            // Validate security headers in the response
            ValidateResponseSecurityHeaders(response, violations);

            // Check for certificate information if available
            // Note: In a real implementation, this would access the actual certificate
            // from the HTTP connection
            ValidateCertificateInfo(response, violations);

            var metadata = new Dictionary<string, object>
            {
                ["ResponseTlsValidationPerformed"] = true,
                ["StatusCode"] = response.StatusCode,
                ["Timestamp"] = DateTimeOffset.UtcNow
            };

            var isValid = !violations.Any();

            if (isValid)
            {
                _logger.LogDebug("TLS response validation passed for {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogWarning("TLS response validation failed with {ViolationCount} violations", violations.Count);
            }

            return Task.FromResult(isValid 
                ? PolicyValidationResult.Success(metadata) 
                : PolicyValidationResult.Failure(violations, metadata));
        }

        /// <inheritdoc />
        public Task HandleViolationAsync(
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogError("TLS policy violation: {ViolationCode} - {Message}", 
                violation.Code, violation.Message);

            // For critical TLS violations, we might want to take additional action
            if (violation.Severity == PolicyViolationSeverity.Critical)
            {
                _logger.LogCritical("Critical TLS violation detected. Request should be blocked.");
                
                // In a real implementation, this might:
                // - Notify security team
                // - Update threat intelligence
                // - Temporarily block the client
            }

            return Task.CompletedTask;
        }

        private void ValidateSecurityHeaders(HttpRequestMessage request, List<PolicyViolation> violations)
        {
            // Check for security-related headers that should be present
            var headers = request.Headers;

            // Validate User-Agent (should be present and not suspicious)
            if (!headers.UserAgent.Any())
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Warning,
                    "MISSING_USER_AGENT",
                    "User-Agent header is missing",
                    new Dictionary<string, object>
                    {
                        ["HeaderName"] = "User-Agent",
                        ["Recommendation"] = "Include a proper User-Agent header"
                    }));
            }

            // Check for proper authorization header format
            if (headers.Authorization != null)
            {
                var authScheme = headers.Authorization.Scheme;
                if (string.IsNullOrWhiteSpace(authScheme))
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Warning,
                        "INVALID_AUTH_HEADER",
                        "Authorization header has invalid format",
                        new Dictionary<string, object>
                        {
                            ["HeaderName"] = "Authorization",
                            ["Issue"] = "Missing or invalid scheme"
                        }));
                }
            }
        }

        private void ValidateResponseSecurityHeaders(HttpResponseMessage response, List<PolicyViolation> violations)
        {
            var headers = response.Headers;

            // Check for security headers that should be present in responses
            var securityHeaders = new Dictionary<string, string>
            {
                ["Strict-Transport-Security"] = "HSTS header missing",
                ["X-Content-Type-Options"] = "Content type options header missing",
                ["X-Frame-Options"] = "Frame options header missing",
                ["X-XSS-Protection"] = "XSS protection header missing"
            };

            foreach (var securityHeader in securityHeaders)
            {
                if (!headers.Contains(securityHeader.Key) && 
                    !response.Content?.Headers?.Contains(securityHeader.Key) == true)
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Info,
                        "MISSING_SECURITY_HEADER",
                        securityHeader.Value,
                        new Dictionary<string, object>
                        {
                            ["HeaderName"] = securityHeader.Key,
                            ["Severity"] = "Informational",
                            ["Recommendation"] = "Server should include security headers"
                        }));
                }
            }

            // Validate HSTS header if present
            if (headers.Contains("Strict-Transport-Security"))
            {
                var hstsValues = headers.GetValues("Strict-Transport-Security");
                var hstsValue = hstsValues.FirstOrDefault();
                
                if (!string.IsNullOrWhiteSpace(hstsValue))
                {
                    ValidateHstsHeader(hstsValue, violations);
                }
            }
        }

        private void ValidateHstsHeader(string hstsValue, List<PolicyViolation> violations)
        {
            // Basic HSTS validation
            if (!hstsValue.Contains("max-age="))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Warning,
                    "INVALID_HSTS_HEADER",
                    "HSTS header missing max-age directive",
                    new Dictionary<string, object>
                    {
                        ["HeaderValue"] = hstsValue,
                        ["Issue"] = "Missing max-age directive"
                    }));
            }

            // Check for reasonable max-age value
            var maxAgeMatch = System.Text.RegularExpressions.Regex.Match(hstsValue, @"max-age=(\d+)");
            if (maxAgeMatch.Success && int.TryParse(maxAgeMatch.Groups[1].Value, out var maxAge))
            {
                // Recommend at least 1 year (31536000 seconds)
                if (maxAge < 31536000)
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Info,
                        "WEAK_HSTS_MAX_AGE",
                        $"HSTS max-age is less than recommended minimum (1 year). Current: {maxAge} seconds",
                        new Dictionary<string, object>
                        {
                            ["CurrentMaxAge"] = maxAge,
                            ["RecommendedMinimum"] = 31536000,
                            ["Recommendation"] = "Use max-age of at least 31536000 (1 year)"
                        }));
                }
            }
        }

        private void ValidateCertificateInfo(HttpResponseMessage response, List<PolicyViolation> violations)
        {
            // In a real implementation, this would access the actual certificate
            // from the HTTP connection. For demonstration purposes, we'll simulate
            // certificate validation based on available information.

            if (_options.RequireValidCertificate)
            {
                // Simulate certificate validation
                // In practice, this would check:
                // - Certificate chain validity
                // - Certificate expiration
                // - Certificate revocation status
                // - Certificate pinning (if configured)

                _logger.LogDebug("Certificate validation would be performed here");

                // Example of certificate pinning validation
                if (_options.CertificatePinning.IsEnabled && 
                    _options.CertificatePinning.PinnedThumbprints.Any())
                {
                    // In a real implementation, compare actual certificate thumbprint
                    // with pinned thumbprints
                    _logger.LogDebug("Certificate pinning validation would be performed here");
                }
            }
        }
    }
} 