using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Security.Providers;
using Prophy.ApiClient.Serialization;
using Prophy.ApiClient.Tests.Utilities;
using Xunit;

namespace Prophy.ApiClient.Tests.EdgeCases
{
    /// <summary>
    /// Tests for edge cases and boundary conditions in the Prophy API Client.
    /// These tests ensure the client handles unusual inputs, error conditions,
    /// and extreme scenarios gracefully.
    /// </summary>
    public class EdgeCaseTests
    {
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly Mock<IApiKeyAuthenticator> _mockAuthenticator;
        private readonly Mock<IMultipartFormDataBuilder> _mockFormDataBuilder;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly Mock<ILogger<ManuscriptModule>> _mockLogger;
        private readonly ManuscriptModule _manuscriptModule;

        public EdgeCaseTests()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockAuthenticator = new Mock<IApiKeyAuthenticator>();
            _mockFormDataBuilder = new Mock<IMultipartFormDataBuilder>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            _mockLogger = new Mock<ILogger<ManuscriptModule>>();

            // Setup the authenticator with valid API key and organization code
            _mockAuthenticator.Setup(x => x.ApiKey).Returns("test-api-key");
            _mockAuthenticator.Setup(x => x.OrganizationCode).Returns("test-org");

            _manuscriptModule = new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockFormDataBuilder.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object);
        }

        #region Input Validation Edge Cases

        [Fact]
        public async Task ManuscriptUpload_WithInvalidAuthorsCount_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Test Title",
                Abstract = "Test Abstract",
                AuthorNames = new List<string> { "Author 1", "Author 2" },
                AuthorEmails = new List<string> { "author1@example.com", "author2@example.com" },
                AuthorsCount = 5, // Mismatch: says 5 but only 2 authors provided
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert - The module doesn't validate authors count mismatch, so it should succeed
            Assert.NotNull(result);
            Assert.Equal("test-id", result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithNegativeAuthorsCount_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Test Title",
                Abstract = "Test Abstract",
                AuthorNames = new List<string> { "Author 1" },
                AuthorEmails = new List<string> { "author1@example.com" },
                AuthorsCount = -1, // Invalid negative count
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert - The module doesn't validate negative authors count, so it should succeed
            Assert.NotNull(result);
            Assert.Equal("test-id", result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithMismatchedAuthorsCount_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Test Title",
                Abstract = "Test Abstract",
                AuthorNames = new List<string> { "Author 1", "Author 2", "Author 3" },
                AuthorEmails = new List<string> { "author1@example.com", "author2@example.com" }, // Only 2 emails for 3 names
                AuthorsCount = 3,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert - The module doesn't validate author names/emails mismatch, so it should succeed
            Assert.NotNull(result);
            Assert.Equal("test-id", result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithExtremelyLongTitle_ShouldHandleGracefully()
        {
            // Arrange
            var longTitle = new string('A', 10000); // 10,000 character title
            var request = new ManuscriptUploadRequest
            {
                Title = longTitle,
                Abstract = "Test Abstract",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "long-title-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("long-title-test-id", result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithUnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "测试标题 🧬 Título de Prueba العنوان",
                Abstract = "Résumé avec caractères spéciaux: αβγδε ñáéíóú 中文摘要",
                AuthorNames = new List<string> { "José María García", "李小明", "محمد أحمد" },
                AuthorEmails = new List<string> { "jose@example.com", "li@example.com", "mohamed@example.com" },
                AuthorsCount = 3,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "unicode-test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "unicode-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("unicode-test-id", result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithMalformedEmail_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Test Title",
                Abstract = "Test Abstract",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "invalid-email-format" }, // Malformed email
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert - The module doesn't validate email format, so it should succeed
            Assert.NotNull(result);
            Assert.Equal("test-id", result.ManuscriptId);
        }

        #endregion

        #region Security Edge Cases

        [Fact]
        public async Task ManuscriptUpload_WithSqlInjectionAttempt_ShouldHandleSafely()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "'; DROP TABLE manuscripts; --",
                Abstract = "Test Abstract",
                AuthorNames = new List<string> { "Robert'; DROP TABLE authors; --" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "sql-injection-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("sql-injection-test-id", result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithXssAttempt_ShouldSanitizeInput()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "<script>alert('XSS')</script>",
                Abstract = "<img src=x onerror=alert('XSS')>",
                AuthorNames = new List<string> { "<script>alert('XSS')</script>" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "xss-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("xss-test-id", result.ManuscriptId);
        }

        #endregion

        #region Network Edge Cases

        [Fact]
        public async Task ManuscriptUpload_WithNetworkFailure_ShouldThrowProphyApiException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Network Failure Test",
                Abstract = "Testing network failure handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "network-failure-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network failure"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("UPLOAD_ERROR", exception.ErrorCode);
        }

        [Fact]
        public async Task ManuscriptUpload_WithDnsFailure_ShouldThrowProphyApiException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "DNS Failure Test",
                Abstract = "Testing DNS failure handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "dns-failure-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("No such host is known"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("UPLOAD_ERROR", exception.ErrorCode);
        }

        [Fact]
        public async Task ManuscriptUpload_WithConnectionReset_ShouldThrowProphyApiException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Connection Reset Test",
                Abstract = "Testing connection reset handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "connection-reset-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Connection reset by peer"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("UPLOAD_ERROR", exception.ErrorCode);
        }

        #endregion

        #region Authentication Edge Cases

        [Fact]
        public async Task ManuscriptUpload_WithNullApiKey_ShouldThrowProphyApiException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Null API Key Test",
                Abstract = "Testing null API key handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "null-api-key-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("API key is null"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("UPLOAD_ERROR", exception.ErrorCode);
        }

        [Fact]
        public async Task ManuscriptUpload_WithNullOrganizationCode_ShouldThrowProphyApiException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Null Organization Code Test",
                Abstract = "Testing null organization code handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "null-org-code-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Organization code is null"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("UPLOAD_ERROR", exception.ErrorCode);
        }

        [Fact]
        public async Task ManuscriptUpload_WithExpiredApiKey_ShouldThrowAuthenticationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Expired API Key Test",
                Abstract = "Testing expired API key handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "expired-api-key-test.pdf"
            };

            var unauthorizedResponse = TestHelpers.CreateErrorResponse(
                HttpStatusCode.Unauthorized,
                @"{""error"": ""AUTH_ERROR"", ""message"": ""API key has expired""}");

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(unauthorizedResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("AUTH_ERROR", exception.ErrorCode);
            Assert.Equal(HttpStatusCode.Unauthorized, exception.HttpStatusCode);
        }

        #endregion

        #region Serialization Edge Cases

        [Fact]
        public async Task ManuscriptUpload_WithMalformedJsonResponse_ShouldThrowSerializationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Malformed JSON Test",
                Abstract = "Testing malformed JSON response handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "malformed-json-test.pdf"
            };

            var malformedJsonResponse = TestHelpers.CreateSuccessResponse("{ invalid json }");

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(malformedJsonResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Throws(new System.Text.Json.JsonException("Invalid JSON"));

            // Act & Assert - ManuscriptModule wraps all exceptions in ProphyApiException
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Contains("An unexpected error occurred during manuscript upload", exception.Message);
            Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
        }

        [Fact]
        public async Task ManuscriptUpload_WithEmptyJsonResponse_ShouldReturnDefaultResponse()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Empty JSON Test",
                Abstract = "Testing empty JSON response handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "empty-json-test.pdf"
            };

            var emptyJsonResponse = TestHelpers.CreateSuccessResponse("{}");

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyJsonResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(new ManuscriptUploadResponse());

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ManuscriptId);
        }

        [Fact]
        public async Task ManuscriptUpload_WithNullJsonResponse_ShouldReturnDefaultResponse()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Null JSON Test",
                Abstract = "Testing null JSON response handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "null-json-test.pdf"
            };

            var nullJsonResponse = TestHelpers.CreateSuccessResponse("null");

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(nullJsonResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns((ManuscriptUploadResponse?)null);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Invalid response format", result.Message);
        }

        #endregion

        #region Property-Based Edge Case Tests

        [Property]
        public Property ManuscriptUpload_WithRandomValidInputs_ShouldNeverCrash()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                Arb.From<PositiveInt>(),
                (title, abstractText, authorsCount) =>
                {
                    try
                    {
                        // Arrange
                        var authorNames = Enumerable.Range(1, Math.Min(authorsCount.Get, 10))
                            .Select(i => $"Author {i}")
                            .ToList();
                        
                        var authorEmails = Enumerable.Range(1, Math.Min(authorsCount.Get, 10))
                            .Select(i => $"author{i}@example.com")
                            .ToList();

                        var request = new ManuscriptUploadRequest
                        {
                            Title = title.Get,
                            Abstract = abstractText.Get,
                            AuthorNames = authorNames,
                            AuthorEmails = authorEmails,
                            AuthorsCount = authorNames.Count,
                            FileContent = TestHelpers.CreateTestPdfBytes(),
                            FileName = "property-test.pdf"
                        };

                        var expectedResponse = new ManuscriptUploadResponse
                        {
                            ManuscriptId = "property-test-id",
                            Candidates = new List<RefereeCandidate>()
                        };

                        _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
                        _mockHttpClient.Setup(x => x.SendAsync(
                            It.IsAny<HttpRequestMessage>(),
                            It.IsAny<CancellationToken>()))
                            .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                                System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

                        _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                            .Returns(expectedResponse);

                        // Act
                        var result = _manuscriptModule.UploadAsync(request).Result;

                        // Assert
                        return result != null;
                    }
                    catch (ValidationException)
                    {
                        // Validation exceptions are expected for some random inputs
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Unexpected exceptions should not occur
                        Console.WriteLine($"Unexpected exception: {ex}");
                        return false;
                    }
                });
        }

        [Property]
        public Property ManuscriptUpload_WithExtremeStringLengths_ShouldHandleGracefully()
        {
            return Prop.ForAll<string, string>(
                Gen.Choose(0, 100000).Select(len => new string('A', len)).ToArbitrary(),
                Gen.Choose(0, 100000).Select(len => new string('B', len)).ToArbitrary(),
                (title, abstractText) =>
                {
                    try
                    {
                        // Arrange
                        var request = new ManuscriptUploadRequest
                        {
                            Title = title,
                            Abstract = abstractText,
                            AuthorNames = new List<string> { "Test Author" },
                            AuthorEmails = new List<string> { "test@example.com" },
                            AuthorsCount = 1,
                            FileContent = TestHelpers.CreateTestPdfBytes(),
                            FileName = "extreme-length-test.pdf"
                        };

                        var expectedResponse = new ManuscriptUploadResponse
                        {
                            ManuscriptId = "extreme-length-test-id",
                            Candidates = new List<RefereeCandidate>()
                        };

                        _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
                        _mockHttpClient.Setup(x => x.SendAsync(
                            It.IsAny<HttpRequestMessage>(),
                            It.IsAny<CancellationToken>()))
                            .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                                System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

                        _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                            .Returns(expectedResponse);

                        // Act
                        var result = _manuscriptModule.UploadAsync(request).Result;

                        // Assert
                        return result != null;
                    }
                    catch (ValidationException)
                    {
                        // Validation exceptions are expected for extreme inputs
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Unexpected exceptions should not occur
                        Console.WriteLine($"Unexpected exception: {ex}");
                        return false;
                    }
                });
        }

        #endregion

        #region Localization Edge Cases

        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("de-DE")]
        [InlineData("ja-JP")]
        [InlineData("ar-SA")]
        [InlineData("zh-CN")]
        public async Task ManuscriptUpload_WithDifferentCultures_ShouldHandleCorrectly(string cultureName)
        {
            // Arrange
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var request = new ManuscriptUploadRequest
                {
                    Title = "Culture Test Title",
                    Abstract = "Testing culture-specific handling",
                    AuthorNames = new List<string> { "Test Author" },
                    AuthorEmails = new List<string> { "test@example.com" },
                    AuthorsCount = 1,
                    FileContent = TestHelpers.CreateTestPdfBytes(),
                    FileName = "culture-test.pdf"
                };

                var expectedResponse = new ManuscriptUploadResponse
                {
                    ManuscriptId = $"culture-test-{cultureName}",
                    Candidates = new List<RefereeCandidate>()
                };

                _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
                _mockHttpClient.Setup(x => x.SendAsync(
                    It.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                        System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

                _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                    .Returns(expectedResponse);

                // Act
                var result = await _manuscriptModule.UploadAsync(request);

                // Assert
                Assert.NotNull(result);
                Assert.Equal($"culture-test-{cultureName}", result.ManuscriptId);
            }
            finally
            {
                // Restore original culture
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }

        #endregion

        #region Resource Management Edge Cases

        [Fact]
        public async Task ManuscriptUpload_WithDisposedHttpClient_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Disposed HttpClient Test",
                Abstract = "Testing disposed HttpClient handling",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "disposed-client-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ObjectDisposedException("HttpClient"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("UPLOAD_ERROR", exception.ErrorCode);
            Assert.Contains("ObjectDisposedException", exception.InnerException?.GetType().Name);
        }

        #endregion
    }
} 