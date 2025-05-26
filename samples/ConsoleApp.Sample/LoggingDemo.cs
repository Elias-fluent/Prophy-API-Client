using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Diagnostics;
using Prophy.ApiClient.Http;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates the comprehensive logging and diagnostics capabilities of the Prophy API Client.
    /// </summary>
    public class LoggingDemo
    {
        private readonly ILogger<LoggingDemo> _logger;
        private readonly ProphyApiClient _client;

        public LoggingDemo(ILogger<LoggingDemo> logger, ProphyApiClient client)
        {
            _logger = logger;
            _client = client;
        }

        /// <summary>
        /// Runs the logging demonstration showing various logging features.
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("🔍 Prophy API Client - Logging & Diagnostics Demo");
            Console.WriteLine("=" + new string('=', 50));
            Console.WriteLine();

            try
            {
                // Demonstrate structured logging
                await DemonstrateStructuredLogging();
                Console.WriteLine();

                // Demonstrate performance metrics
                await DemonstratePerformanceMetrics();
                Console.WriteLine();

                // Demonstrate diagnostic events
                await DemonstrateDiagnosticEvents();
                Console.WriteLine();

                // Demonstrate error logging
                await DemonstrateErrorLogging();
                Console.WriteLine();

                // Show final metrics summary
                ShowMetricsSummary();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demo execution failed");
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
            }
        }

        private async Task DemonstrateStructuredLogging()
        {
            Console.WriteLine("📝 Structured Logging Demo");
            Console.WriteLine("-" + new string('-', 30));

            _logger.LogInformation("Starting structured logging demonstration");

            // Log with structured data
            using var scope = _logger.BeginScope(new { DemoSection = "StructuredLogging", UserId = "demo-user" });

            _logger.LogInformation("User {UserId} started operation {Operation} at {Timestamp}",
                "demo-user", "StructuredLoggingDemo", DateTime.UtcNow);

            // Simulate API operations with structured logging
            var organizationCode = "demo-org";
            var operationId = Guid.NewGuid().ToString("N")[..8];

            using var apiScope = DiagnosticEvents.Scopes.ApiOperation(_logger, "DemoOperation", organizationCode);

            _logger.LogInformation("Executing demo API operation {OperationId} for organization {OrganizationCode}",
                operationId, organizationCode);

            // Simulate some work
            await Task.Delay(100);

            _logger.LogInformation("Demo API operation {OperationId} completed successfully", operationId);

            Console.WriteLine("✅ Structured logging completed - check logs for detailed structured data");
        }

        private async Task DemonstratePerformanceMetrics()
        {
            Console.WriteLine("⚡ Performance Metrics Demo");
            Console.WriteLine("-" + new string('-', 30));

            _logger.LogInformation("Starting performance metrics demonstration");

            // Reset metrics for clean demo
            DiagnosticEvents.Metrics.Reset();

            // Simulate various operations with metrics
            var stopwatch = Stopwatch.StartNew();

            // Simulate HTTP requests
            for (int i = 0; i < 5; i++)
            {
                var requestDuration = TimeSpan.FromMilliseconds(50 + (i * 20));
                var isSuccess = i < 4; // Last one fails

                DiagnosticEvents.Metrics.RecordHttpRequestDuration("GET", "/api/demo", requestDuration, isSuccess);
                
                _logger.LogInformation("Simulated HTTP GET request {RequestNumber} - Duration: {Duration}ms, Success: {Success}",
                    i + 1, requestDuration.TotalMilliseconds, isSuccess);

                await Task.Delay(10);
            }

            // Simulate serialization operations
            for (int i = 0; i < 3; i++)
            {
                var serializationDuration = TimeSpan.FromMilliseconds(10 + (i * 5));
                DiagnosticEvents.Metrics.RecordSerializationDuration("Serialize", typeof(string), serializationDuration);
                
                _logger.LogInformation("Simulated serialization operation {OperationNumber} - Duration: {Duration}ms",
                    i + 1, serializationDuration.TotalMilliseconds);
            }

            // Simulate API operations
            var apiOperationDuration = stopwatch.Elapsed;
            DiagnosticEvents.Metrics.RecordApiOperationDuration("DemoOperation", apiOperationDuration, true);

            stopwatch.Stop();

            Console.WriteLine("✅ Performance metrics recorded - see metrics summary below");
        }

        private async Task DemonstrateDiagnosticEvents()
        {
            Console.WriteLine("🔬 Diagnostic Events Demo");
            Console.WriteLine("-" + new string('-', 30));

            _logger.LogInformation("Starting diagnostic events demonstration");

            // Log client lifecycle events
            _logger.LogClientInitialized("demo-org", "https://api.demo.com");

            // Simulate HTTP request lifecycle
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var method = "POST";
            var uri = "https://api.demo.com/manuscripts";

            using var httpScope = DiagnosticEvents.Scopes.HttpRequest(_logger, method, uri, requestId);

            _logger.LogHttpRequestStarted(method, uri, requestId);

            // Simulate request processing
            await Task.Delay(150);

            var statusCode = 201;
            var elapsed = TimeSpan.FromMilliseconds(150);
            _logger.LogHttpRequestCompleted(method, uri, requestId, statusCode, elapsed);

            // Simulate manuscript upload
            var fileName = "demo-manuscript.pdf";
            var fileSize = 2048L;

            _logger.LogManuscriptUploadStarted(fileName, fileSize);
            await Task.Delay(200);
            _logger.LogManuscriptUploadCompleted(fileName, fileSize.ToString(), TimeSpan.FromMilliseconds(200));

            // Simulate webhook processing
            var webhookId = "webhook-" + Guid.NewGuid().ToString("N")[..8];
            var eventType = "MarkAsReferee";

            _logger.LogWebhookProcessingStarted(eventType, webhookId);
            await Task.Delay(50);
            _logger.LogWebhookProcessingCompleted(eventType, webhookId, TimeSpan.FromMilliseconds(50));

            Console.WriteLine("✅ Diagnostic events logged - check logs for detailed event information");
        }

        private async Task DemonstrateErrorLogging()
        {
            Console.WriteLine("🚨 Error Logging Demo");
            Console.WriteLine("-" + new string('-', 30));

            _logger.LogInformation("Starting error logging demonstration");

            try
            {
                // Simulate authentication failure
                var authException = new UnauthorizedAccessException("Invalid API key provided");
                _logger.LogError(DiagnosticEvents.EventIds.AuthenticationFailed, authException,
                    "Authentication failed for organization {OrganizationCode} after {ElapsedMs:F1}ms",
                    "demo-org", 100.0);

                // Simulate HTTP request failure
                var requestId = Guid.NewGuid().ToString("N")[..8];
                var httpException = new HttpRequestException("Network timeout occurred");
                _logger.LogHttpRequestFailed(httpException, "GET", "https://api.demo.com/timeout", requestId, TimeSpan.FromMilliseconds(5000));

                // Simulate serialization failure
                var serializationException = new InvalidOperationException("Unable to serialize complex object");
                _logger.LogError(DiagnosticEvents.EventIds.SerializationFailed, serializationException,
                    "Serialization failed for operation {Operation} on type {Type} after {ElapsedMs:F1}ms",
                    "Serialize", typeof(object).Name, 25.0);

                // Simulate webhook processing failure
                var webhookException = new ArgumentException("Invalid webhook payload format");
                var webhookId = "webhook-" + Guid.NewGuid().ToString("N")[..8];
                _logger.LogWebhookProcessingFailed(webhookException, "InvalidEvent", webhookId, TimeSpan.FromMilliseconds(75));

                Console.WriteLine("✅ Error scenarios logged - check logs for detailed error information");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during error logging demonstration");
                throw;
            }

            await Task.CompletedTask;
        }

        private void ShowMetricsSummary()
        {
            Console.WriteLine("📊 Performance Metrics Summary");
            Console.WriteLine("-" + new string('-', 30));

            var counters = DiagnosticEvents.Metrics.GetCounters();

            if (counters.Count == 0)
            {
                Console.WriteLine("No metrics recorded");
                return;
            }

            foreach (var kvp in counters)
            {
                var counter = kvp.Value;
                Console.WriteLine($"📈 {kvp.Key}:");
                Console.WriteLine($"   Count: {counter.Count}");
                
                if (counter.Count > 0 && counter.Average > 0)
                {
                    Console.WriteLine($"   Average: {counter.Average:F2}ms");
                    Console.WriteLine($"   Min: {counter.Min:F2}ms");
                    Console.WriteLine($"   Max: {counter.Max:F2}ms");
                }
                
                Console.WriteLine();
            }

            // Log performance summary
            var metricsData = new Dictionary<string, object>();
            foreach (var kvp in counters)
            {
                metricsData[kvp.Key] = kvp.Value.ToString();
            }
            _logger.LogPerformanceMetrics(metricsData);
        }

        /// <summary>
        /// Creates a service collection configured with comprehensive logging for the demo.
        /// </summary>
        public static ServiceCollection CreateServiceCollection()
        {
            var services = new ServiceCollection();

            // Configure logging with console provider
            services.AddLogging(builder =>
            {
                builder.AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                });
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Configure Prophy API Client with logging
            services.AddSingleton<ProphyApiClientConfiguration>(provider =>
            {
                return new ProphyApiClientConfiguration
                {
                    ApiKey = "demo-api-key-for-logging-demo",
                    OrganizationCode = "demo-org",
                    BaseUrl = "https://api.demo.com"
                };
            });

            // Configure HTTP client with logging handler
            services.AddHttpClient<ProphyApiClient>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<ProphyApiClientConfiguration>();
                client.BaseAddress = new Uri(config.BaseUrl);
                client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
            })
            .AddHttpMessageHandler<LoggingHandler>();

            // Register logging handler
            services.AddTransient<LoggingHandler>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<LoggingHandler>>();
                var options = new LoggingOptions
                {
                    LogRequests = true,
                    LogResponses = true,
                    LogHeaders = true,
                    LogRequestBody = true,
                    LogResponseBody = true,
                    LogPerformanceMetrics = true,
                    MaxBodyLogLength = 1024,
                    SlowRequestThresholdMs = 1000
                };
                return new LoggingHandler(logger, options);
            });

            // Register the API client
            services.AddTransient<ProphyApiClient>();

            // Register the demo class
            services.AddTransient<LoggingDemo>();

            return services;
        }
    }
} 