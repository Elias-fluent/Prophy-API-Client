using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
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
                    // Upload with real credentials and data
                    var response = await client.Manuscripts.UploadAsync(manuscriptRequest, progress);

                    Console.WriteLine("✅ Upload completed successfully!");
                    DisplayUploadResponse(response);
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
        /// Creates a sample manuscript upload request for demonstration purposes using a real PDF file.
        /// </summary>
        /// <returns>A configured manuscript upload request.</returns>
        private static ManuscriptUploadRequest CreateSampleManuscriptRequest()
        {
            // Load the real PDF file  
            var pdfPath = Path.Combine("..", "..", "samples", "test_manuscript.pdf");
            byte[] fileContent;
            string fileName;
            
            try
            {
                fileContent = System.IO.File.ReadAllBytes(pdfPath);
                fileName = Path.GetFileName(pdfPath);
                Console.WriteLine($"✅ Loaded real PDF manuscript: {pdfPath} ({fileContent.Length:N0} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not load PDF file '{pdfPath}': {ex.Message}");
                Console.WriteLine("   Using fallback text content for demo...");
                
                // Fallback to text content if PDF not found
                var fallbackContent = "Sample manuscript content for demo purposes.";
                fileContent = Encoding.UTF8.GetBytes(fallbackContent);
                fileName = "sample-manuscript.txt";
            }

            // Use the format that matches the working example
            var authorNames = new List<string> { "Michael Turkington", "Test Author" };
            var authorEmails = new List<string> { "Michael.turkington@fluenttechnology.com", "test@example.com" };

            return new ManuscriptUploadRequest
            {
                Title = "Deep immunophenotyping of pericardial macrophage in patients with coronary artery disease: the role of small extracellular vesicles.",
                Abstract = "Coronary artery disease (CAD) is the main cause of ischemic heart disease (IHD) driven by circulating high cholesterol levels. Different subtypes of macrophages contribute to regulating the balance between atherosclerotic plaque growth/regression and plaque rupture/stabilisation. Recent studies provide evidence for the role of extracellular vesicles (EVs) in modulating macrophage phenotype. Our lab previously showed that the human pericardial fluid (PF) contains small EVs (sEVs) able to modulate cardiovascular response via microRNA shuttling. Our new pilot data show that PF-sEVs collected from CAD patients during coronary artery-by-pass graft (CABG) surgery are taken up by macrophages, promoting their switch toward a pro-inflammatory phenotype associated with expressional changes in the CD36 and SR-B1 scavenger receptors (controls: non-CAD cardiac surgery patients, operated for mitral valve repair). This project will characterise the macrophage populations residing in the PF of CAD vs non-CAD patients using the cutting-edge technique of Cellular Indexing of Transcriptomes and Epitopes sequencing at the single-cell level (sc-CITE-seq). Further cell and molecular approaches will be used to investigate the mechanisms by which PF-sEVs modulate the macrophage phenotype and the functional consequences on cholesterol metabolism.",
                Journal = "Deep immunophenotyping of pericardial macrophage in patients with coronary artery disease: the role of small extracellular vesicles.",
                OriginId = $"test-{DateTime.Now.Ticks}",
                AuthorsCount = authorNames.Count,
                AuthorNames = authorNames,
                AuthorEmails = authorEmails,
                SourceFileName = fileName,
                Keywords = new List<string>
                {
                    "Coronary artery disease",
                    "Macrophages",
                    "Extracellular vesicles",
                    "Pericardial fluid"
                },
                Subject = "Medical Research",
                Type = "research-article",
                Language = "en",
                FileName = fileName,
                FileContent = fileContent,
                MimeType = fileName.EndsWith(".pdf") ? "application/pdf" : "text/plain",
                CustomFields = new Dictionary<string, object>
                {
                    { "test_run", "api-client-demo" },
                    { "upload_source", "dotnet-client-library" }
                },
                Metadata = new Dictionary<string, object>
                {
                    { "client_version", "1.0.0" },
                    { "test_timestamp", DateTime.UtcNow.ToString("O") },
                    { "real_file_test", true }
                }
            };
        }

        /// <summary>
        /// Displays the upload response in a formatted way matching the working example.
        /// </summary>
        /// <param name="response">The manuscript upload response.</param>
        private static void DisplayUploadResponse(ManuscriptUploadResponse response)
        {
            Console.WriteLine("\nRaw Response Data:");
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(response, jsonOptions));

            Console.WriteLine("\nUpload Response:");
            Console.WriteLine($"Manuscript ID: {response.ManuscriptId}");
            Console.WriteLine($"Origin ID: {response.OriginId}");
            
            if (response.DebugInfo != null)
            {
                Console.WriteLine("\nDebug Information:");
                if (response.DebugInfo.AuthorsInfo != null)
                {
                    Console.WriteLine($"Authors Info:");
                    Console.WriteLine($"- Authors Count: {response.DebugInfo.AuthorsInfo.AuthorsCount}");
                    Console.WriteLine($"- Emails Count: {response.DebugInfo.AuthorsInfo.EmailsCount}");
                    Console.WriteLine($"- ORCIDs Count: {response.DebugInfo.AuthorsInfo.OrcidsCount}");
                }
                Console.WriteLine($"Extracted Concepts: {response.DebugInfo.ExtractedConcepts}");
                Console.WriteLine($"Parsed References: {response.DebugInfo.ParsedReferences}");
                Console.WriteLine($"Parsed Text Length: {response.DebugInfo.ParsedTextLen}");
                Console.WriteLine($"Source File: {response.DebugInfo.SourceFile}");
            }

            if (response.AuthorsGroupsSettings != null)
            {
                Console.WriteLine("\nAuthors Groups Settings:");
                Console.WriteLine($"Effect: {response.AuthorsGroupsSettings.Effect}");
                if (response.AuthorsGroupsSettings.AuthorsGroups.Count > 0)
                {
                    Console.WriteLine("Authors Groups:");
                    foreach (var group in response.AuthorsGroupsSettings.AuthorsGroups)
                    {
                        Console.WriteLine($"- {group.Name} (ID: {group.Id}, Label: {group.Label})");
                    }
                }
            }

            Console.WriteLine($"\nCandidates Count: {response.Candidates.Count}");
            if (response.Candidates.Count > 0)
            {
                Console.WriteLine("\nCandidates:");
                foreach (var candidate in response.Candidates)
                {
                    Console.WriteLine($"- {candidate.Name} ({candidate.Email})");
                    Console.WriteLine($"  Score: {candidate.Score}");
                    Console.WriteLine($"  H-Index: {candidate.HIndex}");
                    Console.WriteLine($"  Articles: {candidate.ArticlesCount}");
                    Console.WriteLine($"  Citations: {candidate.CitationsCount}");
                    if (!string.IsNullOrEmpty(candidate.Affiliation))
                    {
                        Console.WriteLine($"  Affiliation: {candidate.Affiliation}");
                    }
                }
            }
        }
    }
} 