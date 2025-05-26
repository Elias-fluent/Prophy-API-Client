using System;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the data payload for a manuscript status change webhook event.
    /// This event is triggered when a manuscript's status changes in the system.
    /// </summary>
    public class ManuscriptStatusChangedEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_id")]
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_title")]
        public string ManuscriptTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the previous status of the manuscript.
        /// </summary>
        [JsonPropertyName("previous_status")]
        public string PreviousStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new status of the manuscript.
        /// </summary>
        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the status changed.
        /// </summary>
        [JsonPropertyName("changed_at")]
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who initiated the status change.
        /// </summary>
        [JsonPropertyName("changed_by")]
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for the status change.
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the folder/journal associated with the manuscript.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets additional details about the status change.
        /// </summary>
        [JsonPropertyName("details")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets whether this status change requires action from external systems.
        /// </summary>
        [JsonPropertyName("requires_action")]
        public bool RequiresAction { get; set; } = false;
    }
} 