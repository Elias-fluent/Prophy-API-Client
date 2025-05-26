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
        /// Gets or sets the list of author names.
        /// </summary>
        [JsonPropertyName("authors")]
        public List<string>? Authors { get; set; }

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
        /// Gets or sets the origin ID for tracking purposes.
        /// </summary>
        [JsonPropertyName("originId")]
        public string? OriginId { get; set; }

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
    }
} 