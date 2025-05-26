using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// Custom JSON converter for handling dynamic custom field values.
    /// Supports string, number, date, single-option, and multi-option field types.
    /// </summary>
    public class CustomFieldJsonConverter : JsonConverter<object>
    {
        private readonly ILogger<CustomFieldJsonConverter> _logger;

        /// <summary>
        /// Initializes a new instance of the CustomFieldJsonConverter class.
        /// </summary>
        /// <param name="logger">The logger instance for logging conversion operations.</param>
        public CustomFieldJsonConverter(ILogger<CustomFieldJsonConverter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the CustomFieldJsonConverter class with a null logger.
        /// </summary>
        public CustomFieldJsonConverter()
        {
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomFieldJsonConverter>.Instance;
        }

        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        var stringValue = reader.GetString();
                        _logger.LogDebug("Converted custom field to string value");
                        return stringValue;

                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out var intValue))
                        {
                            _logger.LogDebug("Converted custom field to int32 value: {Value}", intValue);
                            return intValue;
                        }
                        if (reader.TryGetInt64(out var longValue))
                        {
                            _logger.LogDebug("Converted custom field to int64 value: {Value}", longValue);
                            return longValue;
                        }
                        if (reader.TryGetDouble(out var doubleValue))
                        {
                            _logger.LogDebug("Converted custom field to double value: {Value}", doubleValue);
                            return doubleValue;
                        }
                        break;

                    case JsonTokenType.True:
                        _logger.LogDebug("Converted custom field to boolean value: true");
                        return true;

                    case JsonTokenType.False:
                        _logger.LogDebug("Converted custom field to boolean value: false");
                        return false;

                    case JsonTokenType.StartArray:
                        var list = new List<object?>();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            var item = Read(ref reader, typeof(object), options);
                            list.Add(item);
                        }
                        _logger.LogDebug("Converted custom field to array with {Count} items", list.Count);
                        return list;

                    case JsonTokenType.StartObject:
                        var dictionary = new Dictionary<string, object?>();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                var propertyName = reader.GetString();
                                reader.Read();
                                var value = Read(ref reader, typeof(object), options);
                                if (propertyName != null)
                                {
                                    dictionary[propertyName] = value;
                                }
                            }
                        }
                        _logger.LogDebug("Converted custom field to object with {Count} properties", dictionary.Count);
                        return dictionary;

                    case JsonTokenType.Null:
                        _logger.LogDebug("Converted custom field to null value");
                        return null;

                    default:
                        _logger.LogWarning("Unexpected JSON token type for custom field: {TokenType}", reader.TokenType);
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert custom field value from JSON");
                throw;
            }

            _logger.LogWarning("Could not convert custom field value, returning null");
            return null;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            try
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    _logger.LogDebug("Wrote null custom field value");
                    return;
                }

                switch (value)
                {
                    case string stringValue:
                        writer.WriteStringValue(stringValue);
                        _logger.LogDebug("Wrote string custom field value");
                        break;

                    case int intValue:
                        writer.WriteNumberValue(intValue);
                        _logger.LogDebug("Wrote int32 custom field value: {Value}", intValue);
                        break;

                    case long longValue:
                        writer.WriteNumberValue(longValue);
                        _logger.LogDebug("Wrote int64 custom field value: {Value}", longValue);
                        break;

                    case double doubleValue:
                        writer.WriteNumberValue(doubleValue);
                        _logger.LogDebug("Wrote double custom field value: {Value}", doubleValue);
                        break;

                    case float floatValue:
                        writer.WriteNumberValue(floatValue);
                        _logger.LogDebug("Wrote float custom field value: {Value}", floatValue);
                        break;

                    case decimal decimalValue:
                        writer.WriteNumberValue(decimalValue);
                        _logger.LogDebug("Wrote decimal custom field value: {Value}", decimalValue);
                        break;

                    case bool boolValue:
                        writer.WriteBooleanValue(boolValue);
                        _logger.LogDebug("Wrote boolean custom field value: {Value}", boolValue);
                        break;

                    case DateTime dateTimeValue:
                        writer.WriteStringValue(dateTimeValue.ToString("O")); // ISO 8601 format
                        _logger.LogDebug("Wrote DateTime custom field value: {Value}", dateTimeValue);
                        break;

                    case DateTimeOffset dateTimeOffsetValue:
                        writer.WriteStringValue(dateTimeOffsetValue.ToString("O")); // ISO 8601 format
                        _logger.LogDebug("Wrote DateTimeOffset custom field value: {Value}", dateTimeOffsetValue);
                        break;

                    default:
                        // For complex objects, serialize using the default serializer
                        JsonSerializer.Serialize(writer, value, value.GetType(), options);
                        _logger.LogDebug("Wrote complex custom field value of type: {Type}", value.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write custom field value to JSON");
                throw;
            }
        }
    }
} 