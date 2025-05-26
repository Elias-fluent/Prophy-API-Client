using System.Collections.Generic;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Provides functionality for validating webhook payloads and signatures.
    /// </summary>
    public interface IWebhookValidator
    {
        /// <summary>
        /// Validates the HMAC signature of a webhook payload.
        /// </summary>
        /// <param name="payload">The raw webhook payload as a string.</param>
        /// <param name="signature">The HMAC signature from the webhook headers.</param>
        /// <param name="secret">The secret key used for HMAC generation.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        bool ValidateSignature(string payload, string signature, string secret);

        /// <summary>
        /// Validates the HMAC signature using multiple possible secrets.
        /// This is useful for key rotation scenarios.
        /// </summary>
        /// <param name="payload">The raw webhook payload as a string.</param>
        /// <param name="signature">The HMAC signature from the webhook headers.</param>
        /// <param name="secrets">A collection of possible secret keys.</param>
        /// <returns>True if the signature is valid with any of the provided secrets, false otherwise.</returns>
        bool ValidateSignature(string payload, string signature, IEnumerable<string> secrets);

        /// <summary>
        /// Generates an HMAC signature for a given payload and secret.
        /// This is primarily used for testing and verification purposes.
        /// </summary>
        /// <param name="payload">The payload to sign.</param>
        /// <param name="secret">The secret key to use for signing.</param>
        /// <returns>The generated HMAC signature.</returns>
        string GenerateSignature(string payload, string secret);

        /// <summary>
        /// Extracts the signature from webhook headers.
        /// Handles different signature header formats (e.g., "sha256=signature").
        /// </summary>
        /// <param name="signatureHeader">The signature header value.</param>
        /// <returns>The extracted signature without prefixes.</returns>
        string ExtractSignature(string signatureHeader);

        /// <summary>
        /// Validates the structure and required fields of a webhook payload.
        /// </summary>
        /// <param name="payload">The webhook payload to validate.</param>
        /// <returns>True if the payload structure is valid, false otherwise.</returns>
        bool ValidatePayloadStructure(string payload);
    }
} 