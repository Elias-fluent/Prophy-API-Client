using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates the usage of the CustomFieldModule for managing organization-specific custom fields.
    /// </summary>
    public class CustomFieldDemo
    {
        private readonly ProphyApiClient _client;
        private readonly ILogger<CustomFieldDemo> _logger;

        public CustomFieldDemo(ProphyApiClient client, ILogger<CustomFieldDemo> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs all custom field demonstration scenarios.
        /// </summary>
        public async Task RunAllDemosAsync()
        {
            _logger.LogInformation("=== Custom Field Module Demonstration ===");

            try
            {
                await CustomFieldDiscoveryDemoAsync();
                await LocalValidationDemoAsync();
                await CustomFieldValueManagementDemoAsync();
                await SerializationDemoAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during custom field demonstration");
            }

            _logger.LogInformation("=== Custom Field Module Demonstration Complete ===");
        }

        /// <summary>
        /// Demonstrates discovering custom field definitions for the organization.
        /// </summary>
        public async Task CustomFieldDiscoveryDemoAsync()
        {
            _logger.LogInformation("\n--- Custom Field Discovery Demo ---");

            try
            {
                // Get all custom field definitions
                _logger.LogInformation("Retrieving all custom field definitions...");
                var allDefinitions = await _client.CustomFields.GetAllDefinitionsAsync();
                
                _logger.LogInformation("Found {Count} custom field definitions:", allDefinitions.Count);
                foreach (var definition in allDefinitions)
                {
                    _logger.LogInformation("  - {Name} ({ApiId}): {DataType}, Required: {IsRequired}", 
                        definition.Name, definition.ApiId, definition.DataType, definition.IsRequired);
                }

                // Get custom fields for a specific entity type
                _logger.LogInformation("\nRetrieving custom fields for manuscripts...");
                var manuscriptFields = await _client.CustomFields.GetAllDefinitionsAsync("manuscript");
                
                _logger.LogInformation("Found {Count} manuscript-specific custom fields:", manuscriptFields.Count);
                foreach (var field in manuscriptFields)
                {
                    _logger.LogInformation("  - {Name}: {Description}", field.Name, field.Description ?? "No description");
                    
                    if (field.Options?.Count > 0)
                    {
                        _logger.LogInformation("    Options: {Options}", 
                            string.Join(", ", field.Options.ConvertAll(o => o.Value)));
                    }
                }

                // Get custom fields with advanced filtering
                _logger.LogInformation("\nRetrieving custom fields with advanced filtering...");
                var request = new CustomFieldDefinitionsRequest
                {
                    EntityType = "author",
                    EnabledOnly = true,
                    VisibleOnly = true,
                    IncludeOptions = true,
                    Page = 1,
                    PageSize = 10,
                    SortBy = "name",
                    SortDirection = "asc"
                };

                var response = await _client.CustomFields.GetDefinitionsAsync(request);
                _logger.LogInformation("Advanced query returned {Count} fields (Total: {Total})", 
                    response.CustomFields.Count, response.TotalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during custom field discovery");
            }
        }

        /// <summary>
        /// Demonstrates local validation of custom field values.
        /// </summary>
        public async Task LocalValidationDemoAsync()
        {
            _logger.LogInformation("\n--- Local Validation Demo ---");

            try
            {
                // Create sample custom field definitions
                var definitions = new List<CustomField>
                {
                    new CustomField
                    {
                        ApiId = "research_area",
                        Name = "Research Area",
                        DataType = CustomFieldDataType.SingleOption,
                        IsRequired = true,
                        IsEnabled = true,
                        Options = new List<CustomFieldOption>
                        {
                            new CustomFieldOption { Value = "Physics", IsEnabled = true },
                            new CustomFieldOption { Value = "Chemistry", IsEnabled = true },
                            new CustomFieldOption { Value = "Biology", IsEnabled = true }
                        }
                    },
                    new CustomField
                    {
                        ApiId = "impact_factor",
                        Name = "Expected Impact Factor",
                        DataType = CustomFieldDataType.Number,
                        IsRequired = false,
                        IsEnabled = true,
                        MinValue = 0.1,
                        MaxValue = 50.0
                    },
                    new CustomField
                    {
                        ApiId = "keywords",
                        Name = "Keywords",
                        DataType = CustomFieldDataType.String,
                        IsRequired = true,
                        IsEnabled = true,
                        MinLength = 10,
                        MaxLength = 500
                    },
                    new CustomField
                    {
                        ApiId = "open_access",
                        Name = "Open Access",
                        DataType = CustomFieldDataType.Boolean,
                        IsRequired = false,
                        IsEnabled = true
                    }
                };

                // Test valid values
                _logger.LogInformation("Testing valid custom field values...");
                var validValues = new Dictionary<string, object>
                {
                    { "research_area", "Physics" },
                    { "impact_factor", 2.5 },
                    { "keywords", "quantum mechanics, theoretical physics, particle physics" },
                    { "open_access", true }
                };

                var validationResult = await _client.CustomFields.ValidateValuesLocallyAsync(validValues, definitions, true);
                _logger.LogInformation("Validation result: {IsValid}", validationResult.IsValid);
                
                if (validationResult.IsValid)
                {
                    _logger.LogInformation("All {Count} values are valid!", validationResult.ValidatedValues.Count);
                }

                // Test invalid values
                _logger.LogInformation("\nTesting invalid custom field values...");
                var invalidValues = new Dictionary<string, object>
                {
                    { "research_area", "InvalidArea" }, // Invalid option
                    { "impact_factor", 100.0 }, // Too high
                    { "keywords", "short" }, // Too short
                    // Missing required field "research_area" in strict mode
                };

                var invalidResult = await _client.CustomFields.ValidateValuesLocallyAsync(invalidValues, definitions, true);
                _logger.LogInformation("Validation result: {IsValid}", invalidResult.IsValid);
                
                if (!invalidResult.IsValid)
                {
                    _logger.LogInformation("Found {Count} validation errors:", invalidResult.Errors.Count);
                    foreach (var error in invalidResult.Errors)
                    {
                        _logger.LogInformation("  - {FieldName}: {ErrorMessage}", 
                            error.FieldName ?? error.FieldApiId, error.ErrorMessage);
                    }
                }

                // Test default values
                _logger.LogInformation("\nTesting default value generation...");
                foreach (var definition in definitions)
                {
                    var defaultValue = _client.CustomFields.GetDefaultValue(definition);
                    _logger.LogInformation("Default value for {Name} ({DataType}): {DefaultValue}", 
                        definition.Name, definition.DataType, defaultValue ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during local validation demo");
            }
        }

        /// <summary>
        /// Demonstrates custom field value management operations.
        /// </summary>
        public async Task CustomFieldValueManagementDemoAsync()
        {
            _logger.LogInformation("\n--- Custom Field Value Management Demo ---");

            try
            {
                // Note: These operations would typically require a real API connection
                // For demo purposes, we'll show the request structure

                var entityId = "manuscript_123";
                var entityType = "manuscript";

                // Retrieve custom field values for an entity
                _logger.LogInformation("Retrieving custom field values for entity {EntityId}...", entityId);
                
                var getValuesRequest = new CustomFieldValuesRequest
                {
                    EntityId = entityId,
                    EntityType = entityType,
                    IncludeDefinitions = true
                };

                try
                {
                    var valuesResponse = await _client.CustomFields.GetValuesAsync(getValuesRequest);
                    _logger.LogInformation("Retrieved {Count} custom field values", valuesResponse.Values.Count);
                    
                    foreach (var kvp in valuesResponse.Values)
                    {
                        _logger.LogInformation("  - {FieldId}: {Value}", kvp.Key, kvp.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not retrieve values (expected in demo): {Message}", ex.Message);
                }

                // Update custom field values
                _logger.LogInformation("\nUpdating custom field values for entity {EntityId}...", entityId);
                
                var updateRequest = new CustomFieldUpdateRequest
                {
                    EntityId = entityId,
                    EntityType = entityType,
                    Values = new Dictionary<string, object>
                    {
                        { "research_area", "Physics" },
                        { "impact_factor", 3.2 },
                        { "keywords", "quantum computing, machine learning, artificial intelligence" },
                        { "open_access", true }
                    },
                    ValidateBeforeUpdate = true,
                    PartialUpdate = true
                };

                try
                {
                    var updateResponse = await _client.CustomFields.UpdateValuesAsync(updateRequest);
                    _logger.LogInformation("Successfully updated {Count} custom field values", updateResponse.Values.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not update values (expected in demo): {Message}", ex.Message);
                }

                // Validate values using API
                _logger.LogInformation("\nValidating custom field values using API...");
                
                var validationRequest = new CustomFieldValidationRequest
                {
                    Values = updateRequest.Values,
                    EntityType = entityType,
                    StrictValidation = true,
                    IncludeDetails = true
                };

                try
                {
                    var apiValidationResult = await _client.CustomFields.ValidateValuesAsync(validationRequest);
                    _logger.LogInformation("API validation result: {IsValid}", apiValidationResult.IsValid);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not validate via API (expected in demo): {Message}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during value management demo");
            }
        }

        /// <summary>
        /// Demonstrates custom field value serialization and deserialization.
        /// </summary>
        public async Task SerializationDemoAsync()
        {
            _logger.LogInformation("\n--- Serialization Demo ---");

            try
            {
                // Create sample definitions with different data types
                var definitions = new List<CustomField>
                {
                    new CustomField
                    {
                        ApiId = "submission_date",
                        Name = "Submission Date",
                        DataType = CustomFieldDataType.Date
                    },
                    new CustomField
                    {
                        ApiId = "page_count",
                        Name = "Page Count",
                        DataType = CustomFieldDataType.Number
                    },
                    new CustomField
                    {
                        ApiId = "title",
                        Name = "Title",
                        DataType = CustomFieldDataType.String
                    },
                    new CustomField
                    {
                        ApiId = "peer_reviewed",
                        Name = "Peer Reviewed",
                        DataType = CustomFieldDataType.Boolean
                    }
                };

                // Original values with different types
                var originalValues = new Dictionary<string, object>
                {
                    { "submission_date", DateTime.UtcNow },
                    { "page_count", 25 },
                    { "title", "Advanced Quantum Computing Algorithms" },
                    { "peer_reviewed", true }
                };

                _logger.LogInformation("Original values:");
                foreach (var kvp in originalValues)
                {
                    _logger.LogInformation("  - {Key}: {Value} ({Type})", 
                        kvp.Key, kvp.Value, kvp.Value.GetType().Name);
                }

                // Serialize values
                _logger.LogInformation("\nSerializing values for API transmission...");
                var serializedValues = await _client.CustomFields.SerializeValuesAsync(originalValues, definitions);
                
                _logger.LogInformation("Serialized values:");
                foreach (var kvp in serializedValues)
                {
                    _logger.LogInformation("  - {Key}: {Value} ({Type})", 
                        kvp.Key, kvp.Value, kvp.Value.GetType().Name);
                }

                // Deserialize values
                _logger.LogInformation("\nDeserializing values from API response...");
                var deserializedValues = await _client.CustomFields.DeserializeValuesAsync(serializedValues, definitions);
                
                _logger.LogInformation("Deserialized values:");
                foreach (var kvp in deserializedValues)
                {
                    _logger.LogInformation("  - {Key}: {Value} ({Type})", 
                        kvp.Key, kvp.Value, kvp.Value.GetType().Name);
                }

                // Test with unknown fields
                _logger.LogInformation("\nTesting serialization with unknown fields...");
                var valuesWithUnknown = new Dictionary<string, object>
                {
                    { "known_field", "test value" },
                    { "unknown_field", "should pass through unchanged" }
                };

                var knownDefinitions = new List<CustomField>
                {
                    new CustomField
                    {
                        ApiId = "known_field",
                        DataType = CustomFieldDataType.String
                    }
                };

                var serializedUnknown = await _client.CustomFields.SerializeValuesAsync(valuesWithUnknown, knownDefinitions);
                _logger.LogInformation("Unknown field handling - serialized values:");
                foreach (var kvp in serializedUnknown)
                {
                    _logger.LogInformation("  - {Key}: {Value}", kvp.Key, kvp.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during serialization demo");
            }
        }
    }
} 