using System;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the data payload for a referee status update webhook event.
    /// This event is triggered when a referee's status is updated in the system.
    /// </summary>
    public class RefereeStatusUpdatedEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the manuscript/proposal.
        /// </summary>
        [JsonPropertyName("manuscript_id")]
        public string ManuscriptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the manuscript.
        /// </summary>
        [JsonPropertyName("manuscript_title")]
        public string ManuscriptTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the referee whose status was updated.
        /// </summary>
        [JsonPropertyName("referee")]
        public RefereeCandidate Referee { get; set; } = new RefereeCandidate();

        /// <summary>
        /// Gets or sets the previous status of the referee.
        /// </summary>
        [JsonPropertyName("previous_status")]
        public string PreviousStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new status of the referee.
        /// </summary>
        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the status was updated.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who updated the referee status.
        /// </summary>
        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for the status update.
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets any notes associated with the status update.
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the folder/journal associated with the manuscript.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets whether this status update was automated.
        /// </summary>
        [JsonPropertyName("automated")]
        public bool IsAutomated { get; set; } = false;
    }
} 