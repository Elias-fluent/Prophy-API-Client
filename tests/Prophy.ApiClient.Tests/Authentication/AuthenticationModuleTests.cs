using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Models.Requests;
using Xunit;

namespace Prophy.ApiClient.Tests.Authentication
{
    public class AuthenticationModuleTests
    {
        private readonly Mock<IApiKeyAuthenticator> _mockApiKeyAuthenticator;
        private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
        private readonly Mock<ILogger<AuthenticationModule>> _mockLogger;
        private readonly AuthenticationModule _authenticationModule;

        public AuthenticationModuleTests()
        {
            _mockApiKeyAuthenticator = new Mock<IApiKeyAuthenticator>();
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _mockLogger = new Mock<ILogger<AuthenticationModule>>();
            
            _authenticationModule = new AuthenticationModule(
                _mockApiKeyAuthenticator.Object,
                _mockJwtTokenGenerator.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Arrange & Act
            var module = new AuthenticationModule(
                _mockApiKeyAuthenticator.Object,
                _mockJwtTokenGenerator.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(module);
        }

        [Fact]
        public void Constructor_WithNullApiKeyAuthenticator_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthenticationModule(
                null!,
                _mockJwtTokenGenerator.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullJwtTokenGenerator_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthenticationModule(
                _mockApiKeyAuthenticator.Object,
                null!,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthenticationModule(
                _mockApiKeyAuthenticator.Object,
                _mockJwtTokenGenerator.Object,
                null!));
        }

        [Fact]
        public void ApiKey_ShouldReturnValueFromApiKeyAuthenticator()
        {
            // Arrange
            const string expectedApiKey = "test-api-key";
            _mockApiKeyAuthenticator.Setup(x => x.ApiKey).Returns(expectedApiKey);

            // Act
            var apiKey = _authenticationModule.ApiKey;

            // Assert
            Assert.Equal(expectedApiKey, apiKey);
        }

        [Fact]
        public void SetApiKey_WithValidApiKey_ShouldCallApiKeyAuthenticatorAndSetOrganizationCode()
        {
            // Arrange
            const string apiKey = "test-api-key";
            const string organizationCode = "test-org";

            // Act
            _authenticationModule.SetApiKey(apiKey, organizationCode);

            // Assert
            _mockApiKeyAuthenticator.Verify(x => x.SetApiKey(apiKey), Times.Once);
            Assert.Equal(organizationCode, _authenticationModule.OrganizationCode);
        }

        [Fact]
        public void SetApiKey_WithNullApiKey_ShouldThrowArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _authenticationModule.SetApiKey(null!));
        }

