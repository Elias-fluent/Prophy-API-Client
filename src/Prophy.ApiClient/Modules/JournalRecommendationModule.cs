using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Implementation of journal recommendation operations for the Prophy API.
    /// Provides methods for retrieving journal recommendations based on manuscript content.
    /// </summary>
    public class JournalRecommendationModule : IJournalRecommendationModule
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IApiKeyAuthenticator _authenticator;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger<JournalRecommendationModule> _logger;

        /// <summary>
        /// Initializes a new instance of the JournalRecommendationModule class.
        /// </summary>
        /// <param name="httpClient">The HTTP client wrapper for making API requests.</param>
        /// <param name="authenticator">The API key authenticator for request authentication.</param>
        /// <param name="serializer">The JSON serializer for request/response serialization.</param>
        /// <param name="logger">The logger for logging operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public JournalRecommendationModule(
            IHttpClientWrapper httpClient,
            IApiKeyAuthenticator authenticator,
            IJsonSerializer serializer,
            ILogger<JournalRecommendationModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JournalRecommendationResponse> GetRecommendationsAsync(string manuscriptId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(manuscriptId))
                throw new ArgumentException("Manuscript ID cannot be null or empty.", nameof(manuscriptId));

            var request = new JournalRecommendationRequest
            {
                ManuscriptId = manuscriptId
            };

            return await GetRecommendationsAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<JournalRecommendationResponse> GetRecommendationsAsync(JournalRecommendationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate the request
            ValidateRequest(request);

            _logger.LogDebug("Getting journal recommendations for manuscript: {ManuscriptId}", request.ManuscriptId);

            try
            {
                // Build the API endpoint URL
                var endpoint = $"external/recommend-journals/{request.ManuscriptId}/";
                
                // Create HTTP request message
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                
                // Add authentication headers
                _authenticator.AuthenticateRequest(httpRequest);

                // Add query parameters for filtering
                var queryParams = BuildQueryParameters(request);
                if (!string.IsNullOrEmpty(queryParams))
                {
                    httpRequest.RequestUri = new Uri($"{endpoint}?{queryParams}", UriKind.Relative);
                }

                _logger.LogDebug("Sending journal recommendation request to: {Endpoint}", httpRequest.RequestUri);

                // Send the request
                var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

                // Handle the response
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                
                _logger.LogDebug("Received journal recommendation response. Status: {StatusCode}, Content Length: {ContentLength}", 
                    httpResponse.StatusCode, responseContent?.Length ?? 0);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMessage = $"Journal recommendation request failed with status {httpResponse.StatusCode}";
                    _logger.LogError("{ErrorMessage}. Response: {ResponseContent}", errorMessage, responseContent);
                    throw new ProphyApiException(errorMessage, "JOURNAL_RECOMMENDATION_ERROR", httpResponse.StatusCode, responseContent);
                }

                // Deserialize the response
                var response = _serializer.Deserialize<JournalRecommendationResponse>(responseContent);
                
                if (response == null)
                {
                    throw new ProphyApiException("Failed to deserialize journal recommendation response", "DESERIALIZATION_ERROR", httpResponse.StatusCode, responseContent);
                }

                _logger.LogInformation("Successfully retrieved {Count} journal recommendations for manuscript: {ManuscriptId}", 
                    response.Recommendations?.Count ?? 0, request.ManuscriptId);

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while getting journal recommendations for manuscript: {ManuscriptId}", request.ManuscriptId);
                throw new ProphyApiException("Network error occurred while getting journal recommendations", "NETWORK_ERROR", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout occurred while getting journal recommendations for manuscript: {ManuscriptId}", request.ManuscriptId);
                throw new ProphyApiException("Request timeout while getting journal recommendations", "TIMEOUT_ERROR", ex);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error occurred while getting journal recommendations for manuscript: {ManuscriptId}", request.ManuscriptId);
                throw new ProphyApiException("Unexpected error occurred while getting journal recommendations", "UNEXPECTED_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<JournalRecommendationResponse> GetRecommendationsAsync(
            string manuscriptId,
            int? limit = null,
            double? minRelevanceScore = null,
            bool? openAccessOnly = null,
            bool? includeRelatedArticles = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(manuscriptId))
                throw new ArgumentException("Manuscript ID cannot be null or empty.", nameof(manuscriptId));

            var request = new JournalRecommendationRequest
            {
                ManuscriptId = manuscriptId,
                Limit = limit,
                MinRelevanceScore = minRelevanceScore,
                OpenAccessOnly = openAccessOnly,
                IncludeRelatedArticles = includeRelatedArticles
            };

            return await GetRecommendationsAsync(request, cancellationToken);
        }

        /// <summary>
        /// Validates the journal recommendation request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="Prophy.ApiClient.Exceptions.ValidationException">Thrown when validation fails.</exception>
        private void ValidateRequest(JournalRecommendationRequest request)
        {
            var validationResults = new System.Collections.Generic.List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                var errorMessage = $"Journal recommendation request validation failed: {string.Join(", ", errors)}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new Prophy.ApiClient.Exceptions.ValidationException(errorMessage, errors);
            }

            // Additional business logic validation
            if (request.Limit.HasValue && request.Limit.Value <= 0)
            {
                throw new Prophy.ApiClient.Exceptions.ValidationException("Limit must be greater than 0", new[] { "Limit must be a positive number" });
            }

            if (request.MinRelevanceScore.HasValue && (request.MinRelevanceScore.Value < 0 || request.MinRelevanceScore.Value > 1))
            {
                throw new Prophy.ApiClient.Exceptions.ValidationException("MinRelevanceScore must be between 0 and 1", new[] { "MinRelevanceScore must be between 0 and 1" });
            }

            if (request.MinImpactFactor.HasValue && request.MinImpactFactor.Value < 0)
            {
                throw new Prophy.ApiClient.Exceptions.ValidationException("MinImpactFactor must be non-negative", new[] { "MinImpactFactor must be non-negative" });
            }

            if (request.MaxImpactFactor.HasValue && request.MaxImpactFactor.Value < 0)
            {
                throw new Prophy.ApiClient.Exceptions.ValidationException("MaxImpactFactor must be non-negative", new[] { "MaxImpactFactor must be non-negative" });
            }

            if (request.MinImpactFactor.HasValue && request.MaxImpactFactor.HasValue && 
                request.MinImpactFactor.Value > request.MaxImpactFactor.Value)
            {
                throw new Prophy.ApiClient.Exceptions.ValidationException("MinImpactFactor cannot be greater than MaxImpactFactor", 
                    new[] { "MinImpactFactor cannot be greater than MaxImpactFactor" });
            }

            if (request.MaxRelatedArticles.HasValue && request.MaxRelatedArticles.Value < 0)
            {
                throw new Prophy.ApiClient.Exceptions.ValidationException("MaxRelatedArticles must be non-negative", new[] { "MaxRelatedArticles must be non-negative" });
            }

            _logger.LogDebug("Request validation passed for manuscript: {ManuscriptId}", request.ManuscriptId);
        }

        /// <summary>
        /// Builds query parameters from the journal recommendation request.
        /// </summary>
        /// <param name="request">The journal recommendation request.</param>
        /// <returns>A query string with the request parameters.</returns>
        private string BuildQueryParameters(JournalRecommendationRequest request)
        {
            var parameters = new System.Collections.Generic.List<string>();

            if (request.Limit.HasValue)
                parameters.Add($"limit={request.Limit.Value}");

            if (request.MinRelevanceScore.HasValue)
                parameters.Add($"min_relevance_score={request.MinRelevanceScore.Value:F2}");

            if (request.OpenAccessOnly.HasValue)
                parameters.Add($"open_access_only={request.OpenAccessOnly.Value.ToString().ToLowerInvariant()}");

            if (request.MinImpactFactor.HasValue)
                parameters.Add($"min_impact_factor={request.MinImpactFactor.Value:F2}");

            if (request.MaxImpactFactor.HasValue)
                parameters.Add($"max_impact_factor={request.MaxImpactFactor.Value:F2}");

            if (request.IncludeRelatedArticles.HasValue)
                parameters.Add($"include_related_articles={request.IncludeRelatedArticles.Value.ToString().ToLowerInvariant()}");

            if (request.MaxRelatedArticles.HasValue)
                parameters.Add($"max_related_articles={request.MaxRelatedArticles.Value}");

            if (request.SubjectAreas?.Any() == true)
            {
                foreach (var area in request.SubjectAreas)
                {
                    parameters.Add($"subject_areas={Uri.EscapeDataString(area)}");
                }
            }

            if (request.Publishers?.Any() == true)
            {
                foreach (var publisher in request.Publishers)
                {
                    parameters.Add($"publishers={Uri.EscapeDataString(publisher)}");
                }
            }

            if (request.ExcludePublishers?.Any() == true)
            {
                foreach (var publisher in request.ExcludePublishers)
                {
                    parameters.Add($"exclude_publishers={Uri.EscapeDataString(publisher)}");
                }
            }

            if (request.ExcludeJournals?.Any() == true)
            {
                foreach (var journal in request.ExcludeJournals)
                {
                    parameters.Add($"exclude_journals={Uri.EscapeDataString(journal)}");
                }
            }

            if (request.Filters?.Any() == true)
            {
                foreach (var filter in request.Filters)
                {
                    parameters.Add($"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value?.ToString() ?? "")}");
                }
            }

            return string.Join("&", parameters);
        }
    }
} 