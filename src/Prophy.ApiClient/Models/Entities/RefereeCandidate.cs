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
        /// Gets or sets the unique identifier for the referee candidate.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the author information for the referee candidate.
        /// </summary>
        [JsonPropertyName("author")]
        public Author? Author { get; set; }

        /// <summary>
        /// Gets or sets the relevance score for this referee candidate.
        /// </summary>
        [JsonPropertyName("relevanceScore")]
        public double? RelevanceScore { get; set; }

        /// <summary>
        /// Gets or sets the expertise score for this referee candidate.
        /// </summary>
        [JsonPropertyName("expertiseScore")]
        public double? ExpertiseScore { get; set; }

        /// <summary>
        /// Gets or sets the overall score for this referee candidate.
        /// </summary>
        [JsonPropertyName("overallScore")]
        public double? OverallScore { get; set; }

        /// <summary>
        /// Gets or sets the ranking position of this candidate.
        /// </summary>
        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        /// <summary>
        /// Gets or sets whether this candidate has a conflict of interest.
        /// </summary>
        [JsonPropertyName("hasConflictOfInterest")]
        public bool? HasConflictOfInterest { get; set; }

        /// <summary>
        /// Gets or sets the type of conflict of interest if any.
        /// </summary>
        [JsonPropertyName("conflictType")]
        public string? ConflictType { get; set; }

        /// <summary>
        /// Gets or sets the reason for the conflict of interest.
        /// </summary>
        [JsonPropertyName("conflictReason")]
        public string? ConflictReason { get; set; }

        /// <summary>
        /// Gets or sets whether this candidate is excluded from consideration.
        /// </summary>
        [JsonPropertyName("isExcluded")]
        public bool? IsExcluded { get; set; }

        /// <summary>
        /// Gets or sets the reason for exclusion if applicable.
        /// </summary>
        [JsonPropertyName("exclusionReason")]
        public string? ExclusionReason { get; set; }

        /// <summary>
        /// Gets or sets the candidate's availability status.
        /// </summary>
        [JsonPropertyName("availability")]
        public string? Availability { get; set; }

        /// <summary>
        /// Gets or sets the candidate's response to the review invitation.
        /// </summary>
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        /// <summary>
        /// Gets or sets the date when the invitation was sent.
        /// </summary>
        [JsonPropertyName("invitationSentAt")]
        public DateTime? InvitationSentAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the candidate responded.
        /// </summary>
        [JsonPropertyName("respondedAt")]
        public DateTime? RespondedAt { get; set; }

        /// <summary>
        /// Gets or sets the candidate's review deadline.
        /// </summary>
        [JsonPropertyName("reviewDeadline")]
        public DateTime? ReviewDeadline { get; set; }

        /// <summary>
        /// Gets or sets the candidate's expertise areas relevant to the manuscript.
        /// </summary>
        [JsonPropertyName("expertiseAreas")]
        public List<string>? ExpertiseAreas { get; set; }

        /// <summary>
        /// Gets or sets the candidate's recent publications relevant to the manuscript.
        /// </summary>
        [JsonPropertyName("relevantPublications")]
        public List<string>? RelevantPublications { get; set; }

        /// <summary>
        /// Gets or sets additional notes about the candidate.
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the referee candidate.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the date when the candidate record was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the candidate record was last updated.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
} 