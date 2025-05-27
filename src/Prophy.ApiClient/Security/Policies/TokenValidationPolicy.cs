using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Security policy that validates API keys and JWT tokens.
    /// </summary>
    public class TokenValidationPolicy : ISecurityPolicy
    {
        private readonly ILogger _logger;
        private readonly TokenValidationOptions _options;
        private readonly JwtSecurityTokenHandler _jwtHandler;

        /// <inheritdoc />
        public string Name => "Token Validation";

        /// <inheritdoc />
        public int Priority => 80; // High priority, but lower than TLS and throttling

        /// <inheritdoc />
        public bool IsEnabled => _options.IsEnabled;

        /// <summary>
        /// Initializes a new instance of the TokenValidationPolicy class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The token validation options.</param>
        public TokenValidationPolicy(ILogger logger, TokenValidationOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new TokenValidationOptions();
            _jwtHandler = new JwtSecurityTokenHandler();
        }

        /// <inheritdoc />
        public Task<PolicyValidationResult> ValidateRequestAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            var violations = new List<PolicyViolation>();

            // Validate API key if present
            ValidateApiKey(request, context, violations);

            // Validate JWT token if present
            ValidateJwtToken(request, context, violations);

            // Validate authorization header format
            ValidateAuthorizationHeader(request, violations);

            var metadata = new Dictionary<string, object>
            {
                ["TokenValidationPerformed"] = true,
                ["ApiKeyValidation"] = _options.ValidateApiKeyFormat,
                ["JwtValidation"] = _options.ValidateJwtTokens,
                ["MaxTokenAge"] = _options.MaxTokenAgeMinutes,
                ["Timestamp"] = DateTimeOffset.UtcNow
            };

            var isValid = !violations.Any(v => v.Severity >= PolicyViolationSeverity.Error);

            if (isValid)
            {
                _logger.LogDebug("Token validation passed for request to {RequestUri}", request.RequestUri);
            }
            else
            {
                _logger.LogWarning("Token validation failed for request to {RequestUri} with {ViolationCount} violations", 
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

            // Check for authentication-related response headers
            ValidateAuthenticationResponseHeaders(response, violations);

            // Check for token expiration warnings in response
            ValidateTokenExpirationHeaders(response, violations);

            var metadata = new Dictionary<string, object>
            {
                ["ResponseTokenValidationPerformed"] = true,
                ["StatusCode"] = response.StatusCode,
                ["Timestamp"] = DateTimeOffset.UtcNow
            };

            var isValid = !violations.Any(v => v.Severity >= PolicyViolationSeverity.Error);

            if (isValid)
            {
                _logger.LogDebug("Token response validation passed for {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Token response validation failed with {ViolationCount} violations", violations.Count);
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
            _logger.LogWarning("Token validation violation: {ViolationCode} - {Message}", 
                violation.Code, violation.Message);

            // For critical token violations, we might want to take additional action
            if (violation.Severity >= PolicyViolationSeverity.Error)
            {
                _logger.LogError("Blocking request due to token validation violation");
                
                // In a real implementation, this might:
                // - Invalidate the token
                // - Notify the authentication service
                // - Log security events for monitoring
                // - Trigger token refresh if applicable
            }

            return Task.CompletedTask;
        }

        private void ValidateApiKey(HttpRequestMessage request, SecurityContext context, List<PolicyViolation> violations)
        {
            if (!_options.ValidateApiKeyFormat)
                return;

            var apiKey = context.ApiKey;
            
            // Check if API key is present when required
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Check for API key in headers
                if (request.Headers.Contains("X-ApiKey"))
                {
                    apiKey = request.Headers.GetValues("X-ApiKey").FirstOrDefault();
                }
                else if (request.Headers.Contains("X-API-Key"))
                {
                    apiKey = request.Headers.GetValues("X-API-Key").FirstOrDefault();
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "MISSING_API_KEY",
                    "API key is required but not provided",
                    new Dictionary<string, object>
                    {
                        ["ExpectedHeaders"] = new[] { "X-ApiKey", "X-API-Key" },
                        ["Recommendation"] = "Include a valid API key in the request headers"
                    }));
                return;
            }

            // Validate API key format
            if (!IsValidApiKeyFormat(apiKey))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "INVALID_API_KEY_FORMAT",
                    "API key format is invalid",
                    new Dictionary<string, object>
                    {
                        ["ApiKeyLength"] = apiKey.Length,
                        ["ExpectedFormat"] = "Base64 encoded string or UUID format",
                        ["Recommendation"] = "Ensure API key follows the expected format"
                    }));
            }

            // Check for suspicious API key patterns
            if (IsSuspiciousApiKey(apiKey))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Warning,
                    "SUSPICIOUS_API_KEY",
                    "API key appears to be a test or placeholder value",
                    new Dictionary<string, object>
                    {
                        ["ApiKeyPrefix"] = apiKey.Length > 10 ? apiKey.Substring(0, 10) + "..." : apiKey,
                        ["Recommendation"] = "Use a production API key for live environments"
                    }));
            }
        }

        private void ValidateJwtToken(HttpRequestMessage request, SecurityContext context, List<PolicyViolation> violations)
        {
            if (!_options.ValidateJwtTokens)
                return;

            var authHeader = request.Headers.Authorization;
            if (authHeader?.Scheme?.Equals("Bearer", StringComparison.OrdinalIgnoreCase) != true)
                return;

            var token = authHeader.Parameter;
            if (string.IsNullOrWhiteSpace(token))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "MISSING_JWT_TOKEN",
                    "Bearer token is missing from Authorization header",
                    new Dictionary<string, object>
                    {
                        ["AuthorizationScheme"] = authHeader.Scheme,
                        ["Recommendation"] = "Include a valid JWT token in the Bearer authorization header"
                    }));
                return;
            }

            try
            {
                // Parse JWT token without validation (just to read claims)
                var jwtToken = _jwtHandler.ReadJwtToken(token);
                
                // Validate token age
                ValidateTokenAge(jwtToken, violations);
                
                // Validate required claims
                ValidateRequiredClaims(jwtToken, violations);
                
                // Validate issuer if configured
                ValidateIssuer(jwtToken, violations);
                
                // Validate audience if configured
                ValidateAudience(jwtToken, violations);

                _logger.LogDebug("JWT token validation completed for token with {ClaimCount} claims", 
                    jwtToken.Claims.Count());
            }
            catch (ArgumentException ex)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "INVALID_JWT_FORMAT",
                    $"JWT token format is invalid: {ex.Message}",
                    new Dictionary<string, object>
                    {
                        ["TokenLength"] = token.Length,
                        ["Error"] = ex.Message,
                        ["Recommendation"] = "Ensure JWT token is properly formatted"
                    }));
            }
            catch (Exception ex)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "JWT_PARSING_ERROR",
                    $"Failed to parse JWT token: {ex.Message}",
                    new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message,
                        ["Recommendation"] = "Check JWT token format and encoding"
                    }));
            }
        }

        private void ValidateAuthorizationHeader(HttpRequestMessage request, List<PolicyViolation> violations)
        {
            var authHeader = request.Headers.Authorization;
            if (authHeader == null)
                return;

            // Validate authorization scheme
            var validSchemes = new[] { "Bearer", "Basic", "ApiKey" };
            if (!validSchemes.Contains(authHeader.Scheme, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Warning,
                    "UNKNOWN_AUTH_SCHEME",
                    $"Unknown authorization scheme: {authHeader.Scheme}",
                    new Dictionary<string, object>
                    {
                        ["ProvidedScheme"] = authHeader.Scheme,
                        ["ValidSchemes"] = validSchemes,
                        ["Recommendation"] = "Use a standard authorization scheme"
                    }));
            }

            // Validate parameter is present
            if (string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "MISSING_AUTH_PARAMETER",
                    $"Authorization parameter is missing for scheme: {authHeader.Scheme}",
                    new Dictionary<string, object>
                    {
                        ["AuthorizationScheme"] = authHeader.Scheme,
                        ["Recommendation"] = "Include the authorization parameter (token, credentials, etc.)"
                    }));
            }
        }

        private void ValidateAuthenticationResponseHeaders(HttpResponseMessage response, List<PolicyViolation> violations)
        {
            // Check for authentication challenges in 401 responses
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (!response.Headers.Contains("WWW-Authenticate"))
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Info,
                        "MISSING_AUTH_CHALLENGE",
                        "401 response missing WWW-Authenticate header",
                        new Dictionary<string, object>
                        {
                            ["StatusCode"] = response.StatusCode,
                            ["Recommendation"] = "Server should include WWW-Authenticate header in 401 responses"
                        }));
                }
            }

            // Check for token refresh hints
            if (response.Headers.Contains("X-Token-Expires-In"))
            {
                var expiresInValues = response.Headers.GetValues("X-Token-Expires-In");
                var expiresIn = expiresInValues.FirstOrDefault();
                
                if (int.TryParse(expiresIn, out var seconds) && seconds < 300) // Less than 5 minutes
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Warning,
                        "TOKEN_EXPIRING_SOON",
                        $"Token expires in {seconds} seconds",
                        new Dictionary<string, object>
                        {
                            ["ExpiresInSeconds"] = seconds,
                            ["Recommendation"] = "Consider refreshing the token soon"
                        }));
                }
            }
        }

        private void ValidateTokenExpirationHeaders(HttpResponseMessage response, List<PolicyViolation> violations)
        {
            // Check for token expiration warnings
            if (response.Headers.Contains("X-Token-Warning"))
            {
                var warnings = response.Headers.GetValues("X-Token-Warning");
                foreach (var warning in warnings)
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Warning,
                        "TOKEN_WARNING",
                        $"Token warning from server: {warning}",
                        new Dictionary<string, object>
                        {
                            ["ServerWarning"] = warning,
                            ["Recommendation"] = "Address the token warning to prevent authentication issues"
                        }));
                }
            }
        }

        private void ValidateTokenAge(JwtSecurityToken jwtToken, List<PolicyViolation> violations)
        {
            var now = DateTimeOffset.UtcNow;
            var maxAge = TimeSpan.FromMinutes(_options.MaxTokenAgeMinutes);

            // Check issued at time
            if (jwtToken.IssuedAt != DateTime.MinValue)
            {
                var issuedAt = new DateTimeOffset(jwtToken.IssuedAt, TimeSpan.Zero);
                var age = now - issuedAt;
                
                if (age > maxAge)
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Error,
                        "TOKEN_TOO_OLD",
                        $"Token is too old: issued {age.TotalMinutes:F1} minutes ago (max: {_options.MaxTokenAgeMinutes} minutes)",
                        new Dictionary<string, object>
                        {
                            ["TokenAge"] = age.TotalMinutes,
                            ["MaxAge"] = _options.MaxTokenAgeMinutes,
                            ["IssuedAt"] = issuedAt,
                            ["Recommendation"] = "Obtain a new token"
                        }));
                }
            }

            // Check expiration time
            if (jwtToken.ValidTo != DateTime.MinValue)
            {
                var validTo = new DateTimeOffset(jwtToken.ValidTo, TimeSpan.Zero);
                
                if (now > validTo)
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Error,
                        "TOKEN_EXPIRED",
                        $"Token has expired: valid until {validTo:yyyy-MM-dd HH:mm:ss} UTC",
                        new Dictionary<string, object>
                        {
                            ["ValidTo"] = validTo,
                            ["CurrentTime"] = now,
                            ["Recommendation"] = "Obtain a new token"
                        }));
                }
                else if ((validTo - now).TotalMinutes < 5) // Expires within 5 minutes
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Warning,
                        "TOKEN_EXPIRING_SOON",
                        $"Token expires soon: valid until {validTo:yyyy-MM-dd HH:mm:ss} UTC",
                        new Dictionary<string, object>
                        {
                            ["ValidTo"] = validTo,
                            ["MinutesUntilExpiry"] = (validTo - now).TotalMinutes,
                            ["Recommendation"] = "Consider refreshing the token"
                        }));
                }
            }
        }

        private void ValidateRequiredClaims(JwtSecurityToken jwtToken, List<PolicyViolation> violations)
        {
            if (!_options.RequiredClaims.Any())
                return;

            var tokenClaims = new HashSet<string>(jwtToken.Claims.Select(c => c.Type), StringComparer.OrdinalIgnoreCase);
            
            foreach (var requiredClaim in _options.RequiredClaims)
            {
                if (!tokenClaims.Contains(requiredClaim))
                {
                    violations.Add(new PolicyViolation(
                        Name,
                        PolicyViolationSeverity.Error,
                        "MISSING_REQUIRED_CLAIM",
                        $"Required claim '{requiredClaim}' is missing from token",
                        new Dictionary<string, object>
                        {
                            ["RequiredClaim"] = requiredClaim,
                            ["AvailableClaims"] = tokenClaims.ToArray(),
                            ["Recommendation"] = "Ensure token includes all required claims"
                        }));
                }
            }
        }

        private void ValidateIssuer(JwtSecurityToken jwtToken, List<PolicyViolation> violations)
        {
            if (!_options.AllowedIssuers.Any())
                return;

            var issuer = jwtToken.Issuer;
            if (string.IsNullOrWhiteSpace(issuer))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "MISSING_ISSUER",
                    "Token issuer claim is missing",
                    new Dictionary<string, object>
                    {
                        ["AllowedIssuers"] = _options.AllowedIssuers.ToArray(),
                        ["Recommendation"] = "Token must include a valid issuer claim"
                    }));
                return;
            }

            if (!_options.AllowedIssuers.Contains(issuer, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "INVALID_ISSUER",
                    $"Token issuer '{issuer}' is not in the allowed list",
                    new Dictionary<string, object>
                    {
                        ["TokenIssuer"] = issuer,
                        ["AllowedIssuers"] = _options.AllowedIssuers.ToArray(),
                        ["Recommendation"] = "Use a token from an allowed issuer"
                    }));
            }
        }

        private void ValidateAudience(JwtSecurityToken jwtToken, List<PolicyViolation> violations)
        {
            if (!_options.AllowedAudiences.Any())
                return;

            var audiences = jwtToken.Audiences.ToList();
            if (!audiences.Any())
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "MISSING_AUDIENCE",
                    "Token audience claim is missing",
                    new Dictionary<string, object>
                    {
                        ["AllowedAudiences"] = _options.AllowedAudiences.ToArray(),
                        ["Recommendation"] = "Token must include a valid audience claim"
                    }));
                return;
            }

            var hasValidAudience = audiences.Any(aud => 
                _options.AllowedAudiences.Contains(aud, StringComparer.OrdinalIgnoreCase));

            if (!hasValidAudience)
            {
                violations.Add(new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Error,
                    "INVALID_AUDIENCE",
                    $"Token audiences [{string.Join(", ", audiences)}] are not in the allowed list",
                    new Dictionary<string, object>
                    {
                        ["TokenAudiences"] = audiences.ToArray(),
                        ["AllowedAudiences"] = _options.AllowedAudiences.ToArray(),
                        ["Recommendation"] = "Use a token with an allowed audience"
                    }));
            }
        }

        private static bool IsValidApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            // Check for common API key formats
            // 1. Base64 encoded (at least 16 characters)
            if (apiKey.Length >= 16 && IsBase64String(apiKey))
                return true;

            // 2. UUID format
            if (Guid.TryParse(apiKey, out _))
                return true;

            // 3. Hexadecimal format (at least 32 characters)
            if (apiKey.Length >= 32 && IsHexString(apiKey))
                return true;

            // 4. Custom format with prefix (e.g., "pk_live_...", "sk_test_...")
            if (Regex.IsMatch(apiKey, @"^[a-z]{2,4}_[a-z]{4,8}_[a-zA-Z0-9]{20,}$"))
                return true;

            return false;
        }

        private static bool IsSuspiciousApiKey(string apiKey)
        {
            var suspiciousPatterns = new[]
            {
                "test", "demo", "example", "sample", "placeholder",
                "12345", "abcde", "aaaaa", "00000"
            };

            var lowerKey = apiKey.ToLowerInvariant();
            return suspiciousPatterns.Any(pattern => lowerKey.Contains(pattern));
        }

        private static bool IsBase64String(string value)
        {
            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHexString(string value)
        {
            return Regex.IsMatch(value, @"^[0-9a-fA-F]+$");
        }
    }
} 