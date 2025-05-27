using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
    /// <summary>
    /// Unit tests for the AuthorGroupModule class.
    /// </summary>
    public class AuthorGroupModuleTests : IDisposable
    {
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly Mock<IApiKeyAuthenticator> _mockAuthenticator;
        private readonly Mock<IJsonSerializer> _mockSerializer;
        private readonly Mock<ILogger<AuthorGroupModule>> _mockLogger;
        private readonly AuthorGroupModule _authorGroupModule;

        public AuthorGroupModuleTests()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockAuthenticator = new Mock<IApiKeyAuthenticator>();
            _mockSerializer = new Mock<IJsonSerializer>();
            _mockLogger = new Mock<ILogger<AuthorGroupModule>>();

            _authorGroupModule = new AuthorGroupModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockSerializer.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            // No resources to dispose
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthorGroupModule(
                null!,
                _mockAuthenticator.Object,
                _mockSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullAuthenticator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthorGroupModule(
                _mockHttpClient.Object,
                null!,
                _mockSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullSerializer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthorGroupModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                null!,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthorGroupModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockSerializer.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_authorGroupModule);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _authorGroupModule.CreateAsync(null!));
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ReturnsAuthorGroupResponse()
        {
            // Arrange
            var request = new CreateAuthorGroupRequest
            {
                GroupName = "Test Group",
                OwnerTeam = "Admin Team"
            };

            var expectedResponse = new AuthorGroupResponse
            {
                Data = new AuthorGroup { Id = "123", GroupName = "Test Group" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"id\":\"123\"}}")
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Test Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123", result.Data?.Id);
            Assert.True(result.Success);
            _mockAuthenticator.Verify(a => a.AuthenticateRequest(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithHttpError_ThrowsProphyApiException()
        {
            // Arrange
            var request = new CreateAuthorGroupRequest
            {
                GroupName = "Test Group",
                OwnerTeam = "Admin Team"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Test Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _authorGroupModule.CreateAsync(request));
            
            Assert.Contains("AUTHOR_GROUP_CREATE_FAILED", exception.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest, exception.HttpStatusCode);
        }

        #endregion

        #region GetByIdAsync Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByIdAsync_WithInvalidGroupId_ThrowsArgumentException(string groupId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.GetByIdAsync(groupId));
        }

        [Fact]
        public async Task GetByIdAsync_WithValidGroupId_ReturnsAuthorGroup()
        {
            // Arrange
            var groupId = "123";
            var expectedResponse = new AuthorGroupResponse
            {
                Data = new AuthorGroup { Id = groupId, GroupName = "Test Group" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"id\":\"123\"}}")
            };

            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.GetByIdAsync(groupId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(groupId, result.Data?.Id);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetByIdAsync_WithIncludeAuthors_AddsQueryParameter()
        {
            // Arrange
            var groupId = "123";
            var expectedResponse = new AuthorGroupResponse
            {
                Data = new AuthorGroup { Id = groupId, GroupName = "Test Group" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"id\":\"123\"}}")
            };

            HttpRequestMessage? capturedRequest = null;
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            await _authorGroupModule.GetByIdAsync(groupId, includeAuthors: true);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Contains("include_authors=true", capturedRequest.RequestUri?.ToString());
        }

        #endregion

        #region GetAllAsync Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetAllAsync_WithInvalidPage_ThrowsArgumentException(int page)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.GetAllAsync(page));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1001)]
        public async Task GetAllAsync_WithInvalidPageSize_ThrowsArgumentException(int pageSize)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.GetAllAsync(pageSize: pageSize));
        }

        [Fact]
        public async Task GetAllAsync_WithValidParameters_ReturnsAuthorGroupList()
        {
            // Arrange
            var expectedResponse = new AuthorGroupListResponse
            {
                Data = new List<AuthorGroup>
                {
                    new AuthorGroup { Id = "1", GroupName = "Group 1" },
                    new AuthorGroup { Id = "2", GroupName = "Group 2" }
                },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"id\":\"1\"},{\"id\":\"2\"}]}")
            };

            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupListResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
            Assert.True(result.Success);
        }

        #endregion

        #region UpdateAsync Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateAsync_WithInvalidGroupId_ThrowsArgumentException(string groupId)
        {
            // Arrange
            var request = new UpdateAuthorGroupRequest { GroupName = "Updated Group" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.UpdateAsync(groupId, request));
        }

        [Fact]
        public async Task UpdateAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _authorGroupModule.UpdateAsync("123", null!));
        }

        [Fact]
        public async Task UpdateAsync_WithValidRequest_ReturnsUpdatedAuthorGroup()
        {
            // Arrange
            var groupId = "123";
            var request = new UpdateAuthorGroupRequest { GroupName = "Updated Group" };
            var expectedResponse = new AuthorGroupResponse
            {
                Data = new AuthorGroup { Id = groupId, GroupName = "Updated Group" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"id\":\"123\"}}")
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Updated Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.UpdateAsync(groupId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(groupId, result.Data?.Id);
            Assert.Equal("Updated Group", result.Data?.GroupName);
            Assert.True(result.Success);
        }

        #endregion

        #region DeleteAsync Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteAsync_WithInvalidGroupId_ThrowsArgumentException(string groupId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.DeleteAsync(groupId));
        }

        [Fact]
        public async Task DeleteAsync_WithValidGroupId_CompletesSuccessfully()
        {
            // Arrange
            var groupId = "123";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await _authorGroupModule.DeleteAsync(groupId);

            // Verify the correct HTTP method and URL were used
            _mockHttpClient.Verify(h => h.SendAsync(
                It.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Delete && 
                    req.RequestUri!.ToString().Contains($"api/external/authors-group/{groupId}/")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Author Management Tests

        [Theory]
        [InlineData(null, "client123")]
        [InlineData("", "client123")]
        [InlineData("   ", "client123")]
        [InlineData("group123", null)]
        [InlineData("group123", "")]
        [InlineData("group123", "   ")]
        public async Task AddAuthorAsync_WithInvalidParameters_ThrowsArgumentException(string groupId, string clientId)
        {
            // Arrange
            var request = new AuthorFromGroupRequest { Name = "John Doe" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.AddAuthorAsync(groupId, clientId, request));
        }

        [Fact]
        public async Task AddAuthorAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _authorGroupModule.AddAuthorAsync("group123", "client123", null!));
        }

        [Fact]
        public async Task AddAuthorAsync_WithValidRequest_ReturnsAuthorResponse()
        {
            // Arrange
            var groupId = "group123";
            var clientId = "client123";
            var request = new AuthorFromGroupRequest { Name = "John Doe" };
            var expectedResponse = new AuthorFromGroupResponse
            {
                Data = new Author { Name = "John Doe" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"data\":{\"name\":\"John Doe\"}}")
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"name\":\"John Doe\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorFromGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.AddAuthorAsync(groupId, clientId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Doe", result.Data?.Name);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetAuthorAsync_WithValidParameters_ReturnsAuthor()
        {
            // Arrange
            var groupId = "group123";
            var clientId = "client123";
            var expectedResponse = new AuthorFromGroupResponse
            {
                Data = new Author { Name = "John Doe" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"name\":\"John Doe\"}}")
            };

            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorFromGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.GetAuthorAsync(groupId, clientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Doe", result.Data?.Name);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task UpdateAuthorAsync_WithValidRequest_ReturnsUpdatedAuthor()
        {
            // Arrange
            var groupId = "group123";
            var clientId = "client123";
            var request = new AuthorFromGroupRequest { Name = "Jane Doe" };
            var expectedResponse = new AuthorFromGroupResponse
            {
                Data = new Author { Name = "Jane Doe" },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"name\":\"Jane Doe\"}}")
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"name\":\"Jane Doe\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorFromGroupResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.UpdateAuthorAsync(groupId, clientId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Jane Doe", result.Data?.Name);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DeleteAuthorAsync_WithValidParameters_CompletesSuccessfully()
        {
            // Arrange
            var groupId = "group123";
            var clientId = "client123";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await _authorGroupModule.DeleteAuthorAsync(groupId, clientId);

            // Verify the correct HTTP method and URL were used
            _mockHttpClient.Verify(h => h.SendAsync(
                It.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Delete && 
                    req.RequestUri!.ToString().Contains($"api/external/author-from-group/{groupId}/{clientId}/")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAuthorsAsync_WithValidParameters_ReturnsAuthorsList()
        {
            // Arrange
            var groupId = "group123";
            var expectedAuthors = new List<Author>
            {
                new Author { Name = "John Doe" },
                new Author { Name = "Jane Smith" }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"name\":\"John Doe\"},{\"name\":\"Jane Smith\"}]")
            };

            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<List<Author>>(It.IsAny<string>()))
                .Returns(expectedAuthors);

            // Act
            var result = await _authorGroupModule.GetAuthorsAsync(groupId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("John Doe", result[0].Name);
            Assert.Equal("Jane Smith", result[1].Name);
        }

        #endregion

        #region SearchAsync Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchAsync_WithInvalidSearchTerm_ThrowsArgumentException(string searchTerm)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _authorGroupModule.SearchAsync(searchTerm));
        }

        [Fact]
        public async Task SearchAsync_WithValidSearchTerm_ReturnsSearchResults()
        {
            // Arrange
            var searchTerm = "physics";
            var expectedResponse = new AuthorGroupListResponse
            {
                Data = new List<AuthorGroup>
                {
                    new AuthorGroup { Id = "1", GroupName = "Physics Experts" }
                },
                Success = true
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"id\":\"1\",\"group_name\":\"Physics Experts\"}]}")
            };

            HttpRequestMessage? capturedRequest = null;
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupListResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _authorGroupModule.SearchAsync(searchTerm);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal("Physics Experts", result.Data[0].GroupName);
            Assert.True(result.Success);

            // Verify URL encoding
            Assert.NotNull(capturedRequest);
            Assert.Contains($"q={Uri.EscapeDataString(searchTerm)}", capturedRequest.RequestUri?.ToString());
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateAsync_WithNetworkError_ThrowsProphyApiException()
        {
            // Arrange
            var request = new CreateAuthorGroupRequest
            {
                GroupName = "Test Group",
                OwnerTeam = "Admin Team"
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Test Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _authorGroupModule.CreateAsync(request));
            
            Assert.Contains("NETWORK_ERROR", exception.ErrorCode);
            Assert.IsType<HttpRequestException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateAsync_WithTimeout_ThrowsProphyApiException()
        {
            // Arrange
            var request = new CreateAuthorGroupRequest
            {
                GroupName = "Test Group",
                OwnerTeam = "Admin Team"
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Test Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Timeout", new TimeoutException()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _authorGroupModule.CreateAsync(request));
            
            Assert.Contains("REQUEST_TIMEOUT", exception.ErrorCode);
        }

        [Fact]
        public async Task CreateAsync_WithDeserializationFailure_ThrowsProphyApiException()
        {
            // Arrange
            var request = new CreateAuthorGroupRequest
            {
                GroupName = "Test Group",
                OwnerTeam = "Admin Team"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"id\":\"123\"}}")
            };

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Test Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);
            _mockSerializer.Setup(s => s.Deserialize<AuthorGroupResponse>(It.IsAny<string>()))
                .Returns((AuthorGroupResponse?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _authorGroupModule.CreateAsync(request));
            
            Assert.Contains("DESERIALIZATION_FAILED", exception.ErrorCode);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task CreateAsync_WithCancellation_ThrowsProphyApiException()
        {
            // Arrange
            var request = new CreateAuthorGroupRequest
            {
                GroupName = "Test Group",
                OwnerTeam = "Admin Team"
            };

            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockSerializer.Setup(s => s.Serialize(request)).Returns("{\"group_name\":\"Test Group\"}");
            _mockHttpClient.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _authorGroupModule.CreateAsync(request, cts.Token));
            
            Assert.Contains("UNEXPECTED_ERROR", exception.ErrorCode);
            Assert.IsType<TaskCanceledException>(exception.InnerException);
        }

        #endregion
    }
} 