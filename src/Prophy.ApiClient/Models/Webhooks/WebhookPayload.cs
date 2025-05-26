using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the base structure of a webhook payload received from the Prophy API.
    /// </summary>
    public class WebhookPayload
    {
        /// <summary>
        /// Gets or sets the unique identifier for this webhook event.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of webhook event.
        /// </summary>
        [JsonPropertyName("event_type")]
        public WebhookEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the event occurred.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the organization code associated with this event.
        /// </summary>
        [JsonPropertyName("organization")]
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API version used for this webhook.
        /// </summary>
        [JsonPropertyName("api_version")]
        public string ApiVersion { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the event data specific to the webhook type.
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets additional metadata for the webhook event.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the delivery attempt number for this webhook.
        /// </summary>
        [JsonPropertyName("delivery_attempt")]
        public int DeliveryAttempt { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether this is a test webhook event.
        /// </summary>
        [JsonPropertyName("test")]
        public bool IsTest { get; set; } = false;
    }
} 