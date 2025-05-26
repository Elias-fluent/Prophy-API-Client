using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Models.Webhooks;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Provides functionality for validating webhook payloads and HMAC signatures.
    /// </summary>
    public class WebhookValidator : IWebhookValidator
    {
        private readonly ILogger<WebhookValidator> _logger;

        /// <summary>
        /// Initializes a new instance of the WebhookValidator class.
        /// </summary>
        /// <param name="logger">The logger for recording validation operations.</param>
        public WebhookValidator(ILogger<WebhookValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool ValidateSignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
            
            if (string.IsNullOrEmpty(signature))
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));
            
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));

            try
            {
                var expectedSignature = GenerateSignature(payload, secret);
                var providedSignature = ExtractSignature(signature);

                // Use constant-time comparison to prevent timing attacks
                var isValid = ConstantTimeEquals(expectedSignature, providedSignature);

                _logger.LogDebug("Webhook signature validation result: {IsValid}", isValid);
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature");
                return false;
            }
        }

        /// <inheritdoc />
        public bool ValidateSignature(string payload, string signature, IEnumerable<string> secrets)
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

            _logger.LogDebug("Validating webhook signature against {SecretCount} possible secrets", secretList.Count);

            foreach (var secret in secretList)
            {
                if (string.IsNullOrEmpty(secret))
                    continue;

                if (ValidateSignature(payload, signature, secret))
                {
                    _logger.LogDebug("Webhook signature validated successfully");
                    return true;
                }
            }

            _logger.LogWarning("Webhook signature validation failed against all provided secrets");
            return false;
        }

        /// <inheritdoc />
        public string GenerateSignature(string payload, string secret)
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
            
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));

            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(secret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using var hmac = new HMACSHA256(keyBytes);
                var hashBytes = hmac.ComputeHash(payloadBytes);
                
                // Convert to lowercase hexadecimal string
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HMAC signature");
                throw;
            }
        }

        /// <inheritdoc />
        public string ExtractSignature(string signatureHeader)
        {
            if (string.IsNullOrEmpty(signatureHeader))
                throw new ArgumentException("Signature header cannot be null or empty.", nameof(signatureHeader));

            // Handle different signature formats:
            // - "sha256=abc123..." (GitHub style)
            // - "abc123..." (raw signature)
            
            var trimmedHeader = signatureHeader.Trim();
            
            // Check for prefixed format
            if (trimmedHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedHeader.Substring(7); // Remove "sha256=" prefix
            }
            
            if (trimmedHeader.StartsWith("sha1=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedHeader.Substring(5); // Remove "sha1=" prefix
            }

            // Return as-is if no recognized prefix
            return trimmedHeader;
        }

        /// <inheritdoc />
        public bool ValidatePayloadStructure(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("Webhook payload is null or empty");
                return false;
            }

            try
            {
                // Try to parse as JSON
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;

                // Check for required fields
                var requiredFields = new[] { "id", "event_type", "timestamp", "organization" };
                
                foreach (var field in requiredFields)
                {
                    if (!root.TryGetProperty(field, out _))
                    {
                        _logger.LogWarning("Webhook payload missing required field: {Field}", field);
                        return false;
                    }
                }

                // Validate event_type is a known value
                if (root.TryGetProperty("event_type", out var eventTypeElement))
                {
                    var eventTypeString = eventTypeElement.GetString();
                    if (!Enum.TryParse<WebhookEventType>(eventTypeString, true, out _))
                    {
                        _logger.LogWarning("Unknown webhook event type: {EventType}", eventTypeString);
                        // Don't fail validation for unknown event types to support future extensibility
                    }
                }

                _logger.LogDebug("Webhook payload structure validation passed");
                return true;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Webhook payload is not valid JSON");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook payload structure");
                return false;
            }
        }

        /// <summary>
        /// Performs constant-time string comparison to prevent timing attacks.
        /// </summary>
        /// <param name="expected">The expected string.</param>
        /// <param name="actual">The actual string to compare.</param>
        /// <returns>True if the strings are equal, false otherwise.</returns>
        private static bool ConstantTimeEquals(string expected, string actual)
        {
            // Convert both strings to lowercase for case-insensitive comparison
            var expectedLower = expected.ToLowerInvariant();
            var actualLower = actual.ToLowerInvariant();
            
            if (expectedLower.Length != actualLower.Length)
                return false;

            var result = 0;
            for (var i = 0; i < expectedLower.Length; i++)
            {
                result |= expectedLower[i] ^ actualLower[i];
            }

            return result == 0;
        }
    }
} 