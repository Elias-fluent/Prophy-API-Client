using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Implementation of author group operations for the Prophy API.
    /// Provides methods for managing author groups and their members.
    /// </summary>
    public class AuthorGroupModule : IAuthorGroupModule
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IApiKeyAuthenticator _authenticator;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger<AuthorGroupModule> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthorGroupModule class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        /// <param name="authenticator">The authenticator for API key authentication.</param>
        /// <param name="serializer">The JSON serializer for request/response handling.</param>
        /// <param name="logger">The logger for recording operations.</param>
        public AuthorGroupModule(
            IHttpClientWrapper httpClient,
            IApiKeyAuthenticator authenticator,
            IJsonSerializer serializer,
            ILogger<AuthorGroupModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<AuthorGroupResponse> CreateAsync(CreateAuthorGroupRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateRequest(request);

            _logger.LogInformation("Creating author group: {GroupName}", request.GroupName);

            try
            {
                var requestJson = _serializer.Serialize(request);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/external/authors-group/create/")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author group creation failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GROUP_CREATE_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author group creation response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully created author group with ID: {GroupId}", response.Data?.Id);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while creating author group: {GroupName}", request.GroupName);
                throw new ProphyApiException("Network error occurred while creating author group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while creating author group: {GroupName}", request.GroupName);
                throw new ProphyApiException("Request timeout while creating author group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while creating author group: {GroupName}", request.GroupName);
                throw new ProphyApiException("Unexpected error occurred while creating author group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorGroupResponse> GetByIdAsync(string groupId, bool includeAuthors = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));

            _logger.LogInformation("Getting author group by ID: {GroupId}", groupId);

            try
            {
                var url = $"api/external/authors-group/{groupId}/";
                if (includeAuthors)
                {
                    url += "?include_authors=true";
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author group retrieval failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GROUP_GET_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author group response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully retrieved author group: {GroupId}", groupId);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while getting author group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while getting author group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while getting author group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while getting author group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while getting author group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while getting author group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorGroupListResponse> GetAllAsync(int page = 1, int pageSize = 50, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(page));
            if (pageSize < 1 || pageSize > 1000)
                throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));

            _logger.LogInformation("Getting all author groups (page: {Page}, size: {PageSize})", page, pageSize);

            try
            {
                var url = $"api/external/authors-group/?page={page}&page_size={pageSize}";
                if (includeInactive)
                {
                    url += "&include_inactive=true";
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author groups list retrieval failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GROUP_LIST_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorGroupListResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author groups list response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully retrieved {Count} author groups", response.Data?.Count ?? 0);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while getting author groups list");
                throw new ProphyApiException("Network error occurred while getting author groups list", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while getting author groups list");
                throw new ProphyApiException("Request timeout while getting author groups list", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while getting author groups list");
                throw new ProphyApiException("Unexpected error occurred while getting author groups list", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorGroupResponse> UpdateAsync(string groupId, UpdateAuthorGroupRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateUpdateRequest(request);

            if (!request.HasUpdates())
            {
                throw new ArgumentException("At least one field must be provided for update", nameof(request));
            }

            _logger.LogInformation("Updating author group: {GroupId}", groupId);

            try
            {
                var requestJson = _serializer.Serialize(request);
                var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"api/external/authors-group/{groupId}/")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author group update failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GROUP_UPDATE_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author group update response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully updated author group: {GroupId}", groupId);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while updating author group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while updating author group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while updating author group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while updating author group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while updating author group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while updating author group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string groupId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));

            _logger.LogInformation("Deleting author group: {GroupId}", groupId);

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/external/authors-group/{groupId}/");

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var errorMessage = $"Author group deletion failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GROUP_DELETE_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully deleted author group: {GroupId}", groupId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while deleting author group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while deleting author group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while deleting author group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while deleting author group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting author group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while deleting author group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorFromGroupResponse> AddAuthorAsync(string groupId, string clientId, AuthorFromGroupRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateAuthorRequest(request);

            _logger.LogInformation("Adding author to group: {GroupId}, Client ID: {ClientId}", groupId, clientId);

            try
            {
                var requestJson = _serializer.Serialize(request);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/external/author-from-group/{groupId}/{clientId}/")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author addition failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_ADD_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorFromGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author addition response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully added author to group: {GroupId}", groupId);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while adding author to group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while adding author to group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while adding author to group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while adding author to group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while adding author to group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while adding author to group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorFromGroupResponse> GetAuthorAsync(string groupId, string clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));

            _logger.LogInformation("Getting author from group: {GroupId}, Client ID: {ClientId}", groupId, clientId);

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"api/external/author-from-group/{groupId}/{clientId}/");

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author retrieval failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GET_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorFromGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully retrieved author from group: {GroupId}", groupId);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while getting author from group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while getting author from group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while getting author from group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while getting author from group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while getting author from group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while getting author from group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorFromGroupResponse> UpdateAuthorAsync(string groupId, string clientId, AuthorFromGroupRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ValidateAuthorRequest(request);

            _logger.LogInformation("Updating author in group: {GroupId}, Client ID: {ClientId}", groupId, clientId);

            try
            {
                var requestJson = _serializer.Serialize(request);
                var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"api/external/author-from-group/{groupId}/{clientId}/")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author update failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_UPDATE_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorFromGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author update response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully updated author in group: {GroupId}", groupId);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while updating author in group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while updating author in group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while updating author in group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while updating author in group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while updating author in group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while updating author in group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorFromGroupResponse> PartialUpdateAuthorAsync(string groupId, string clientId, AuthorPartialUpdateRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate that at least one field is being updated
            if (!request.HasUpdates())
                throw new ArgumentException("At least one field must be provided for partial update", nameof(request));

            ValidatePartialUpdateRequest(request);

            _logger.LogInformation("Partially updating author in group: {GroupId}, Client ID: {ClientId}", groupId, clientId);

            try
            {
                var requestJson = _serializer.Serialize(request);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/external/author-from-group/{groupId}/{clientId}/partial/")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };

                // Override the method to PATCH since .NET Standard 2.0 doesn't have HttpMethod.Patch
                httpRequest.Method = new HttpMethod("PATCH");

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author partial update failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_PARTIAL_UPDATE_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorFromGroupResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author partial update response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully partially updated author in group: {GroupId}", groupId);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while partially updating author in group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while partially updating author in group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while partially updating author in group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while partially updating author in group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while partially updating author in group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while partially updating author in group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteAuthorAsync(string groupId, string clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));

            _logger.LogInformation("Deleting author from group: {GroupId}, Client ID: {ClientId}", groupId, clientId);

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/external/author-from-group/{groupId}/{clientId}/");

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var errorMessage = $"Author deletion failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_DELETE_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully deleted author from group: {GroupId}", groupId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while deleting author from group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while deleting author from group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while deleting author from group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while deleting author from group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting author from group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while deleting author from group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<Author>> GetAuthorsAsync(string groupId, int page = 1, int pageSize = 100, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(page));
            if (pageSize < 1 || pageSize > 1000)
                throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));

            _logger.LogInformation("Getting authors from group: {GroupId} (page: {Page}, size: {PageSize})", groupId, page, pageSize);

            try
            {
                var url = $"api/external/authors-group/{groupId}/authors/?page={page}&page_size={pageSize}";
                if (includeInactive)
                {
                    url += "&include_inactive=true";
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Authors list retrieval failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHORS_LIST_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var authors = _serializer.Deserialize<List<Author>>(responseContent);
                
                if (authors == null)
                {
                    throw new ProphyApiException("Failed to deserialize authors list response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully retrieved {Count} authors from group: {GroupId}", authors.Count, groupId);
                return authors;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while getting authors from group: {GroupId}", groupId);
                throw new ProphyApiException("Network error occurred while getting authors from group", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while getting authors from group: {GroupId}", groupId);
                throw new ProphyApiException("Request timeout while getting authors from group", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while getting authors from group: {GroupId}", groupId);
                throw new ProphyApiException("Unexpected error occurred while getting authors from group", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<AuthorGroupListResponse> SearchAsync(string searchTerm, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(page));
            if (pageSize < 1 || pageSize > 1000)
                throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));

            _logger.LogInformation("Searching author groups with term: {SearchTerm}", searchTerm);

            try
            {
                var url = $"api/external/authors-group/search/?q={Uri.EscapeDataString(searchTerm)}&page={page}&page_size={pageSize}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Author groups search failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "AUTHOR_GROUP_SEARCH_FAILED", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<AuthorGroupListResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize author groups search response", "DESERIALIZATION_FAILED", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully found {Count} author groups matching: {SearchTerm}", response.Data?.Count ?? 0, searchTerm);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while searching author groups: {SearchTerm}", searchTerm);
                throw new ProphyApiException("Network error occurred while searching author groups", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while searching author groups: {SearchTerm}", searchTerm);
                throw new ProphyApiException("Request timeout while searching author groups", "REQUEST_TIMEOUT", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while searching author groups: {SearchTerm}", searchTerm);
                throw new ProphyApiException("Unexpected error occurred while searching author groups", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <summary>
        /// Validates the create author group request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="Prophy.ApiClient.Exceptions.ValidationException">Thrown when validation fails.</exception>
        private void ValidateRequest(CreateAuthorGroupRequest request)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                var errorMessage = $"Author group request validation failed: {string.Join(", ", errors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, errors);
            }

            // Additional custom validation
            var customErrors = request.Validate();
            if (customErrors.Count > 0)
            {
                var errorMessage = $"Author group request validation failed: {string.Join(", ", customErrors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, customErrors);
            }
        }

        /// <summary>
        /// Validates the update author group request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="Prophy.ApiClient.Exceptions.ValidationException">Thrown when validation fails.</exception>
        private void ValidateUpdateRequest(UpdateAuthorGroupRequest request)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                var errorMessage = $"Author group update request validation failed: {string.Join(", ", errors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, errors);
            }

            // Additional custom validation
            var customErrors = request.Validate();
            if (customErrors.Count > 0)
            {
                var errorMessage = $"Author group update request validation failed: {string.Join(", ", customErrors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, customErrors);
            }
        }

        /// <summary>
        /// Validates the author from group request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="Prophy.ApiClient.Exceptions.ValidationException">Thrown when validation fails.</exception>
        private void ValidateAuthorRequest(AuthorFromGroupRequest request)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                var errorMessage = $"Author request validation failed: {string.Join(", ", errors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, errors);
            }

            // Additional custom validation
            var customErrors = request.Validate();
            if (customErrors.Count > 0)
            {
                var errorMessage = $"Author request validation failed: {string.Join(", ", customErrors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, customErrors);
            }
        }

        /// <summary>
        /// Validates the author partial update request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="Prophy.ApiClient.Exceptions.ValidationException">Thrown when validation fails.</exception>
        private void ValidatePartialUpdateRequest(AuthorPartialUpdateRequest request)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                var errorMessage = $"Author partial update request validation failed: {string.Join(", ", errors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, errors);
            }

            // Additional custom validation
            var customErrors = request.Validate();
            if (customErrors.Count > 0)
            {
                var errorMessage = $"Author partial update request validation failed: {string.Join(", ", customErrors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, customErrors);
            }
        }
    }
} 