using System;
using System.Collections.Generic;

namespace Prophy.ApiClient.Models.Webhooks
{
    /// <summary>
    /// Represents the result of processing a webhook event.
    /// </summary>
    public class WebhookProcessingResult
    {
        /// <summary>
        /// Gets or sets whether the webhook processing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the processed webhook payload.
        /// </summary>
        public WebhookPayload? Payload { get; set; }

        /// <summary>
        /// Gets or sets the number of handlers that successfully processed the event.
        /// </summary>
        public int HandlersExecuted { get; set; }

        /// <summary>
        /// Gets or sets the number of handlers that failed to process the event.
        /// </summary>
        public int HandlersFailed { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during processing.
        /// </summary>
        public List<Exception> Errors { get; set; } = new List<Exception>();

        /// <summary>
        /// Gets or sets additional processing details or messages.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the time taken to process the webhook.
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets whether the webhook signature was valid.
        /// </summary>
        public bool SignatureValid { get; set; }

        /// <summary>
        /// Gets or sets whether the webhook payload structure was valid.
        /// </summary>
        public bool PayloadValid { get; set; }

        /// <summary>
        /// Gets whether any handlers failed during processing.
        /// </summary>
        public bool HasErrors => Errors.Count > 0 || HandlersFailed > 0;

        /// <summary>
        /// Gets the total number of handlers that were invoked.
        /// </summary>
        public int TotalHandlers => HandlersExecuted + HandlersFailed;

        /// <summary>
        /// Creates a successful processing result.
        /// </summary>
        /// <param name="payload">The processed webhook payload.</param>
        /// <param name="handlersExecuted">The number of handlers that successfully processed the event.</param>
        /// <param name="processingTime">The time taken to process the webhook.</param>
        /// <returns>A successful webhook processing result.</returns>
        public static WebhookProcessingResult CreateSuccess(WebhookPayload payload, int handlersExecuted, TimeSpan processingTime)
        {
            return new WebhookProcessingResult
            {
                Success = true,
                Payload = payload,
                HandlersExecuted = handlersExecuted,
                HandlersFailed = 0,
                ProcessingTime = processingTime,
                SignatureValid = true,
                PayloadValid = true,
                Message = $"Successfully processed webhook with {handlersExecuted} handler(s)"
            };
        }

        /// <summary>
        /// Creates a failed processing result.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errors">Any exceptions that occurred.</param>
        /// <returns>A failed webhook processing result.</returns>
        public static WebhookProcessingResult Failure(string message, params Exception[] errors)
        {
            return new WebhookProcessingResult
            {
                Success = false,
                Message = message,
                Errors = new List<Exception>(errors),
                SignatureValid = false,
                PayloadValid = false
            };
        }

        /// <summary>
        /// Creates a processing result for signature validation failure.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        /// <returns>A webhook processing result indicating signature validation failure.</returns>
        public static WebhookProcessingResult SignatureValidationFailure(string message = "Webhook signature validation failed")
        {
            return new WebhookProcessingResult
            {
                Success = false,
                Message = message,
                SignatureValid = false,
                PayloadValid = false
            };
        }

        /// <summary>
        /// Creates a processing result for payload validation failure.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        /// <param name="exception">The exception that occurred during payload parsing.</param>
        /// <returns>A webhook processing result indicating payload validation failure.</returns>
        public static WebhookProcessingResult PayloadValidationFailure(string message, Exception? exception = null)
        {
            var result = new WebhookProcessingResult
            {
                Success = false,
                Message = message,
                SignatureValid = true, // Signature was valid, but payload was not
                PayloadValid = false
            };

            if (exception != null)
            {
                result.Errors.Add(exception);
            }

            return result;
        }
    }
} 