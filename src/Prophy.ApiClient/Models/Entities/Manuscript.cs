using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Entities
{
    /// <summary>
    /// Represents a manuscript with metadata, content, and processing status.
    /// </summary>
    public class Manuscript
    {
        /// <summary>
        /// Gets or sets the unique identifier for the manuscript.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

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
        /// Gets or sets the list of authors for the manuscript.
        /// </summary>
        [JsonPropertyName("authors")]
        public List<Author>? Authors { get; set; }

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
        /// Gets or sets the target journal or folder for the manuscript.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets the manuscript processing status.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the origin ID for tracking purposes.
        /// </summary>
        [JsonPropertyName("originId")]
        public string? OriginId { get; set; }

        /// <summary>
        /// Gets or sets the file name of the uploaded manuscript.
        /// </summary>
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the uploaded file.
        /// </summary>
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

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
        /// Gets or sets debug information from processing.
        /// </summary>
        [JsonPropertyName("debugInfo")]
        public Dictionary<string, object>? DebugInfo { get; set; }

        /// <summary>
        /// Gets or sets the date when the manuscript was submitted.
        /// </summary>
        [JsonPropertyName("submissionDate")]
        public DateTime? SubmissionDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the manuscript was created in the system.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the manuscript was last updated.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the manuscript processing was completed.
        /// </summary>
        [JsonPropertyName("processedAt")]
        public DateTime? ProcessedAt { get; set; }
    }
} 