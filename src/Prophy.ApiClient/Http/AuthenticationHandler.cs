using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;

namespace Prophy.ApiClient.Http
{
    /// <summary>
    /// HTTP message handler that automatically adds authentication headers to outgoing requests.
    /// </summary>
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly IApiKeyAuthenticator _authenticator;
        private readonly ILogger<AuthenticationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationHandler class.
        /// </summary>
        /// <param name="authenticator">The authenticator to use for adding authentication headers.</param>
        /// <param name="logger">The logger instance for logging authentication operations.</param>
        public AuthenticationHandler(IApiKeyAuthenticator authenticator, ILogger<AuthenticationHandler> logger)
        {
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends an HTTP request with authentication headers added.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Add authentication headers to the request
                _authenticator.AuthenticateRequest(request);
                
                _logger.LogDebug("Authentication headers added to request for {RequestUri}", request.RequestUri);
                
                // Continue with the request pipeline
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding authentication to request for {RequestUri}", request.RequestUri);
                throw;
            }
        }
    }
} 