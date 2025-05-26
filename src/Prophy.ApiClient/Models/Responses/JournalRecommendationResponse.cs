using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Responses
{
    /// <summary>
    /// Represents the response from a journal recommendation request.
    /// </summary>
    public class JournalRecommendationResponse
    {
        /// <summary>
        /// Gets or sets whether the recommendation request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the manuscript ID for which recommendations were generated.
        /// </summary>
        [JsonPropertyName("manuscriptId")]
        public string? ManuscriptId { get; set; }

        /// <summary>
        /// Gets or sets the list of recommended journals.
        /// </summary>
        [JsonPropertyName("recommendations")]
        public List<Journal>? Recommendations { get; set; }

        /// <summary>
        /// Gets or sets the total number of recommendations found.
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the number of recommendations returned.
        /// </summary>
        [JsonPropertyName("returnedCount")]
        public int? ReturnedCount { get; set; }

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        [JsonPropertyName("processingTimeMs")]
        public long? ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the filters applied to the recommendations.
        /// </summary>
        [JsonPropertyName("appliedFilters")]
        public Dictionary<string, object>? AppliedFilters { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during recommendation generation.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets any warnings that occurred during recommendation generation.
        /// </summary>
        [JsonPropertyName("warnings")]
        public List<string>? Warnings { get; set; }

        /// <summary>
        /// Gets or sets debug information from the recommendation process.
        /// </summary>
        [JsonPropertyName("debugInfo")]
        public Dictionary<string, object>? DebugInfo { get; set; }

        /// <summary>
        /// Gets or sets additional metadata from the response.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request ID for tracking purposes.
        /// </summary>
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }
} 