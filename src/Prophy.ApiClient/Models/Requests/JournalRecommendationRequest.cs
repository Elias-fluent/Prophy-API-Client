using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Represents a request for journal recommendations from the Prophy API.
    /// </summary>
    public class JournalRecommendationRequest
    {
        /// <summary>
        /// Gets or sets the manuscript ID for which to get journal recommendations.
        /// </summary>
        [Required]
        [JsonPropertyName("manuscriptId")]
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of journal recommendations to return.
        /// </summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets the minimum relevance score threshold for recommendations.
        /// </summary>
        [JsonPropertyName("minRelevanceScore")]
        public double? MinRelevanceScore { get; set; }

        /// <summary>
        /// Gets or sets whether to include open access journals only.
        /// </summary>
        [JsonPropertyName("openAccessOnly")]
        public bool? OpenAccessOnly { get; set; }

        /// <summary>
        /// Gets or sets the minimum impact factor for journal recommendations.
        /// </summary>
        [JsonPropertyName("minImpactFactor")]
        public double? MinImpactFactor { get; set; }

        /// <summary>
        /// Gets or sets the maximum impact factor for journal recommendations.
        /// </summary>
        [JsonPropertyName("maxImpactFactor")]
        public double? MaxImpactFactor { get; set; }

        /// <summary>
        /// Gets or sets specific subject areas to filter recommendations.
        /// </summary>
        [JsonPropertyName("subjectAreas")]
        public List<string>? SubjectAreas { get; set; }

        /// <summary>
        /// Gets or sets specific publishers to include in recommendations.
        /// </summary>
        [JsonPropertyName("publishers")]
        public List<string>? Publishers { get; set; }

        /// <summary>
        /// Gets or sets specific publishers to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludePublishers")]
        public List<string>? ExcludePublishers { get; set; }

        /// <summary>
        /// Gets or sets specific journals to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludeJournals")]
        public List<string>? ExcludeJournals { get; set; }

        /// <summary>
        /// Gets or sets whether to include related articles in the response.
        /// </summary>
        [JsonPropertyName("includeRelatedArticles")]
        public bool? IncludeRelatedArticles { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of related articles per journal.
        /// </summary>
        [JsonPropertyName("maxRelatedArticles")]
        public int? MaxRelatedArticles { get; set; }

        /// <summary>
        /// Gets or sets additional filters for the recommendation request.
        /// </summary>
        [JsonPropertyName("filters")]
        public Dictionary<string, object>? Filters { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the request.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }
} 