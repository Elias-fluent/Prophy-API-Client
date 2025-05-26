using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the different types of webhook events that can be received from the Prophy API.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WebhookEventType
    {
        /// <summary>
        /// Event triggered when a referee is marked for a proposal/manuscript.
        /// </summary>
        MarkAsProposalReferee,

        /// <summary>
        /// Event triggered when a referee status is updated.
        /// </summary>
        RefereeStatusUpdated,

        /// <summary>
        /// Event triggered when a manuscript status changes.
        /// </summary>
        ManuscriptStatusChanged,

        /// <summary>
        /// Event triggered when a new manuscript is uploaded.
        /// </summary>
        ManuscriptUploaded,

        /// <summary>
        /// Event triggered when referee recommendations are generated.
        /// </summary>
        RefereeRecommendationsGenerated,

        /// <summary>
        /// Unknown or unsupported event type.
        /// </summary>
        Unknown
    }
} 