        [Fact]
        public void SetApiKey_WithEmptyApiKey_ShouldThrowArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _authenticationModule.SetApiKey(string.Empty));
        }

        [Fact]
        public void SetApiKey_WithoutOrganizationCode_ShouldSetOrganizationCodeToNull()
        {
            // Arrange
            const string apiKey = "test-api-key";

            // Act
            _authenticationModule.SetApiKey(apiKey);

            // Assert
            _mockApiKeyAuthenticator.Verify(x => x.SetApiKey(apiKey), Times.Once);
            Assert.Null(_authenticationModule.OrganizationCode);
        }

        [Fact]
        public void AuthenticateRequest_WithValidRequest_ShouldCallApiKeyAuthenticator()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            _mockApiKeyAuthenticator.Setup(x => x.ApiKey).Returns("test-api-key");

            // Act
            _authenticationModule.AuthenticateRequest(request);

            // Assert
            _mockApiKeyAuthenticator.Verify(x => x.AuthenticateRequest(request), Times.Once);
        }

        [Fact]
        public void AuthenticateRequest_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _authenticationModule.AuthenticateRequest(null!));
        }

        [Fact]
        public void AuthenticateRequest_WithNoApiKeyConfigured_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            _mockApiKeyAuthenticator.Setup(x => x.ApiKey).Returns((string?)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _authenticationModule.AuthenticateRequest(request));
        }

        [Fact]
        public void GenerateJwtToken_WithValidClaims_ShouldCallJwtTokenGenerator()
        {
            // Arrange
            var claims = CreateTestClaims();
            const string secretKey = "test-secret-key";
            const string expectedToken = "test-jwt-token";

            _mockJwtTokenGenerator.Setup(x => x.GenerateToken(claims, secretKey))
                .Returns(expectedToken);

            // Act
            var token = _authenticationModule.GenerateJwtToken(claims, secretKey);

            // Assert
            Assert.Equal(expectedToken, token);
            _mockJwtTokenGenerator.Verify(x => x.GenerateToken(claims, secretKey), Times.Once);
        }

        [Fact]
        public void GenerateJwtToken_WithNullClaims_ShouldThrowArgumentNullException()
        {
            // Arrange
            const string secretKey = "test-secret-key";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _authenticationModule.GenerateJwtToken(null!, secretKey));
        }

        [Fact]
        public void GenerateJwtToken_WithNullSecretKey_ShouldThrowArgumentException()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _authenticationModule.GenerateJwtToken(claims, null!));
        }

        [Fact]
        public void GenerateLoginUrl_WithValidClaims_ShouldCallJwtTokenGenerator()
        {
            // Arrange
            var claims = CreateTestClaims();
            const string secretKey = "test-secret-key";
            const string baseUrl = "https://custom.example.com/auth/";
            const string expectedUrl = "https://custom.example.com/auth/?token=test-token";

            _mockJwtTokenGenerator.Setup(x => x.GenerateLoginUrl(claims, secretKey, baseUrl))
                .Returns(expectedUrl);

            // Act
            var loginUrl = _authenticationModule.GenerateLoginUrl(claims, secretKey, baseUrl);

            // Assert
            Assert.Equal(expectedUrl, loginUrl);
            _mockJwtTokenGenerator.Verify(x => x.GenerateLoginUrl(claims, secretKey, baseUrl), Times.Once);
        }

        [Fact]
        public void GenerateLoginUrl_WithNullClaims_ShouldThrowArgumentNullException()
        {
            // Arrange
            const string secretKey = "test-secret-key";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _authenticationModule.GenerateLoginUrl(null!, secretKey));
        }

        [Fact]
        public void GenerateLoginUrl_WithNullSecretKey_ShouldThrowArgumentException()
        {
            // Arrange
            var claims = CreateTestClaims();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _authenticationModule.GenerateLoginUrl(claims, null!));
        }

        [Fact]
        public void IsValidJwtTokenFormat_WithValidToken_ShouldCallJwtTokenGenerator()
        {
            // Arrange
            const string token = "valid.jwt.token";
            _mockJwtTokenGenerator.Setup(x => x.IsValidTokenFormat(token)).Returns(true);

            // Act
            var isValid = _authenticationModule.IsValidJwtTokenFormat(token);

            // Assert
            Assert.True(isValid);
            _mockJwtTokenGenerator.Verify(x => x.IsValidTokenFormat(token), Times.Once);
        }

        [Fact]
        public void IsValidJwtTokenFormat_WithInvalidToken_ShouldReturnFalse()
        {
            // Arrange
            const string token = "invalid.token";
            _mockJwtTokenGenerator.Setup(x => x.IsValidTokenFormat(token)).Returns(false);

            // Act
            var isValid = _authenticationModule.IsValidJwtTokenFormat(token);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidJwtTokenFormat_WithNullToken_ShouldReturnFalse()
        {
            // Act
            var isValid = _authenticationModule.IsValidJwtTokenFormat(null!);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ClearAuthentication_ShouldCallApiKeyAuthenticatorAndClearOrganizationCode()
        {
            // Arrange
            _authenticationModule.SetApiKey("test-key", "test-org");

            // Act
            _authenticationModule.ClearAuthentication();

            // Assert
            _mockApiKeyAuthenticator.Verify(x => x.ClearApiKey(), Times.Once);
            Assert.Null(_authenticationModule.OrganizationCode);
        }

        [Fact]
        public void SetApiKey_WhenApiKeyAuthenticatorThrows_ShouldPropagateException()
        {
            // Arrange
            const string apiKey = "test-api-key";
            var expectedException = new InvalidOperationException("Test exception");
            _mockApiKeyAuthenticator.Setup(x => x.SetApiKey(apiKey)).Throws(expectedException);

            // Act & Assert
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                _authenticationModule.SetApiKey(apiKey));
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void AuthenticateRequest_WhenApiKeyAuthenticatorThrows_ShouldPropagateException()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            _mockApiKeyAuthenticator.Setup(x => x.ApiKey).Returns("test-api-key");
            var expectedException = new InvalidOperationException("Test exception");
            _mockApiKeyAuthenticator.Setup(x => x.AuthenticateRequest(request)).Throws(expectedException);

            // Act & Assert
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                _authenticationModule.AuthenticateRequest(request));
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void GenerateJwtToken_WhenJwtTokenGeneratorThrows_ShouldPropagateException()
        {
            // Arrange
            var claims = CreateTestClaims();
            const string secretKey = "test-secret-key";
            var expectedException = new InvalidOperationException("Test exception");
            _mockJwtTokenGenerator.Setup(x => x.GenerateToken(claims, secretKey)).Throws(expectedException);

            // Act & Assert
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                _authenticationModule.GenerateJwtToken(claims, secretKey));
            Assert.Same(expectedException, actualException);
        }

        private static JwtLoginClaims CreateTestClaims()
        {
            return new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com",
                ExpirationSeconds = 3600,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };
        }
    }
} 