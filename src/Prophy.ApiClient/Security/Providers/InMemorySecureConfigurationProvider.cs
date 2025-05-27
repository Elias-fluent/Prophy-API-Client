using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prophy.ApiClient.Security.Providers
{
    /// <summary>
    /// In-memory implementation of ISecureConfigurationProvider for testing and fallback scenarios.
    /// WARNING: This provider stores secrets in memory and should not be used in production for sensitive data.
    /// </summary>
    public class InMemorySecureConfigurationProvider : ISecureConfigurationProvider
    {
        private readonly ConcurrentDictionary<string, string> _secrets;
        private readonly bool _isAvailable;

        /// <summary>
        /// Initializes a new instance of the InMemorySecureConfigurationProvider class.
        /// </summary>
        /// <param name="initialSecrets">Optional initial secrets to populate the provider with.</param>
        /// <param name="isAvailable">Whether the provider should report as available. Default is true.</param>
        public InMemorySecureConfigurationProvider(
            IDictionary<string, string>? initialSecrets = null, 
            bool isAvailable = true)
        {
            _secrets = new ConcurrentDictionary<string, string>(initialSecrets ?? new Dictionary<string, string>());
            _isAvailable = isAvailable;
        }

        /// <inheritdoc />
        public string ProviderName => "InMemory";

        /// <inheritdoc />
        public bool IsAvailable => _isAvailable;

        /// <inheritdoc />
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _secrets.TryGetValue(secretName, out var secret);
            return Task.FromResult(secret);
        }

        /// <inheritdoc />
        public Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default)
        {
            if (secretNames == null)
            {
                throw new ArgumentNullException(nameof(secretNames));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var results = new Dictionary<string, string>();

            foreach (var secretName in secretNames)
            {
                if (!string.IsNullOrWhiteSpace(secretName) && _secrets.TryGetValue(secretName, out var secret))
                {
                    results[secretName] = secret;
                }
            }

            return Task.FromResult(results);
        }

        /// <inheritdoc />
        public Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            if (string.IsNullOrEmpty(secretValue))
            {
                throw new ArgumentException("Secret value cannot be null or empty.", nameof(secretValue));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _secrets.AddOrUpdate(secretName, secretValue, (key, oldValue) => secretValue);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _secrets.TryRemove(secretName, out _);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_isAvailable);
        }

        /// <summary>
        /// Gets all secret names currently stored in the provider.
        /// </summary>
        /// <returns>A collection of secret names.</returns>
        public IEnumerable<string> GetSecretNames()
        {
            return _secrets.Keys.ToList();
        }

        /// <summary>
        /// Gets the count of secrets currently stored in the provider.
        /// </summary>
        /// <returns>The number of secrets stored.</returns>
        public int SecretCount => _secrets.Count;

        /// <summary>
        /// Clears all secrets from the provider.
        /// </summary>
        public void Clear()
        {
            _secrets.Clear();
        }

        /// <summary>
        /// Checks if a secret with the specified name exists.
        /// </summary>
        /// <param name="secretName">The name of the secret to check.</param>
        /// <returns>True if the secret exists, false otherwise.</returns>
        public bool ContainsSecret(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                return false;
            }

            return _secrets.ContainsKey(secretName);
        }
    }
} 