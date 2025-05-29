using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// Factory for creating serialization components with proper configuration.
    /// </summary>
    public static class SerializationFactory
    {
        /// <summary>
        /// Creates a configured JSON serializer for the Prophy API.
        /// </summary>
        /// <param name="logger">Optional logger for the serializer.</param>
        /// <returns>A configured IJsonSerializer instance.</returns>
        public static IJsonSerializer CreateJsonSerializer(ILogger<SystemTextJsonSerializer>? logger = null)
        {
            var actualLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SystemTextJsonSerializer>.Instance;
            return new SystemTextJsonSerializer(actualLogger);
        }

        /// <summary>
        /// Creates a configured JSON serializer with custom options for the Prophy API.
        /// </summary>
        /// <param name="options">Custom JSON serializer options.</param>
        /// <param name="logger">Optional logger for the serializer.</param>
        /// <returns>A configured IJsonSerializer instance.</returns>
        public static IJsonSerializer CreateJsonSerializer(JsonSerializerOptions options, ILogger<SystemTextJsonSerializer>? logger = null)
        {
            var actualLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SystemTextJsonSerializer>.Instance;
            return new SystemTextJsonSerializer(options, actualLogger);
        }

        /// <summary>
        /// Creates a multipart form data builder for file uploads.
        /// </summary>
        /// <param name="logger">Optional logger for the builder.</param>
        /// <returns>A configured IMultipartFormDataBuilder instance.</returns>
        public static IMultipartFormDataBuilder CreateMultipartFormDataBuilder(ILogger<MultipartFormDataBuilder>? logger = null)
        {
            var actualLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MultipartFormDataBuilder>.Instance;
            return new MultipartFormDataBuilder(actualLogger);
        }

        /// <summary>
        /// Creates JSON serializer options configured for the Prophy API.
        /// </summary>
        /// <param name="includeCustomFieldConverter">Whether to include the custom field converter.</param>
        /// <returns>Configured JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions CreateJsonSerializerOptions(bool includeCustomFieldConverter = true)
        {
            var options = new JsonSerializerOptions
            {
                // Use camelCase property naming to match API expectations
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                
                // Allow reading and writing of comments in JSON
                ReadCommentHandling = JsonCommentHandling.Skip,
                
                // Note: DefaultIgnoreCondition is not available in System.Text.Json 4.7.2 (.NET Framework 4.8)
                // Null handling will be managed through JsonPropertyName attributes on individual properties
                
                // Allow trailing commas in JSON
                AllowTrailingCommas = true,
                
                // Case-insensitive property matching for robustness
                PropertyNameCaseInsensitive = true,
                
                // Write indented JSON for debugging (can be disabled in production)
                WriteIndented = false
            };

            // Add standard converters
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            
            // Add custom field converter if requested
            if (includeCustomFieldConverter)
            {
                options.Converters.Add(new CustomFieldJsonConverter());
            }
            
            return options;
        }
    }
} 