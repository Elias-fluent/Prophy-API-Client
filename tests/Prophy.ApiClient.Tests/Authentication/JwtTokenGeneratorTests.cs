using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Models.Requests;
using Xunit;

namespace Prophy.ApiClient.Tests.Authentication
{
    public class JwtTokenGeneratorTests
    {
        private readonly Mock<ILogger<JwtTokenGenerator>> _mockLogger;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly string _testSecretKey = "test-secret-key-that-is-long-enough-for-hmac-sha256-algorithm";

        public JwtTokenGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<JwtTokenGenerator>>();
            _jwtTokenGenerator = new JwtTokenGenerator(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidLogger_ShouldInitialize()
        {
            // Arrange & Act
            var generator = new JwtTokenGenerator(_mockLogger.Object);

            // Assert
            Assert.NotNull(generator);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JwtTokenGenerator(null!));
        }

        [Fact]
        public void GenerateToken_WithValidClaims_ShouldReturnValidJwtToken()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act
            var token = _jwtTokenGenerator.GenerateToken(claims, _testSecretKey);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Verify it's a valid JWT format (3 parts separated by dots)
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);

            // Verify token can be parsed
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            Assert.NotNull(jsonToken);
        }

        [Fact]
        public void GenerateToken_WithValidClaims_ShouldContainExpectedClaims()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act
            var token = _jwtTokenGenerator.GenerateToken(claims, _testSecretKey);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal(claims.Subject, jsonToken.Claims.First(c => c.Type == "sub").Value);
            Assert.Equal(claims.Organization, jsonToken.Claims.First(c => c.Type == "organization").Value);
            Assert.Equal(claims.Email, jsonToken.Claims.First(c => c.Type == "email").Value);
            Assert.Equal(claims.FirstName, jsonToken.Claims.First(c => c.Type == "first_name").Value);
            Assert.Equal(claims.LastName, jsonToken.Claims.First(c => c.Type == "last_name").Value);
        }

        [Fact]
        public void GenerateToken_WithNullClaims_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _jwtTokenGenerator.GenerateToken(null!, _testSecretKey));
        }

        [Fact]
        public void GenerateToken_WithNullSecretKey_ShouldThrowArgumentException()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _jwtTokenGenerator.GenerateToken(claims, null!));
        }

        [Fact]
        public void GenerateToken_WithEmptySecretKey_ShouldThrowArgumentException()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _jwtTokenGenerator.GenerateToken(claims, string.Empty));
        }

        [Fact]
        public void GenerateLoginUrl_WithValidClaims_ShouldReturnValidUrl()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act
            var loginUrl = _jwtTokenGenerator.GenerateLoginUrl(claims, _testSecretKey);

            // Assert
            Assert.NotNull(loginUrl);
            Assert.StartsWith("https://www.prophy.ai/api/auth/api-jwt-login/?token=", loginUrl);
            
            // Extract and verify the token
            var tokenParam = loginUrl.Split("?token=")[1];
            var decodedToken = Uri.UnescapeDataString(tokenParam);
            Assert.True(_jwtTokenGenerator.IsValidTokenFormat(decodedToken));
        }

        [Fact]
        public void GenerateLoginUrl_WithCustomBaseUrl_ShouldUseCustomUrl()
        {
            // Arrange
            var claims = CreateTestClaims();
            var customBaseUrl = "https://custom.example.com/auth/";

            // Act
            var loginUrl = _jwtTokenGenerator.GenerateLoginUrl(claims, _testSecretKey, customBaseUrl);

            // Assert
            Assert.NotNull(loginUrl);
            Assert.StartsWith("https://custom.example.com/auth/?token=", loginUrl);
        }

        [Fact]
        public void GenerateLoginUrl_WithBaseUrlWithoutTrailingSlash_ShouldAddSlash()
        {
            // Arrange
            var claims = CreateTestClaims();
            var customBaseUrl = "https://custom.example.com/auth";

            // Act
            var loginUrl = _jwtTokenGenerator.GenerateLoginUrl(claims, _testSecretKey, customBaseUrl);

            // Assert
            Assert.NotNull(loginUrl);
            Assert.StartsWith("https://custom.example.com/auth/?token=", loginUrl);
        }

        [Fact]
        public void GenerateLoginUrl_WithNullClaims_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _jwtTokenGenerator.GenerateLoginUrl(null!, _testSecretKey));
        }

        [Fact]
        public void GenerateLoginUrl_WithNullSecretKey_ShouldThrowArgumentException()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _jwtTokenGenerator.GenerateLoginUrl(claims, null!));
        }

        [Fact]
        public void IsValidTokenFormat_WithValidToken_ShouldReturnTrue()
        {
            // Arrange
            var claims = CreateTestClaims();
            var token = _jwtTokenGenerator.GenerateToken(claims, _testSecretKey);

            // Act
            var isValid = _jwtTokenGenerator.IsValidTokenFormat(token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidTokenFormat_WithInvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.format";

            // Act
            var isValid = _jwtTokenGenerator.IsValidTokenFormat(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidTokenFormat_WithNullToken_ShouldReturnFalse()
        {
            // Act
            var isValid = _jwtTokenGenerator.IsValidTokenFormat(null!);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidTokenFormat_WithEmptyToken_ShouldReturnFalse()
        {
            // Act
            var isValid = _jwtTokenGenerator.IsValidTokenFormat(string.Empty);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GenerateToken_WithOptionalClaims_ShouldIncludeOnlyProvidedClaims()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrg",
                Email = "test@example.com",
                // Only some optional fields provided
                FirstName = "John",
                // LastName, Folder, OriginId, Name, Role not provided
                ExpirationSeconds = 3600,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };

            // Act
            var token = _jwtTokenGenerator.GenerateToken(claims, _testSecretKey);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            Assert.Contains(jsonToken.Claims, c => c.Type == "first_name" && c.Value == "John");
            Assert.DoesNotContain(jsonToken.Claims, c => c.Type == "last_name");
            Assert.DoesNotContain(jsonToken.Claims, c => c.Type == "folder");
            Assert.DoesNotContain(jsonToken.Claims, c => c.Type == "origin_id");
        }

        private static JwtLoginClaims CreateTestClaims()
        {
            return new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                Role = "User",
                Folder = "TestFolder",
                OriginId = "TestOriginId",
                ExpirationSeconds = 3600,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };
        }
    }
} 