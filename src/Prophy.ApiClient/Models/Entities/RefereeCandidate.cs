using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Entities
{
    /// <summary>
    /// Represents a referee candidate with scoring and conflict of interest information.
    /// </summary>
    public class RefereeCandidate
    {
        /// <summary>
        /// Gets or sets the candidate's name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the candidate's email address.
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the relevance score for this referee candidate.
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; }

        /// <summary>
        /// Gets or sets the candidate's H-index.
        /// </summary>
        [JsonPropertyName("h_index")]
        public int HIndex { get; set; }

        /// <summary>
        /// Gets or sets the number of articles published by the candidate.
        /// </summary>
        [JsonPropertyName("articles_count")]
        public int ArticlesCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of citations for the candidate.
        /// </summary>
        [JsonPropertyName("citations_count")]
        public int CitationsCount { get; set; }

        /// <summary>
        /// Gets or sets the candidate's primary affiliation.
        /// </summary>
        [JsonPropertyName("affiliation")]
        public string? Affiliation { get; set; }

        /// <summary>
        /// Gets or sets the candidate's ORCID identifier.
        /// </summary>
        [JsonPropertyName("orcid")]
        public string? Orcid { get; set; }

        /// <summary>
        /// Gets or sets the candidate's country.
        /// </summary>
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets whether this candidate has a conflict of interest.
        /// </summary>
        [JsonPropertyName("has_conflict_of_interest")]
        public bool? HasConflictOfInterest { get; set; }

        /// <summary>
        /// Gets or sets the type of conflict of interest if any.
        /// </summary>
        [JsonPropertyName("conflict_type")]
        public string? ConflictType { get; set; }

        /// <summary>
        /// Gets or sets the reason for the conflict of interest.
        /// </summary>
        [JsonPropertyName("conflict_reason")]
        public string? ConflictReason { get; set; }

        /// <summary>
        /// Gets or sets whether this candidate is excluded from consideration.
        /// </summary>
        [JsonPropertyName("is_excluded")]
        public bool? IsExcluded { get; set; }

        /// <summary>
        /// Gets or sets the reason for exclusion if applicable.
        /// </summary>
        [JsonPropertyName("exclusion_reason")]
        public string? ExclusionReason { get; set; }

        /// <summary>
        /// Gets or sets the candidate's expertise areas relevant to the manuscript.
        /// </summary>
        [JsonPropertyName("expertise_areas")]
        public List<string>? ExpertiseAreas { get; set; }

        /// <summary>
        /// Gets or sets the candidate's recent publications relevant to the manuscript.
        /// </summary>
        [JsonPropertyName("relevant_publications")]
        public List<string>? RelevantPublications { get; set; }

        // Legacy properties for backward compatibility
        /// <summary>
        /// Gets or sets the unique identifier for the referee candidate.
        /// </summary>
        [JsonIgnore]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the author information for the referee candidate.
        /// </summary>
        [JsonIgnore]
        public Author? Author { get; set; }

        /// <summary>
        /// Gets or sets the relevance score for this referee candidate (legacy).
        /// </summary>
        [JsonIgnore]
        public double? RelevanceScore 
        { 
            get => Score; 
            set => Score = value ?? 0; 
        }

        /// <summary>
        /// Gets or sets the expertise score for this referee candidate.
        /// </summary>
        [JsonIgnore]
        public double? ExpertiseScore { get; set; }

        /// <summary>
        /// Gets or sets the overall score for this referee candidate (legacy).
        /// </summary>
        [JsonIgnore]
        public double? OverallScore 
        { 
            get => Score; 
            set => Score = value ?? 0; 
        }

        /// <summary>
        /// Gets or sets the ranking position of this candidate.
        /// </summary>
        [JsonIgnore]
        public int? Rank { get; set; }

        /// <summary>
        /// Gets or sets the candidate's availability status.
        /// </summary>
        [JsonIgnore]
        public string? Availability { get; set; }

        /// <summary>
        /// Gets or sets the candidate's response to the review invitation.
        /// </summary>
        [JsonIgnore]
        public string? Response { get; set; }

        /// <summary>
        /// Gets or sets the date when the invitation was sent.
        /// </summary>
        [JsonIgnore]
        public DateTime? InvitationSentAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the candidate responded.
        /// </summary>
        [JsonIgnore]
        public DateTime? RespondedAt { get; set; }

        /// <summary>
        /// Gets or sets the candidate's review deadline.
        /// </summary>
        [JsonIgnore]
        public DateTime? ReviewDeadline { get; set; }

        /// <summary>
        /// Gets or sets additional notes about the candidate.
        /// </summary>
        [JsonIgnore]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the referee candidate.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the date when the candidate record was created.
        /// </summary>
        [JsonIgnore]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the candidate record was last updated.
        /// </summary>
        [JsonIgnore]
        public DateTime? UpdatedAt { get; set; }
    }
} 