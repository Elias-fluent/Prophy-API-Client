using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// Custom JSON converter for CustomFieldDataType enum that uses JsonPropertyName attributes.
    /// </summary>
    public class CustomFieldDataTypeConverter : JsonConverter<CustomFieldDataType>
    {
        public override CustomFieldDataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string token, got {reader.TokenType}");
            }

            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                throw new JsonException("Cannot convert null or empty string to CustomFieldDataType");
            }

            // Get all enum values and find the one with matching JsonPropertyName
            foreach (CustomFieldDataType enumValue in Enum.GetValues(typeof(CustomFieldDataType)))
            {
                var memberInfo = typeof(CustomFieldDataType).GetMember(enumValue.ToString())[0];
                var jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
                
                if (jsonPropertyNameAttribute?.Name == stringValue)
                {
                    return enumValue;
                }
            }

            throw new JsonException($"Unable to convert \"{stringValue}\" to CustomFieldDataType");
        }

        public override void Write(Utf8JsonWriter writer, CustomFieldDataType value, JsonSerializerOptions options)
        {
            var memberInfo = typeof(CustomFieldDataType).GetMember(value.ToString())[0];
            var jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            
            if (jsonPropertyNameAttribute?.Name != null)
            {
                writer.WriteStringValue(jsonPropertyNameAttribute.Name);
            }
            else
            {
                // Fallback to enum name if no JsonPropertyName attribute
                writer.WriteStringValue(value.ToString());
            }
        }
    }
} 