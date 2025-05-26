using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Represents a request for referee recommendations from the Prophy API.
    /// </summary>
    public class RefereeRecommendationRequest
    {
        /// <summary>
        /// Gets or sets the manuscript ID for which to get referee recommendations.
        /// </summary>
        [Required]
        [JsonPropertyName("manuscriptId")]
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of referee recommendations to return.
        /// </summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets the minimum relevance score threshold for recommendations.
        /// </summary>
        [JsonPropertyName("minRelevanceScore")]
        public double? MinRelevanceScore { get; set; }

        /// <summary>
        /// Gets or sets the minimum expertise score threshold for recommendations.
        /// </summary>
        [JsonPropertyName("minExpertiseScore")]
        public double? MinExpertiseScore { get; set; }

        /// <summary>
        /// Gets or sets whether to exclude authors with conflicts of interest.
        /// </summary>
        [JsonPropertyName("excludeConflicts")]
        public bool? ExcludeConflicts { get; set; }

        /// <summary>
        /// Gets or sets the minimum h-index for referee candidates.
        /// </summary>
        [JsonPropertyName("minHIndex")]
        public int? MinHIndex { get; set; }

        /// <summary>
        /// Gets or sets the minimum citation count for referee candidates.
        /// </summary>
        [JsonPropertyName("minCitationCount")]
        public int? MinCitationCount { get; set; }

        /// <summary>
        /// Gets or sets the minimum publication count for referee candidates.
        /// </summary>
        [JsonPropertyName("minPublicationCount")]
        public int? MinPublicationCount { get; set; }

        /// <summary>
        /// Gets or sets specific countries to include in recommendations.
        /// </summary>
        [JsonPropertyName("countries")]
        public List<string>? Countries { get; set; }

        /// <summary>
        /// Gets or sets specific countries to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludeCountries")]
        public List<string>? ExcludeCountries { get; set; }

        /// <summary>
        /// Gets or sets specific institutions to include in recommendations.
        /// </summary>
        [JsonPropertyName("institutions")]
        public List<string>? Institutions { get; set; }

        /// <summary>
        /// Gets or sets specific institutions to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludeInstitutions")]
        public List<string>? ExcludeInstitutions { get; set; }

        /// <summary>
        /// Gets or sets specific authors to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludeAuthors")]
        public List<string>? ExcludeAuthors { get; set; }

        /// <summary>
        /// Gets or sets specific ORCID IDs to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludeOrcids")]
        public List<string>? ExcludeOrcids { get; set; }

        /// <summary>
        /// Gets or sets specific email addresses to exclude from recommendations.
        /// </summary>
        [JsonPropertyName("excludeEmails")]
        public List<string>? ExcludeEmails { get; set; }

        /// <summary>
        /// Gets or sets required expertise areas for referee candidates.
        /// </summary>
        [JsonPropertyName("requiredExpertise")]
        public List<string>? RequiredExpertise { get; set; }

        /// <summary>
        /// Gets or sets preferred expertise areas for referee candidates.
        /// </summary>
        [JsonPropertyName("preferredExpertise")]
        public List<string>? PreferredExpertise { get; set; }

        /// <summary>
        /// Gets or sets whether to include conflict of interest analysis.
        /// </summary>
        [JsonPropertyName("includeConflictAnalysis")]
        public bool? IncludeConflictAnalysis { get; set; }

        /// <summary>
        /// Gets or sets whether to include relevant publications for each candidate.
        /// </summary>
        [JsonPropertyName("includeRelevantPublications")]
        public bool? IncludeRelevantPublications { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of relevant publications per candidate.
        /// </summary>
        [JsonPropertyName("maxRelevantPublications")]
        public int? MaxRelevantPublications { get; set; }

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