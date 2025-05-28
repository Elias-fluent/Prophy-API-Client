using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Security;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.Security.Enhanced
{
    /// <summary>
    /// Enhanced tests for authentication and security components using property-based testing.
    /// </summary>
    public class SecurityEnhancedTests
    {
        private readonly Mock<ILogger<ApiKeyAuthenticator>> _mockAuthLogger;
        private readonly Mock<ILogger<JwtTokenGenerator>> _mockJwtLogger;

        public SecurityEnhancedTests()
        {
            _mockAuthLogger = TestHelpers.CreateMockLogger<ApiKeyAuthenticator>();
            _mockJwtLogger = TestHelpers.CreateMockLogger<JwtTokenGenerator>();
        }

        #region Property-Based Tests for API Key Authentication

        [Property]
        public Property ApiKeyAuthenticator_WithValidApiKeys_ShouldAlwaysAddHeader()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                apiKey =>
                {
                    try
                    {
                        // Arrange
                        var authenticator = new ApiKeyAuthenticator(apiKey.Get, _mockAuthLogger.Object);
                        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

                        // Act
                        authenticator.AuthenticateRequest(request);

                        // Assert
                        return request.Headers.Contains("X-ApiKey") && 
                               request.Headers.GetValues("X-ApiKey").First() == apiKey.Get;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property ApiKeyAuthenticator_WithDifferentApiKeys_ShouldProduceDifferentHeaders()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>().Filter(s => s.Get != ""),
                (apiKey1, apiKey2) =>
                {
                    if (apiKey1.Get == apiKey2.Get) return true; // Skip identical keys

                    try
                    {
                        // Arrange
                        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
                        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

                        var authenticator1 = new ApiKeyAuthenticator(apiKey1.Get, _mockAuthLogger.Object);
                        var authenticator2 = new ApiKeyAuthenticator(apiKey2.Get, _mockAuthLogger.Object);

                        // Act
                        authenticator1.AuthenticateRequest(request1);
                        authenticator2.AuthenticateRequest(request2);

                        // Assert
                        var header1 = request1.Headers.GetValues("X-ApiKey").First();
                        var header2 = request2.Headers.GetValues("X-ApiKey").First();
                        return header1 != header2;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property JwtTokenGenerator_WithValidClaims_ShouldGenerateValidTokens()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (subject, organization, email) =>
                {
                    try
                    {
                        // Arrange
                        var claims = new JwtLoginClaims
                        {
                            Subject = subject.Get,
                            Organization = organization.Get,
                            Email = email.Get,
                            Folder = "test-folder",
                            OriginId = "test-origin"
                        };

                        var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
                        var secret = "test-secret-key-for-property-testing-with-sufficient-length";

                        // Act
                        var token = generator.GenerateToken(claims, secret);

                        // Assert - Token should not be null or empty
                        return !string.IsNullOrEmpty(token) && token.Split('.').Length == 3; // JWT has 3 parts
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Security Validation Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApiKeyAuthenticator_WithInvalidApiKey_ShouldThrowArgumentException(string invalidApiKey)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ApiKeyAuthenticator(invalidApiKey, _mockAuthLogger.Object));
        }

        [Fact]
        public void ApiKeyAuthenticator_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator("valid-api-key", _mockAuthLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                authenticator.AuthenticateRequest(null!));
        }

        [Fact]
        public void ApiKeyAuthenticator_WithoutApiKey_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator(_mockAuthLogger.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                authenticator.AuthenticateRequest(request));
        }

        #endregion

        #region JWT Token Security Tests

        [Fact]
        public void JwtTokenGenerator_WithNullClaims_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                generator.GenerateToken(null!, "secret"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void JwtTokenGenerator_WithInvalidSecret_ShouldThrowArgumentException(string invalidSecret)
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var claims = new JwtLoginClaims
            {
                Subject = "test",
                Organization = "test-org",
                Email = "test@example.com"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                generator.GenerateToken(claims, invalidSecret));
        }

        [Fact]
        public void JwtTokenGenerator_GeneratedToken_ShouldHaveCorrectStructure()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var claims = new JwtLoginClaims
            {
                Subject = "test-subject",
                Organization = "test-org",
                Email = "test@example.com",
                Folder = "test-folder",
                OriginId = "test-origin"
            };
            var secret = "strong-secret-key-for-testing-purposes-with-sufficient-length";

            // Act
            var token = generator.GenerateToken(claims, secret);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length); // Header.Payload.Signature
            
            // Each part should be base64 encoded (no spaces, proper length)
            Assert.All(parts, part => 
            {
                Assert.NotEmpty(part);
                Assert.DoesNotContain(" ", part);
            });
        }

        [Fact]
        public void JwtTokenGenerator_IsValidTokenFormat_WithValidToken_ShouldReturnTrue()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var claims = new JwtLoginClaims
            {
                Subject = "test-subject",
                Organization = "test-org",
                Email = "test@example.com"
            };
            var secret = "strong-secret-key-for-testing-purposes-with-sufficient-length";
            var token = generator.GenerateToken(claims, secret);

            // Act
            var isValid = generator.IsValidTokenFormat(token);

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid-token")]
        [InlineData("not.a.jwt")]
        [InlineData("too.many.parts.here.invalid")]
        public void JwtTokenGenerator_IsValidTokenFormat_WithInvalidToken_ShouldReturnFalse(string invalidToken)
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);

            // Act
            var isValid = generator.IsValidTokenFormat(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        #endregion

        #region Authentication Header Security Tests

        [Fact]
        public void ApiKeyAuthenticator_WithMultipleRequests_ShouldApplyHeaderConsistently()
        {
            // Arrange
            var apiKey = "consistent-api-key";
            var authenticator = new ApiKeyAuthenticator(apiKey, _mockAuthLogger.Object);
            
            var requests = Enumerable.Range(1, 5)
                .Select(i => new HttpRequestMessage(HttpMethod.Get, $"https://api.prophy.ai/test{i}"))
                .ToList();

            // Act
            foreach (var request in requests)
            {
                authenticator.AuthenticateRequest(request);
            }

            // Assert
            Assert.All(requests, request =>
            {
                Assert.True(request.Headers.Contains("X-ApiKey"));
                Assert.Equal(apiKey, request.Headers.GetValues("X-ApiKey").First());
            });
        }

        [Fact]
        public void ApiKeyAuthenticator_SetApiKey_ShouldUpdateApiKey()
        {
            // Arrange
            var initialApiKey = "initial-key";
            var newApiKey = "new-key";
            var authenticator = new ApiKeyAuthenticator(initialApiKey, _mockAuthLogger.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");

            // Act
            authenticator.SetApiKey(newApiKey);
            authenticator.AuthenticateRequest(request);

            // Assert
            Assert.Equal(newApiKey, authenticator.ApiKey);
            Assert.True(request.Headers.Contains("X-ApiKey"));
            Assert.Equal(newApiKey, request.Headers.GetValues("X-ApiKey").First());
        }

        [Fact]
        public void ApiKeyAuthenticator_ClearApiKey_ShouldRemoveApiKey()
        {
            // Arrange
            var apiKey = "test-key";
            var authenticator = new ApiKeyAuthenticator(apiKey, _mockAuthLogger.Object);

            // Act
            authenticator.ClearApiKey();

            // Assert
            Assert.Null(authenticator.ApiKey);
        }

        [Fact]
        public void ApiKeyAuthenticator_SetOrganizationCode_ShouldUpdateOrganizationCode()
        {
            // Arrange
            var apiKey = "test-key";
            var organizationCode = "test-org";
            var authenticator = new ApiKeyAuthenticator(apiKey, _mockAuthLogger.Object);

            // Act
            authenticator.SetOrganizationCode(organizationCode);

            // Assert
            Assert.Equal(organizationCode, authenticator.OrganizationCode);
        }

        #endregion

        #region Concurrent Authentication Tests

        [Fact]
        public async Task ApiKeyAuthenticator_WithConcurrentRequests_ShouldBeThreadSafe()
        {
            // Arrange
            var apiKey = "thread-safe-api-key";
            var authenticator = new ApiKeyAuthenticator(apiKey, _mockAuthLogger.Object);

            var tasks = Enumerable.Range(1, 10)
                .Select(i => Task.Run(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.prophy.ai/test{i}");
                    authenticator.AuthenticateRequest(request);
                    return request;
                }))
                .ToList();

            // Act
            var requests = await Task.WhenAll(tasks);

            // Assert
            Assert.All(requests, request =>
            {
                Assert.True(request.Headers.Contains("X-ApiKey"));
                Assert.Equal(apiKey, request.Headers.GetValues("X-ApiKey").First());
            });
        }

        [Fact]
        public async Task JwtTokenGenerator_WithConcurrentGeneration_ShouldBeThreadSafe()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var secret = "thread-safe-secret-key-for-testing-with-sufficient-length";

            var tasks = Enumerable.Range(1, 10)
                .Select(i => Task.Run(() =>
                {
                    var claims = new JwtLoginClaims
                    {
                        Subject = $"subject-{i}",
                        Organization = $"org-{i}",
                        Email = $"user{i}@example.com"
                    };
                    return generator.GenerateToken(claims, secret);
                }))
                .ToList();

            // Act
            var tokens = await Task.WhenAll(tasks);

            // Assert
            Assert.All(tokens, token =>
            {
                Assert.NotNull(token);
                Assert.NotEmpty(token);
                Assert.Equal(3, token.Split('.').Length);
            });

            // All tokens should be different (due to different claims)
            var uniqueTokens = tokens.Distinct().ToList();
            Assert.Equal(tokens.Length, uniqueTokens.Count);
        }

        #endregion

        #region JWT Login URL Generation Tests

        [Fact]
        public void JwtTokenGenerator_GenerateLoginUrl_ShouldCreateValidUrl()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var claims = new JwtLoginClaims
            {
                Subject = "test-subject",
                Organization = "test-org",
                Email = "test@example.com"
            };
            var secret = "strong-secret-key-for-testing-purposes-with-sufficient-length";

            // Act
            var loginUrl = generator.GenerateLoginUrl(claims, secret);

            // Assert
            Assert.NotNull(loginUrl);
            Assert.NotEmpty(loginUrl);
            Assert.StartsWith("https://www.prophy.ai/api/auth/api-jwt-login/", loginUrl);
            Assert.Contains("?token=", loginUrl);
        }

        [Fact]
        public void JwtTokenGenerator_GenerateLoginUrl_WithCustomBaseUrl_ShouldUseCustomUrl()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var claims = new JwtLoginClaims
            {
                Subject = "test-subject",
                Organization = "test-org",
                Email = "test@example.com"
            };
            var secret = "strong-secret-key-for-testing-purposes-with-sufficient-length";
            var customBaseUrl = "https://custom.prophy.ai/login/";

            // Act
            var loginUrl = generator.GenerateLoginUrl(claims, secret, customBaseUrl);

            // Assert
            Assert.NotNull(loginUrl);
            Assert.NotEmpty(loginUrl);
            Assert.StartsWith(customBaseUrl, loginUrl);
            Assert.Contains("?token=", loginUrl);
        }

        #endregion

        #region Security Input Validation Tests

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ApiKeyAuthenticator_SetApiKey_WithInvalidKey_ShouldThrowArgumentException(string invalidKey)
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator("initial-key", _mockAuthLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => authenticator.SetApiKey(invalidKey));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ApiKeyAuthenticator_SetOrganizationCode_WithInvalidCode_ShouldThrowArgumentException(string invalidCode)
        {
            // Arrange
            var authenticator = new ApiKeyAuthenticator("test-key", _mockAuthLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => authenticator.SetOrganizationCode(invalidCode));
        }

        [Fact]
        public void JwtTokenGenerator_GenerateLoginUrl_WithNullClaims_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                generator.GenerateLoginUrl(null!, "secret"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void JwtTokenGenerator_GenerateLoginUrl_WithInvalidSecret_ShouldThrowArgumentException(string invalidSecret)
        {
            // Arrange
            var generator = new JwtTokenGenerator(_mockJwtLogger.Object);
            var claims = new JwtLoginClaims
            {
                Subject = "test",
                Organization = "test-org",
                Email = "test@example.com"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                generator.GenerateLoginUrl(claims, invalidSecret));
        }

        #endregion
    }
} 