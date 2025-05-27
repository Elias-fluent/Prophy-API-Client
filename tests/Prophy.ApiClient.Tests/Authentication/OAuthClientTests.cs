using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Models.Responses;
using Xunit;

namespace Prophy.ApiClient.Tests.Authentication
{
    /// <summary>
    /// Unit tests for the OAuth client functionality.
    /// </summary>
    public class OAuthClientTests : IDisposable
    {
        private readonly Mock<ILogger<OAuthClient>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly OAuthClient _oauthClient;

        public OAuthClientTests()
        {
            _mockLogger = new Mock<ILogger<OAuthClient>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _oauthClient = new OAuthClient(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            var client = new OAuthClient(_httpClient, _mockLogger.Object);

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OAuthClient(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OAuthClient(_httpClient, null!));
        }

        [Fact]
        public async Task GetClientCredentialsTokenAsync_WithValidParameters_ShouldReturnToken()
        {
            // Arrange
            var tokenResponse = new OAuthTokenResponse
            {
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "api:read api:write"
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _oauthClient.GetClientCredentialsTokenAsync(
                "https://auth.example.com/token",
                "test_client_id",
                "test_client_secret",
                "api:read api:write");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_access_token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Equal(3600, result.ExpiresIn);
            Assert.Equal("api:read api:write", result.Scope);
        }

        [Fact]
        public async Task GetClientCredentialsTokenAsync_WithInvalidResponse_ShouldThrowException()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid_client\"}", Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                _oauthClient.GetClientCredentialsTokenAsync(
                    "https://auth.example.com/token",
                    "test_client_id",
                    "test_client_secret"));
        }

        [Theory]
        [InlineData(null, "client_id", "client_secret")]
        [InlineData("", "client_id", "client_secret")]
        [InlineData("https://auth.example.com/token", null, "client_secret")]
        [InlineData("https://auth.example.com/token", "", "client_secret")]
        [InlineData("https://auth.example.com/token", "client_id", null)]
        [InlineData("https://auth.example.com/token", "client_id", "")]
        public async Task GetClientCredentialsTokenAsync_WithInvalidParameters_ShouldThrowArgumentException(
            string tokenEndpoint, string clientId, string clientSecret)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _oauthClient.GetClientCredentialsTokenAsync(tokenEndpoint, clientId, clientSecret));
        }

        [Fact]
        public async Task GetAuthorizationCodeTokenAsync_WithValidParameters_ShouldReturnToken()
        {
            // Arrange
            var tokenResponse = new OAuthTokenResponse
            {
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = "test_refresh_token",
                Scope = "openid profile email"
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _oauthClient.GetAuthorizationCodeTokenAsync(
                "https://auth.example.com/token",
                "test_client_id",
                "test_auth_code",
                "https://app.example.com/callback",
                codeVerifier: "test_code_verifier");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_access_token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Equal(3600, result.ExpiresIn);
            Assert.Equal("test_refresh_token", result.RefreshToken);
            Assert.Equal("openid profile email", result.Scope);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidParameters_ShouldReturnNewToken()
        {
            // Arrange
            var tokenResponse = new OAuthTokenResponse
            {
                AccessToken = "new_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = "new_refresh_token"
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _oauthClient.RefreshTokenAsync(
                "https://auth.example.com/token",
                "test_client_id",
                "test_refresh_token");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new_access_token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Equal(3600, result.ExpiresIn);
            Assert.Equal("new_refresh_token", result.RefreshToken);
        }

        [Fact]
        public void BuildAuthorizationUrl_WithValidParameters_ShouldReturnCorrectUrl()
        {
            // Arrange
            var authorizationEndpoint = "https://auth.example.com/authorize";
            var clientId = "test_client_id";
            var redirectUri = "https://app.example.com/callback";
            var scope = "openid profile email";
            var state = "random_state_123";
            var codeChallenge = "test_code_challenge";
            var codeChallengeMethod = "S256";

            // Act
            var result = _oauthClient.BuildAuthorizationUrl(
                authorizationEndpoint,
                clientId,
                redirectUri,
                scope,
                state,
                codeChallenge,
                codeChallengeMethod);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("response_type=code", result);
            Assert.Contains($"client_id={clientId}", result);
            Assert.Contains($"redirect_uri={Uri.EscapeDataString(redirectUri)}", result);
            Assert.Contains($"scope={Uri.EscapeDataString(scope)}", result);
            Assert.Contains($"state={state}", result);
            Assert.Contains($"code_challenge={codeChallenge}", result);
            Assert.Contains($"code_challenge_method={codeChallengeMethod}", result);
        }

        [Theory]
        [InlineData(null, "client_id", "redirect_uri")]
        [InlineData("", "client_id", "redirect_uri")]
        [InlineData("https://auth.example.com/authorize", null, "redirect_uri")]
        [InlineData("https://auth.example.com/authorize", "", "redirect_uri")]
        [InlineData("https://auth.example.com/authorize", "client_id", null)]
        [InlineData("https://auth.example.com/authorize", "client_id", "")]
        public void BuildAuthorizationUrl_WithInvalidParameters_ShouldThrowArgumentException(
            string authorizationEndpoint, string clientId, string redirectUri)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _oauthClient.BuildAuthorizationUrl(authorizationEndpoint, clientId, redirectUri));
        }

        [Fact]
        public void BuildAuthorizationUrl_WithMinimalParameters_ShouldReturnValidUrl()
        {
            // Arrange
            var authorizationEndpoint = "https://auth.example.com/authorize";
            var clientId = "test_client_id";
            var redirectUri = "https://app.example.com/callback";

            // Act
            var result = _oauthClient.BuildAuthorizationUrl(authorizationEndpoint, clientId, redirectUri);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("response_type=code", result);
            Assert.Contains($"client_id={clientId}", result);
            Assert.Contains($"redirect_uri={Uri.EscapeDataString(redirectUri)}", result);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 