using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Models.Requests;
using Xunit;

namespace Prophy.ApiClient.Tests.Authentication
{
    /// <summary>
    /// Unit tests for the JWT validator functionality.
    /// </summary>
    public class JwtValidatorTests
    {
        private readonly Mock<ILogger<JwtValidator>> _mockLogger;
        private readonly JwtValidator _jwtValidator;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly string _secretKey = "MyTestSecretKeyForJWTValidation123456789";

        public JwtValidatorTests()
        {
            _mockLogger = new Mock<ILogger<JwtValidator>>();
            _jwtValidator = new JwtValidator(_mockLogger.Object);
            
            var tokenLogger = new Mock<ILogger<JwtTokenGenerator>>();
            _tokenGenerator = new JwtTokenGenerator(tokenLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidLogger_ShouldInitialize()
        {
            // Arrange & Act
            var validator = new JwtValidator(_mockLogger.Object);

            // Assert
            Assert.NotNull(validator);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JwtValidator(null!));
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnSuccess()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg",
                Role = "User"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Principal);
            Assert.NotNull(result.Token);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ShouldReturnFailure()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var result = _jwtValidator.ValidateToken(invalidToken, _secretKey);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
            Assert.Null(result.Principal);
            Assert.Null(result.Token);
        }

        [Fact]
        public void ValidateToken_WithWrongSecretKey_ShouldReturnFailure()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var wrongSecretKey = "WrongSecretKey123456789";

            // Act
            var result = _jwtValidator.ValidateToken(token, wrongSecretKey);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(null, "secret")]
        [InlineData("", "secret")]
        [InlineData("token", null)]
        [InlineData("token", "")]
        public void ValidateToken_WithInvalidParameters_ShouldThrowArgumentException(string token, string secretKey)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _jwtValidator.ValidateToken(token, secretKey));
        }

        [Fact]
        public void ValidateToken_WithValidationOptions_ShouldValidateCorrectly()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg",
                Role = "Admin"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var options = new JwtValidationOptions
            {
                ValidateIssuer = true,
                ValidIssuer = "Prophy",
                ValidateAudience = true,
                ValidAudience = "Prophy",
                RequiredClaims = new[] { "sub", "email", "organization" }
            };

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey, options);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("test_user", result.GetSubject());
            Assert.Equal("test@example.com", result.GetEmail());
            Assert.Equal("TestOrg", result.GetOrganization());
            Assert.Equal("Admin", result.GetRole());
        }

        [Fact]
        public void ValidateToken_WithRequiredOrganization_ShouldValidateCorrectly()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var options = new JwtValidationOptions
            {
                RequiredOrganization = "TestOrg"
            };

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey, options);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateToken_WithWrongRequiredOrganization_ShouldReturnFailure()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var options = new JwtValidationOptions
            {
                RequiredOrganization = "WrongOrg"
            };

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("organization", result.ErrorMessage!.ToLower());
        }

        [Fact]
        public void ValidateToken_WithMissingRequiredClaims_ShouldReturnFailure()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg"
                // Role is not set, so it won't be added to the token
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var options = new JwtValidationOptions
            {
                RequiredClaims = new[] { "sub", "email", "organization", "role" }
            };

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("missing", result.ErrorMessage!.ToLower());
        }

        [Fact]
        public void ValidateToken_WithRequiredClaimValues_ShouldValidateCorrectly()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg",
                Role = "Admin"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var options = new JwtValidationOptions
            {
                RequiredClaimValues = new Dictionary<string, string>
                {
                    { ClaimTypes.Role, "Admin" },
                    { "organization", "TestOrg" }
                }
            };

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey, options);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateToken_WithWrongRequiredClaimValues_ShouldReturnFailure()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "test_user",
                Email = "test@example.com",
                Organization = "TestOrg",
                Role = "User"
            };

            var token = _tokenGenerator.GenerateToken(claims, _secretKey);
            var options = new JwtValidationOptions
            {
                RequiredClaimValues = new Dictionary<string, string>
                {
                    { ClaimTypes.Role, "Admin" }
                }
            };

            // Act
            var result = _jwtValidator.ValidateToken(token, _secretKey, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("does not match required value", result.ErrorMessage!.ToLower());
        }

        [Fact]
        public void HasRequiredClaims_WithValidClaims_ShouldReturnTrue()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", "test_user"),
                new Claim("email", "test@example.com"),
                new Claim("organization", "TestOrg")
            }));

            var requiredClaims = new[] { "sub", "email" };

            // Act
            var result = _jwtValidator.HasRequiredClaims(principal, requiredClaims);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRequiredClaims_WithMissingClaims_ShouldReturnFalse()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", "test_user"),
                new Claim("email", "test@example.com")
            }));

            var requiredClaims = new[] { "sub", "email", "organization" };

            // Act
            var result = _jwtValidator.HasRequiredClaims(principal, requiredClaims);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasRole_WithCorrectRole_ShouldReturnTrue()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            }));

            // Act
            var result = _jwtValidator.HasRole(principal, "Admin");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRole_WithWrongRole_ShouldReturnFalse()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "User")
            }));

            // Act
            var result = _jwtValidator.HasRole(principal, "Admin");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasAnyRole_WithMatchingRole_ShouldReturnTrue()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            }));

            var roles = new[] { "User", "Admin", "Manager" };

            // Act
            var result = _jwtValidator.HasAnyRole(principal, roles);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasAnyRole_WithNoMatchingRole_ShouldReturnFalse()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Guest")
            }));

            var roles = new[] { "User", "Admin", "Manager" };

            // Act
            var result = _jwtValidator.HasAnyRole(principal, roles);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetClaimValue_WithExistingClaim_ShouldReturnValue()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("email", "test@example.com")
            }));

            // Act
            var result = _jwtValidator.GetClaimValue(principal, "email");

            // Assert
            Assert.Equal("test@example.com", result);
        }

        [Fact]
        public void GetClaimValue_WithNonExistingClaim_ShouldReturnNull()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("email", "test@example.com")
            }));

            // Act
            var result = _jwtValidator.GetClaimValue(principal, "phone");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void JwtValidationOptions_ForProphy_ShouldCreateCorrectOptions()
        {
            // Arrange
            var organization = "TestOrg";

            // Act
            var options = JwtValidationOptions.ForProphy(organization);

            // Assert
            Assert.True(options.ValidateIssuer);
            Assert.Equal("Prophy", options.ValidIssuer);
            Assert.True(options.ValidateAudience);
            Assert.Equal("Prophy", options.ValidAudience);
            Assert.Equal(organization, options.RequiredOrganization);
            Assert.Contains("sub", options.RequiredClaims!);
            Assert.Contains("email", options.RequiredClaims!);
            Assert.Contains("organization", options.RequiredClaims!);
        }
    }
} 