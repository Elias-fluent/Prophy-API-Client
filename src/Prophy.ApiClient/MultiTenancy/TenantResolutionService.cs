using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Service that coordinates tenant resolution and context management.
    /// </summary>
    public class TenantResolutionService
    {
        private readonly ITenantResolver _tenantResolver;
        private readonly IOrganizationContextProvider _contextProvider;
        private readonly ILogger<TenantResolutionService> _logger;

        /// <summary>
        /// Initializes a new instance of the TenantResolutionService class.
        /// </summary>
        /// <param name="tenantResolver">The tenant resolver.</param>
        /// <param name="contextProvider">The organization context provider.</param>
        /// <param name="logger">The logger instance.</param>
        public TenantResolutionService(
            ITenantResolver tenantResolver,
            IOrganizationContextProvider contextProvider,
            ILogger<TenantResolutionService> logger)
        {
            _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Resolves and sets the tenant context for an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the resolved organization context, or null if not resolved.</returns>
        public async Task<OrganizationContext?> ResolveAndSetContextAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                _logger.LogWarning("Cannot resolve tenant context from null request");
                return null;
            }

            try
            {
                // First, check if there's already a current context
                var currentContext = _contextProvider.GetCurrentContext();
                if (currentContext != null)
                {
                    _logger.LogTrace("Using existing organization context: {OrganizationCode}", 
                        currentContext.OrganizationCode);
                    return currentContext;
                }

                // Resolve organization code from the request
                var organizationCode = await _tenantResolver.ResolveFromRequestAsync(request);
                if (string.IsNullOrWhiteSpace(organizationCode))
                {
                    _logger.LogDebug("No organization code resolved from request");
                    return null;
                }

                // Resolve or create the organization context
                var context = await _contextProvider.ResolveContextAsync(organizationCode);
                if (context == null)
                {
                    _logger.LogWarning("Failed to resolve organization context for code: {OrganizationCode}", 
                        organizationCode);
                    return null;
                }

                // Set the context for the current operation
                _contextProvider.SetCurrentContext(context);
                
                _logger.LogDebug("Successfully resolved and set organization context: {OrganizationCode}", 
                    context.OrganizationCode);
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve tenant context from request");
                return null;
            }
        }

        /// <summary>
        /// Resolves tenant context without setting it as current.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the resolved organization context, or null if not resolved.</returns>
        public async Task<OrganizationContext?> ResolveContextAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                _logger.LogWarning("Cannot resolve tenant context from null request");
                return null;
            }

            try
            {
                var organizationCode = await _tenantResolver.ResolveFromRequestAsync(request);
                if (string.IsNullOrWhiteSpace(organizationCode))
                {
                    _logger.LogDebug("No organization code resolved from request");
                    return null;
                }

                var context = await _contextProvider.ResolveContextAsync(organizationCode);
                if (context == null)
                {
                    _logger.LogWarning("Failed to resolve organization context for code: {OrganizationCode}", 
                        organizationCode);
                    return null;
                }

                _logger.LogDebug("Successfully resolved organization context: {OrganizationCode}", 
                    context.OrganizationCode);
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve tenant context from request");
                return null;
            }
        }

        /// <summary>
        /// Ensures that a tenant context is available, either from resolution or fallback.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="fallbackOrganizationCode">The fallback organization code to use if resolution fails.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the organization context.</returns>
        public async Task<OrganizationContext> EnsureContextAsync(HttpRequestMessage request, string fallbackOrganizationCode)
        {
            if (string.IsNullOrWhiteSpace(fallbackOrganizationCode))
                throw new ArgumentException("Fallback organization code cannot be null or empty.", nameof(fallbackOrganizationCode));

            var context = await ResolveAndSetContextAsync(request);
            if (context != null)
            {
                return context;
            }

            // Use fallback organization code
            _logger.LogDebug("Using fallback organization code: {OrganizationCode}", fallbackOrganizationCode);
            
            var fallbackContext = await _contextProvider.ResolveContextAsync(fallbackOrganizationCode);
            if (fallbackContext == null)
            {
                // Create a minimal context if resolution fails
                fallbackContext = new OrganizationContext(fallbackOrganizationCode);
                _logger.LogDebug("Created minimal organization context for fallback: {OrganizationCode}", 
                    fallbackOrganizationCode);
            }

            _contextProvider.SetCurrentContext(fallbackContext);
            return fallbackContext;
        }

        /// <summary>
        /// Clears the current tenant context.
        /// </summary>
        public void ClearContext()
        {
            _contextProvider.ClearCurrentContext();
            _logger.LogDebug("Cleared current organization context");
        }

        /// <summary>
        /// Gets the current tenant context without resolution.
        /// </summary>
        /// <returns>The current organization context, or null if not set.</returns>
        public OrganizationContext? GetCurrentContext()
        {
            return _contextProvider.GetCurrentContext();
        }
    }
} 