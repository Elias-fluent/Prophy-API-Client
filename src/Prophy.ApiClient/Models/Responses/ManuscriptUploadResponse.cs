using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Responses
{
    /// <summary>
    /// Represents the response from a manuscript upload request.
    /// </summary>
    public class ManuscriptUploadResponse
    {
        /// <summary>
        /// Gets or sets the manuscript ID assigned by Prophy.
        /// </summary>
        [JsonPropertyName("manuscript_id")]
        public object? ManuscriptId { get; set; }

        /// <summary>
        /// Gets the manuscript ID as a string.
        /// </summary>
        [JsonIgnore]
        public string? ManuscriptIdString => ManuscriptId?.ToString();

        /// <summary>
        /// Gets or sets the origin ID that was provided in the request.
        /// </summary>
        [JsonPropertyName("origin_id")]
        public string? OriginId { get; set; }

        /// <summary>
        /// Gets or sets debug information from the upload process.
        /// </summary>
        [JsonPropertyName("debug_info")]
        public DebugInfo? DebugInfo { get; set; }

        /// <summary>
        /// Gets or sets the authors groups settings.
        /// </summary>
        [JsonPropertyName("authors_groups_settings")]
        public AuthorsGroupsSettings? AuthorsGroupsSettings { get; set; }

        /// <summary>
        /// Gets or sets the list of referee candidates.
        /// </summary>
        [JsonPropertyName("candidates")]
        public List<RefereeCandidate> Candidates { get; set; } = new List<RefereeCandidate>();

        // Legacy properties for backward compatibility
        /// <summary>
        /// Gets or sets whether the upload was successful.
        /// </summary>
        [JsonIgnore]
        public bool Success => ManuscriptId != null && !string.IsNullOrEmpty(ManuscriptId.ToString());

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        [JsonIgnore]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the uploaded manuscript data.
        /// </summary>
        [JsonIgnore]
        public Manuscript? Manuscript { get; set; }

        /// <summary>
        /// Gets or sets the processing status of the manuscript.
        /// </summary>
        [JsonIgnore]
        public string? ProcessingStatus { get; set; }

        /// <summary>
        /// Gets or sets the estimated processing time in minutes.
        /// </summary>
        [JsonIgnore]
        public int? EstimatedProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during upload.
        /// </summary>
        [JsonIgnore]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets any warnings that occurred during upload.
        /// </summary>
        [JsonIgnore]
        public List<string>? Warnings { get; set; }

        /// <summary>
        /// Gets or sets additional metadata from the response.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated.
        /// </summary>
        [JsonIgnore]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request ID for tracking purposes.
        /// </summary>
        [JsonIgnore]
        public string? RequestId { get; set; }
    }

    /// <summary>
    /// Represents debug information from the manuscript upload process.
    /// </summary>
    public class DebugInfo
    {
        /// <summary>
        /// Gets or sets information about the authors.
        /// </summary>
        [JsonPropertyName("authors_info")]
        public AuthorsInfo? AuthorsInfo { get; set; }

        /// <summary>
        /// Gets or sets the number of extracted concepts.
        /// </summary>
        [JsonPropertyName("extracted_concepts")]
        public int ExtractedConcepts { get; set; }

        /// <summary>
        /// Gets or sets the number of parsed references.
        /// </summary>
        [JsonPropertyName("parsed_references")]
        public int ParsedReferences { get; set; }

        /// <summary>
        /// Gets or sets the length of parsed text.
        /// </summary>
        [JsonPropertyName("parsed_text_len")]
        public int ParsedTextLen { get; set; }

        /// <summary>
        /// Gets or sets the source file name.
        /// </summary>
        [JsonPropertyName("source_file")]
        public string? SourceFile { get; set; }
    }

    /// <summary>
    /// Represents information about authors in the debug info.
    /// </summary>
    public class AuthorsInfo
    {
        /// <summary>
        /// Gets or sets the number of authors.
        /// </summary>
        [JsonPropertyName("authors_count")]
        public int AuthorsCount { get; set; }

        /// <summary>
        /// Gets or sets the number of email addresses.
        /// </summary>
        [JsonPropertyName("emails_count")]
        public int EmailsCount { get; set; }

        /// <summary>
        /// Gets or sets the number of ORCID identifiers.
        /// </summary>
        [JsonPropertyName("orcids_count")]
        public int OrcidsCount { get; set; }
    }

    /// <summary>
    /// Represents authors groups settings in the response.
    /// </summary>
    public class AuthorsGroupsSettings
    {
        /// <summary>
        /// Gets or sets the effect of the authors groups.
        /// </summary>
        [JsonPropertyName("effect")]
        public string? Effect { get; set; }

        /// <summary>
        /// Gets or sets the list of authors groups.
        /// </summary>
        [JsonPropertyName("authors_groups")]
        public List<AuthorsGroup> AuthorsGroups { get; set; } = new List<AuthorsGroup>();
    }

    /// <summary>
    /// Represents an authors group.
    /// </summary>
    public class AuthorsGroup
    {
        /// <summary>
        /// Gets or sets the group ID.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the group label.
        /// </summary>
        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }
} 