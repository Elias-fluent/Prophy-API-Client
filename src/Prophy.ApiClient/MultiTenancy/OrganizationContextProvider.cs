using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Provides organization context management using AsyncLocal for context propagation.
    /// </summary>
    public class OrganizationContextProvider : IOrganizationContextProvider
    {
        private static readonly AsyncLocal<OrganizationContext?> _currentContext = new AsyncLocal<OrganizationContext?>();
        private readonly ConcurrentDictionary<string, OrganizationContext> _contextCache;
        private readonly ILogger<OrganizationContextProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the OrganizationContextProvider class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public OrganizationContextProvider(ILogger<OrganizationContextProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextCache = new ConcurrentDictionary<string, OrganizationContext>(StringComparer.OrdinalIgnoreCase);
            
            _logger.LogDebug("OrganizationContextProvider initialized");
        }

        /// <inheritdoc />
        public OrganizationContext? GetCurrentContext()
        {
            var context = _currentContext.Value;
            _logger.LogTrace("Retrieved current context: {OrganizationCode}", 
                context?.OrganizationCode ?? "null");
            return context;
        }

        /// <inheritdoc />
        public void SetCurrentContext(OrganizationContext? context)
        {
            _currentContext.Value = context;
            
            if (context != null)
            {
                // Cache the context for future resolution
                _contextCache.TryAdd(context.OrganizationCode, context);
                _logger.LogDebug("Set current context to: {OrganizationCode}", context.OrganizationCode);
            }
            else
            {
                _logger.LogDebug("Cleared current context");
            }
        }

        /// <inheritdoc />
        public Task<OrganizationContext?> ResolveContextAsync(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
            {
                _logger.LogWarning("Attempted to resolve context with null or empty organization code");
                return Task.FromResult<OrganizationContext?>(null);
            }

            // Try to get from cache first
            if (_contextCache.TryGetValue(organizationCode, out var cachedContext))
            {
                _logger.LogDebug("Resolved context from cache: {OrganizationCode}", organizationCode);
                return Task.FromResult<OrganizationContext?>(cachedContext);
            }

            // Create a basic context if not found in cache
            // In a real implementation, this might involve database lookups or external service calls
            var context = new OrganizationContext(organizationCode);
            _contextCache.TryAdd(organizationCode, context);
            
            _logger.LogDebug("Created new context for: {OrganizationCode}", organizationCode);
            return Task.FromResult<OrganizationContext?>(context);
        }

        /// <inheritdoc />
        public void ClearCurrentContext()
        {
            var previousContext = _currentContext.Value;
            _currentContext.Value = null;
            
            _logger.LogDebug("Cleared current context (was: {OrganizationCode})", 
                previousContext?.OrganizationCode ?? "null");
        }

        /// <summary>
        /// Registers a context in the cache for future resolution.
        /// </summary>
        /// <param name="context">The context to register.</param>
        public void RegisterContext(OrganizationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _contextCache.AddOrUpdate(context.OrganizationCode, context, (key, existing) => context);
            _logger.LogDebug("Registered context: {OrganizationCode}", context.OrganizationCode);
        }

        /// <summary>
        /// Removes a context from the cache.
        /// </summary>
        /// <param name="organizationCode">The organization code to remove.</param>
        /// <returns>True if the context was removed; otherwise, false.</returns>
        public bool UnregisterContext(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                return false;

            var removed = _contextCache.TryRemove(organizationCode, out _);
            if (removed)
            {
                _logger.LogDebug("Unregistered context: {OrganizationCode}", organizationCode);
            }
            
            return removed;
        }

        /// <summary>
        /// Gets the number of cached contexts.
        /// </summary>
        public int CachedContextCount => _contextCache.Count;

        /// <summary>
        /// Clears all cached contexts.
        /// </summary>
        public void ClearCache()
        {
            var count = _contextCache.Count;
            _contextCache.Clear();
            _logger.LogDebug("Cleared context cache ({Count} contexts removed)", count);
        }
    }
} 