using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the data payload for a referee recommendations generated webhook event.
    /// This event is triggered when referee recommendations are generated for a manuscript.
    /// </summary>
    public class RefereeRecommendationsGeneratedEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_id")]
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_title")]
        public string ManuscriptTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when recommendations were generated.
        /// </summary>
        [JsonPropertyName("generated_at")]
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who requested the recommendations.
        /// </summary>
        [JsonPropertyName("requested_by")]
        public string RequestedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of recommendations generated.
        /// </summary>
        [JsonPropertyName("total_recommendations")]
        public int TotalRecommendations { get; set; }

        /// <summary>
        /// Gets or sets the number of high-quality recommendations.
        /// </summary>
        [JsonPropertyName("high_quality_count")]
        public int HighQualityCount { get; set; }

        /// <summary>
        /// Gets or sets the list of recommended referee candidates.
        /// </summary>
        [JsonPropertyName("recommendations")]
        public List<RefereeCandidate>? Recommendations { get; set; }

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        [JsonPropertyName("processing_time_ms")]
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the folder/journal associated with the manuscript.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets the search parameters used for generating recommendations.
        /// </summary>
        [JsonPropertyName("search_parameters")]
        public Dictionary<string, object>? SearchParameters { get; set; }

        /// <summary>
        /// Gets or sets the minimum relevance score threshold used.
        /// </summary>
        [JsonPropertyName("min_relevance_score")]
        public double? MinRelevanceScore { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of recommendations requested.
        /// </summary>
        [JsonPropertyName("max_recommendations")]
        public int? MaxRecommendations { get; set; }

        /// <summary>
        /// Gets or sets whether conflict of interest filtering was applied.
        /// </summary>
        [JsonPropertyName("coi_filtering")]
        public bool ConflictOfInterestFiltering { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of candidates excluded due to conflicts of interest.
        /// </summary>
        [JsonPropertyName("coi_excluded_count")]
        public int ConflictOfInterestExcludedCount { get; set; }

        /// <summary>
        /// Gets or sets any error messages or warnings from the recommendation process.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<string>? Messages { get; set; }

        /// <summary>
        /// Gets or sets whether this was an automatic or manual recommendation request.
        /// </summary>
        [JsonPropertyName("automatic")]
        public bool IsAutomatic { get; set; } = false;
    }
} 