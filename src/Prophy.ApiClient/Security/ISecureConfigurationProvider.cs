using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Interface for secure configuration providers that can retrieve sensitive configuration
    /// from secure storage systems like Azure Key Vault or AWS Secrets Manager.
    /// </summary>
    public interface ISecureConfigurationProvider
    {
        /// <summary>
        /// Gets the name of the provider (e.g., "AzureKeyVault", "AWSSecretsManager").
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Indicates whether the provider is available and properly configured.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Retrieves a secret value by its key/name.
        /// </summary>
        /// <param name="secretName">The name or key of the secret to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The secret value, or null if not found.</returns>
        Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves multiple secrets in a single operation for efficiency.
        /// </summary>
        /// <param name="secretNames">The names or keys of the secrets to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary of secret names to their values. Missing secrets will not be included.</returns>
        Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores or updates a secret value.
        /// </summary>
        /// <param name="secretName">The name or key of the secret to store.</param>
        /// <param name="secretValue">The secret value to store.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a secret from the secure storage.
        /// </summary>
        /// <param name="secretName">The name or key of the secret to delete.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to the secure storage provider.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the connection is successful, false otherwise.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
} 