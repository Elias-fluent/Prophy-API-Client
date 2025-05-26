using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Entities
{
    /// <summary>
    /// Represents a journal with metadata, relevance scoring, and related articles.
    /// </summary>
    public class Journal
    {
        /// <summary>
        /// Gets or sets the unique identifier for the journal.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the journal title.
        /// </summary>
        [Required]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the journal's abbreviated title.
        /// </summary>
        [JsonPropertyName("abbreviatedTitle")]
        public string? AbbreviatedTitle { get; set; }

        /// <summary>
        /// Gets or sets the journal's ISSN.
        /// </summary>
        [JsonPropertyName("issn")]
        public string? Issn { get; set; }

        /// <summary>
        /// Gets or sets the journal's electronic ISSN.
        /// </summary>
        [JsonPropertyName("eIssn")]
        public string? EIssn { get; set; }

        /// <summary>
        /// Gets or sets the publisher name.
        /// </summary>
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        /// <summary>
        /// Gets or sets the journal's website URL.
        /// </summary>
        [Url]
        [JsonPropertyName("website")]
        public string? Website { get; set; }

        /// <summary>
        /// Gets or sets the journal's description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the journal's subject areas or categories.
        /// </summary>
        [JsonPropertyName("subjectAreas")]
        public List<string>? SubjectAreas { get; set; }

        /// <summary>
        /// Gets or sets the journal's keywords.
        /// </summary>
        [JsonPropertyName("keywords")]
        public List<string>? Keywords { get; set; }

        /// <summary>
        /// Gets or sets the journal's impact factor.
        /// </summary>
        [JsonPropertyName("impactFactor")]
        public double? ImpactFactor { get; set; }

        /// <summary>
        /// Gets or sets the journal's h-index.
        /// </summary>
        [JsonPropertyName("hIndex")]
        public int? HIndex { get; set; }

        /// <summary>
        /// Gets or sets the journal's citation count.
        /// </summary>
        [JsonPropertyName("citationCount")]
        public int? CitationCount { get; set; }

        /// <summary>
        /// Gets or sets the relevance score for this journal recommendation.
        /// </summary>
        [JsonPropertyName("relevanceScore")]
        public double? RelevanceScore { get; set; }

        /// <summary>
        /// Gets or sets the ranking position of this journal recommendation.
        /// </summary>
        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        /// <summary>
        /// Gets or sets whether this journal is open access.
        /// </summary>
        [JsonPropertyName("isOpenAccess")]
        public bool? IsOpenAccess { get; set; }

        /// <summary>
        /// Gets or sets the journal's peer review type.
        /// </summary>
        [JsonPropertyName("peerReviewType")]
        public string? PeerReviewType { get; set; }

        /// <summary>
        /// Gets or sets the journal's publication frequency.
        /// </summary>
        [JsonPropertyName("publicationFrequency")]
        public string? PublicationFrequency { get; set; }

        /// <summary>
        /// Gets or sets the journal's acceptance rate.
        /// </summary>
        [JsonPropertyName("acceptanceRate")]
        public double? AcceptanceRate { get; set; }

        /// <summary>
        /// Gets or sets the average time to first decision.
        /// </summary>
        [JsonPropertyName("timeToFirstDecision")]
        public int? TimeToFirstDecision { get; set; }

        /// <summary>
        /// Gets or sets the average time to publication.
        /// </summary>
        [JsonPropertyName("timeToPublication")]
        public int? TimeToPublication { get; set; }

        /// <summary>
        /// Gets or sets related articles for this journal recommendation.
        /// </summary>
        [JsonPropertyName("relatedArticles")]
        public List<RelatedArticle>? RelatedArticles { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the journal.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the date when the journal record was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the journal record was last updated.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents a related article for a journal recommendation.
    /// </summary>
    public class RelatedArticle
    {
        /// <summary>
        /// Gets or sets the article title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the article authors.
        /// </summary>
        [JsonPropertyName("authors")]
        public List<string>? Authors { get; set; }

        /// <summary>
        /// Gets or sets the article DOI.
        /// </summary>
        [JsonPropertyName("doi")]
        public string? Doi { get; set; }

        /// <summary>
        /// Gets or sets the article URL.
        /// </summary>
        [Url]
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the article publication date.
        /// </summary>
        [JsonPropertyName("publicationDate")]
        public DateTime? PublicationDate { get; set; }

        /// <summary>
        /// Gets or sets the article abstract.
        /// </summary>
        [JsonPropertyName("abstract")]
        public string? Abstract { get; set; }

        /// <summary>
        /// Gets or sets the relevance score of this article.
        /// </summary>
        [JsonPropertyName("relevanceScore")]
        public double? RelevanceScore { get; set; }
    }
} 