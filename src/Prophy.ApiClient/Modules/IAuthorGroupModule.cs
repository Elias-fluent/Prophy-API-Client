using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Interface for author group operations in the Prophy API.
    /// Provides methods for managing author groups and their members.
    /// </summary>
    public interface IAuthorGroupModule
    {
        /// <summary>
        /// Creates a new author group.
        /// </summary>
        /// <param name="request">The request containing author group details.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created author group.</returns>
        Task<AuthorGroupResponse> CreateAsync(CreateAuthorGroupRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an author group by its ID.
        /// </summary>
        /// <param name="groupId">The ID of the author group to retrieve.</param>
        /// <param name="includeAuthors">Whether to include the list of authors in the response.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the author group.</returns>
        Task<AuthorGroupResponse> GetByIdAsync(string groupId, bool includeAuthors = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all author groups for the organization.
        /// </summary>
        /// <param name="page">The page number for pagination (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="includeInactive">Whether to include inactive groups.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of author groups.</returns>
        Task<AuthorGroupListResponse> GetAllAsync(int page = 1, int pageSize = 50, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group to update.</param>
        /// <param name="request">The request containing updated author group details.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated author group.</returns>
        Task<AuthorGroupResponse> UpdateAsync(string groupId, UpdateAuthorGroupRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group to delete.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync(string groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds an author to an author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group.</param>
        /// <param name="clientId">The client-specific ID for the author.</param>
        /// <param name="request">The request containing author details.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added author.</returns>
        Task<AuthorFromGroupResponse> AddAuthorAsync(string groupId, string clientId, AuthorFromGroupRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an author from an author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group.</param>
        /// <param name="clientId">The client-specific ID for the author.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the author.</returns>
        Task<AuthorFromGroupResponse> GetAuthorAsync(string groupId, string clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an author in an author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group.</param>
        /// <param name="clientId">The client-specific ID for the author.</param>
        /// <param name="request">The request containing updated author details.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated author.</returns>
        Task<AuthorFromGroupResponse> UpdateAuthorAsync(string groupId, string clientId, AuthorFromGroupRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an author from an author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group.</param>
        /// <param name="clientId">The client-specific ID for the author.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAuthorAsync(string groupId, string clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all authors in an author group.
        /// </summary>
        /// <param name="groupId">The ID of the author group.</param>
        /// <param name="page">The page number for pagination (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="includeInactive">Whether to include inactive authors.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of authors.</returns>
        Task<List<Author>> GetAuthorsAsync(string groupId, int page = 1, int pageSize = 100, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for author groups by name or other criteria.
        /// </summary>
        /// <param name="searchTerm">The search term to match against group names.</param>
        /// <param name="page">The page number for pagination (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the matching author groups.</returns>
        Task<AuthorGroupListResponse> SearchAsync(string searchTerm, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    }
} 