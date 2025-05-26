using System;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Webhooks;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Represents a handler for webhook events.
    /// </summary>
    /// <typeparam name="TEvent">The type of event data this handler processes.</typeparam>
    public interface IWebhookEventHandler<in TEvent>
    {
        /// <summary>
        /// Handles a webhook event asynchronously.
        /// </summary>
        /// <param name="payload">The webhook payload containing the event.</param>
        /// <param name="eventData">The strongly-typed event data.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(WebhookPayload payload, TEvent eventData, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a generic handler for any webhook event.
    /// </summary>
    public interface IWebhookEventHandler
    {
        /// <summary>
        /// Gets the event type this handler supports.
        /// </summary>
        WebhookEventType EventType { get; }

        /// <summary>
        /// Handles a webhook event asynchronously.
        /// </summary>
        /// <param name="payload">The webhook payload containing the event.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(WebhookPayload payload, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a handler specifically for "mark as referee" events.
    /// </summary>
    public interface IMarkAsRefereeEventHandler : IWebhookEventHandler<MarkAsRefereeEvent>
    {
    }

    /// <summary>
    /// Represents a handler specifically for manuscript status change events.
    /// </summary>
    public interface IManuscriptStatusChangedEventHandler : IWebhookEventHandler<ManuscriptStatusChangedEvent>
    {
    }

    /// <summary>
    /// Represents a handler specifically for referee status update events.
    /// </summary>
    public interface IRefereeStatusUpdatedEventHandler : IWebhookEventHandler<RefereeStatusUpdatedEvent>
    {
    }

    /// <summary>
    /// Represents a handler specifically for manuscript upload events.
    /// </summary>
    public interface IManuscriptUploadedEventHandler : IWebhookEventHandler<ManuscriptUploadedEvent>
    {
    }

    /// <summary>
    /// Represents a handler specifically for referee recommendations generated events.
    /// </summary>
    public interface IRefereeRecommendationsGeneratedEventHandler : IWebhookEventHandler<RefereeRecommendationsGeneratedEvent>
    {
    }
} 