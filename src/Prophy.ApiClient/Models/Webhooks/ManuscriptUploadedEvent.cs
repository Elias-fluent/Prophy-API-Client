using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the data payload for a manuscript upload webhook event.
    /// This event is triggered when a new manuscript is uploaded to the system.
    /// </summary>
    public class ManuscriptUploadedEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the uploaded manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_id")]
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the uploaded manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_title")]
        public string ManuscriptTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the abstract of the manuscript.
        /// </summary>
        [JsonPropertyName("abstract")]
        public string? Abstract { get; set; }

        /// <summary>
        /// Gets or sets the list of authors for the manuscript.
        /// </summary>
        [JsonPropertyName("authors")]
        public List<Author>? Authors { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the manuscript was uploaded.
        /// </summary>
        [JsonPropertyName("uploaded_at")]
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who uploaded the manuscript.
        /// </summary>
        [JsonPropertyName("uploaded_by")]
        public string UploadedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the folder/journal where the manuscript was uploaded.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets the file name of the uploaded manuscript.
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the file type/format of the manuscript.
        /// </summary>
        [JsonPropertyName("file_type")]
        public string? FileType { get; set; }

        /// <summary>
        /// Gets or sets the initial status of the manuscript after upload.
        /// </summary>
        [JsonPropertyName("initial_status")]
        public string InitialStatus { get; set; } = "uploaded";

        /// <summary>
        /// Gets or sets any custom fields associated with the manuscript.
        /// </summary>
        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object>? CustomFields { get; set; }

        /// <summary>
        /// Gets or sets whether referee recommendations were automatically generated.
        /// </summary>
        [JsonPropertyName("auto_recommendations")]
        public bool AutoRecommendations { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of referee candidates found (if auto-recommendations enabled).
        /// </summary>
        [JsonPropertyName("candidates_found")]
        public int? CandidatesFound { get; set; }

        /// <summary>
        /// Gets or sets any processing notes or messages.
        /// </summary>
        [JsonPropertyName("processing_notes")]
        public string? ProcessingNotes { get; set; }
    }
} 