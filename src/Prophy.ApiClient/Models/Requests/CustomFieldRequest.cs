using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Request model for validating custom field values.
    /// </summary>
    public class CustomFieldValidationRequest
    {
        /// <summary>
        /// Gets or sets the custom field values to validate.
        /// Key is the custom field API ID, value is the field value.
        /// </summary>
        [Required]
        [JsonPropertyName("values")]
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the entity type these values belong to (e.g., "manuscript", "author").
        /// </summary>
        [JsonPropertyName("entityType")]
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets whether to perform strict validation (all required fields must be present).
        /// </summary>
        [JsonPropertyName("strictValidation")]
        public bool StrictValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to return detailed validation information.
        /// </summary>
        [JsonPropertyName("includeDetails")]
        public bool IncludeDetails { get; set; } = false;
    }

    /// <summary>
    /// Request model for updating custom field values.
    /// </summary>
    public class CustomFieldUpdateRequest
    {
        /// <summary>
        /// Gets or sets the entity ID to update custom field values for.
        /// </summary>
        [Required]
        [JsonPropertyName("entityId")]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity type (e.g., "manuscript", "author").
        /// </summary>
        [Required]
        [JsonPropertyName("entityType")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the custom field values to update.
        /// Key is the custom field API ID, value is the field value.
        /// </summary>
        [Required]
        [JsonPropertyName("values")]
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether to validate the values before updating.
        /// </summary>
        [JsonPropertyName("validateBeforeUpdate")]
        public bool ValidateBeforeUpdate { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to perform a partial update (only update provided fields).
        /// </summary>
        [JsonPropertyName("partialUpdate")]
        public bool PartialUpdate { get; set; } = true;
    }

    /// <summary>
    /// Request model for retrieving custom field definitions.
    /// </summary>
    public class CustomFieldDefinitionsRequest
    {
        /// <summary>
        /// Gets or sets the entity type to retrieve custom fields for (optional).
        /// </summary>
        [JsonPropertyName("entityType")]
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets whether to include only enabled custom fields.
        /// </summary>
        [JsonPropertyName("enabledOnly")]
        public bool EnabledOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include only visible custom fields.
        /// </summary>
        [JsonPropertyName("visibleOnly")]
        public bool VisibleOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include custom field options for single/multi-option fields.
        /// </summary>
        [JsonPropertyName("includeOptions")]
        public bool IncludeOptions { get; set; } = true;

        /// <summary>
        /// Gets or sets the page number for pagination (1-based).
        /// </summary>
        [JsonPropertyName("page")]
        public int? Page { get; set; }

        /// <summary>
        /// Gets or sets the page size for pagination.
        /// </summary>
        [JsonPropertyName("pageSize")]
        public int? PageSize { get; set; }

        /// <summary>
        /// Gets or sets the field to sort by.
        /// </summary>
        [JsonPropertyName("sortBy")]
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort direction ("asc" or "desc").
        /// </summary>
        [JsonPropertyName("sortDirection")]
        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// Request model for retrieving custom field values for an entity.
    /// </summary>
    public class CustomFieldValuesRequest
    {
        /// <summary>
        /// Gets or sets the entity ID to retrieve custom field values for.
        /// </summary>
        [Required]
        [JsonPropertyName("entityId")]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity type (e.g., "manuscript", "author").
        /// </summary>
        [Required]
        [JsonPropertyName("entityType")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets specific custom field API IDs to retrieve (optional).
        /// If not provided, all custom field values will be returned.
        /// </summary>
        [JsonPropertyName("fieldApiIds")]
        public List<string>? FieldApiIds { get; set; }

        /// <summary>
        /// Gets or sets whether to include custom field definitions in the response.
        /// </summary>
        [JsonPropertyName("includeDefinitions")]
        public bool IncludeDefinitions { get; set; } = false;
    }
} 