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
        /// Gets or sets whether the upload was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the uploaded manuscript data.
        /// </summary>
        [JsonPropertyName("manuscript")]
        public Manuscript? Manuscript { get; set; }

        /// <summary>
        /// Gets or sets the processing status of the manuscript.
        /// </summary>
        [JsonPropertyName("processingStatus")]
        public string? ProcessingStatus { get; set; }

        /// <summary>
        /// Gets or sets the estimated processing time in minutes.
        /// </summary>
        [JsonPropertyName("estimatedProcessingTime")]
        public int? EstimatedProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during upload.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets any warnings that occurred during upload.
        /// </summary>
        [JsonPropertyName("warnings")]
        public List<string>? Warnings { get; set; }

        /// <summary>
        /// Gets or sets debug information from the upload process.
        /// </summary>
        [JsonPropertyName("debugInfo")]
        public Dictionary<string, object>? DebugInfo { get; set; }

        /// <summary>
        /// Gets or sets additional metadata from the response.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request ID for tracking purposes.
        /// </summary>
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }
} 