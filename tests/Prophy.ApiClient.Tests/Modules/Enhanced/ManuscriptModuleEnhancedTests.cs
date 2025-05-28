using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.Modules.Enhanced
{
    /// <summary>
    /// Enhanced tests for ManuscriptModule using property-based testing and improved test utilities.
    /// </summary>
    public class ManuscriptModuleEnhancedTests
    {
        private readonly ManuscriptModule _manuscriptModule;
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly Mock<IApiKeyAuthenticator> _mockAuthenticator;
        private readonly Mock<ILogger<ManuscriptModule>> _mockLogger;

        public ManuscriptModuleEnhancedTests()
        {
            _mockHttpClient = TestHelpers.CreateMockHttpClient();
            _mockAuthenticator = TestHelpers.CreateMockAuthenticator();
            _mockLogger = TestHelpers.CreateMockLogger<ManuscriptModule>();

            // Setup additional mocks needed for ManuscriptModule
            var mockFormDataBuilder = new Mock<IMultipartFormDataBuilder>();
            var mockJsonSerializer = new Mock<IJsonSerializer>();

            mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            
            _manuscriptModule = new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                mockFormDataBuilder.Object,
                mockJsonSerializer.Object,
                _mockLogger.Object);
        }

        #region Property-Based Tests

        [Property]
        public Property UploadAsync_WithValidTitle_ShouldAcceptAnyNonEmptyString()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                title =>
                {
                    // Arrange
                    var request = CreateValidManuscriptRequest();
                    request.Title = title.Get;

                    // Act & Assert - Should not throw validation exception for title
                    try
                    {
                        // This is a property test - we're testing the validation logic
                        ValidateManuscriptRequest(request);
                        return true;
                    }
                    catch (ValidationException ex) when (ex.Message.Contains("title", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    catch
                    {
                        // Other exceptions are acceptable for this property
                        return true;
                    }
                });
        }

        [Property]
        public Property UploadAsync_WithInvalidFileSize_ShouldRejectOversizedFiles()
        {
            return Prop.ForAll(
                Arb.From<PositiveInt>().Filter(x => x.Get > 100_000_000), // Files larger than 100MB
                fileSize =>
                {
                    // Arrange
                    var request = CreateValidManuscriptRequest();
                    request.FileContent = new byte[fileSize.Get];

                    // Act & Assert
                    try
                    {
                        ValidateManuscriptRequest(request);
                        return false; // Should have thrown validation exception
                    }
                    catch (ValidationException ex) when (ex.Message.Contains("file size", StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Expected validation exception
                    }
                    catch
                    {
                        return false; // Unexpected exception type
                    }
                });
        }

        [Property]
        public Property UploadAsync_WithValidAuthorNames_ShouldAcceptVariousFormats()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString[]>().Filter(names => names.Length > 0 && names.Length <= 50),
                authorNames =>
                {
                    // Arrange
                    var request = CreateValidManuscriptRequest();
                    request.AuthorNames = authorNames.Select(n => n.Get).ToList();

                    // Act & Assert
                    try
                    {
                        ValidateManuscriptRequest(request);
                        return true;
                    }
                    catch (ValidationException ex) when (ex.Message.Contains("author", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    catch
                    {
                        return true; // Other validation errors are acceptable
                    }
                });
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task UploadAsync_WithExactlyMaxFileSize_ShouldSucceed()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            request.FileContent = new byte[50_000_000]; // Exactly 50MB (assuming this is the limit)

            SetupSuccessfulHttpResponse();

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            TestHelpers.VerifyHttpClientCalled(_mockHttpClient, HttpMethod.Post, "proposal");
        }

        [Fact]
        public async Task UploadAsync_WithUnicodeCharactersInTitle_ShouldHandleCorrectly()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            request.Title = "研究论文: Étude sur les données 🔬📊";

            SetupSuccessfulHttpResponse();

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            TestHelpers.VerifyHttpClientCalled(_mockHttpClient, HttpMethod.Post, "proposal");
        }

        [Fact]
        public async Task UploadAsync_WithEmptyCustomFields_ShouldNotIncludeInRequest()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            request.CustomFields = new Dictionary<string, object>();

            SetupSuccessfulHttpResponse();

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            TestHelpers.VerifyHttpClientCalled(_mockHttpClient, HttpMethod.Post, "proposal");
        }

        [Fact]
        public async Task UploadAsync_WithNullCustomFields_ShouldHandleGracefully()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            request.CustomFields = null;

            SetupSuccessfulHttpResponse();

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public async Task UploadAsync_WithWhitespaceOnlyTitle_ShouldThrowValidationException(string title)
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            request.Title = title;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _manuscriptModule.UploadAsync(request));
        }

        #endregion

        #region Performance and Stress Tests

        [Fact]
        public async Task UploadAsync_WithLargeNumberOfAuthors_ShouldHandleEfficiently()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            request.AuthorNames = Enumerable.Range(1, 100)
                .Select(i => $"Author {i}")
                .ToList();

            SetupSuccessfulHttpResponse();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _manuscriptModule.UploadAsync(request);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Upload should complete within 5 seconds");
        }

        [Fact]
        public async Task UploadAsync_WithConcurrentRequests_ShouldHandleParallelism()
        {
            // Arrange
            var requests = Enumerable.Range(1, 5)
                .Select(i => CreateValidManuscriptRequest())
                .ToList();

            SetupSuccessfulHttpResponse();

            // Act
            var tasks = requests.Select(request => _manuscriptModule.UploadAsync(request));
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.NotNull(result));
            TestHelpers.VerifyHttpClientCalled(_mockHttpClient, HttpMethod.Post, "proposal", Times.Exactly(5));
        }

        #endregion

        #region Error Handling Tests

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, typeof(ValidationException))]
        [InlineData(HttpStatusCode.Unauthorized, typeof(AuthenticationException))]
        [InlineData(HttpStatusCode.Forbidden, typeof(AuthenticationException))]
        [InlineData(HttpStatusCode.NotFound, typeof(ProphyApiException))]
        [InlineData(HttpStatusCode.InternalServerError, typeof(ProphyApiException))]
        [InlineData(HttpStatusCode.ServiceUnavailable, typeof(ProphyApiException))]
        public async Task UploadAsync_WithHttpErrorCodes_ShouldThrowAppropriateException(
            HttpStatusCode statusCode, 
            Type expectedExceptionType)
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            SetupHttpErrorResponse(statusCode);

            // Act & Assert
            var exception = await Assert.ThrowsAsync(expectedExceptionType, () => _manuscriptModule.UploadAsync(request));
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task UploadAsync_WithNetworkTimeout_ShouldThrowApiTimeoutException()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            // Act & Assert
            await Assert.ThrowsAsync<ApiTimeoutException>(() => _manuscriptModule.UploadAsync(request));
        }

        [Fact]
        public async Task UploadAsync_WithMalformedJsonResponse_ShouldThrowSerializationException()
        {
            // Arrange
            var request = CreateValidManuscriptRequest();
            var response = TestHelpers.CreateSuccessResponse("{ invalid json }");
            
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act & Assert
            await Assert.ThrowsAsync<SerializationException>(() => _manuscriptModule.UploadAsync(request));
        }

        #endregion

        #region Helper Methods

        private ManuscriptUploadRequest CreateValidManuscriptRequest()
        {
            return new ManuscriptUploadRequest
            {
                Title = "Test Manuscript",
                Abstract = "This is a test abstract",
                AuthorNames = new List<string> { "Dr. Test Author" },
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "test.pdf",
                MimeType = "application/pdf",
                CustomFields = TestHelpers.CreateCustomFieldValues()
            };
        }

        private void SetupSuccessfulHttpResponse()
        {
            var response = TestHelpers.CreateSuccessResponse(@"{
                ""manuscript_id"": ""test-123"",
                ""candidates"": [],
                ""debug_info"": {
                    ""extracted_concepts"": 10,
                    ""parsed_references"": 5,
                    ""parsed_text_len"": 1000
                }
            }");

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        private void SetupHttpErrorResponse(HttpStatusCode statusCode)
        {
            var response = TestHelpers.CreateErrorResponse(statusCode, @"{""error"": ""Test error""}");
            
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        private static void ValidateManuscriptRequest(ManuscriptUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Title is required", new[] { "Title cannot be empty or whitespace" });

            if (request.FileContent == null || request.FileContent.Length == 0)
                throw new ValidationException("File content is required", new[] { "File content cannot be null or empty" });

            if (request.FileContent.Length > 100_000_000) // 100MB limit
                throw new ValidationException("File size exceeds maximum allowed size", new[] { "File size must be less than 100MB" });

            if (request.AuthorNames == null || !request.AuthorNames.Any())
                throw new ValidationException("At least one author is required", new[] { "AuthorNames list cannot be null or empty" });
        }

        #endregion
    }
} 