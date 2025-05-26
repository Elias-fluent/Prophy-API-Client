using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Serialization;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Implementation of custom field operations for the Prophy API.
    /// Provides comprehensive support for custom field discovery, validation, and value management.
    /// </summary>
    public class CustomFieldModule : ICustomFieldModule
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IApiKeyAuthenticator _authenticator;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<CustomFieldModule> _logger;

        /// <summary>
        /// Initializes a new instance of the CustomFieldModule class.
        /// </summary>
        /// <param name="httpClient">The HTTP client wrapper for making API requests.</param>
        /// <param name="authenticator">The API key authenticator for request authentication.</param>
        /// <param name="jsonSerializer">The JSON serializer for request/response serialization.</param>
        /// <param name="logger">The logger for recording module operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public CustomFieldModule(
            IHttpClientWrapper httpClient,
            IApiKeyAuthenticator authenticator,
            IJsonSerializer jsonSerializer,
            ILogger<CustomFieldModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<CustomFieldDefinitionsResponse> GetDefinitionsAsync(CustomFieldDefinitionsRequest? request = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving custom field definitions");

            try
            {
                // Build query parameters
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    if (!string.IsNullOrEmpty(request.EntityType))
                        queryParams.Add($"entityType={Uri.EscapeDataString(request.EntityType)}");
                    
                    if (!request.EnabledOnly)
                        queryParams.Add("enabledOnly=false");
                    
                    if (!request.VisibleOnly)
                        queryParams.Add("visibleOnly=false");
                    
                    if (!request.IncludeOptions)
                        queryParams.Add("includeOptions=false");
                    
                    if (request.Page.HasValue)
                        queryParams.Add($"page={request.Page.Value}");
                    
                    if (request.PageSize.HasValue)
                        queryParams.Add($"pageSize={request.PageSize.Value}");
                    
                    if (!string.IsNullOrEmpty(request.SortBy))
                        queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
                    
                    if (!string.IsNullOrEmpty(request.SortDirection))
                        queryParams.Add($"sortDirection={Uri.EscapeDataString(request.SortDirection)}");
                }

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var endpoint = $"external/custom-fields/all/{queryString}";

                // Create HTTP request
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                _authenticator.AuthenticateRequest(httpRequest);

                // Send request
                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = _jsonSerializer.Deserialize<CustomFieldDefinitionsResponse>(responseContent);
                    
                    _logger.LogInformation("Successfully retrieved {Count} custom field definitions", 
                        result?.CustomFields?.Count ?? 0);
                    
                    return result ?? new CustomFieldDefinitionsResponse { Success = false, Message = "Empty response" };
                }
                else
                {
                    _logger.LogError("Failed to retrieve custom field definitions. Status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    
                    throw new ProphyApiException($"Failed to retrieve custom field definitions: {response.StatusCode}", 
                        "CUSTOM_FIELDS_ERROR", response.StatusCode, responseContent);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Custom field definitions retrieval was cancelled");
                throw;
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error retrieving custom field definitions");
                throw new ProphyApiException("Unexpected error retrieving custom field definitions", "CUSTOM_FIELDS_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<CustomField>> GetAllDefinitionsAsync(string? entityType = null, CancellationToken cancellationToken = default)
        {
            var request = new CustomFieldDefinitionsRequest
            {
                EntityType = entityType,
                EnabledOnly = true,
                VisibleOnly = true,
                IncludeOptions = true
            };

            var response = await GetDefinitionsAsync(request, cancellationToken);
            
            if (!response.Success)
            {
                throw new ProphyApiException($"Failed to retrieve custom field definitions: {response.Message}", "CUSTOM_FIELDS_ERROR");
            }

            return response.CustomFields;
        }

        /// <inheritdoc />
        public async Task<CustomFieldValidationResponse> ValidateValuesAsync(CustomFieldValidationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Validating custom field values for {Count} fields", request.Values.Count);

            try
            {
                var endpoint = "external/custom-fields/validate";
                var requestJson = _jsonSerializer.Serialize(request);

                // Create HTTP request
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };
                _authenticator.AuthenticateRequest(httpRequest);

                // Send request
                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = _jsonSerializer.Deserialize<CustomFieldValidationResponse>(responseContent);
                    
                    _logger.LogInformation("Custom field validation completed. Valid: {IsValid}, Errors: {ErrorCount}", 
                        result?.IsValid ?? false, result?.Errors?.Count ?? 0);
                    
                    return result ?? new CustomFieldValidationResponse { Success = false, Message = "Empty response" };
                }
                else
                {
                    _logger.LogError("Failed to validate custom field values. Status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    
                    throw new ProphyApiException($"Failed to validate custom field values: {response.StatusCode}", 
                        "CUSTOM_FIELDS_VALIDATION_ERROR", response.StatusCode, responseContent);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Custom field validation was cancelled");
                throw;
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error validating custom field values");
                throw new ProphyApiException("Unexpected error validating custom field values", "CUSTOM_FIELDS_VALIDATION_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<CustomFieldValidationResponse> ValidateValuesLocallyAsync(Dictionary<string, object> values, List<CustomField> definitions, bool strictValidation = true)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            _logger.LogDebug("Performing local validation for {Count} custom field values", values.Count);

            var response = new CustomFieldValidationResponse
            {
                Success = true,
                IsValid = true,
                Errors = new List<CustomFieldValidationError>(),
                ValidatedValues = new Dictionary<string, object>()
            };

            // Create a lookup for definitions by API ID
            var definitionLookup = definitions.ToDictionary(d => d.ApiId, d => d);

            // Validate each provided value
            foreach (var kvp in values)
            {
                var fieldApiId = kvp.Key;
                var value = kvp.Value;

                if (!definitionLookup.TryGetValue(fieldApiId, out var definition))
                {
                    if (strictValidation)
                    {
                        response.Errors.Add(new CustomFieldValidationError
                        {
                            FieldApiId = fieldApiId,
                            ErrorMessage = $"Custom field '{fieldApiId}' is not defined for this organization",
                            ErrorCode = "FIELD_NOT_FOUND"
                        });
                    }
                    continue;
                }

                var validationErrors = GetValidationErrors(value, definition);
                foreach (var error in validationErrors)
                {
                    response.Errors.Add(new CustomFieldValidationError
                    {
                        FieldApiId = fieldApiId,
                        FieldName = definition.Name,
                        ErrorMessage = error,
                        ErrorCode = "VALIDATION_FAILED",
                        InvalidValue = value
                    });
                }

                // Add validated value if valid
                if (validationErrors.Count == 0)
                {
                    response.ValidatedValues[fieldApiId] = value;
                }
            }

            // Check for missing required fields in strict mode
            if (strictValidation)
            {
                var requiredFields = definitions.Where(d => d.IsRequired && d.IsEnabled).ToList();
                foreach (var requiredField in requiredFields)
                {
                    if (!values.ContainsKey(requiredField.ApiId))
                    {
                        response.Errors.Add(new CustomFieldValidationError
                        {
                            FieldApiId = requiredField.ApiId,
                            FieldName = requiredField.Name,
                            ErrorMessage = $"Required field '{requiredField.Name}' is missing",
                            ErrorCode = "REQUIRED_FIELD_MISSING"
                        });
                    }
                }
            }

            response.IsValid = response.Errors.Count == 0;
            
            _logger.LogDebug("Local validation completed. Valid: {IsValid}, Errors: {ErrorCount}", 
                response.IsValid, response.Errors.Count);

            return await Task.FromResult(response);
        }

        /// <inheritdoc />
        public async Task<CustomFieldValueResponse> GetValuesAsync(CustomFieldValuesRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Retrieving custom field values for entity {EntityId} of type {EntityType}", 
                request.EntityId, request.EntityType);

            try
            {
                // Build query parameters
                var queryParams = new List<string>
                {
                    $"entityId={Uri.EscapeDataString(request.EntityId)}",
                    $"entityType={Uri.EscapeDataString(request.EntityType)}"
                };

                if (request.FieldApiIds?.Count > 0)
                {
                    queryParams.Add($"fieldApiIds={string.Join(",", request.FieldApiIds.Select(Uri.EscapeDataString))}");
                }

                if (request.IncludeDefinitions)
                {
                    queryParams.Add("includeDefinitions=true");
                }

                var queryString = "?" + string.Join("&", queryParams);
                var endpoint = $"external/custom-fields/values{queryString}";

                // Create HTTP request
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                _authenticator.AuthenticateRequest(httpRequest);

                // Send request
                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = _jsonSerializer.Deserialize<CustomFieldValueResponse>(responseContent);
                    
                    _logger.LogInformation("Successfully retrieved {Count} custom field values", 
                        result?.Values?.Count ?? 0);
                    
                    return result ?? new CustomFieldValueResponse { Success = false, Message = "Empty response" };
                }
                else
                {
                    _logger.LogError("Failed to retrieve custom field values. Status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    
                    throw new ProphyApiException($"Failed to retrieve custom field values: {response.StatusCode}", 
                        "CUSTOM_FIELDS_VALUES_ERROR", response.StatusCode, responseContent);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Custom field values retrieval was cancelled");
                throw;
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error retrieving custom field values");
                throw new ProphyApiException("Unexpected error retrieving custom field values", "CUSTOM_FIELDS_VALUES_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<CustomFieldValueResponse> UpdateValuesAsync(CustomFieldUpdateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Updating custom field values for entity {EntityId} of type {EntityType}", 
                request.EntityId, request.EntityType);

            try
            {
                // Validate values if requested
                if (request.ValidateBeforeUpdate)
                {
                    var definitions = await GetAllDefinitionsAsync(request.EntityType, cancellationToken);
                    var validationResponse = await ValidateValuesLocallyAsync(request.Values, definitions, !request.PartialUpdate);
                    
                    if (!validationResponse.IsValid)
                    {
                        var errorMessages = string.Join(", ", validationResponse.Errors.Select(e => e.ErrorMessage));
                        throw new Prophy.ApiClient.Exceptions.ValidationException($"Custom field validation failed: {errorMessages}", 
                            validationResponse.Errors.Select(e => e.ErrorMessage).ToList());
                    }
                }

                var endpoint = "external/custom-fields/values";
                var requestJson = _jsonSerializer.Serialize(request);

                // Create HTTP request
                using var httpRequest = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };
                _authenticator.AuthenticateRequest(httpRequest);

                // Send request
                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = _jsonSerializer.Deserialize<CustomFieldValueResponse>(responseContent);
                    
                    _logger.LogInformation("Successfully updated {Count} custom field values", 
                        result?.Values?.Count ?? 0);
                    
                    return result ?? new CustomFieldValueResponse { Success = false, Message = "Empty response" };
                }
                else
                {
                    _logger.LogError("Failed to update custom field values. Status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    
                    throw new ProphyApiException($"Failed to update custom field values: {response.StatusCode}", 
                        "CUSTOM_FIELDS_UPDATE_ERROR", response.StatusCode, responseContent);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Custom field values update was cancelled");
                throw;
            }
            catch (Exception ex) when (!(ex is ProphyApiException) && !(ex is Prophy.ApiClient.Exceptions.ValidationException))
            {
                _logger.LogError(ex, "Unexpected error updating custom field values");
                throw new ProphyApiException("Unexpected error updating custom field values", "CUSTOM_FIELDS_UPDATE_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, object>> SerializeValuesAsync(Dictionary<string, object> values, List<CustomField> definitions)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            var definitionLookup = definitions.ToDictionary(d => d.ApiId, d => d);
            var serializedValues = new Dictionary<string, object>();

            foreach (var kvp in values)
            {
                var fieldApiId = kvp.Key;
                var value = kvp.Value;

                if (!definitionLookup.TryGetValue(fieldApiId, out var definition))
                {
                    // Include unknown fields as-is
                    serializedValues[fieldApiId] = value;
                    continue;
                }

                var serializedValue = SerializeValue(value, definition);
                serializedValues[fieldApiId] = serializedValue;
            }

            return await Task.FromResult(serializedValues);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, object>> DeserializeValuesAsync(Dictionary<string, object> values, List<CustomField> definitions)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            var definitionLookup = definitions.ToDictionary(d => d.ApiId, d => d);
            var deserializedValues = new Dictionary<string, object>();

            foreach (var kvp in values)
            {
                var fieldApiId = kvp.Key;
                var value = kvp.Value;

                if (!definitionLookup.TryGetValue(fieldApiId, out var definition))
                {
                    // Include unknown fields as-is
                    deserializedValues[fieldApiId] = value;
                    continue;
                }

                var deserializedValue = DeserializeValue(value, definition);
                deserializedValues[fieldApiId] = deserializedValue;
            }

            return await Task.FromResult(deserializedValues);
        }

        /// <inheritdoc />
        public object? GetDefaultValue(CustomField definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (definition.DefaultValue != null)
            {
                return definition.DefaultValue;
            }

            // Return type-specific defaults
            return definition.DataType switch
            {
                CustomFieldDataType.String => string.Empty,
                CustomFieldDataType.Number => 0.0,
                CustomFieldDataType.Boolean => false,
                CustomFieldDataType.Date => DateTime.MinValue,
                CustomFieldDataType.SingleOption => definition.Options?.FirstOrDefault(o => o.IsDefault)?.Value,
                CustomFieldDataType.MultiOption => new List<string>(),
                CustomFieldDataType.Array => new List<object>(),
                CustomFieldDataType.Object => new Dictionary<string, object>(),
                _ => null
            };
        }

        /// <inheritdoc />
        public bool IsValidValue(object? value, CustomField definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var errors = GetValidationErrors(value, definition);
            return errors.Count == 0;
        }

        /// <inheritdoc />
        public List<string> GetValidationErrors(object? value, CustomField definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var errors = new List<string>();

            // Check if required field is missing
            if (definition.IsRequired && (value == null || (value is string str && string.IsNullOrWhiteSpace(str))))
            {
                errors.Add($"Field '{definition.Name}' is required");
                return errors; // No point in further validation if required field is missing
            }

            // If value is null and field is not required, it's valid
            if (value == null && !definition.IsRequired)
            {
                return errors;
            }

            // Type-specific validation
            switch (definition.DataType)
            {
                case CustomFieldDataType.String:
                    ValidateStringField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.Number:
                    ValidateNumberField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.Boolean:
                    ValidateBooleanField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.Date:
                    ValidateDateField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.SingleOption:
                    ValidateSingleOptionField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.MultiOption:
                    ValidateMultiOptionField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.Array:
                    ValidateArrayField(value, definition, errors);
                    break;
                
                case CustomFieldDataType.Object:
                    ValidateObjectField(value, definition, errors);
                    break;
            }

            return errors;
        }

        #region Private Helper Methods

        private object SerializeValue(object? value, CustomField definition)
        {
            if (value == null)
                return null!;

            return definition.DataType switch
            {
                CustomFieldDataType.Date when value is DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                CustomFieldDataType.Date when value is DateTimeOffset dto => dto.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                _ => value
            };
        }

        private object DeserializeValue(object? value, CustomField definition)
        {
            if (value == null)
                return null!;

            return definition.DataType switch
            {
                CustomFieldDataType.Date when value is string dateStr => DateTime.TryParse(dateStr, out var dt) ? dt : value,
                CustomFieldDataType.Number when value is string numStr => double.TryParse(numStr, out var num) ? num : value,
                CustomFieldDataType.Boolean when value is string boolStr => bool.TryParse(boolStr, out var b) ? b : value,
                _ => value
            };
        }

        private void ValidateStringField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is string stringValue))
            {
                errors.Add($"Field '{definition.Name}' must be a string");
                return;
            }

            if (definition.MinLength.HasValue && stringValue.Length < definition.MinLength.Value)
            {
                errors.Add($"Field '{definition.Name}' must be at least {definition.MinLength.Value} characters long");
            }

            if (definition.MaxLength.HasValue && stringValue.Length > definition.MaxLength.Value)
            {
                errors.Add($"Field '{definition.Name}' must be no more than {definition.MaxLength.Value} characters long");
            }

            if (!string.IsNullOrEmpty(definition.ValidationPattern))
            {
                try
                {
                    if (!Regex.IsMatch(stringValue, definition.ValidationPattern))
                    {
                        var message = !string.IsNullOrEmpty(definition.ValidationMessage) 
                            ? definition.ValidationMessage 
                            : $"Field '{definition.Name}' does not match the required pattern";
                        errors.Add(message);
                    }
                }
                catch (ArgumentException)
                {
                    errors.Add($"Field '{definition.Name}' has an invalid validation pattern");
                }
            }
        }

        private void ValidateNumberField(object? value, CustomField definition, List<string> errors)
        {
            double numericValue;

            if (value is double d)
            {
                numericValue = d;
            }
            else if (value is int i)
            {
                numericValue = i;
            }
            else if (value is decimal dec)
            {
                numericValue = (double)dec;
            }
            else if (value is string str && double.TryParse(str, out var parsed))
            {
                numericValue = parsed;
            }
            else
            {
                errors.Add($"Field '{definition.Name}' must be a number");
                return;
            }

            if (definition.MinValue.HasValue && numericValue < definition.MinValue.Value)
            {
                errors.Add($"Field '{definition.Name}' must be at least {definition.MinValue.Value}");
            }

            if (definition.MaxValue.HasValue && numericValue > definition.MaxValue.Value)
            {
                errors.Add($"Field '{definition.Name}' must be no more than {definition.MaxValue.Value}");
            }
        }

        private void ValidateBooleanField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is bool) && !(value is string str && bool.TryParse(str, out _)))
            {
                errors.Add($"Field '{definition.Name}' must be a boolean value");
            }
        }

        private void ValidateDateField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is DateTime) && !(value is DateTimeOffset) && 
                !(value is string str && DateTime.TryParse(str, out _)))
            {
                errors.Add($"Field '{definition.Name}' must be a valid date");
            }
        }

        private void ValidateSingleOptionField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is string stringValue))
            {
                errors.Add($"Field '{definition.Name}' must be a string");
                return;
            }

            if (definition.Options?.Count > 0)
            {
                var validOptions = definition.Options.Where(o => o.IsEnabled).Select(o => o.Value).ToList();
                if (!validOptions.Contains(stringValue))
                {
                    errors.Add($"Field '{definition.Name}' must be one of: {string.Join(", ", validOptions)}");
                }
            }
        }

        private void ValidateMultiOptionField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is IEnumerable<object> enumerable))
            {
                errors.Add($"Field '{definition.Name}' must be an array");
                return;
            }

            var values = enumerable.Select(v => v?.ToString()).Where(v => v != null).ToList();

            if (definition.Options?.Count > 0)
            {
                var validOptions = definition.Options.Where(o => o.IsEnabled).Select(o => o.Value).ToList();
                var invalidValues = values.Where(v => !validOptions.Contains(v)).ToList();
                
                if (invalidValues.Count > 0)
                {
                    errors.Add($"Field '{definition.Name}' contains invalid options: {string.Join(", ", invalidValues)}. Valid options are: {string.Join(", ", validOptions)}");
                }
            }
        }

        private void ValidateArrayField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is IEnumerable<object>))
            {
                errors.Add($"Field '{definition.Name}' must be an array");
            }
        }

        private void ValidateObjectField(object? value, CustomField definition, List<string> errors)
        {
            if (!(value is Dictionary<string, object>) && !(value is JsonElement))
            {
                errors.Add($"Field '{definition.Name}' must be an object");
            }
        }

        #endregion
    }
} 