using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Serialization;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates the serialization layer capabilities of the Prophy API Client.
    /// </summary>
    public static class SerializationDemo
    {
        /// <summary>
        /// Runs the serialization demonstration.
        /// </summary>
        /// <param name="logger">Logger for output.</param>
        public static async Task RunAsync(ILogger logger)
        {
            logger.LogInformation("🔧 Serialization Layer Demo");
            logger.LogInformation("============================");

            await DemoJsonSerialization(logger);
            await DemoMultipartFormData(logger);
            await DemoCustomFieldHandling(logger);

            logger.LogInformation("✅ Serialization layer demo completed successfully!");
        }

        private static async Task DemoJsonSerialization(ILogger logger)
        {
            logger.LogInformation("\n📄 JSON Serialization Demo");
            logger.LogInformation("---------------------------");

            // Create a JSON serializer
            var serializer = SerializationFactory.CreateJsonSerializer();

            // Demo object
            var testObject = new
            {
                Title = "Sample Manuscript",
                Authors = new[] { "Dr. John Doe", "Dr. Jane Smith" },
                SubmissionDate = DateTime.UtcNow,
                Keywords = new[] { "research", "science", "peer-review" },
                Metadata = new Dictionary<string, object>
                {
                    { "pages", 15 },
                    { "figures", 3 },
                    { "isOpenAccess", true }
                }
            };

            // Serialize to JSON
            var json = serializer.Serialize(testObject);
            logger.LogInformation("✅ Serialized object to JSON ({Length} characters)", json.Length);
            logger.LogInformation("JSON Preview: {JsonPreview}...", json.Substring(0, Math.Min(100, json.Length)));

            // Deserialize back
            var deserialized = serializer.Deserialize<Dictionary<string, object>>(json);
            logger.LogInformation("✅ Deserialized JSON back to object with {Count} properties", deserialized.Count);

            // Demo async stream serialization
            using var stream = new MemoryStream();
            await serializer.SerializeAsync(stream, testObject);
            logger.LogInformation("✅ Serialized object to stream ({Size} bytes)", stream.Length);

            // Demo async stream deserialization
            stream.Position = 0;
            var streamDeserialized = await serializer.DeserializeAsync<Dictionary<string, object>>(stream);
            logger.LogInformation("✅ Deserialized from stream with {Count} properties", streamDeserialized.Count);
        }

        private static Task DemoMultipartFormData(ILogger logger)
        {
            logger.LogInformation("\n📎 Multipart Form Data Demo");
            logger.LogInformation("----------------------------");

            // Create a multipart form data builder
            var builder = SerializationFactory.CreateMultipartFormDataBuilder();

            // Add various fields
            builder
                .AddField("title", "Sample Manuscript Upload")
                .AddField("abstract", "This is a sample abstract for demonstration purposes.")
                .AddField("authors", "Dr. John Doe, Dr. Jane Smith")
                .AddField("keywords", "research,science,peer-review");

            // Add metadata fields
            var metadata = new Dictionary<string, string>
            {
                { "pages", "15" },
                { "figures", "3" },
                { "isOpenAccess", "true" }
            };
            builder.AddFields(metadata);

            // Add a sample file (simulated)
            var fileContent = Encoding.UTF8.GetBytes("This is sample manuscript content for demonstration.");
            builder.AddFile("manuscript", "sample-paper.txt", fileContent, "text/plain");

            // Add another file using stream
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Supplementary material content"));
            builder.AddFile("supplementary", "supplement.txt", stream, "text/plain");

            // Build the multipart content
            using var multipartContent = builder.Build();
            logger.LogInformation("✅ Built multipart form data with {Count} parts", multipartContent.Count());

            // Demo clearing and rebuilding
            builder.Clear()
                   .AddField("newField", "newValue");
            
            using var clearedContent = builder.Build();
            logger.LogInformation("✅ Cleared and rebuilt with {Count} parts", clearedContent.Count());

            return Task.CompletedTask;
        }

        private static Task DemoCustomFieldHandling(ILogger logger)
        {
            logger.LogInformation("\n🔧 Custom Field Handling Demo");
            logger.LogInformation("------------------------------");

            // Create serializer with custom field converter
            var options = SerializationFactory.CreateJsonSerializerOptions(includeCustomFieldConverter: true);
            var serializer = SerializationFactory.CreateJsonSerializer(options);

            // Demo various custom field types
            var customFields = new Dictionary<string, object>
            {
                { "stringField", "Sample text value" },
                { "numberField", 42 },
                { "decimalField", 3.14159 },
                { "booleanField", true },
                { "dateField", DateTime.UtcNow },
                { "arrayField", new[] { "option1", "option2", "option3" } },
                { "objectField", new { nested = "value", count = 5 } }
            };

            // Serialize custom fields
            var json = serializer.Serialize(customFields);
            logger.LogInformation("✅ Serialized custom fields to JSON ({Length} characters)", json.Length);

            // Deserialize custom fields
            var deserialized = serializer.Deserialize<Dictionary<string, object>>(json);
            logger.LogInformation("✅ Deserialized custom fields with {Count} fields", deserialized.Count);

            // Log field types
            foreach (var field in deserialized)
            {
                var valueType = field.Value?.GetType().Name ?? "null";
                logger.LogInformation("  - {FieldName}: {ValueType}", field.Key, valueType);
            }

            return Task.CompletedTask;
        }
    }
} 