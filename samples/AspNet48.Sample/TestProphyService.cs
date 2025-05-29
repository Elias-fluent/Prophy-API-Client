using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AspNet48.Sample.Services;

namespace AspNet48.Sample
{
    /// <summary>
    /// Simple test class to verify ProphyService functionality and Polly.Core error handling.
    /// </summary>
    public class TestProphyService
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Testing ProphyService with .NET Framework 4.8 compatibility...");
            
            // Create a simple logger
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<ProphyService>();
            
            // Test with dummy credentials
            var service = new ProphyService("test-api-key", "test-org", logger);
            
            try
            {
                Console.WriteLine("Attempting to create ProphyApiClient...");
                
                // This should trigger the Polly.Core exception and our error handling
                await service.GetCustomFieldsAsync();
                
                Console.WriteLine("SUCCESS: ProphyService worked without Polly.Core issues!");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"EXPECTED: Caught NotSupportedException: {ex.Message}");
                Console.WriteLine("This confirms our error handling is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNEXPECTED ERROR: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("Test completed. Press any key to exit...");
            Console.ReadKey();
        }
    }
} 