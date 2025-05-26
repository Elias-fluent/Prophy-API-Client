using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Models.Webhooks;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Provides functionality for processing webhook events and managing event handlers.
    /// </summary>
    public class WebhookModule : IWebhookModule
    {
        private readonly IWebhookValidator _validator;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<WebhookModule> _logger;
        private readonly ConcurrentDictionary<WebhookEventType, List<IWebhookEventHandler>> _handlers;
        private readonly ConcurrentDictionary<WebhookEventType, List<object>> _typedHandlers;

        /// <summary>
        /// Initializes a new instance of the WebhookModule class.
        /// </summary>
        /// <param name="validator">The webhook validator for signature verification.</param>
        /// <param name="jsonSerializer">The JSON serializer for payload processing.</param>
        /// <param name="logger">The logger for recording module operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public WebhookModule(
            IWebhookValidator validator,
            IJsonSerializer jsonSerializer,
            ILogger<WebhookModule> logger)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = new ConcurrentDictionary<WebhookEventType, List<IWebhookEventHandler>>();
            _typedHandlers = new ConcurrentDictionary<WebhookEventType, List<object>>();
        }

        /// <inheritdoc />
        public async Task<WebhookProcessingResult> ProcessWebhookAsync(string payload, string signature, string secret, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
            
            if (string.IsNullOrEmpty(signature))
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));
            
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));

            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Processing webhook with signature validation");

            try
            {
                // Validate signature
                if (!_validator.ValidateSignature(payload, signature, secret))
                {
                    _logger.LogWarning("Webhook signature validation failed");
                    return WebhookProcessingResult.SignatureValidationFailure();
                }

                // Validate payload structure
                if (!_validator.ValidatePayloadStructure(payload))
                {
                    _logger.LogWarning("Webhook payload structure validation failed");
                    return WebhookProcessingResult.PayloadValidationFailure("Invalid payload structure");
                }

                // Parse payload
                WebhookPayload webhookPayload;
                try
                {
                    webhookPayload = ParsePayload(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse webhook payload");
                    return WebhookProcessingResult.PayloadValidationFailure("Failed to parse webhook payload", ex);
                }

                // Process the webhook
                var result = await ProcessWebhookInternalAsync(webhookPayload, cancellationToken);
                result.ProcessingTime = stopwatch.Elapsed;
                result.SignatureValid = true;
                result.PayloadValid = true;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing webhook");
                stopwatch.Stop();
                return WebhookProcessingResult.Failure("Unexpected error processing webhook", ex);
            }
        }

        /// <inheritdoc />
        public async Task<WebhookProcessingResult> ProcessWebhookAsync(string payload, string signature, IEnumerable<string> secrets, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
            
            if (string.IsNullOrEmpty(signature))
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));
            
            if (secrets == null)
                throw new ArgumentNullException(nameof(secrets));

            var secretList = secrets.ToList();
            if (secretList.Count == 0)
                throw new ArgumentException("At least one secret must be provided.", nameof(secrets));

            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Processing webhook with multi-secret signature validation");

            try
            {
                // Validate signature with multiple secrets
                if (!_validator.ValidateSignature(payload, signature, secretList))
                {
                    _logger.LogWarning("Webhook signature validation failed against all provided secrets");
                    return WebhookProcessingResult.SignatureValidationFailure();
                }

                // Validate payload structure
                if (!_validator.ValidatePayloadStructure(payload))
                {
                    _logger.LogWarning("Webhook payload structure validation failed");
                    return WebhookProcessingResult.PayloadValidationFailure("Invalid payload structure");
                }

                // Parse payload
                WebhookPayload webhookPayload;
                try
                {
                    webhookPayload = ParsePayload(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse webhook payload");
                    return WebhookProcessingResult.PayloadValidationFailure("Failed to parse webhook payload", ex);
                }

                // Process the webhook
                var result = await ProcessWebhookInternalAsync(webhookPayload, cancellationToken);
                result.ProcessingTime = stopwatch.Elapsed;
                result.SignatureValid = true;
                result.PayloadValid = true;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing webhook");
                stopwatch.Stop();
                return WebhookProcessingResult.Failure("Unexpected error processing webhook", ex);
            }
        }

        /// <inheritdoc />
        public WebhookPayload ParsePayload(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));

            try
            {
                var webhookPayload = _jsonSerializer.Deserialize<WebhookPayload>(payload);
                if (webhookPayload == null)
                {
                    throw new SerializationException("Deserialized webhook payload is null");
                }

                _logger.LogDebug("Successfully parsed webhook payload with event type: {EventType}", webhookPayload.EventType);
                return webhookPayload;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize webhook payload");
                throw new SerializationException("Failed to deserialize webhook payload", typeof(WebhookPayload), ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing webhook payload");
                throw new SerializationException("Unexpected error parsing webhook payload", typeof(WebhookPayload), ex);
            }
        }

        /// <inheritdoc />
        public TEvent ExtractEventData<TEvent>(WebhookPayload payload) where TEvent : class
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            try
            {
                // Serialize the data dictionary back to JSON and then deserialize to the target type
                var dataJson = _jsonSerializer.Serialize(payload.Data);
                var eventData = _jsonSerializer.Deserialize<TEvent>(dataJson);
                
                if (eventData == null)
                {
                    throw new SerializationException($"Failed to extract event data of type {typeof(TEvent).Name}");
                }

                _logger.LogDebug("Successfully extracted event data of type: {EventType}", typeof(TEvent).Name);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract event data of type: {EventType}", typeof(TEvent).Name);
                throw new SerializationException($"Failed to extract event data of type {typeof(TEvent).Name}", typeof(TEvent), ex);
            }
        }

        /// <inheritdoc />
        public void RegisterHandler(IWebhookEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = handler.EventType;
            _handlers.AddOrUpdate(eventType, 
                new List<IWebhookEventHandler> { handler },
                (key, existing) =>
                {
                    existing.Add(handler);
                    return existing;
                });

            _logger.LogDebug("Registered generic webhook handler for event type: {EventType}", eventType);
        }

        /// <inheritdoc />
        public void RegisterHandler<TEvent>(WebhookEventType eventType, IWebhookEventHandler<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _typedHandlers.AddOrUpdate(eventType,
                new List<object> { handler },
                (key, existing) =>
                {
                    existing.Add(handler);
                    return existing;
                });

            _logger.LogDebug("Registered typed webhook handler for event type: {EventType}, data type: {DataType}", 
                eventType, typeof(TEvent).Name);
        }

        /// <inheritdoc />
        public void UnregisterHandlers(WebhookEventType eventType)
        {
            _handlers.TryRemove(eventType, out _);
            _typedHandlers.TryRemove(eventType, out _);
            
            _logger.LogDebug("Unregistered all handlers for event type: {EventType}", eventType);
        }

        /// <inheritdoc />
        public void UnregisterHandler(IWebhookEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = handler.EventType;
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                }
            }

            _logger.LogDebug("Unregistered specific webhook handler for event type: {EventType}", eventType);
        }

        /// <inheritdoc />
        public IEnumerable<IWebhookEventHandler> GetHandlers(WebhookEventType eventType)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                return handlers.ToList(); // Return a copy to prevent modification
            }

            return Enumerable.Empty<IWebhookEventHandler>();
        }

        /// <inheritdoc />
        public bool ValidateSignature(string payload, string signature, string secret)
        {
            return _validator.ValidateSignature(payload, signature, secret);
        }

        /// <summary>
        /// Processes a webhook payload internally by dispatching it to registered handlers.
        /// </summary>
        /// <param name="payload">The parsed webhook payload.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation with the processing result.</returns>
        private async Task<WebhookProcessingResult> ProcessWebhookInternalAsync(WebhookPayload payload, CancellationToken cancellationToken)
        {
            var eventType = payload.EventType;
            var handlersExecuted = 0;
            var handlersFailed = 0;
            var errors = new List<Exception>();

            _logger.LogInformation("Processing webhook event: {EventType} (ID: {EventId})", eventType, payload.Id);

            // Process generic handlers
            if (_handlers.TryGetValue(eventType, out var genericHandlers))
            {
                foreach (var handler in genericHandlers)
                {
                    try
                    {
                        await handler.HandleAsync(payload, cancellationToken);
                        handlersExecuted++;
                        _logger.LogDebug("Generic handler executed successfully for event: {EventType}", eventType);
                    }
                    catch (Exception ex)
                    {
                        handlersFailed++;
                        errors.Add(ex);
                        _logger.LogError(ex, "Generic handler failed for event: {EventType}", eventType);
                    }
                }
            }

            // Process typed handlers
            if (_typedHandlers.TryGetValue(eventType, out var typedHandlers))
            {
                foreach (var handler in typedHandlers)
                {
                    try
                    {
                        await ProcessTypedHandlerAsync(handler, payload, cancellationToken);
                        handlersExecuted++;
                        _logger.LogDebug("Typed handler executed successfully for event: {EventType}", eventType);
                    }
                    catch (Exception ex)
                    {
                        handlersFailed++;
                        errors.Add(ex);
                        _logger.LogError(ex, "Typed handler failed for event: {EventType}", eventType);
                    }
                }
            }

            var totalHandlers = handlersExecuted + handlersFailed;
            if (totalHandlers == 0)
            {
                _logger.LogWarning("No handlers registered for webhook event type: {EventType}", eventType);
            }

            var result = new WebhookProcessingResult
            {
                Success = handlersFailed == 0,
                Payload = payload,
                HandlersExecuted = handlersExecuted,
                HandlersFailed = handlersFailed,
                Errors = errors,
                Message = $"Processed webhook with {handlersExecuted} successful and {handlersFailed} failed handler(s)"
            };

            _logger.LogInformation("Webhook processing completed. Success: {Success}, Handlers: {Total} ({Executed} successful, {Failed} failed)",
                result.Success, totalHandlers, handlersExecuted, handlersFailed);

            return result;
        }

        /// <summary>
        /// Processes a typed handler by extracting the appropriate event data and invoking the handler.
        /// </summary>
        /// <param name="handler">The typed handler to process.</param>
        /// <param name="payload">The webhook payload.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessTypedHandlerAsync(object handler, WebhookPayload payload, CancellationToken cancellationToken)
        {
            var handlerType = handler.GetType();
            var eventType = payload.EventType;

            // Use reflection to determine the event data type and call the appropriate method
            switch (eventType)
            {
                case WebhookEventType.MarkAsProposalReferee:
                    if (handler is IWebhookEventHandler<MarkAsRefereeEvent> markAsRefereeHandler)
                    {
                        var eventData = ExtractEventData<MarkAsRefereeEvent>(payload);
                        await markAsRefereeHandler.HandleAsync(payload, eventData, cancellationToken);
                    }
                    break;

                case WebhookEventType.ManuscriptStatusChanged:
                    if (handler is IWebhookEventHandler<ManuscriptStatusChangedEvent> statusChangedHandler)
                    {
                        var eventData = ExtractEventData<ManuscriptStatusChangedEvent>(payload);
                        await statusChangedHandler.HandleAsync(payload, eventData, cancellationToken);
                    }
                    break;

                case WebhookEventType.RefereeStatusUpdated:
                    if (handler is IWebhookEventHandler<RefereeStatusUpdatedEvent> refereeStatusHandler)
                    {
                        var eventData = ExtractEventData<RefereeStatusUpdatedEvent>(payload);
                        await refereeStatusHandler.HandleAsync(payload, eventData, cancellationToken);
                    }
                    break;

                case WebhookEventType.ManuscriptUploaded:
                    if (handler is IWebhookEventHandler<ManuscriptUploadedEvent> manuscriptUploadedHandler)
                    {
                        var eventData = ExtractEventData<ManuscriptUploadedEvent>(payload);
                        await manuscriptUploadedHandler.HandleAsync(payload, eventData, cancellationToken);
                    }
                    break;

                case WebhookEventType.RefereeRecommendationsGenerated:
                    if (handler is IWebhookEventHandler<RefereeRecommendationsGeneratedEvent> recommendationsHandler)
                    {
                        var eventData = ExtractEventData<RefereeRecommendationsGeneratedEvent>(payload);
                        await recommendationsHandler.HandleAsync(payload, eventData, cancellationToken);
                    }
                    break;

                case WebhookEventType.Unknown:
                    _logger.LogWarning("Received webhook with unknown event type, skipping typed handler processing");
                    break;

                default:
                    _logger.LogWarning("No typed handler processing implemented for event type: {EventType}", eventType);
                    break;
            }
        }
    }
} 