using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;

namespace ConsoleApp.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Prophy API Client - Console Sample");
            Console.WriteLine("==================================");
            Console.WriteLine();

            // Create a simple console logger
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            var logger = loggerFactory.CreateLogger<ProphyApiClient>();

            try
            {
                // Initialize the Prophy API client with sample credentials
                // Note: In a real application, these would come from configuration
                var apiKey = "VVfPN8VqhhYgImx3jLqb_4aZBLhSM9XdMq1Pm0rj";
                var organizationCode = "Flexigrant";

                Console.WriteLine($"Initializing Prophy API Client...");
                Console.WriteLine($"Organization: {organizationCode}");
                Console.WriteLine($"API Key: {apiKey[..10]}...");
                Console.WriteLine();

                using var client = new ProphyApiClient(apiKey, organizationCode, logger: logger);

                Console.WriteLine($"✅ Client initialized successfully!");
                Console.WriteLine($"   Base URL: {client.BaseUrl}");
                Console.WriteLine($"   Organization: {client.OrganizationCode}");
                Console.WriteLine();

                // Demonstrate getting the HTTP client wrapper
                var httpClient = client.GetHttpClient();
                Console.WriteLine($"✅ HTTP client wrapper obtained: {httpClient.GetType().Name}");

                // Demonstrate getting the authenticator
                var authenticator = client.GetAuthenticator();
                Console.WriteLine($"✅ Authenticator obtained: {authenticator.GetType().Name}");
                Console.WriteLine($"   API Key: {authenticator.ApiKey[..10]}...");
                Console.WriteLine($"   Organization: {authenticator.OrganizationCode}");
                Console.WriteLine();

                Console.WriteLine("🎉 HTTP infrastructure is working correctly!");
                Console.WriteLine();

                // Demonstrate the serialization layer
                Console.WriteLine("Running serialization layer demonstration...");
                Console.WriteLine();
                await SerializationDemo.RunAsync(logger);
                Console.WriteLine();

                Console.WriteLine("Next steps:");
                Console.WriteLine("- Task 4: Define core data models");
                Console.WriteLine("- Task 5: Implement manuscript upload API");
                Console.WriteLine("- Task 6: Implement journal recommendation API");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return;
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
