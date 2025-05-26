using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Interface for custom field operations in the Prophy API.
    /// Provides methods for discovering custom field definitions, validating values, and managing custom field data.
    /// </summary>
    public interface ICustomFieldModule
    {
        /// <summary>
        /// Retrieves custom field definitions for the organization.
        /// </summary>
        /// <param name="request">The request parameters for retrieving custom field definitions.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the custom field definitions response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ApiTimeoutException">Thrown when the request times out.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API returns an error.</exception>
        Task<CustomFieldDefinitionsResponse> GetDefinitionsAsync(CustomFieldDefinitionsRequest? request = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all custom field definitions for the organization.
        /// </summary>
        /// <param name="entityType">Optional entity type to filter custom fields (e.g., "manuscript", "author").</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains all custom field definitions.</returns>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ApiTimeoutException">Thrown when the request times out.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API returns an error.</exception>
        Task<List<CustomField>> GetAllDefinitionsAsync(string? entityType = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates custom field values against their definitions.
        /// </summary>
        /// <param name="request">The validation request containing values to validate.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the validation response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ApiTimeoutException">Thrown when the request times out.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API returns an error.</exception>
        Task<CustomFieldValidationResponse> ValidateValuesAsync(CustomFieldValidationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates custom field values using local validation rules.
        /// This method performs client-side validation without making an API call.
        /// </summary>
        /// <param name="values">The custom field values to validate.</param>
        /// <param name="definitions">The custom field definitions to validate against.</param>
        /// <param name="strictValidation">Whether to perform strict validation (all required fields must be present).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the validation response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when values or definitions is null.</exception>
        Task<CustomFieldValidationResponse> ValidateValuesLocallyAsync(Dictionary<string, object> values, List<CustomField> definitions, bool strictValidation = true);

        /// <summary>
        /// Retrieves custom field values for a specific entity.
        /// </summary>
        /// <param name="request">The request parameters for retrieving custom field values.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the custom field values response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ApiTimeoutException">Thrown when the request times out.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API returns an error.</exception>
        Task<CustomFieldValueResponse> GetValuesAsync(CustomFieldValuesRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates custom field values for a specific entity.
        /// </summary>
        /// <param name="request">The request containing the values to update.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated values response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ApiTimeoutException">Thrown when the request times out.</exception>
        /// <exception cref="ProphyApiException">Thrown when the API returns an error.</exception>
        Task<CustomFieldValueResponse> UpdateValuesAsync(CustomFieldUpdateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes custom field values to a dictionary suitable for API requests.
        /// </summary>
        /// <param name="values">The custom field values to serialize.</param>
        /// <param name="definitions">The custom field definitions for type information.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the serialized values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when values or definitions is null.</exception>
        Task<Dictionary<string, object>> SerializeValuesAsync(Dictionary<string, object> values, List<CustomField> definitions);

        /// <summary>
        /// Deserializes custom field values from a dictionary received from API responses.
        /// </summary>
        /// <param name="values">The serialized custom field values to deserialize.</param>
        /// <param name="definitions">The custom field definitions for type information.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when values or definitions is null.</exception>
        Task<Dictionary<string, object>> DeserializeValuesAsync(Dictionary<string, object> values, List<CustomField> definitions);

        /// <summary>
        /// Gets the default value for a custom field based on its definition.
        /// </summary>
        /// <param name="definition">The custom field definition.</param>
        /// <returns>The default value for the custom field, or null if no default is specified.</returns>
        /// <exception cref="ArgumentNullException">Thrown when definition is null.</exception>
        object? GetDefaultValue(CustomField definition);

        /// <summary>
        /// Checks if a custom field value is valid according to its definition.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="definition">The custom field definition.</param>
        /// <returns>True if the value is valid, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when definition is null.</exception>
        bool IsValidValue(object? value, CustomField definition);

        /// <summary>
        /// Gets validation errors for a custom field value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="definition">The custom field definition.</param>
        /// <returns>A list of validation error messages, or empty list if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when definition is null.</exception>
        List<string> GetValidationErrors(object? value, CustomField definition);
    }
} 