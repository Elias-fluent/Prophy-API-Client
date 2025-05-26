using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prophy.ApiClient.Models.Entities;

namespace Prophy.ApiClient.Models.Responses
{
    /// <summary>
    /// Response model for retrieving custom field definitions.
    /// </summary>
    public class CustomFieldDefinitionsResponse
    {
        /// <summary>
        /// Gets or sets the list of custom field definitions.
        /// </summary>
        [JsonPropertyName("customFields")]
        public List<CustomField> CustomFields { get; set; } = new List<CustomField>();

        /// <summary>
        /// Gets or sets the organization code these custom fields belong to.
        /// </summary>
        [JsonPropertyName("organizationCode")]
        public string? OrganizationCode { get; set; }

        /// <summary>
        /// Gets or sets the total count of custom fields.
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets whether the request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any error message if the request failed.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for custom field validation operations.
    /// </summary>
    public class CustomFieldValidationResponse
    {
        /// <summary>
        /// Gets or sets whether the validation was successful.
        /// </summary>
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<CustomFieldValidationError> Errors { get; set; } = new List<CustomFieldValidationError>();

        /// <summary>
        /// Gets or sets the validated custom field values.
        /// </summary>
        [JsonPropertyName("validatedValues")]
        public Dictionary<string, object>? ValidatedValues { get; set; }

        /// <summary>
        /// Gets or sets whether the request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any error message if the request failed.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Represents a validation error for a custom field.
    /// </summary>
    public class CustomFieldValidationError
    {
        /// <summary>
        /// Gets or sets the API ID of the custom field that failed validation.
        /// </summary>
        [JsonPropertyName("fieldApiId")]
        public string FieldApiId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the custom field that failed validation.
        /// </summary>
        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }

        /// <summary>
        /// Gets or sets the validation error message.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error code for the validation failure.
        /// </summary>
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the invalid value that caused the validation failure.
        /// </summary>
        [JsonPropertyName("invalidValue")]
        public object? InvalidValue { get; set; }

        /// <summary>
        /// Gets or sets the expected value format or constraint.
        /// </summary>
        [JsonPropertyName("expectedFormat")]
        public string? ExpectedFormat { get; set; }
    }

    /// <summary>
    /// Response model for custom field value operations.
    /// </summary>
    public class CustomFieldValueResponse
    {
        /// <summary>
        /// Gets or sets the custom field values.
        /// </summary>
        [JsonPropertyName("values")]
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the entity ID these values belong to.
        /// </summary>
        [JsonPropertyName("entityId")]
        public string? EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity type these values belong to.
        /// </summary>
        [JsonPropertyName("entityType")]
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets whether the request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any error message if the request failed.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the values were last updated.
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime? LastUpdated { get; set; }
    }
} 