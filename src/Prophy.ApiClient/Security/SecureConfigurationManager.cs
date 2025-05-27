using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Diagnostics;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Manages secure configuration retrieval from multiple providers with fallback support.
    /// </summary>
    public class SecureConfigurationManager : ISecureConfigurationManager
    {
        private readonly List<ISecureConfigurationProvider> _providers;
        private readonly ILogger<SecureConfigurationManager> _logger;
        private readonly SecureConfigurationOptions _options;

        /// <summary>
        /// Initializes a new instance of the SecureConfigurationManager class.
        /// </summary>
        /// <param name="providers">The list of secure configuration providers to use.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">Configuration options for the secure configuration manager.</param>
        public SecureConfigurationManager(
            IEnumerable<ISecureConfigurationProvider> providers,
            ILogger<SecureConfigurationManager> logger,
            SecureConfigurationOptions? options = null)
        {
            _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new SecureConfigurationOptions();

            if (!_providers.Any())
            {
                throw new ArgumentException("At least one secure configuration provider must be provided.", nameof(providers));
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            using var scope = DiagnosticEvents.Scopes.ApiOperation(_logger, "GetSecret");
            
            _logger.LogDebug("Attempting to retrieve secret {SecretName} from {ProviderCount} providers", 
                secretName, _providers.Count);

            foreach (var provider in _providers.Where(p => p.IsAvailable))
            {
                try
                {
                    _logger.LogDebug("Trying provider {ProviderName} for secret {SecretName}", 
                        provider.ProviderName, secretName);

                    var secret = await provider.GetSecretAsync(secretName, cancellationToken);
                    
                    if (!string.IsNullOrEmpty(secret))
                    {
                        _logger.LogInformation("Successfully retrieved secret {SecretName} from provider {ProviderName}", 
                            secretName, provider.ProviderName);
                        
                        // Record metrics
                        DiagnosticEvents.Metrics.IncrementCounter($"security.secret.retrieved.{provider.ProviderName.ToLowerInvariant()}");
                        
                        return secret;
                    }
                    
                    _logger.LogDebug("Secret {SecretName} not found in provider {ProviderName}", 
                        secretName, provider.ProviderName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve secret {SecretName} from provider {ProviderName}", 
                        secretName, provider.ProviderName);
                    
                    // Record metrics
                    DiagnosticEvents.Metrics.IncrementCounter($"security.secret.error.{provider.ProviderName.ToLowerInvariant()}");
                    
                    if (!_options.ContinueOnProviderFailure)
                    {
                        throw;
                    }
                }
            }

            _logger.LogWarning("Secret {SecretName} not found in any available provider", secretName);
            DiagnosticEvents.Metrics.IncrementCounter("security.secret.not_found");
            
            return null;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default)
        {
            if (secretNames == null)
            {
                throw new ArgumentNullException(nameof(secretNames));
            }

            var secretNamesList = secretNames.ToList();
            if (!secretNamesList.Any())
            {
                return new Dictionary<string, string>();
            }

            using var scope = DiagnosticEvents.Scopes.ApiOperation(_logger, "GetSecrets");
            
            _logger.LogDebug("Attempting to retrieve {SecretCount} secrets from {ProviderCount} providers", 
                secretNamesList.Count, _providers.Count);

            var results = new Dictionary<string, string>();
            var remainingSecrets = new HashSet<string>(secretNamesList);

            foreach (var provider in _providers.Where(p => p.IsAvailable))
            {
                if (!remainingSecrets.Any())
                {
                    break; // All secrets found
                }

                try
                {
                    _logger.LogDebug("Trying provider {ProviderName} for {RemainingCount} remaining secrets", 
                        provider.ProviderName, remainingSecrets.Count);

                    var providerResults = await provider.GetSecretsAsync(remainingSecrets, cancellationToken);
                    
                    foreach (var kvp in providerResults)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value))
                        {
                            results[kvp.Key] = kvp.Value;
                            remainingSecrets.Remove(kvp.Key);
                            
                            _logger.LogDebug("Retrieved secret {SecretName} from provider {ProviderName}", 
                                kvp.Key, provider.ProviderName);
                        }
                    }
                    
                    // Record metrics
                    DiagnosticEvents.Metrics.RecordValue($"security.secrets.retrieved.{provider.ProviderName.ToLowerInvariant()}", 
                        providerResults.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve secrets from provider {ProviderName}", 
                        provider.ProviderName);
                    
                    // Record metrics
                    DiagnosticEvents.Metrics.IncrementCounter($"security.secrets.error.{provider.ProviderName.ToLowerInvariant()}");
                    
                    if (!_options.ContinueOnProviderFailure)
                    {
                        throw;
                    }
                }
            }

            if (remainingSecrets.Any())
            {
                _logger.LogWarning("Failed to retrieve {MissingCount} secrets: {MissingSecrets}", 
                    remainingSecrets.Count, string.Join(", ", remainingSecrets));
            }

            _logger.LogInformation("Successfully retrieved {RetrievedCount} out of {RequestedCount} secrets", 
                results.Count, secretNamesList.Count);

            return results;
        }

        /// <inheritdoc />
        public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            if (string.IsNullOrEmpty(secretValue))
            {
                throw new ArgumentException("Secret value cannot be null or empty.", nameof(secretValue));
            }

            using var scope = DiagnosticEvents.Scopes.ApiOperation(_logger, "SetSecret");
            
            _logger.LogDebug("Attempting to store secret {SecretName}", secretName);

            var primaryProvider = _providers.FirstOrDefault(p => p.IsAvailable);
            if (primaryProvider == null)
            {
                throw new InvalidOperationException("No available secure configuration providers found.");
            }

            try
            {
                await primaryProvider.SetSecretAsync(secretName, secretValue, cancellationToken);
                
                _logger.LogInformation("Successfully stored secret {SecretName} using provider {ProviderName}", 
                    secretName, primaryProvider.ProviderName);
                
                // Record metrics
                DiagnosticEvents.Metrics.IncrementCounter($"security.secret.stored.{primaryProvider.ProviderName.ToLowerInvariant()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store secret {SecretName} using provider {ProviderName}", 
                    secretName, primaryProvider.ProviderName);
                
                // Record metrics
                DiagnosticEvents.Metrics.IncrementCounter($"security.secret.store_error.{primaryProvider.ProviderName.ToLowerInvariant()}");
                
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            using var scope = DiagnosticEvents.Scopes.ApiOperation(_logger, "DeleteSecret");
            
            _logger.LogDebug("Attempting to delete secret {SecretName}", secretName);

            var primaryProvider = _providers.FirstOrDefault(p => p.IsAvailable);
            if (primaryProvider == null)
            {
                throw new InvalidOperationException("No available secure configuration providers found.");
            }

            try
            {
                await primaryProvider.DeleteSecretAsync(secretName, cancellationToken);
                
                _logger.LogInformation("Successfully deleted secret {SecretName} using provider {ProviderName}", 
                    secretName, primaryProvider.ProviderName);
                
                // Record metrics
                DiagnosticEvents.Metrics.IncrementCounter($"security.secret.deleted.{primaryProvider.ProviderName.ToLowerInvariant()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete secret {SecretName} using provider {ProviderName}", 
                    secretName, primaryProvider.ProviderName);
                
                // Record metrics
                DiagnosticEvents.Metrics.IncrementCounter($"security.secret.delete_error.{primaryProvider.ProviderName.ToLowerInvariant()}");
                
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            using var scope = DiagnosticEvents.Scopes.ApiOperation(_logger, "TestConnection");
            
            _logger.LogDebug("Testing connection to {ProviderCount} providers", _providers.Count);

            var results = new List<bool>();

            foreach (var provider in _providers)
            {
                try
                {
                    var isConnected = await provider.TestConnectionAsync(cancellationToken);
                    results.Add(isConnected);
                    
                    _logger.LogDebug("Provider {ProviderName} connection test: {Result}", 
                        provider.ProviderName, isConnected ? "Success" : "Failed");
                    
                    // Record metrics
                    DiagnosticEvents.Metrics.IncrementCounter($"security.connection_test.{provider.ProviderName.ToLowerInvariant()}.{(isConnected ? "success" : "failure")}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Connection test failed for provider {ProviderName}", 
                        provider.ProviderName);
                    
                    results.Add(false);
                    
                    // Record metrics
                    DiagnosticEvents.Metrics.IncrementCounter($"security.connection_test.{provider.ProviderName.ToLowerInvariant()}.error");
                }
            }

            var hasAnyConnection = results.Any(r => r);
            
            _logger.LogInformation("Connection test completed: {SuccessfulProviders}/{TotalProviders} providers available", 
                results.Count(r => r), results.Count);

            return hasAnyConnection;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAvailableProviders()
        {
            return _providers.Where(p => p.IsAvailable).Select(p => p.ProviderName);
        }
    }

    /// <summary>
    /// Interface for the secure configuration manager.
    /// </summary>
    public interface ISecureConfigurationManager
    {
        /// <summary>
        /// Retrieves a secret value by its key/name from available providers.
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
        /// Stores or updates a secret value using the primary provider.
        /// </summary>
        /// <param name="secretName">The name or key of the secret to store.</param>
        /// <param name="secretValue">The secret value to store.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a secret from the secure storage using the primary provider.
        /// </summary>
        /// <param name="secretName">The name or key of the secret to delete.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to all configured providers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if at least one provider is available, false otherwise.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the names of all available providers.
        /// </summary>
        /// <returns>A collection of provider names that are currently available.</returns>
        IEnumerable<string> GetAvailableProviders();
    }

    /// <summary>
    /// Configuration options for the secure configuration manager.
    /// </summary>
    public class SecureConfigurationOptions
    {
        /// <summary>
        /// Gets or sets whether to continue trying other providers when one fails.
        /// Default is true.
        /// </summary>
        public bool ContinueOnProviderFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for individual provider operations in milliseconds.
        /// Default is 30 seconds.
        /// </summary>
        public int ProviderTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets whether to cache secrets in memory for performance.
        /// Default is false for security reasons.
        /// </summary>
        public bool EnableMemoryCache { get; set; } = false;

        /// <summary>
        /// Gets or sets the cache expiration time in minutes when memory cache is enabled.
        /// Default is 5 minutes.
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 5;
    }
} 