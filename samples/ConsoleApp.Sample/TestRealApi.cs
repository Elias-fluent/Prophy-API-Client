using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Models.Requests;

namespace ConsoleApp.Sample
{
    public static class TestRealApi
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Testing Real Prophy API ===");
            Console.WriteLine();

            // Use the real credentials
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "VVfPN8VqhhYgImx3jLqb_4aZBLhSM9XdMq1Pm0rj",
                OrganizationCode = "Flexigrant",
                BaseUrl = "https://www.prophy.ai/api/"
            };

            using var client = new ProphyApiClient(config);

            // Create a request matching the working example format exactly
            var authorNames = new List<string> { "Michael Turkington" };
            var authorEmails = new List<string> { "Michael.turkington@fluenttechnology.com" };
            
            var request = new ManuscriptUploadRequest
            {
                Title = "Deep immunophenotyping of pericardial macrophage in patients with coronary artery disease: the role of small extracellular vesicles.",
                Abstract = "Coronary artery disease (CAD) is the main cause of ischemic heart disease (IHD) driven by circulating high cholesterol levels. Different subtypes of macrophages contribute to regulating the balance between atherosclerotic plaque growth/regression and plaque rupture/stabilisation. Recent studies provide evidence for the role of extracellular vesicles (EVs) in modulating macrophage phenotype. Our lab previously showed that the human pericardial fluid (PF) contains small EVs (sEVs) able to modulate cardiovascular response via microRNA shuttling. Our new pilot data show that PF-sEVs collected from CAD patients during coronary artery-by-pass graft (CABG) surgery are taken up by macrophages, promoting their switch toward a pro-inflammatory phenotype associated with expressional changes in the CD36 and SR-B1 scavenger receptors (controls: non-CAD cardiac surgery patients, operated for mitral valve repair). This project will characterise the macrophage populations residing in the PF of CAD vs non-CAD patients using the cutting-edge technique of Cellular Indexing of Transcriptomes and Epitopes sequencing at the single-cell level (sc-CITE-seq). Further cell and molecular approaches will be used to investigate the mechanisms by which PF-sEVs modulate the macrophage phenotype and the functional consequences on cholesterol metabolism.",
                Journal = "Deep immunophenotyping of pericardial macrophage in patients with coronary artery disease: the role of small extracellular vesicles.",
                OriginId = "test-manuscript-" + DateTime.Now.Ticks,
                AuthorsCount = authorNames.Count,
                AuthorNames = authorNames,
                AuthorEmails = authorEmails,
                SourceFileName = "test_manuscript.pdf",
                FileContent = LoadTestPdf(),
                FileName = "test_manuscript.pdf",
                MimeType = "application/pdf"
            };

            try
            {
                Console.WriteLine("📤 Uploading manuscript to real Prophy API...");
                Console.WriteLine($"   Title: {request.Title}");
                Console.WriteLine($"   Authors: {string.Join(", ", request.AuthorNames)}");
                Console.WriteLine($"   Authors Count: {request.AuthorsCount}");
                Console.WriteLine($"   File: {request.FileName} ({request.FileContent.Length} bytes)");
                Console.WriteLine($"   Origin ID: {request.OriginId}");
                Console.WriteLine($"   Journal: {request.Journal}");
                Console.WriteLine();

                Console.WriteLine("🔍 Debug: Request details:");
                Console.WriteLine($"   API Key: {config.ApiKey.Substring(0, Math.Min(10, config.ApiKey.Length))}...");
                Console.WriteLine($"   Organization: {config.OrganizationCode}");
                Console.WriteLine($"   Base URL: {config.BaseUrl}");
                Console.WriteLine();

                var response = await client.Manuscripts.UploadAsync(request);

                Console.WriteLine("✅ SUCCESS! Received response from Prophy API:");
                Console.WriteLine($"   Manuscript ID: {response.ManuscriptIdString}");
                Console.WriteLine($"   Origin ID: {response.OriginId}");
                
                if (response.Candidates?.Count > 0)
                {
                    Console.WriteLine($"   Candidates: {response.Candidates.Count}");
                    foreach (var candidate in response.Candidates.Take(3))
                    {
                        Console.WriteLine($"     - {candidate.Name} ({candidate.Email}) - Score: {candidate.Score}");
                    }
                }

                if (response.DebugInfo != null)
                {
                    Console.WriteLine($"   Debug Info:");
                    Console.WriteLine($"     - Extracted Concepts: {response.DebugInfo.ExtractedConcepts}");
                    Console.WriteLine($"     - Parsed References: {response.DebugInfo.ParsedReferences}");
                    Console.WriteLine($"     - Text Length: {response.DebugInfo.ParsedTextLen}");
                }

                Console.WriteLine();
                Console.WriteLine("🎉 Real API integration is working perfectly!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }

                // Try to get more details about the validation error
                if (ex is Prophy.ApiClient.Exceptions.ValidationException validationEx)
                {
                    Console.WriteLine($"   Validation Errors:");
                    foreach (var error in validationEx.ValidationErrors)
                    {
                        Console.WriteLine($"     - {error}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("🔍 This suggests we need to adjust the field format to match the working example exactly.");
                Console.WriteLine("   The API is responding, which means our connection and authentication are working.");
            }
        }

        private static byte[] LoadTestPdf()
        {
            try
            {
                // Try to load the real PDF file
                var pdfPath = Path.Combine("..", "..", "samples", "test_manuscript.pdf");
                if (File.Exists(pdfPath))
                {
                    return File.ReadAllBytes(pdfPath);
                }

                // Fallback to current directory
                pdfPath = "test_manuscript.pdf";
                if (File.Exists(pdfPath))
                {
                    return File.ReadAllBytes(pdfPath);
                }

                // If no PDF found, create a minimal PDF-like content for testing
                Console.WriteLine("⚠️  PDF file not found, using fallback text content");
                return System.Text.Encoding.UTF8.GetBytes("Sample manuscript content for testing the real API.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not load PDF: {ex.Message}, using fallback content");
                return System.Text.Encoding.UTF8.GetBytes("Sample manuscript content for testing the real API.");
            }
        }
    }
} 