using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Responses
{
    /// <summary>
    /// Represents the response from a referee recommendation request.
    /// </summary>
    public class RefereeRecommendationResponse
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
        /// Gets or sets the list of recommended referee candidates.
        /// </summary>
        [JsonPropertyName("recommendations")]
        public List<RefereeCandidate>? Recommendations { get; set; }

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
        /// Gets or sets conflict of interest analysis results.
        /// </summary>
        [JsonPropertyName("conflictAnalysis")]
        public ConflictAnalysisResult? ConflictAnalysis { get; set; }

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

    /// <summary>
    /// Represents the results of conflict of interest analysis.
    /// </summary>
    public class ConflictAnalysisResult
    {
        /// <summary>
        /// Gets or sets the total number of candidates analyzed.
        /// </summary>
        [JsonPropertyName("totalCandidates")]
        public int? TotalCandidates { get; set; }

        /// <summary>
        /// Gets or sets the number of candidates with conflicts.
        /// </summary>
        [JsonPropertyName("candidatesWithConflicts")]
        public int? CandidatesWithConflicts { get; set; }

        /// <summary>
        /// Gets or sets the number of candidates excluded due to conflicts.
        /// </summary>
        [JsonPropertyName("candidatesExcluded")]
        public int? CandidatesExcluded { get; set; }

        /// <summary>
        /// Gets or sets the conflict types found and their counts.
        /// </summary>
        [JsonPropertyName("conflictTypes")]
        public Dictionary<string, int>? ConflictTypes { get; set; }

        /// <summary>
        /// Gets or sets detailed conflict information.
        /// </summary>
        [JsonPropertyName("conflictDetails")]
        public List<ConflictDetail>? ConflictDetails { get; set; }
    }

    /// <summary>
    /// Represents detailed information about a specific conflict.
    /// </summary>
    public class ConflictDetail
    {
        /// <summary>
        /// Gets or sets the candidate ID with the conflict.
        /// </summary>
        [JsonPropertyName("candidateId")]
        public string? CandidateId { get; set; }

        /// <summary>
        /// Gets or sets the candidate name.
        /// </summary>
        [JsonPropertyName("candidateName")]
        public string? CandidateName { get; set; }

        /// <summary>
        /// Gets or sets the type of conflict.
        /// </summary>
        [JsonPropertyName("conflictType")]
        public string? ConflictType { get; set; }

        /// <summary>
        /// Gets or sets the reason for the conflict.
        /// </summary>
        [JsonPropertyName("conflictReason")]
        public string? ConflictReason { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the conflict detection.
        /// </summary>
        [JsonPropertyName("confidenceScore")]
        public double? ConfidenceScore { get; set; }
    }
} 