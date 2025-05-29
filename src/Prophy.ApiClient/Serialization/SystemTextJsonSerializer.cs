using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// System.Text.Json implementation of IJsonSerializer with Prophy API-specific configuration.
    /// </summary>
    public class SystemTextJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _options;
        private readonly ILogger<SystemTextJsonSerializer> _logger;

        /// <summary>
        /// Initializes a new instance of the SystemTextJsonSerializer class.
        /// </summary>
        /// <param name="logger">The logger instance for logging serialization operations.</param>
        public SystemTextJsonSerializer(ILogger<SystemTextJsonSerializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = CreateJsonSerializerOptions();
        }

        /// <summary>
        /// Initializes a new instance of the SystemTextJsonSerializer class with custom options.
        /// </summary>
        /// <param name="options">Custom JSON serializer options.</param>
        /// <param name="logger">The logger instance for logging serialization operations.</param>
        public SystemTextJsonSerializer(JsonSerializerOptions options, ILogger<SystemTextJsonSerializer> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public string Serialize<T>(T value)
        {
            try
            {
                _logger.LogDebug("Serializing object of type {Type}", typeof(T).Name);
                var json = JsonSerializer.Serialize(value, _options);
                _logger.LogDebug("Successfully serialized object to JSON ({Length} characters)", json.Length);
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize object of type {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            try
            {
                _logger.LogDebug("Deserializing JSON to type {Type} ({Length} characters)", typeof(T).Name, json.Length);
                var result = JsonSerializer.Deserialize<T>(json, _options);
                _logger.LogDebug("Successfully deserialized JSON to type {Type}", typeof(T).Name);
                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize JSON to type {Type}. JSON: {Json}", typeof(T).Name, json);
                throw;
            }
        }

        /// <inheritdoc />
        public object? Deserialize(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            try
            {
                _logger.LogDebug("Deserializing JSON to type {Type} ({Length} characters)", type.Name, json.Length);
                var result = JsonSerializer.Deserialize(json, type, _options);
                _logger.LogDebug("Successfully deserialized JSON to type {Type}", type.Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize JSON to type {Type}. JSON: {Json}", type.Name, json);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                _logger.LogDebug("Asynchronously deserializing stream to type {Type}", typeof(T).Name);
                var result = await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken);
                _logger.LogDebug("Successfully deserialized stream to type {Type}", typeof(T).Name);
                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize stream to type {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            try
            {
                _logger.LogDebug("Asynchronously deserializing stream to type {Type}", type.Name);
                var result = await JsonSerializer.DeserializeAsync(stream, type, _options, cancellationToken);
                _logger.LogDebug("Successfully deserialized stream to type {Type}", type.Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize stream to type {Type}", type.Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                _logger.LogDebug("Asynchronously serializing object of type {Type} to stream", typeof(T).Name);
                await JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken);
                _logger.LogDebug("Successfully serialized object of type {Type} to stream", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize object of type {Type} to stream", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Creates the default JSON serializer options for the Prophy API.
        /// </summary>
        /// <returns>Configured JsonSerializerOptions instance.</returns>
        private static JsonSerializerOptions CreateJsonSerializerOptions()
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

            // Add custom converters for specific types
            options.Converters.Add(new CustomFieldDataTypeConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            
            return options;
        }
    }
} 