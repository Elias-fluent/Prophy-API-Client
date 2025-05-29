using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Entities
{
    /// <summary>
    /// Represents a custom field definition with type information and validation options.
    /// </summary>
    public class CustomField
    {
        /// <summary>
        /// Gets or sets the unique API identifier for the custom field.
        /// </summary>
        [Required]
        [JsonPropertyName("apiId")]
        public string ApiId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the custom field.
        /// </summary>
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the custom field.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the data type of the custom field.
        /// </summary>
        [Required]
        [JsonPropertyName("dataType")]
        public CustomFieldDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets whether the custom field is required.
        /// </summary>
        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets whether the custom field allows multiple values.
        /// </summary>
        [JsonPropertyName("isMultiple")]
        public bool IsMultiple { get; set; }

        /// <summary>
        /// Gets or sets the default value for the custom field.
        /// </summary>
        [JsonPropertyName("defaultValue")]
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the available options for single-option or multi-option fields.
        /// </summary>
        [JsonPropertyName("options")]
        public List<CustomFieldOption>? Options { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for number fields.
        /// </summary>
        [JsonPropertyName("minValue")]
        public double? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for number fields.
        /// </summary>
        [JsonPropertyName("maxValue")]
        public double? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum length for string fields.
        /// </summary>
        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length for string fields.
        /// </summary>
        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the validation pattern (regex) for string fields.
        /// </summary>
        [JsonPropertyName("validationPattern")]
        public string? ValidationPattern { get; set; }

        /// <summary>
        /// Gets or sets the validation error message.
        /// </summary>
        [JsonPropertyName("validationMessage")]
        public string? ValidationMessage { get; set; }

        /// <summary>
        /// Gets or sets the display order of the custom field.
        /// </summary>
        [JsonPropertyName("displayOrder")]
        public int? DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets whether the custom field is visible in the UI.
        /// </summary>
        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the custom field is enabled.
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets additional metadata for the custom field.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the date when the custom field was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date when the custom field was last updated.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents an option for single-option or multi-option custom fields.
    /// </summary>
    public class CustomFieldOption
    {
        /// <summary>
        /// Gets or sets the unique identifier for the option.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the display value of the option.
        /// </summary>
        [Required]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display label of the option.
        /// </summary>
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets whether the option is selected by default.
        /// </summary>
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets whether the option is enabled.
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the display order of the option.
        /// </summary>
        [JsonPropertyName("displayOrder")]
        public int? DisplayOrder { get; set; }
    }

    /// <summary>
    /// Defines the supported data types for custom fields.
    /// </summary>
    [JsonConverter(typeof(Prophy.ApiClient.Serialization.CustomFieldDataTypeConverter))]
    public enum CustomFieldDataType
    {
        /// <summary>
        /// String/text data type.
        /// </summary>
        String,

        /// <summary>
        /// Numeric data type.
        /// </summary>
        Number,

        /// <summary>
        /// Date data type.
        /// </summary>
        Date,

        /// <summary>
        /// Boolean data type.
        /// </summary>
        Boolean,

        /// <summary>
        /// Single-option selection data type.
        /// </summary>
        SingleOption,

        /// <summary>
        /// Multi-option selection data type.
        /// </summary>
        MultiOption,

        /// <summary>
        /// Array data type.
        /// </summary>
        Array,

        /// <summary>
        /// Object data type.
        /// </summary>
        Object
    }
} 