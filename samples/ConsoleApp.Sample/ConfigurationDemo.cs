using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates the configuration system for the Prophy API Client.
    /// </summary>
    public static class ConfigurationDemo
    {
        /// <summary>
        /// Runs the configuration system demonstration.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static async Task RunAsync(ILogger logger)
        {
            logger.LogInformation("🔧 Configuration System Demo");
            logger.LogInformation("=============================");
            logger.LogInformation("");

            // Demo 1: Configuration from appsettings.json
            await DemoJsonConfiguration(logger);
            logger.LogInformation("");

            // Demo 2: Fluent API configuration
            await DemoFluentConfiguration(logger);
            logger.LogInformation("");

            // Demo 3: Mixed configuration sources
            await DemoMixedConfiguration(logger);
            logger.LogInformation("");

            // Demo 4: Configuration validation
            await DemoConfigurationValidation(logger);
            logger.LogInformation("");

            logger.LogInformation("✅ Configuration system demo completed successfully!");
        }

        /// <summary>
        /// Demonstrates loading configuration from appsettings.json.
        /// </summary>
        private static async Task DemoJsonConfiguration(ILogger logger)
        {
            logger.LogInformation("📄 JSON Configuration Demo");
            logger.LogInformation("---------------------------");

            try
            {
                // Load configuration from appsettings.json
                var configuration = ProphyApiClientConfigurationBuilder.CreateDefault()
                    .Build();

                logger.LogInformation("✅ Configuration loaded from appsettings.json");
                logger.LogInformation($"   API Key: {configuration.ApiKey?[..10]}...");
                logger.LogInformation($"   Organization: {configuration.OrganizationCode}");
                logger.LogInformation($"   Base URL: {configuration.BaseUrl}");
                logger.LogInformation($"   Timeout: {configuration.TimeoutSeconds} seconds");
                logger.LogInformation($"   Max File Size: {configuration.MaxFileSize / (1024 * 1024)} MB");
                logger.LogInformation($"   User Agent: {configuration.UserAgent}");

                // Test client initialization
                using var client = new ProphyApiClient(configuration);
                logger.LogInformation($"✅ Client initialized successfully with organization: {client.OrganizationCode}");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ JSON configuration demo failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay for demo purposes
        }

        /// <summary>
        /// Demonstrates fluent API configuration.
        /// </summary>
        private static async Task DemoFluentConfiguration(ILogger logger)
        {
            logger.LogInformation("🔗 Fluent API Configuration Demo");
            logger.LogInformation("----------------------------------");

            try
            {
                // Build configuration using fluent API
                var configuration = ProphyApiClientConfigurationBuilder.Create()
                    .WithApiKey("VVfPN8VqhhYgImx3jLqb_4aZBLhSM9XdMq1Pm0rj")
                    .WithOrganizationCode("Flexigrant")
                    .WithBaseUrl("https://www.prophy.ai/api/")
                    .WithTimeout(180) // 3 minutes
                    .WithRetryPolicy(maxRetryAttempts: 5, retryDelayMilliseconds: 2000)
                    .WithDetailedLogging(true)
                    .WithMaxFileSize(100 * 1024 * 1024) // 100MB
                    .WithSslValidation(true)
                    .WithUserAgent("Prophy-FluentDemo/1.0.0")
                    .Build();

                logger.LogInformation("✅ Configuration built using fluent API");
                logger.LogInformation($"   Timeout: {configuration.TimeoutSeconds} seconds");
                logger.LogInformation($"   Max Retry Attempts: {configuration.MaxRetryAttempts}");
                logger.LogInformation($"   Retry Delay: {configuration.RetryDelayMilliseconds} ms");
                logger.LogInformation($"   Detailed Logging: {configuration.EnableDetailedLogging}");
                logger.LogInformation($"   Max File Size: {configuration.MaxFileSize / (1024 * 1024)} MB");

                // Test client initialization
                using var client = new ProphyApiClient(configuration);
                logger.LogInformation($"✅ Client initialized with custom configuration");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Fluent configuration demo failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay for demo purposes
        }

        /// <summary>
        /// Demonstrates mixed configuration sources with precedence.
        /// </summary>
        private static async Task DemoMixedConfiguration(ILogger logger)
        {
            logger.LogInformation("🔀 Mixed Configuration Sources Demo");
            logger.LogInformation("------------------------------------");

            try
            {
                // Build configuration from multiple sources
                var configuration = ProphyApiClientConfigurationBuilder.CreateDefault()
                    .WithTimeout(240) // Override timeout in code (takes precedence)
                    .WithDetailedLogging(true) // Override logging in code
                    .Build();

                logger.LogInformation("✅ Configuration built from multiple sources");
                logger.LogInformation("   Sources: appsettings.json + environment variables + in-code values");
                logger.LogInformation($"   API Key: {configuration.ApiKey?[..10]}... (from JSON)");
                logger.LogInformation($"   Organization: {configuration.OrganizationCode} (from JSON)");
                logger.LogInformation($"   Timeout: {configuration.TimeoutSeconds} seconds (from code - overridden)");
                logger.LogInformation($"   Detailed Logging: {configuration.EnableDetailedLogging} (from code - overridden)");
                logger.LogInformation($"   Base URL: {configuration.BaseUrl} (from JSON)");

                // Test client initialization
                using var client = new ProphyApiClient(configuration);
                logger.LogInformation($"✅ Client initialized with mixed configuration");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Mixed configuration demo failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay for demo purposes
        }

        /// <summary>
        /// Demonstrates configuration validation.
        /// </summary>
        private static async Task DemoConfigurationValidation(ILogger logger)
        {
            logger.LogInformation("✅ Configuration Validation Demo");
            logger.LogInformation("---------------------------------");

            // Demo valid configuration
            try
            {
                var validConfig = new ProphyApiClientConfiguration
                {
                    ApiKey = "valid-api-key",
                    OrganizationCode = "valid-org",
                    BaseUrl = "https://api.example.com/",
                    TimeoutSeconds = 120
                };

                var errors = validConfig.Validate();
                logger.LogInformation($"✅ Valid configuration: {(validConfig.IsValid ? "PASSED" : "FAILED")}");
                logger.LogInformation($"   Validation errors: {(errors.Any() ? string.Join(", ", errors) : "None")}");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Valid configuration test failed: {ex.Message}");
            }

            // Demo invalid configuration
            try
            {
                var invalidConfig = new ProphyApiClientConfiguration
                {
                    ApiKey = "", // Invalid
                    OrganizationCode = "", // Invalid
                    BaseUrl = "invalid-url", // Invalid
                    TimeoutSeconds = 0, // Invalid
                    MaxRetryAttempts = -1 // Invalid
                };

                var errors = invalidConfig.Validate().ToList();
                logger.LogInformation($"❌ Invalid configuration: {(invalidConfig.IsValid ? "PASSED" : "FAILED")}");
                logger.LogInformation($"   Validation errors ({errors.Count}):");
                foreach (var error in errors)
                {
                    logger.LogInformation($"     - {error}");
                }

                // Try to create client with invalid configuration
                try
                {
                    using var client = new ProphyApiClient(invalidConfig);
                    logger.LogError("❌ Client creation should have failed with invalid configuration!");
                }
                catch (ArgumentException ex)
                {
                    logger.LogInformation($"✅ Client creation correctly rejected invalid configuration: {ex.Message[..100]}...");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Invalid configuration test failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay for demo purposes
        }
    }
} 