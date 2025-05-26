using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Modules;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates manuscript upload functionality using the Prophy API Client.
    /// </summary>
    public static class ManuscriptDemo
    {
        /// <summary>
        /// Runs the manuscript upload demonstration.
        /// </summary>
        /// <param name="client">The Prophy API client instance.</param>
        /// <param name="logger">The logger instance.</param>
        public static async Task RunAsync(ProphyApiClient client, ILogger logger)
        {
            try
            {
                Console.WriteLine("📄 Manuscript Upload Demo");
                Console.WriteLine("========================");
                Console.WriteLine();

                // Create a sample manuscript upload request
                var manuscriptRequest = CreateSampleManuscriptRequest();

                Console.WriteLine($"📝 Sample Manuscript Details:");
                Console.WriteLine($"   Title: {manuscriptRequest.Title}");
                Console.WriteLine($"   Authors: {string.Join(", ", manuscriptRequest.Authors ?? new List<string>())}");
                Console.WriteLine($"   File: {manuscriptRequest.FileName} ({manuscriptRequest.FileContent?.Length ?? 0} bytes)");
                Console.WriteLine($"   Subject: {manuscriptRequest.Subject}");
                Console.WriteLine($"   Language: {manuscriptRequest.Language}");
                Console.WriteLine();

                // Create progress reporter
                var progress = new Progress<UploadProgress>(p =>
                {
                    Console.WriteLine($"   📊 {p.Stage}: {p.Message} ({p.PercentageComplete:F1}%)");
                });

                Console.WriteLine("🚀 Starting manuscript upload...");
                Console.WriteLine();

                try
                {
                    // Note: This will fail with authentication error since we're using sample credentials
                    // but it demonstrates the API usage
                    var response = await client.Manuscripts.UploadAsync(manuscriptRequest, progress);

                    Console.WriteLine("✅ Upload completed successfully!");
                    Console.WriteLine($"   Manuscript ID: {response.Manuscript?.Id ?? "N/A"}");
                    Console.WriteLine($"   Status: {response.ProcessingStatus ?? "N/A"}");
                    Console.WriteLine($"   Success: {response.Success}");
                    Console.WriteLine($"   Message: {response.Message ?? "N/A"}");
                    Console.WriteLine($"   Request ID: {response.RequestId ?? "N/A"}");
                    
                    if (response.Errors?.Count > 0)
                    {
                        Console.WriteLine($"   Errors: {string.Join(", ", response.Errors)}");
                    }
                    
                    if (response.Warnings?.Count > 0)
                    {
                        Console.WriteLine($"   Warnings: {string.Join(", ", response.Warnings)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Expected error (demo with sample credentials): {ex.GetType().Name}");
                    Console.WriteLine($"   Message: {ex.Message}");
                    Console.WriteLine();
                    Console.WriteLine("💡 This demonstrates the API structure and error handling.");
                    Console.WriteLine("   In a real application, you would use valid API credentials.");
                }

                Console.WriteLine();
                Console.WriteLine("📋 Manuscript Upload API Features Demonstrated:");
                Console.WriteLine("   ✅ Multipart form data handling");
                Console.WriteLine("   ✅ Progress reporting during upload");
                Console.WriteLine("   ✅ File validation (size, type, required fields)");
                Console.WriteLine("   ✅ Metadata support (title, authors, keywords, etc.)");
                Console.WriteLine("   ✅ Custom fields and additional metadata");
                Console.WriteLine("   ✅ Comprehensive error handling");
                Console.WriteLine("   ✅ Cancellation token support");
                Console.WriteLine();

                // Demonstrate status retrieval
                Console.WriteLine("🔍 Demonstrating status retrieval...");
                try
                {
                    var statusResponse = await client.Manuscripts.GetStatusAsync("sample-manuscript-id");
                    Console.WriteLine($"✅ Status retrieved: {statusResponse.ProcessingStatus}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Expected error: {ex.GetType().Name} - {ex.Message}");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in manuscript demo");
                Console.WriteLine($"❌ Demo error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a sample manuscript upload request for demonstration purposes.
        /// </summary>
        /// <returns>A configured manuscript upload request.</returns>
        private static ManuscriptUploadRequest CreateSampleManuscriptRequest()
        {
            // Create sample PDF content (just text for demo)
            var sampleContent = @"
Sample Manuscript Content
========================

Abstract
--------
This is a sample manuscript for demonstrating the Prophy API Client library.
It contains sample content to show how file uploads work with the API.

Introduction
-----------
The Prophy API enables researchers and publishers to find qualified peer reviewers
for academic manuscripts using advanced AI and machine learning techniques.

Methodology
----------
This sample demonstrates the integration capabilities of the Prophy API Client
library for .NET applications.

Results
-------
The API client successfully handles file uploads, metadata, and progress reporting.

Conclusion
----------
The Prophy API Client provides a robust foundation for integrating peer review
capabilities into academic publishing workflows.

References
----------
[1] Prophy Scientific Knowledge Management Platform
[2] .NET Standard 2.0 Compatibility Guidelines
";

            var fileContent = Encoding.UTF8.GetBytes(sampleContent);

            return new ManuscriptUploadRequest
            {
                Title = "Sample Manuscript: Prophy API Integration Demo",
                Abstract = "This sample manuscript demonstrates the capabilities of the Prophy API Client library for .NET applications, showcasing file upload, metadata handling, and progress reporting features.",
                Authors = new List<string>
                {
                    "Dr. Jane Smith",
                    "Prof. John Doe",
                    "Dr. Alice Johnson"
                },
                Keywords = new List<string>
                {
                    "API Integration",
                    "Peer Review",
                    "Academic Publishing",
                    ".NET Development",
                    "Scientific Knowledge Management"
                },
                Subject = "Computer Science",
                Type = "research-article",
                Language = "en",
                Folder = "demo-submissions",
                OriginId = "demo-manuscript-001",
                FileName = "sample-manuscript.txt",
                FileContent = fileContent,
                MimeType = "text/plain",
                CustomFields = new Dictionary<string, object>
                {
                    { "funding_source", "Demo Grant Foundation" },
                    { "research_area", "API Development" },
                    { "submission_type", "demo" }
                },
                Metadata = new Dictionary<string, object>
                {
                    { "demo_version", "1.0" },
                    { "created_by", "Prophy API Client Demo" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                }
            };
        }
    }
} 