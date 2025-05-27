using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Requests;

namespace ConsoleApp.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Prophy API Client - Console Sample");
            Console.WriteLine("==================================");
            Console.WriteLine();

            // Configure logging
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                // Initialize the Prophy API client
                // Note: Replace with your actual API key and organization code
                var apiKey = Environment.GetEnvironmentVariable("PROPHY_API_KEY") ?? "your-api-key-here";
                var organizationCode = Environment.GetEnvironmentVariable("PROPHY_ORG_CODE") ?? "your-org-code";

                if (apiKey == "your-api-key-here" || organizationCode == "your-org-code")
                {
                    logger.LogWarning("Please set PROPHY_API_KEY and PROPHY_ORG_CODE environment variables or update the code with your actual credentials.");
                    return;
                }

                var clientLogger = loggerFactory.CreateLogger<ProphyApiClient>();
                using var client = new ProphyApiClient(apiKey, organizationCode, logger: clientLogger);

                logger.LogInformation("Prophy API Client initialized successfully!");
                logger.LogInformation("Organization: {OrganizationCode}", client.OrganizationCode);
                logger.LogInformation("Base URL: {BaseUrl}", client.BaseUrl);

                // Test Journal Recommendation API
                await TestJournalRecommendations(client, logger);

                // Test Custom Fields API
                await TestCustomFields(client, logger);

                logger.LogInformation("Sample completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during sample execution");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestJournalRecommendations(ProphyApiClient client, ILogger logger)
        {
            try
            {
                logger.LogInformation("Testing Journal Recommendation API...");

                // Test with a sample manuscript ID
                var manuscriptId = "sample-manuscript-123";
                
                var request = new JournalRecommendationRequest
                {
                    ManuscriptId = manuscriptId,
                    Limit = 5,
                    MinRelevanceScore = 0.7,
                    OpenAccessOnly = false,
                    IncludeRelatedArticles = true
                };

                logger.LogInformation("Requesting journal recommendations for manuscript: {ManuscriptId}", manuscriptId);

                var recommendations = await client.Journals.GetRecommendationsAsync(request);

                logger.LogInformation("Received {Count} journal recommendations", recommendations.Recommendations?.Count ?? 0);

                if (recommendations.Recommendations != null)
                {
                    foreach (var journal in recommendations.Recommendations)
                    {
                        logger.LogInformation("Journal: {Title} (Score: {Score:F2})", 
                            journal.Title, journal.RelevanceScore);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error testing Journal Recommendation API");
            }
        }

        private static async Task TestCustomFields(ProphyApiClient client, ILogger logger)
        {
            try
            {
                logger.LogInformation("Testing Custom Fields API...");

                var customFields = await client.CustomFields.GetAllDefinitionsAsync();

                logger.LogInformation("Retrieved {Count} custom fields", customFields.Count);

                foreach (var field in customFields)
                {
                    logger.LogInformation("Custom Field: {Name} ({Type})", field.Name, field.DataType);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error testing Custom Fields API");
            }
        }
    }
}
