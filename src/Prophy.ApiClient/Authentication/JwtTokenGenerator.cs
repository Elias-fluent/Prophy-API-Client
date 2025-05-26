using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Prophy.ApiClient.Models.Requests;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Implementation of IJwtTokenGenerator using System.IdentityModel.Tokens.Jwt.
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly ILogger<JwtTokenGenerator> _logger;
        private const string DefaultLoginUrl = "https://www.prophy.ai/api/auth/api-jwt-login/";

        /// <summary>
        /// Initializes a new instance of the JwtTokenGenerator class.
        /// </summary>
        /// <param name="logger">The logger instance for logging JWT operations.</param>
        public JwtTokenGenerator(ILogger<JwtTokenGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public string GenerateToken(JwtLoginClaims claims, string secretKey)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));

            try
            {
                _logger.LogDebug("Generating JWT token for subject: {Subject}, organization: {Organization}", 
                    claims.Subject, claims.Organization);

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey);
                var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

                var tokenClaims = CreateClaims(claims);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(tokenClaims),
                    Expires = DateTime.UtcNow.AddSeconds(claims.ExpirationSeconds),
                    SigningCredentials = signingCredentials,
                    Issuer = claims.Issuer,
                    Audience = claims.Audience
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogDebug("Successfully generated JWT token with expiration: {Expiration}", 
                    tokenDescriptor.Expires);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate JWT token for subject: {Subject}", claims.Subject);
                throw;
            }
        }

        /// <inheritdoc />
        public string GenerateLoginUrl(JwtLoginClaims claims, string secretKey, string? baseUrl = null)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));

            try
            {
                _logger.LogDebug("Generating login URL for subject: {Subject}, organization: {Organization}", 
                    claims.Subject, claims.Organization);

                var token = GenerateToken(claims, secretKey);
                var loginBaseUrl = baseUrl ?? DefaultLoginUrl;
                
                // Ensure the base URL ends with a slash
                if (!loginBaseUrl.EndsWith("/"))
                    loginBaseUrl += "/";

                var loginUrl = $"{loginBaseUrl}?token={Uri.EscapeDataString(token)}";

                _logger.LogDebug("Successfully generated login URL with token length: {TokenLength}", token.Length);

                return loginUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate login URL for subject: {Subject}", claims.Subject);
                throw;
            }
        }

        /// <inheritdoc />
        public bool IsValidTokenFormat(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                // Check if the token can be read (basic format validation)
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                // Basic validation - token should have header, payload, and signature parts
                return jsonToken != null && 
                       !string.IsNullOrEmpty(jsonToken.RawHeader) && 
                       !string.IsNullOrEmpty(jsonToken.RawPayload);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Token format validation failed for token: {TokenPrefix}...", 
                    token.Length > 20 ? token.Substring(0, 20) : token);
                return false;
            }
        }

        /// <summary>
        /// Creates the claims collection from the JWT login claims.
        /// </summary>
        /// <param name="loginClaims">The login claims to convert.</param>
        /// <returns>A collection of claims for the JWT token.</returns>
        private static IEnumerable<Claim> CreateClaims(JwtLoginClaims loginClaims)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, loginClaims.Subject),
                new Claim("organization", loginClaims.Organization),
                new Claim(JwtRegisteredClaimNames.Email, loginClaims.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add optional claims if they have values
            if (!string.IsNullOrEmpty(loginClaims.Folder))
                claims.Add(new Claim("folder", loginClaims.Folder));

            if (!string.IsNullOrEmpty(loginClaims.OriginId))
                claims.Add(new Claim("origin_id", loginClaims.OriginId));

            if (!string.IsNullOrEmpty(loginClaims.FirstName))
                claims.Add(new Claim("first_name", loginClaims.FirstName));

            if (!string.IsNullOrEmpty(loginClaims.LastName))
                claims.Add(new Claim("last_name", loginClaims.LastName));

            if (!string.IsNullOrEmpty(loginClaims.Name))
                claims.Add(new Claim(JwtRegisteredClaimNames.Name, loginClaims.Name));

            if (!string.IsNullOrEmpty(loginClaims.Role))
                claims.Add(new Claim(ClaimTypes.Role, loginClaims.Role));

            return claims;
        }
    }
} 