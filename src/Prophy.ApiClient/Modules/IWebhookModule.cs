using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Webhooks;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Provides functionality for processing webhook events and managing event handlers.
    /// </summary>
    public interface IWebhookModule
    {
        /// <summary>
        /// Processes a webhook payload, validates its signature, and dispatches it to registered handlers.
        /// </summary>
        /// <param name="payload">The raw webhook payload as a string.</param>
        /// <param name="signature">The HMAC signature from the webhook headers.</param>
        /// <param name="secret">The secret key used for signature validation.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation with the processing result.</returns>
        /// <exception cref="ArgumentException">Thrown when payload, signature, or secret is null or empty.</exception>
        /// <exception cref="Prophy.ApiClient.Exceptions.ValidationException">Thrown when signature validation fails.</exception>
        /// <exception cref="Prophy.ApiClient.Exceptions.SerializationException">Thrown when payload deserialization fails.</exception>
        Task<WebhookProcessingResult> ProcessWebhookAsync(string payload, string signature, string secret, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a webhook payload using multiple possible secrets for validation.
        /// </summary>
        /// <param name="payload">The raw webhook payload as a string.</param>
        /// <param name="signature">The HMAC signature from the webhook headers.</param>
        /// <param name="secrets">A collection of possible secret keys for validation.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation with the processing result.</returns>
        Task<WebhookProcessingResult> ProcessWebhookAsync(string payload, string signature, IEnumerable<string> secrets, CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses a webhook payload into a strongly-typed WebhookPayload object.
        /// </summary>
        /// <param name="payload">The raw webhook payload as a string.</param>
        /// <returns>The parsed webhook payload.</returns>
        /// <exception cref="ArgumentException">Thrown when payload is null or empty.</exception>
        /// <exception cref="Prophy.ApiClient.Exceptions.SerializationException">Thrown when payload deserialization fails.</exception>
        WebhookPayload ParsePayload(string payload);

        /// <summary>
        /// Extracts strongly-typed event data from a webhook payload.
        /// </summary>
        /// <typeparam name="TEvent">The type of event data to extract.</typeparam>
        /// <param name="payload">The webhook payload containing the event data.</param>
        /// <returns>The extracted event data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when payload is null.</exception>
        /// <exception cref="Prophy.ApiClient.Exceptions.SerializationException">Thrown when event data extraction fails.</exception>
        TEvent ExtractEventData<TEvent>(WebhookPayload payload) where TEvent : class;

        /// <summary>
        /// Registers a generic event handler for a specific event type.
        /// </summary>
        /// <param name="handler">The event handler to register.</param>
        void RegisterHandler(IWebhookEventHandler handler);

        /// <summary>
        /// Registers a strongly-typed event handler for a specific event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of event data the handler processes.</typeparam>
        /// <param name="eventType">The webhook event type this handler supports.</param>
        /// <param name="handler">The event handler to register.</param>
        void RegisterHandler<TEvent>(WebhookEventType eventType, IWebhookEventHandler<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Unregisters all handlers for a specific event type.
        /// </summary>
        /// <param name="eventType">The event type to unregister handlers for.</param>
        void UnregisterHandlers(WebhookEventType eventType);

        /// <summary>
        /// Unregisters a specific handler instance.
        /// </summary>
        /// <param name="handler">The handler instance to unregister.</param>
        void UnregisterHandler(IWebhookEventHandler handler);

        /// <summary>
        /// Gets all registered handlers for a specific event type.
        /// </summary>
        /// <param name="eventType">The event type to get handlers for.</param>
        /// <returns>A collection of registered handlers for the specified event type.</returns>
        IEnumerable<IWebhookEventHandler> GetHandlers(WebhookEventType eventType);

        /// <summary>
        /// Validates a webhook signature without processing the payload.
        /// </summary>
        /// <param name="payload">The raw webhook payload as a string.</param>
        /// <param name="signature">The HMAC signature from the webhook headers.</param>
        /// <param name="secret">The secret key used for signature validation.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        bool ValidateSignature(string payload, string signature, string secret);
    }
} 