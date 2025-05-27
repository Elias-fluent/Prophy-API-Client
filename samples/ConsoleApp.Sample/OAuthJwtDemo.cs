using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates OAuth integration and JWT validation functionality.
    /// </summary>
    public static class OAuthJwtDemo
    {
        /// <summary>
        /// Runs the OAuth and JWT validation demonstration.
        /// </summary>
        /// <param name="logger">The logger instance for logging demo operations.</param>
        public static async Task RunAsync(ILogger logger)
        {
            logger.LogInformation("=== OAuth Integration and JWT Validation Demo ===");

            try
            {
                // Demo 1: JWT Token Generation and Validation
                await DemoJwtTokenValidation(logger);

                // Demo 2: OAuth Client Credentials Flow
                await DemoOAuthClientCredentials(logger);

                // Demo 3: OAuth Authorization Code Flow with PKCE
                await DemoOAuthAuthorizationCodeWithPkce(logger);

                // Demo 4: Secure Token Storage
                await DemoSecureTokenStorage(logger);

                // Demo 5: JWT Claims Validation
                await DemoJwtClaimsValidation(logger);

                logger.LogInformation("OAuth and JWT validation demo completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OAuth and JWT validation demo failed");
                throw;
            }
        }

        /// <summary>
        /// Demonstrates JWT token generation and validation.
        /// </summary>
        private static async Task DemoJwtTokenValidation(ILogger logger)
        {
            logger.LogInformation("\n--- JWT Token Generation and Validation ---");

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var jwtLogger = loggerFactory.CreateLogger<JwtTokenGenerator>();
            var validatorLogger = loggerFactory.CreateLogger<JwtValidator>();

            // Create JWT token generator and validator
            var tokenGenerator = new JwtTokenGenerator(jwtLogger);
            var validator = new JwtValidator(validatorLogger);

            // Generate a JWT token
            var claims = new JwtLoginClaims
            {
                Subject = "user123",
                Email = "user@example.com",
                Organization = "TestOrg",
                Role = "Admin"
            };

            var secretKey = "MySecretKeyForJWTTokenGeneration123456789";
            var token = tokenGenerator.GenerateToken(claims, secretKey);

            logger.LogInformation("Generated JWT Token: {Token}", token.Substring(0, 50) + "...");

            // Validate the token
            var validationOptions = JwtValidationOptions.ForProphy("TestOrg");
            var validationResult = validator.ValidateToken(token, secretKey, validationOptions);

            if (validationResult.IsValid)
            {
                logger.LogInformation("✅ JWT token validation successful!");
                logger.LogInformation("Subject: {Subject}", validationResult.GetSubject());
                logger.LogInformation("Email: {Email}", validationResult.GetEmail());
                logger.LogInformation("Organization: {Organization}", validationResult.GetOrganization());
                logger.LogInformation("Role: {Role}", validationResult.GetRole());
            }
            else
            {
                logger.LogWarning("❌ JWT token validation failed: {Error}", validationResult.ErrorMessage);
            }

            // Test invalid token validation
            var invalidResult = validator.ValidateToken("invalid.token.here", secretKey);
            logger.LogInformation("Invalid token validation result: {IsValid}", invalidResult.IsValid);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Demonstrates OAuth client credentials flow.
        /// </summary>
        private static async Task DemoOAuthClientCredentials(ILogger logger)
        {
            logger.LogInformation("\n--- OAuth Client Credentials Flow ---");

            using var httpClient = new HttpClient();
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var oauthLogger = loggerFactory.CreateLogger<OAuthClient>();

            var oauthClient = new OAuthClient(httpClient, oauthLogger);

            // Simulate OAuth client credentials flow
            logger.LogInformation("Simulating OAuth client credentials flow...");

            try
            {
                // Note: This would normally connect to a real OAuth server
                // For demo purposes, we'll show the request structure
                var tokenEndpoint = "https://auth.example.com/oauth/token";
                var clientId = "demo-client-id";
                var clientSecret = "demo-client-secret";
                var scope = "api:read api:write";

                logger.LogInformation("Token Endpoint: {Endpoint}", tokenEndpoint);
                logger.LogInformation("Client ID: {ClientId}", clientId);
                logger.LogInformation("Scope: {Scope}", scope);

                // This would make an actual HTTP request in a real scenario
                logger.LogInformation("Would send POST request to token endpoint with:");
                logger.LogInformation("  grant_type=client_credentials");
                logger.LogInformation("  client_id={ClientId}", clientId);
                logger.LogInformation("  client_secret=[REDACTED]");
                logger.LogInformation("  scope={Scope}", scope);

                // Simulate successful response
                var simulatedResponse = new OAuthTokenResponse
                {
                    AccessToken = "simulated_access_token_12345",
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    Scope = scope
                };

                logger.LogInformation("✅ Simulated OAuth client credentials flow successful!");
                logger.LogInformation("Access Token: {Token}", simulatedResponse.AccessToken.Substring(0, 20) + "...");
                logger.LogInformation("Token Type: {TokenType}", simulatedResponse.TokenType);
                logger.LogInformation("Expires In: {ExpiresIn} seconds", simulatedResponse.ExpiresIn);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OAuth client credentials flow failed");
            }
        }

        /// <summary>
        /// Demonstrates OAuth authorization code flow with PKCE.
        /// </summary>
        private static async Task DemoOAuthAuthorizationCodeWithPkce(ILogger logger)
        {
            logger.LogInformation("\n--- OAuth Authorization Code Flow with PKCE ---");

            using var httpClient = new HttpClient();
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var oauthLogger = loggerFactory.CreateLogger<OAuthClient>();

            var oauthClient = new OAuthClient(httpClient, oauthLogger);

            try
            {
                // Generate PKCE parameters
                var codeVerifier = PkceHelper.GenerateCodeVerifier();
                var codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);

                logger.LogInformation("Generated PKCE parameters:");
                logger.LogInformation("Code Verifier: {CodeVerifier}", codeVerifier.Substring(0, 20) + "...");
                logger.LogInformation("Code Challenge: {CodeChallenge}", codeChallenge);

                // Build authorization URL
                var authUrl = oauthClient.BuildAuthorizationUrl(
                    authorizationEndpoint: "https://auth.example.com/oauth/authorize",
                    clientId: "demo-public-client",
                    redirectUri: "https://app.example.com/callback",
                    scope: "openid profile email",
                    state: "random-state-value-123",
                    codeChallenge: codeChallenge,
                    codeChallengeMethod: "S256"
                );

                logger.LogInformation("✅ Authorization URL built successfully:");
                logger.LogInformation("URL: {AuthUrl}", authUrl);

                // Simulate authorization code exchange
                logger.LogInformation("\nSimulating authorization code exchange...");
                var authCode = "simulated_auth_code_12345";
                
                logger.LogInformation("Would exchange authorization code for tokens:");
                logger.LogInformation("  grant_type=authorization_code");
                logger.LogInformation("  client_id=demo-public-client");
                logger.LogInformation("  code={AuthCode}", authCode);
                logger.LogInformation("  redirect_uri=https://app.example.com/callback");
                logger.LogInformation("  code_verifier={CodeVerifier}", codeVerifier.Substring(0, 20) + "...");

                // Simulate successful token response
                var simulatedTokenResponse = new OAuthTokenResponse
                {
                    AccessToken = "simulated_access_token_pkce_12345",
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    RefreshToken = "simulated_refresh_token_12345",
                    Scope = "openid profile email"
                };

                logger.LogInformation("✅ Simulated authorization code exchange successful!");
                logger.LogInformation("Access Token: {Token}", simulatedTokenResponse.AccessToken.Substring(0, 20) + "...");
                logger.LogInformation("Refresh Token: {RefreshToken}", simulatedTokenResponse.RefreshToken?.Substring(0, 20) + "...");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OAuth authorization code flow with PKCE failed");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Demonstrates secure token storage functionality.
        /// </summary>
        private static async Task DemoSecureTokenStorage(ILogger logger)
        {
            logger.LogInformation("\n--- Secure Token Storage ---");

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var storageLogger = loggerFactory.CreateLogger<SecureTokenStorage>();

            var tokenStorage = new SecureTokenStorage(storageLogger);

            try
            {
                // Create sample tokens
                var token1 = new OAuthTokenResponse
                {
                    AccessToken = "sample_access_token_1",
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    RefreshToken = "sample_refresh_token_1"
                };

                var token2 = new OAuthTokenResponse
                {
                    AccessToken = "sample_access_token_2",
                    TokenType = "Bearer",
                    ExpiresIn = 7200,
                    RefreshToken = "sample_refresh_token_2"
                };

                // Store tokens
                tokenStorage.StoreToken("user1", token1);
                tokenStorage.StoreToken("user2", token2);
                logger.LogInformation("✅ Stored 2 tokens securely with encryption");

                // Retrieve tokens
                var retrievedToken1 = tokenStorage.GetToken("user1");
                var retrievedToken2 = tokenStorage.GetToken("user2");

                if (retrievedToken1 != null && retrievedToken2 != null)
                {
                    logger.LogInformation("✅ Successfully retrieved encrypted tokens");
                    logger.LogInformation("Token 1 - Access Token: {Token}", retrievedToken1.AccessToken);
                    logger.LogInformation("Token 2 - Access Token: {Token}", retrievedToken2.AccessToken);
                }

                // Check token validity
                var isValid1 = tokenStorage.IsTokenValid("user1");
                var isValid2 = tokenStorage.IsTokenValid("user2");
                logger.LogInformation("Token 1 valid: {IsValid}", isValid1);
                logger.LogInformation("Token 2 valid: {IsValid}", isValid2);

                // Check expiration
                var expiringSoon1 = tokenStorage.IsTokenExpiringSoon("user1", TimeSpan.FromMinutes(30));
                var expiringSoon2 = tokenStorage.IsTokenExpiringSoon("user2", TimeSpan.FromMinutes(30));
                logger.LogInformation("Token 1 expiring soon: {ExpiringSoon}", expiringSoon1);
                logger.LogInformation("Token 2 expiring soon: {ExpiringSoon}", expiringSoon2);

                // Cleanup
                tokenStorage.RemoveToken("user1");
                logger.LogInformation("✅ Removed token for user1");

                tokenStorage.ClearAllTokens();
                logger.LogInformation("✅ Cleared all remaining tokens");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Secure token storage demo failed");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Demonstrates JWT claims validation functionality.
        /// </summary>
        private static async Task DemoJwtClaimsValidation(ILogger logger)
        {
            logger.LogInformation("\n--- JWT Claims Validation ---");

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var jwtLogger = loggerFactory.CreateLogger<JwtTokenGenerator>();
            var validatorLogger = loggerFactory.CreateLogger<JwtValidator>();

            var tokenGenerator = new JwtTokenGenerator(jwtLogger);
            var validator = new JwtValidator(validatorLogger);

            try
            {
                // Generate token with specific claims
                var claims = new JwtLoginClaims
                {
                    Subject = "admin123",
                    Email = "admin@prophy.com",
                    Organization = "Prophy",
                    Role = "Administrator"
                };

                var secretKey = "MySecretKeyForJWTTokenGeneration123456789";
                var token = tokenGenerator.GenerateToken(claims, secretKey);

                // Validate with strict options
                var strictOptions = new JwtValidationOptions
                {
                    ValidateIssuer = true,
                    ValidIssuer = "Prophy",
                    ValidateAudience = true,
                    ValidAudience = "Prophy",
                    RequiredClaims = new[] { "sub", "email", "organization", "role" },
                    RequiredOrganization = "Prophy",
                    RequiredClaimValues = new Dictionary<string, string>
                    {
                        { "role", "Administrator" }
                    }
                };

                var validationResult = validator.ValidateToken(token, secretKey, strictOptions);

                if (validationResult.IsValid)
                {
                    logger.LogInformation("✅ Strict JWT validation successful!");
                    
                    // Test claims helper methods
                    var hasRequiredClaims = validator.HasRequiredClaims(
                        validationResult.Principal!, 
                        new[] { "sub", "email", "organization" }
                    );
                    logger.LogInformation("Has required claims: {HasClaims}", hasRequiredClaims);

                    var hasAdminRole = validator.HasRole(validationResult.Principal!, "Administrator");
                    logger.LogInformation("Has Administrator role: {HasRole}", hasAdminRole);

                    var hasAnyRole = validator.HasAnyRole(
                        validationResult.Principal!, 
                        new[] { "User", "Administrator", "Manager" }
                    );
                    logger.LogInformation("Has any specified role: {HasAnyRole}", hasAnyRole);

                    var emailClaim = validator.GetClaimValue(validationResult.Principal!, "email");
                    logger.LogInformation("Email claim value: {Email}", emailClaim);
                }
                else
                {
                    logger.LogWarning("❌ Strict JWT validation failed: {Error}", validationResult.ErrorMessage);
                }

                // Test validation failure scenarios
                logger.LogInformation("\nTesting validation failure scenarios:");

                // Test with wrong organization
                var wrongOrgOptions = JwtValidationOptions.ForProphy("WrongOrg");
                var wrongOrgResult = validator.ValidateToken(token, secretKey, wrongOrgOptions);
                logger.LogInformation("Wrong organization validation: {IsValid} - {Error}", 
                    wrongOrgResult.IsValid, wrongOrgResult.ErrorMessage);

                // Test with missing required claims
                var missingClaimsOptions = new JwtValidationOptions
                {
                    RequiredClaims = new[] { "sub", "email", "missing_claim" }
                };
                var missingClaimsResult = validator.ValidateToken(token, secretKey, missingClaimsOptions);
                logger.LogInformation("Missing claims validation: {IsValid} - {Error}", 
                    missingClaimsResult.IsValid, missingClaimsResult.ErrorMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JWT claims validation demo failed");
            }

            await Task.CompletedTask;
        }
    }
} 