using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient.Tests.Modules
{
    public class ManuscriptModuleTests
    {
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly Mock<IApiKeyAuthenticator> _mockAuthenticator;
        private readonly Mock<IMultipartFormDataBuilder> _mockFormDataBuilder;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly Mock<ILogger<ManuscriptModule>> _mockLogger;
        private readonly ManuscriptModule _manuscriptModule;

        public ManuscriptModuleTests()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockAuthenticator = new Mock<IApiKeyAuthenticator>();
            _mockFormDataBuilder = new Mock<IMultipartFormDataBuilder>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            _mockLogger = new Mock<ILogger<ManuscriptModule>>();

            _manuscriptModule = new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockFormDataBuilder.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ManuscriptModule(
                null!,
                _mockAuthenticator.Object,
                _mockFormDataBuilder.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullAuthenticator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ManuscriptModule(
                _mockHttpClient.Object,
                null!,
                _mockFormDataBuilder.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullFormDataBuilder_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                null!,
                _mockJsonSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullJsonSerializer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockFormDataBuilder.Object,
                null!,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockFormDataBuilder.Object,
                _mockJsonSerializer.Object,
                null!));
        }

        [Fact]
        public async Task UploadAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _manuscriptModule.UploadAsync(null!));
        }

        [Fact]
        public async Task UploadAsync_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "test-id",
                Message = "Upload successful",
                Manuscript = new Manuscript { Id = "123", Title = "Test Manuscript" }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };

            var formData = new MultipartFormDataContent();

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Upload successful", result.Message);
            Assert.Equal("123", result.Manuscript?.Id);

            _mockAuthenticator.Verify(x => x.AuthenticateRequest(It.IsAny<HttpRequestMessage>()), Times.Once);
            _mockFormDataBuilder.Verify(x => x.Clear(), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("title", request.Title), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddFile("file", request.FileName!, request.FileContent!, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UploadAsync_WithProgressReporting_ReportsProgress()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            var progressReports = new List<UploadProgress>();
            var progress = new Progress<UploadProgress>(p => progressReports.Add(p));

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };

            var formData = new MultipartFormDataContent();

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(new ManuscriptUploadResponse { Success = true });

            // Act
            await _manuscriptModule.UploadAsync(request, progress);

            // Assert
            Assert.NotEmpty(progressReports);
            Assert.Contains(progressReports, p => p.Stage == "Validating");
            Assert.Contains(progressReports, p => p.Stage == "Preparing");
            Assert.Contains(progressReports, p => p.Stage == "Uploading");
            Assert.Contains(progressReports, p => p.Stage == "Processing");
            Assert.Contains(progressReports, p => p.Stage == "Completed");
        }

        [Fact]
        public async Task UploadAsync_WithMissingFileContent_ThrowsValidationException()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            request.FileContent = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _manuscriptModule.UploadAsync(request));
            Assert.Contains("FileContent is required", exception.ValidationErrors);
        }

        [Fact]
        public async Task UploadAsync_WithMissingFileName_ThrowsValidationException()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            request.FileName = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _manuscriptModule.UploadAsync(request));
            Assert.Contains("FileName is required", exception.ValidationErrors);
        }

        [Fact]
        public async Task UploadAsync_WithOversizedFile_ThrowsValidationException()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            request.FileContent = new byte[51 * 1024 * 1024]; // 51MB - exceeds 50MB limit

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _manuscriptModule.UploadAsync(request));
            Assert.Contains("File size must be less than 50MB", exception.ValidationErrors);
        }

        [Fact]
        public async Task UploadAsync_WithCancellation_ThrowsApiTimeoutException()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var formData = new MultipartFormDataContent();
            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Operation was cancelled", new OperationCanceledException(cancellationTokenSource.Token)));

            // Act & Assert
            await Assert.ThrowsAsync<ApiTimeoutException>(() => _manuscriptModule.UploadAsync(request, null, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task UploadAsync_WithHttpError_ThrowsProphyApiException()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"Server error\"}", Encoding.UTF8, "application/json")
            };

            var formData = new MultipartFormDataContent();
            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await Assert.ThrowsAsync<ProphyApiException>(() => _manuscriptModule.UploadAsync(request));
        }

        [Fact]
        public async Task GetStatusAsync_WithNullManuscriptId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _manuscriptModule.GetStatusAsync(null!));
        }

        [Fact]
        public async Task GetStatusAsync_WithEmptyManuscriptId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _manuscriptModule.GetStatusAsync(""));
        }

        [Fact]
        public async Task GetStatusAsync_WithValidId_ReturnsStatusResponse()
        {
            // Arrange
            var manuscriptId = "123";
            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = manuscriptId,
                ProcessingStatus = "completed",
                Manuscript = new Manuscript { Id = manuscriptId, Title = "Test Manuscript" }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.GetStatusAsync(manuscriptId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("completed", result.ProcessingStatus);
            Assert.Equal(manuscriptId, result.Manuscript?.Id);

            _mockHttpClient.Verify(x => x.SendAsync(
                It.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"external/proposal/{manuscriptId}/status/")),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockAuthenticator.Verify(x => x.AuthenticateRequest(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Fact]
        public async Task GetStatusAsync_WithCancellation_ThrowsApiTimeoutException()
        {
            // Arrange
            var manuscriptId = "123";
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Operation was cancelled", new OperationCanceledException(cancellationTokenSource.Token)));

            // Act & Assert
            await Assert.ThrowsAsync<ApiTimeoutException>(() => _manuscriptModule.GetStatusAsync(manuscriptId, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetStatusAsync_WithHttpError_ThrowsProphyApiException()
        {
            // Arrange
            var manuscriptId = "123";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":\"Not found\"}", Encoding.UTF8, "application/json")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await Assert.ThrowsAsync<ProphyApiException>(() => _manuscriptModule.GetStatusAsync(manuscriptId));
        }

        [Theory]
        [InlineData(".pdf", "application/pdf")]
        [InlineData(".doc", "application/msword")]
        [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData(".txt", "text/plain")]
        [InlineData(".rtf", "application/rtf")]
        [InlineData(".unknown", "application/octet-stream")]
        public async Task UploadAsync_WithDifferentFileTypes_UseCorrectMimeType(string extension, string expectedMimeType)
        {
            // Arrange
            var request = CreateValidUploadRequest();
            request.FileName = $"test{extension}";
            request.MimeType = null; // Let the module determine the MIME type

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };

            var formData = new MultipartFormDataContent();
            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(new ManuscriptUploadResponse { Success = true });

            // Act
            await _manuscriptModule.UploadAsync(request);

            // Assert
            _mockFormDataBuilder.Verify(x => x.AddFile("file", request.FileName, request.FileContent!, expectedMimeType), Times.Once);
        }

        [Fact]
        public async Task UploadAsync_WithCustomMimeType_UsesProvidedMimeType()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            request.MimeType = "custom/type";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };

            var formData = new MultipartFormDataContent();
            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(new ManuscriptUploadResponse { Success = true });

            // Act
            await _manuscriptModule.UploadAsync(request);

            // Assert
            _mockFormDataBuilder.Verify(x => x.AddFile("file", request.FileName!, request.FileContent!, "custom/type"), Times.Once);
        }

        [Fact]
        public async Task UploadAsync_WithOptionalFields_AddsAllFieldsToFormData()
        {
            // Arrange
            var request = CreateValidUploadRequest();
            request.Abstract = "Test abstract";
            request.Authors = new List<string> { "Test Author" };
            request.Keywords = new List<string> { "keyword1", "keyword2" };
            request.Subject = "Test subject";
            request.Type = "research";
            request.Folder = "test-folder";
            request.OriginId = "origin-123";
            request.Language = "en";
            request.CustomFields = new Dictionary<string, object> { { "field1", "value1" } };
            request.Metadata = new Dictionary<string, object> { { "key1", "value1" } };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };

            var formData = new MultipartFormDataContent();
            _mockFormDataBuilder.Setup(x => x.Build()).Returns(formData);
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(new ManuscriptUploadResponse { Success = true });
            _mockJsonSerializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns("serialized");

            // Act
            await _manuscriptModule.UploadAsync(request);

            // Assert
            _mockFormDataBuilder.Verify(x => x.AddField("abstract", request.Abstract), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("authors", "serialized"), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("keywords", "serialized"), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("subject", request.Subject), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("type", request.Type), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("folder", request.Folder), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("originId", request.OriginId), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("language", request.Language), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("customFields", "serialized"), Times.Once);
            _mockFormDataBuilder.Verify(x => x.AddField("metadata", "serialized"), Times.Once);
        }

        private static ManuscriptUploadRequest CreateValidUploadRequest()
        {
            return new ManuscriptUploadRequest
            {
                Title = "Test Manuscript",
                FileName = "test.pdf",
                FileContent = Encoding.UTF8.GetBytes("Test file content"),
                MimeType = "application/pdf"
            };
        }
    }
} 