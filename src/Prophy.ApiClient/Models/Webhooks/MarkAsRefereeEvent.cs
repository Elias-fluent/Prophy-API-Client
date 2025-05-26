using System;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the data payload for a "mark as referee" webhook event.
    /// This event is triggered when a referee is marked for a proposal/manuscript.
    /// </summary>
    public class MarkAsRefereeEvent
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
        /// Gets or sets the referee candidate that was marked.
        /// </summary>
        [JsonPropertyName("referee")]
        public RefereeCandidate Referee { get; set; } = new RefereeCandidate();

        /// <summary>
        /// Gets or sets the user who marked the referee.
        /// </summary>
        [JsonPropertyName("marked_by")]
        public string MarkedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the referee was marked.
        /// </summary>
        [JsonPropertyName("marked_at")]
        public DateTime MarkedAt { get; set; }

        /// <summary>
        /// Gets or sets the status of the referee marking (e.g., "selected", "invited", "accepted").
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any notes or comments associated with the referee marking.
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the folder/journal associated with the manuscript.
        /// </summary>
        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets the priority level of this referee assignment.
        /// </summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether this referee assignment is due to a conflict of interest resolution.
        /// </summary>
        [JsonPropertyName("coi_resolved")]
        public bool ConflictOfInterestResolved { get; set; } = false;
    }
} 