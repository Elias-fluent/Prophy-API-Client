using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Provides comprehensive JWT token validation including signature verification and claims validation.
    /// </summary>
    public class JwtValidator : IJwtValidator
    {
        private readonly ILogger<JwtValidator> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        /// <summary>
        /// Initializes a new instance of the JwtValidator class.
        /// </summary>
        /// <param name="logger">The logger instance for logging validation operations.</param>
        public JwtValidator(ILogger<JwtValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <inheritdoc />
        public JwtValidationResult ValidateToken(string token, string secretKey, JwtValidationOptions? options = null)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty", nameof(secretKey));

            options ??= new JwtValidationOptions();

            try
            {
                _logger.LogDebug("Validating JWT token with signature verification");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var validationParameters = CreateValidationParameters(key, options);

                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null)
                    return JwtValidationResult.Failed("Token is not a valid JWT");

                // Additional custom validations
                var customValidationResult = PerformCustomValidations(jwtToken, options);
                if (!customValidationResult.IsValid)
                    return customValidationResult;

                _logger.LogDebug("JWT token validation successful for subject: {Subject}", 
                    principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                return JwtValidationResult.Success(principal, jwtToken);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "JWT token has expired");
                return JwtValidationResult.Failed("Token has expired");
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning(ex, "JWT token has invalid signature");
                return JwtValidationResult.Failed("Token signature is invalid");
            }
            catch (SecurityTokenValidationException ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed: {Message}", ex.Message);
                return JwtValidationResult.Failed($"Token validation failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during JWT token validation");
                return JwtValidationResult.Failed($"Validation error: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public bool HasRequiredClaims(ClaimsPrincipal principal, IEnumerable<string> requiredClaims)
        {
            if (principal == null || requiredClaims == null)
                return false;

            var claims = new HashSet<string>(principal.Claims.Select(c => c.Type));
            return requiredClaims.All(required => claims.Contains(required));
        }

        /// <inheritdoc />
        public bool HasRole(ClaimsPrincipal principal, string role)
        {
            if (principal == null || string.IsNullOrEmpty(role))
                return false;

            return principal.HasClaim(ClaimTypes.Role, role);
        }

        /// <inheritdoc />
        public bool HasAnyRole(ClaimsPrincipal principal, IEnumerable<string> roles)
        {
            if (principal == null || roles == null)
                return false;

            return roles.Any(role => principal.HasClaim(ClaimTypes.Role, role));
        }

        /// <inheritdoc />
        public string? GetClaimValue(ClaimsPrincipal principal, string claimType)
        {
            return principal?.FindFirst(claimType)?.Value;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetClaimValues(ClaimsPrincipal principal, string claimType)
        {
            if (principal == null)
                return Enumerable.Empty<string>();

            return principal.FindAll(claimType).Select(c => c.Value);
        }

        /// <summary>
        /// Creates token validation parameters based on the provided options.
        /// </summary>
        /// <param name="signingKey">The signing key for token validation.</param>
        /// <param name="options">The validation options.</param>
        /// <returns>Token validation parameters.</returns>
        private static TokenValidationParameters CreateValidationParameters(SecurityKey signingKey, JwtValidationOptions options)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = options.ValidateIssuer,
                ValidIssuer = options.ValidIssuer,
                ValidateAudience = options.ValidateAudience,
                ValidAudience = options.ValidAudience,
                ValidateLifetime = options.ValidateLifetime,
                ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
                RequireExpirationTime = options.RequireExpirationTime,
                RequireSignedTokens = true
            };
        }

        /// <summary>
        /// Performs additional custom validations on the JWT token.
        /// </summary>
        /// <param name="jwtToken">The JWT token to validate.</param>
        /// <param name="options">The validation options.</param>
        /// <returns>The validation result.</returns>
        private JwtValidationResult PerformCustomValidations(JwtSecurityToken jwtToken, JwtValidationOptions options)
        {
            // Validate required claims
            if (options.RequiredClaims?.Any() == true)
            {
                var tokenClaims = new HashSet<string>(jwtToken.Claims.Select(c => c.Type));
                var missingClaims = options.RequiredClaims.Where(required => !tokenClaims.Contains(required)).ToList();
                
                if (missingClaims.Any())
                {
                    _logger.LogWarning("JWT token missing required claims: {MissingClaims}", string.Join(", ", missingClaims));
                    return JwtValidationResult.Failed($"Token missing required claims: {string.Join(", ", missingClaims)}");
                }
            }

            // Validate organization claim if specified
            if (!string.IsNullOrEmpty(options.RequiredOrganization))
            {
                var orgClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "organization")?.Value;
                if (orgClaim != options.RequiredOrganization)
                {
                    _logger.LogWarning("JWT token organization mismatch. Expected: {Expected}, Actual: {Actual}", 
                        options.RequiredOrganization, orgClaim);
                    return JwtValidationResult.Failed("Token organization does not match required organization");
                }
            }

            // Validate custom claim values
            if (options.RequiredClaimValues?.Any() == true)
            {
                foreach (var requiredClaim in options.RequiredClaimValues)
                {
                    var claimValue = jwtToken.Claims.FirstOrDefault(c => c.Type == requiredClaim.Key)?.Value;
                    if (claimValue != requiredClaim.Value)
                    {
                        _logger.LogWarning("JWT token claim value mismatch for {ClaimType}. Expected: {Expected}, Actual: {Actual}", 
                            requiredClaim.Key, requiredClaim.Value, claimValue);
                        return JwtValidationResult.Failed($"Token claim '{requiredClaim.Key}' value does not match required value");
                    }
                }
            }

            return JwtValidationResult.Success(null, jwtToken);
        }
    }
} 