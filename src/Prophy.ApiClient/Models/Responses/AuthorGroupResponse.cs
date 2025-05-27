using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Responses
{
    /// <summary>
    /// Response model for author group operations.
    /// </summary>
    public class AuthorGroupResponse
    {
        /// <summary>
        /// Gets or sets the author group data.
        /// </summary>
        [JsonPropertyName("data")]
        public AuthorGroup? Data { get; set; }

        /// <summary>
        /// Gets or sets the success status of the operation.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any error message if the operation failed.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional error details.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for listing multiple author groups.
    /// </summary>
    public class AuthorGroupListResponse
    {
        /// <summary>
        /// Gets or sets the list of author groups.
        /// </summary>
        [JsonPropertyName("data")]
        public List<AuthorGroup> Data { get; set; } = new List<AuthorGroup>();

        /// <summary>
        /// Gets or sets the success status of the operation.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any error message if the operation failed.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional error details.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets pagination information.
        /// </summary>
        [JsonPropertyName("pagination")]
        public PaginationInfo? Pagination { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for author operations within a group.
    /// </summary>
    public class AuthorFromGroupResponse
    {
        /// <summary>
        /// Gets or sets the author data.
        /// </summary>
        [JsonPropertyName("data")]
        public Author? Data { get; set; }

        /// <summary>
        /// Gets or sets the success status of the operation.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any error message if the operation failed.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional error details.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Pagination information for list responses.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets whether there is a next page.
        /// </summary>
        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        /// <summary>
        /// Gets or sets whether there is a previous page.
        /// </summary>
        [JsonPropertyName("has_previous")]
        public bool HasPrevious { get; set; }
    }
} 