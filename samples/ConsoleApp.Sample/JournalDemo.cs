using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Exceptions;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates journal recommendation functionality of the Prophy API Client.
    /// Shows various ways to get journal recommendations with different filtering options.
    /// </summary>
    public class JournalDemo
    {
        private readonly ProphyApiClient _client;
        private readonly ILogger<JournalDemo> _logger;

        /// <summary>
        /// Initializes a new instance of the JournalDemo class.
        /// </summary>
        /// <param name="client">The Prophy API client instance.</param>
        /// <param name="logger">The logger for recording demo operations.</param>
        public JournalDemo(ProphyApiClient client, ILogger<JournalDemo> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs all journal recommendation demonstrations.
        /// </summary>
        public async Task RunAllDemosAsync()
        {
            _logger.LogInformation("🔬 Starting Journal Recommendation API Demonstrations");
            _logger.LogInformation("====================================================");
            Console.WriteLine();

            try
            {
                // Demo 1: Basic journal recommendations
                await DemoBasicRecommendations();
                Console.WriteLine();

                // Demo 2: Filtered recommendations
                await DemoFilteredRecommendations();
                Console.WriteLine();

                // Demo 3: Advanced filtering with multiple criteria
                await DemoAdvancedFiltering();
                Console.WriteLine();

                // Demo 4: Open access only recommendations
                await DemoOpenAccessRecommendations();
                Console.WriteLine();

                // Demo 5: Impact factor based filtering
                await DemoImpactFactorFiltering();
                Console.WriteLine();

                // Demo 6: Subject area and publisher filtering
                await DemoSubjectAreaFiltering();
                Console.WriteLine();

                // Demo 7: Error handling demonstration
                await DemoErrorHandling();
                Console.WriteLine();

                _logger.LogInformation("✅ All journal recommendation demonstrations completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during journal recommendation demonstrations");
                throw;
            }
        }

        /// <summary>
        /// Demonstrates basic journal recommendations with minimal parameters.
        /// </summary>
        private async Task DemoBasicRecommendations()
        {
            _logger.LogInformation("📋 Demo 1: Basic Journal Recommendations");
            _logger.LogInformation("----------------------------------------");

            try
            {
                // Test with a sample manuscript ID
                var manuscriptId = "sample-manuscript-basic";
                
                _logger.LogInformation("Requesting basic journal recommendations for manuscript: {ManuscriptId}", manuscriptId);

                var recommendations = await _client.Journals.GetRecommendationsAsync(manuscriptId, cancellationToken: default);

                _logger.LogInformation("✅ Received {Count} journal recommendations", recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null && recommendations.Recommendations.Count > 0)
                {
                    _logger.LogInformation("Top recommendations:");
                    for (int i = 0; i < Math.Min(3, recommendations.Recommendations.Count); i++)
                    {
                        var journal = recommendations.Recommendations[i];
                        _logger.LogInformation("  {Index}. {Title} (Score: {Score:F2})", 
                            i + 1, journal.Title, journal.RelevanceScore);
                    }
                }
                else
                {
                    _logger.LogWarning("No recommendations received");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in basic recommendations demo");
            }
        }

        /// <summary>
        /// Demonstrates journal recommendations with basic filtering.
        /// </summary>
        private async Task DemoFilteredRecommendations()
        {
            _logger.LogInformation("🔍 Demo 2: Filtered Journal Recommendations");
            _logger.LogInformation("------------------------------------------");

            try
            {
                var manuscriptId = "sample-manuscript-filtered";
                
                var request = new JournalRecommendationRequest
                {
                    ManuscriptId = manuscriptId,
                    Limit = 5,
                    MinRelevanceScore = 0.7,
                    IncludeRelatedArticles = true
                };

                _logger.LogInformation("Requesting filtered recommendations:");
                _logger.LogInformation("  - Manuscript ID: {ManuscriptId}", request.ManuscriptId);
                _logger.LogInformation("  - Limit: {Limit}", request.Limit);
                _logger.LogInformation("  - Min Relevance Score: {MinScore:F1}", request.MinRelevanceScore);
                _logger.LogInformation("  - Include Related Articles: {IncludeArticles}", request.IncludeRelatedArticles);

                var recommendations = await _client.Journals.GetRecommendationsAsync(request);

                _logger.LogInformation("✅ Received {Count} filtered recommendations", recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null)
                {
                    foreach (var journal in recommendations.Recommendations)
                    {
                        _logger.LogInformation("📖 {Title}", journal.Title);
                        _logger.LogInformation("   Score: {Score:F2} | Publisher: {Publisher}", 
                            journal.RelevanceScore, journal.Publisher ?? "Unknown");
                        
                        if (journal.RelatedArticles?.Count > 0)
                        {
                            _logger.LogInformation("   Related Articles: {Count}", journal.RelatedArticles.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in filtered recommendations demo");
            }
        }

        /// <summary>
        /// Demonstrates advanced filtering with multiple criteria.
        /// </summary>
        private async Task DemoAdvancedFiltering()
        {
            _logger.LogInformation("⚙️ Demo 3: Advanced Filtering");
            _logger.LogInformation("-----------------------------");

            try
            {
                var manuscriptId = "sample-manuscript-advanced";
                
                var request = new JournalRecommendationRequest
                {
                    ManuscriptId = manuscriptId,
                    Limit = 10,
                    MinRelevanceScore = 0.6,
                    MinImpactFactor = 2.0,
                    MaxImpactFactor = 10.0,
                    IncludeRelatedArticles = true,
                    MaxRelatedArticles = 3
                };

                _logger.LogInformation("Requesting advanced filtered recommendations:");
                _logger.LogInformation("  - Min Relevance Score: {MinScore:F1}", request.MinRelevanceScore);
                _logger.LogInformation("  - Impact Factor Range: {Min:F1} - {Max:F1}", 
                    request.MinImpactFactor, request.MaxImpactFactor);
                _logger.LogInformation("  - Max Related Articles: {MaxArticles}", request.MaxRelatedArticles);

                var recommendations = await _client.Journals.GetRecommendationsAsync(request);

                _logger.LogInformation("✅ Received {Count} advanced filtered recommendations", 
                    recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null)
                {
                    foreach (var journal in recommendations.Recommendations)
                    {
                        _logger.LogInformation("📊 {Title} (IF: {ImpactFactor:F2}, Score: {Score:F2})", 
                            journal.Title, journal.ImpactFactor ?? 0, journal.RelevanceScore);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced filtering demo");
            }
        }

        /// <summary>
        /// Demonstrates open access journal filtering.
        /// </summary>
        private async Task DemoOpenAccessRecommendations()
        {
            _logger.LogInformation("🔓 Demo 4: Open Access Journal Recommendations");
            _logger.LogInformation("---------------------------------------------");

            try
            {
                var manuscriptId = "sample-manuscript-openaccess";
                
                var request = new JournalRecommendationRequest
                {
                    ManuscriptId = manuscriptId,
                    Limit = 5,
                    MinRelevanceScore = 0.5,
                    OpenAccessOnly = true,
                    IncludeRelatedArticles = false
                };

                var recommendations = await _client.Journals.GetRecommendationsAsync(request);

                _logger.LogInformation("✅ Received {Count} open access recommendations", 
                    recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null)
                {
                    foreach (var journal in recommendations.Recommendations)
                    {
                        _logger.LogInformation("🔓 {Title} - Open Access Journal", journal.Title);
                        _logger.LogInformation("   Score: {Score:F2} | Publisher: {Publisher}", 
                            journal.RelevanceScore, journal.Publisher ?? "Unknown");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in open access recommendations demo");
            }
        }

        /// <summary>
        /// Demonstrates impact factor based filtering.
        /// </summary>
        private async Task DemoImpactFactorFiltering()
        {
            _logger.LogInformation("📈 Demo 5: Impact Factor Based Filtering");
            _logger.LogInformation("---------------------------------------");

            try
            {
                var manuscriptId = "sample-manuscript-impact";
                
                var request = new JournalRecommendationRequest
                {
                    ManuscriptId = manuscriptId,
                    Limit = 8,
                    MinImpactFactor = 5.0, // High impact journals only
                    MinRelevanceScore = 0.4
                };

                _logger.LogInformation("Requesting high-impact journal recommendations (IF >= 5.0)");

                var recommendations = await _client.Journals.GetRecommendationsAsync(request);

                _logger.LogInformation("✅ Received {Count} high-impact recommendations", 
                    recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null)
                {
                    var sortedJournals = recommendations.Recommendations
                        .OrderByDescending(j => j.ImpactFactor ?? 0)
                        .ToList();

                    foreach (var journal in sortedJournals)
                    {
                        _logger.LogInformation("🏆 {Title}", journal.Title);
                        _logger.LogInformation("   Impact Factor: {IF:F2} | Relevance: {Score:F2}", 
                            journal.ImpactFactor ?? 0, journal.RelevanceScore);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in impact factor filtering demo");
            }
        }

        /// <summary>
        /// Demonstrates subject area and publisher filtering.
        /// </summary>
        private async Task DemoSubjectAreaFiltering()
        {
            _logger.LogInformation("🎯 Demo 6: Subject Area and Publisher Filtering");
            _logger.LogInformation("----------------------------------------------");

            try
            {
                var manuscriptId = "sample-manuscript-subject";
                
                var request = new JournalRecommendationRequest
                {
                    ManuscriptId = manuscriptId,
                    Limit = 6,
                    SubjectAreas = new List<string> { "Computer Science", "Artificial Intelligence" },
                    Publishers = new List<string> { "Elsevier", "Springer" },
                    MinRelevanceScore = 0.3
                };

                _logger.LogInformation("Requesting recommendations with subject area and publisher filters:");
                _logger.LogInformation("  - Subject Areas: {Areas}", string.Join(", ", request.SubjectAreas));
                _logger.LogInformation("  - Publishers: {Publishers}", string.Join(", ", request.Publishers));

                var recommendations = await _client.Journals.GetRecommendationsAsync(request);

                _logger.LogInformation("✅ Received {Count} subject-filtered recommendations", 
                    recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null)
                {
                    foreach (var journal in recommendations.Recommendations)
                    {
                        _logger.LogInformation("🎯 {Title}", journal.Title);
                        _logger.LogInformation("   Publisher: {Publisher} | Subject: {Subject}", 
                            journal.Publisher ?? "Unknown", 
                            journal.SubjectAreas?.FirstOrDefault() ?? "General");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subject area filtering demo");
            }
        }

        /// <summary>
        /// Demonstrates error handling scenarios.
        /// </summary>
        private async Task DemoErrorHandling()
        {
            _logger.LogInformation("⚠️ Demo 7: Error Handling");
            _logger.LogInformation("------------------------");

            // Test 1: Invalid manuscript ID
            try
            {
                _logger.LogInformation("Testing with empty manuscript ID...");
                var emptyRequest = new JournalRecommendationRequest { ManuscriptId = "" };
                await _client.Journals.GetRecommendationsAsync(emptyRequest);
                _logger.LogWarning("Expected ArgumentException was not thrown");
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("✅ Correctly caught ArgumentException: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception type");
            }

            // Test 2: Invalid request validation
            try
            {
                _logger.LogInformation("Testing with invalid relevance score range...");
                var invalidRequest = new JournalRecommendationRequest
                {
                    ManuscriptId = "test-manuscript",
                    MinRelevanceScore = 1.5 // Invalid: should be between 0 and 1
                };
                await _client.Journals.GetRecommendationsAsync(invalidRequest);
                _logger.LogWarning("Expected ValidationException was not thrown");
            }
            catch (Prophy.ApiClient.Exceptions.ValidationException ex)
            {
                _logger.LogInformation("✅ Correctly caught ValidationException: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception type");
            }

            // Test 3: Invalid impact factor range
            try
            {
                _logger.LogInformation("Testing with invalid impact factor range...");
                var invalidRequest = new JournalRecommendationRequest
                {
                    ManuscriptId = "test-manuscript",
                    MinImpactFactor = 10.0,
                    MaxImpactFactor = 5.0 // Invalid: min > max
                };
                await _client.Journals.GetRecommendationsAsync(invalidRequest);
                _logger.LogWarning("Expected ValidationException was not thrown");
            }
            catch (Prophy.ApiClient.Exceptions.ValidationException ex)
            {
                _logger.LogInformation("✅ Correctly caught ValidationException: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception type");
            }

            _logger.LogInformation("✅ Error handling demonstrations completed");
        }
    }
} 