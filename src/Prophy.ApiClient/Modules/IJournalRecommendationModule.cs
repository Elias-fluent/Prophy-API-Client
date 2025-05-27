using System;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Interface for journal recommendation operations in the Prophy API.
    /// Provides methods for retrieving journal recommendations based on manuscript content.
    /// </summary>
    public interface IJournalRecommendationModule
    {
        /// <summary>
        /// Gets journal recommendations for a specific manuscript.
        /// </summary>
        /// <param name="manuscriptId">The ID of the manuscript to get recommendations for.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the journal recommendations.</returns>
        /// <exception cref="ArgumentException">Thrown when manuscriptId is null or empty.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API request fails.</exception>
        Task<JournalRecommendationResponse> GetRecommendationsAsync(string manuscriptId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets journal recommendations for a specific manuscript with filtering options.
        /// </summary>
        /// <param name="request">The journal recommendation request with filtering parameters.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the journal recommendations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="ValidationException">Thrown when request validation fails.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API request fails.</exception>
        Task<JournalRecommendationResponse> GetRecommendationsAsync(JournalRecommendationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets journal recommendations for a specific manuscript with simple filtering options.
        /// </summary>
        /// <param name="manuscriptId">The ID of the manuscript to get recommendations for.</param>
        /// <param name="limit">Maximum number of recommendations to return.</param>
        /// <param name="minRelevanceScore">Minimum relevance score threshold for recommendations.</param>
        /// <param name="openAccessOnly">Whether to include only open access journals.</param>
        /// <param name="includeRelatedArticles">Whether to include related articles in the response.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the journal recommendations.</returns>
        /// <exception cref="ArgumentException">Thrown when manuscriptId is null or empty.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API request fails.</exception>
        Task<JournalRecommendationResponse> GetRecommendationsAsync(
            string manuscriptId,
            int? limit = null,
            double? minRelevanceScore = null,
            bool? openAccessOnly = null,
            bool? includeRelatedArticles = null,
            CancellationToken cancellationToken = default);
    }
} 