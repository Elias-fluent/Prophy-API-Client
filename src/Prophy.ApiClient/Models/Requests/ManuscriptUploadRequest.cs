using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Represents a request to upload a manuscript to the Prophy API.
    /// </summary>
    public class ManuscriptUploadRequest
    {
        /// <summary>
        /// Gets or sets the manuscript title.
        /// </summary>
        [Required]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the manuscript abstract.
        /// </summary>
        [JsonPropertyName("abstract")]
        public string? Abstract { get; set; }

        /// <summary>
        /// Gets or sets the target journal or folder for the manuscript.
        /// </summary>
        [JsonPropertyName("journal")]
        public string? Journal { get; set; }

        /// <summary>
        /// Gets or sets the origin ID for tracking purposes.
        /// </summary>
        [JsonPropertyName("origin_id")]
        public string? OriginId { get; set; }

        /// <summary>
        /// Gets or sets the number of authors.
        /// </summary>
        [JsonPropertyName("authors_count")]
        public int AuthorsCount { get; set; }

        /// <summary>
        /// Gets or sets the list of author names.
        /// </summary>
        [JsonPropertyName("author_names")]
        public List<string>? AuthorNames { get; set; }

        /// <summary>
        /// Gets or sets the list of author email addresses.
        /// </summary>
        [JsonPropertyName("author_emails")]
        public List<string>? AuthorEmails { get; set; }

        /// <summary>
        /// Gets or sets the source file name.
        /// </summary>
        [JsonPropertyName("source_file_name")]
        public string? SourceFileName { get; set; }

        /// <summary>
        /// Gets or sets the manuscript keywords.
        /// </summary>
        [JsonPropertyName("keywords")]
        public List<string>? Keywords { get; set; }

        /// <summary>
        /// Gets or sets the manuscript subject area or field.
        /// </summary>
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the manuscript type (e.g., research article, review, etc.).
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the target folder for the manuscript (alias for Journal).
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder 
        { 
            get => Journal; 
            set => Journal = value; 
        }

        /// <summary>
        /// Gets or sets the language of the manuscript.
        /// </summary>
        [JsonPropertyName("language")]
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets custom fields specific to the organization.
        /// </summary>
        [JsonPropertyName("customFields")]
        public Dictionary<string, object>? CustomFields { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the manuscript.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the file content as a byte array.
        /// This is used when uploading the file content directly.
        /// </summary>
        [JsonIgnore]
        public byte[]? FileContent { get; set; }

        /// <summary>
        /// Gets or sets the file name of the manuscript.
        /// </summary>
        [JsonIgnore]
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// </summary>
        [JsonIgnore]
        public string? MimeType { get; set; }

        // Legacy properties for backward compatibility
        /// <summary>
        /// Gets or sets the list of author names (legacy format).
        /// </summary>
        [JsonIgnore]
        public List<string>? Authors 
        { 
            get => AuthorNames; 
            set => AuthorNames = value; 
        }

        /// <summary>
        /// Gets or sets the minimum h-index for candidate filtering.
        /// </summary>
        [JsonPropertyName("min_h_index")]
        public int? MinHIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum h-index for candidate filtering.
        /// </summary>
        [JsonPropertyName("max_h_index")]
        public int? MaxHIndex { get; set; }

        /// <summary>
        /// Gets or sets the minimum academic age for candidate filtering.
        /// </summary>
        [JsonPropertyName("min_academic_age")]
        public int? MinAcademicAge { get; set; }

        /// <summary>
        /// Gets or sets the maximum academic age for candidate filtering.
        /// </summary>
        [JsonPropertyName("max_academic_age")]
        public int? MaxAcademicAge { get; set; }

        /// <summary>
        /// Gets or sets the minimum articles count for candidate filtering.
        /// </summary>
        [JsonPropertyName("min_articles_count")]
        public int? MinArticlesCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum articles count for candidate filtering.
        /// </summary>
        [JsonPropertyName("max_articles_count")]
        public int? MaxArticlesCount { get; set; }

        /// <summary>
        /// Gets or sets whether to exclude candidates from the response.
        /// </summary>
        [JsonPropertyName("exclude_candidates")]
        public bool ExcludeCandidates { get; set; }
    }
} 