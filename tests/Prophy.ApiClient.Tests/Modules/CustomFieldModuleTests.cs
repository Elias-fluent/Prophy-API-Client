using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Modules
{
    public class CustomFieldModuleTests
    {
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly Mock<IApiKeyAuthenticator> _mockAuthenticator;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly Mock<ILogger<CustomFieldModule>> _mockLogger;
        private readonly CustomFieldModule _customFieldModule;

        public CustomFieldModuleTests()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockAuthenticator = new Mock<IApiKeyAuthenticator>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            _mockLogger = new Mock<ILogger<CustomFieldModule>>();

            _customFieldModule = new CustomFieldModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act & Assert
            Assert.NotNull(_customFieldModule);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomFieldModule(
                null!,
                _mockAuthenticator.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullAuthenticator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomFieldModule(
                _mockHttpClient.Object,
                null!,
                _mockJsonSerializer.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullJsonSerializer_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomFieldModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                null!,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomFieldModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockJsonSerializer.Object,
                null!));
        }

        #endregion

        #region GetDefinitionsAsync Tests

        [Fact]
        public async Task GetDefinitionsAsync_WithValidRequest_ShouldReturnDefinitions()
        {
            // Arrange
            var request = new CustomFieldDefinitionsRequest
            {
                EntityType = "manuscript",
                EnabledOnly = true
            };

            var expectedResponse = new CustomFieldDefinitionsResponse
            {
                Success = true,
                CustomFields = new List<CustomField>
                {
                    new CustomField
                    {
                        ApiId = "field1",
                        Name = "Test Field",
                        DataType = CustomFieldDataType.String,
                        IsRequired = true,
                        IsEnabled = true
                    }
                }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldDefinitionsResponse>("response content"))
                .Returns(expectedResponse);

            // Act
            var result = await _customFieldModule.GetDefinitionsAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(result.CustomFields);
            Assert.Equal("field1", result.CustomFields[0].ApiId);

            _mockAuthenticator.Verify(x => x.AuthenticateRequest(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Fact]
        public async Task GetDefinitionsAsync_WithNullRequest_ShouldUseDefaults()
        {
            // Arrange
            var expectedResponse = new CustomFieldDefinitionsResponse
            {
                Success = true,
                CustomFields = new List<CustomField>()
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldDefinitionsResponse>("response content"))
                .Returns(expectedResponse);

            // Act
            var result = await _customFieldModule.GetDefinitionsAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetDefinitionsAsync_WithHttpError_ShouldThrowProphyApiException()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("error content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _customFieldModule.GetDefinitionsAsync(new CustomFieldDefinitionsRequest()));

            Assert.Equal("CUSTOM_FIELDS_ERROR", exception.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest, exception.HttpStatusCode);
        }

        [Fact]
        public async Task GetDefinitionsAsync_WithCancellation_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var cancellationToken = new CancellationToken(true);

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _customFieldModule.GetDefinitionsAsync(new CustomFieldDefinitionsRequest(), cancellationToken));
        }

        #endregion

        #region GetAllDefinitionsAsync Tests

        [Fact]
        public async Task GetAllDefinitionsAsync_WithValidEntityType_ShouldReturnCustomFields()
        {
            // Arrange
            var expectedResponse = new CustomFieldDefinitionsResponse
            {
                Success = true,
                CustomFields = new List<CustomField>
                {
                    new CustomField { ApiId = "field1", Name = "Field 1" },
                    new CustomField { ApiId = "field2", Name = "Field 2" }
                }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldDefinitionsResponse>("response content"))
                .Returns(expectedResponse);

            // Act
            var result = await _customFieldModule.GetAllDefinitionsAsync("manuscript");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("field1", result[0].ApiId);
            Assert.Equal("field2", result[1].ApiId);
        }

        [Fact]
        public async Task GetAllDefinitionsAsync_WithFailedResponse_ShouldThrowProphyApiException()
        {
            // Arrange
            var expectedResponse = new CustomFieldDefinitionsResponse
            {
                Success = false,
                Message = "Failed to retrieve definitions"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldDefinitionsResponse>("response content"))
                .Returns(expectedResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ProphyApiException>(() => 
                _customFieldModule.GetAllDefinitionsAsync("manuscript"));

            Assert.Equal("CUSTOM_FIELDS_ERROR", exception.ErrorCode);
            Assert.Contains("Failed to retrieve definitions", exception.Message);
        }

        #endregion

        #region ValidateValuesAsync Tests

        [Fact]
        public async Task ValidateValuesAsync_WithValidRequest_ShouldReturnValidationResponse()
        {
            // Arrange
            var request = new CustomFieldValidationRequest
            {
                Values = new Dictionary<string, object>
                {
                    { "field1", "test value" }
                }
            };

            var expectedResponse = new CustomFieldValidationResponse
            {
                Success = true,
                IsValid = true,
                Errors = new List<CustomFieldValidationError>()
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Serialize(request))
                .Returns("serialized request");

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldValidationResponse>("response content"))
                .Returns(expectedResponse);

            // Act
            var result = await _customFieldModule.ValidateValuesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);

            _mockJsonSerializer.Verify(x => x.Serialize(request), Times.Once);
        }

        [Fact]
        public async Task ValidateValuesAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.ValidateValuesAsync(null!));
        }

        #endregion

        #region ValidateValuesLocallyAsync Tests

        [Fact]
        public async Task ValidateValuesLocallyAsync_WithValidValues_ShouldReturnValidResponse()
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "field1", "test value" }
            };

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "field1",
                    Name = "Test Field",
                    DataType = CustomFieldDataType.String,
                    IsRequired = false,
                    IsEnabled = true
                }
            };

            // Act
            var result = await _customFieldModule.ValidateValuesLocallyAsync(values, definitions, false);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Single(result.ValidatedValues);
            Assert.Equal("test value", result.ValidatedValues["field1"]);
        }

        [Fact]
        public async Task ValidateValuesLocallyAsync_WithMissingRequiredField_ShouldReturnInvalidResponse()
        {
            // Arrange
            var values = new Dictionary<string, object>();

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "field1",
                    Name = "Required Field",
                    DataType = CustomFieldDataType.String,
                    IsRequired = true,
                    IsEnabled = true
                }
            };

            // Act
            var result = await _customFieldModule.ValidateValuesLocallyAsync(values, definitions, true);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("REQUIRED_FIELD_MISSING", result.Errors[0].ErrorCode);
            Assert.Contains("Required Field", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ValidateValuesLocallyAsync_WithInvalidFieldType_ShouldReturnInvalidResponse()
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "field1", 123 } // Number instead of string
            };

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "field1",
                    Name = "String Field",
                    DataType = CustomFieldDataType.String,
                    IsRequired = false,
                    IsEnabled = true
                }
            };

            // Act
            var result = await _customFieldModule.ValidateValuesLocallyAsync(values, definitions, false);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains("must be a string", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ValidateValuesLocallyAsync_WithNullValues_ShouldThrowArgumentNullException()
        {
            // Arrange
            var definitions = new List<CustomField>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.ValidateValuesLocallyAsync(null!, definitions));
        }

        [Fact]
        public async Task ValidateValuesLocallyAsync_WithNullDefinitions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var values = new Dictionary<string, object>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.ValidateValuesLocallyAsync(values, null!));
        }

        #endregion

        #region GetValuesAsync Tests

        [Fact]
        public async Task GetValuesAsync_WithValidRequest_ShouldReturnValues()
        {
            // Arrange
            var request = new CustomFieldValuesRequest
            {
                EntityId = "entity123",
                EntityType = "manuscript"
            };

            var expectedResponse = new CustomFieldValueResponse
            {
                Success = true,
                Values = new Dictionary<string, object>
                {
                    { "field1", "value1" }
                }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldValueResponse>("response content"))
                .Returns(expectedResponse);

            // Act
            var result = await _customFieldModule.GetValuesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(result.Values);
            Assert.Equal("value1", result.Values["field1"]);
        }

        [Fact]
        public async Task GetValuesAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.GetValuesAsync(null!));
        }

        #endregion

        #region UpdateValuesAsync Tests

        [Fact]
        public async Task UpdateValuesAsync_WithValidRequest_ShouldReturnUpdatedValues()
        {
            // Arrange
            var request = new CustomFieldUpdateRequest
            {
                EntityId = "entity123",
                EntityType = "manuscript",
                Values = new Dictionary<string, object>
                {
                    { "field1", "new value" }
                },
                ValidateBeforeUpdate = false
            };

            var expectedResponse = new CustomFieldValueResponse
            {
                Success = true,
                Values = new Dictionary<string, object>
                {
                    { "field1", "new value" }
                }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Serialize(request))
                .Returns("serialized request");

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldValueResponse>("response content"))
                .Returns(expectedResponse);

            // Act
            var result = await _customFieldModule.UpdateValuesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(result.Values);
            Assert.Equal("new value", result.Values["field1"]);
        }

        [Fact]
        public async Task UpdateValuesAsync_WithValidationEnabled_ShouldValidateBeforeUpdate()
        {
            // Arrange
            var request = new CustomFieldUpdateRequest
            {
                EntityId = "entity123",
                EntityType = "manuscript",
                Values = new Dictionary<string, object>
                {
                    { "field1", "valid value" }
                },
                ValidateBeforeUpdate = true,
                PartialUpdate = true
            };

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "field1",
                    Name = "Test Field",
                    DataType = CustomFieldDataType.String,
                    IsRequired = false,
                    IsEnabled = true
                }
            };

            var definitionsResponse = new CustomFieldDefinitionsResponse
            {
                Success = true,
                CustomFields = definitions
            };

            var updateResponse = new CustomFieldValueResponse
            {
                Success = true,
                Values = request.Values
            };

            // Setup different responses for different calls
            var callCount = 0;
            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("response content")
                    };
                });

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldDefinitionsResponse>("response content"))
                .Returns(definitionsResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldValueResponse>("response content"))
                .Returns(updateResponse);

            _mockJsonSerializer.Setup(x => x.Serialize(request))
                .Returns("serialized request");

            // Act
            var result = await _customFieldModule.UpdateValuesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task UpdateValuesAsync_WithValidationFailure_ShouldThrowValidationException()
        {
            // Arrange
            var request = new CustomFieldUpdateRequest
            {
                EntityId = "entity123",
                EntityType = "manuscript",
                Values = new Dictionary<string, object>
                {
                    { "field1", 123 } // Invalid type
                },
                ValidateBeforeUpdate = true
            };

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "field1",
                    Name = "String Field",
                    DataType = CustomFieldDataType.String,
                    IsRequired = false,
                    IsEnabled = true
                }
            };

            var definitionsResponse = new CustomFieldDefinitionsResponse
            {
                Success = true,
                CustomFields = definitions
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response content")
            };

            _mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse);

            _mockJsonSerializer.Setup(x => x.Deserialize<CustomFieldDefinitionsResponse>("response content"))
                .Returns(definitionsResponse);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                _customFieldModule.UpdateValuesAsync(request));
        }

        [Fact]
        public async Task UpdateValuesAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.UpdateValuesAsync(null!));
        }

        #endregion

        #region SerializeValuesAsync Tests

        [Fact]
        public async Task SerializeValuesAsync_WithDateTimeValue_ShouldSerializeToIsoString()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc);
            var values = new Dictionary<string, object>
            {
                { "dateField", dateTime }
            };

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "dateField",
                    DataType = CustomFieldDataType.Date
                }
            };

            // Act
            var result = await _customFieldModule.SerializeValuesAsync(values, definitions);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("2023-12-25T10:30:00.000Z", result["dateField"]);
        }

        [Fact]
        public async Task SerializeValuesAsync_WithUnknownField_ShouldIncludeAsIs()
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "unknownField", "test value" }
            };

            var definitions = new List<CustomField>();

            // Act
            var result = await _customFieldModule.SerializeValuesAsync(values, definitions);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test value", result["unknownField"]);
        }

        [Fact]
        public async Task SerializeValuesAsync_WithNullValues_ShouldThrowArgumentNullException()
        {
            // Arrange
            var definitions = new List<CustomField>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.SerializeValuesAsync(null!, definitions));
        }

        [Fact]
        public async Task SerializeValuesAsync_WithNullDefinitions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var values = new Dictionary<string, object>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.SerializeValuesAsync(values, null!));
        }

        #endregion

        #region DeserializeValuesAsync Tests

        [Fact]
        public async Task DeserializeValuesAsync_WithDateString_ShouldDeserializeToDateTime()
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "dateField", "2023-12-25T10:30:00.000Z" }
            };

            var definitions = new List<CustomField>
            {
                new CustomField
                {
                    ApiId = "dateField",
                    DataType = CustomFieldDataType.Date
                }
            };

            // Act
            var result = await _customFieldModule.DeserializeValuesAsync(values, definitions);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.IsType<DateTime>(result["dateField"]);
        }

        [Fact]
        public async Task DeserializeValuesAsync_WithNullValues_ShouldThrowArgumentNullException()
        {
            // Arrange
            var definitions = new List<CustomField>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.DeserializeValuesAsync(null!, definitions));
        }

        [Fact]
        public async Task DeserializeValuesAsync_WithNullDefinitions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var values = new Dictionary<string, object>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _customFieldModule.DeserializeValuesAsync(values, null!));
        }

        #endregion

        #region GetDefaultValue Tests

        [Fact]
        public void GetDefaultValue_WithStringField_ShouldReturnEmptyString()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.String
            };

            // Act
            var result = _customFieldModule.GetDefaultValue(definition);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetDefaultValue_WithNumberField_ShouldReturnZero()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.Number
            };

            // Act
            var result = _customFieldModule.GetDefaultValue(definition);

            // Assert
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void GetDefaultValue_WithBooleanField_ShouldReturnFalse()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.Boolean
            };

            // Act
            var result = _customFieldModule.GetDefaultValue(definition);

            // Assert
            Assert.Equal(false, result);
        }

        [Fact]
        public void GetDefaultValue_WithSingleOptionFieldWithDefault_ShouldReturnDefaultOption()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.SingleOption,
                Options = new List<CustomFieldOption>
                {
                    new CustomFieldOption { Value = "option1", IsDefault = false },
                    new CustomFieldOption { Value = "option2", IsDefault = true }
                }
            };

            // Act
            var result = _customFieldModule.GetDefaultValue(definition);

            // Assert
            Assert.Equal("option2", result);
        }

        [Fact]
        public void GetDefaultValue_WithCustomDefaultValue_ShouldReturnCustomDefault()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.String,
                DefaultValue = "custom default"
            };

            // Act
            var result = _customFieldModule.GetDefaultValue(definition);

            // Assert
            Assert.Equal("custom default", result);
        }

        [Fact]
        public void GetDefaultValue_WithNullDefinition_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _customFieldModule.GetDefaultValue(null!));
        }

        #endregion

        #region IsValidValue Tests

        [Fact]
        public void IsValidValue_WithValidStringValue_ShouldReturnTrue()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.String,
                IsRequired = false
            };

            // Act
            var result = _customFieldModule.IsValidValue("test value", definition);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidValue_WithInvalidStringValue_ShouldReturnFalse()
        {
            // Arrange
            var definition = new CustomField
            {
                DataType = CustomFieldDataType.String,
                IsRequired = false
            };

            // Act
            var result = _customFieldModule.IsValidValue(123, definition);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidValue_WithNullDefinition_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _customFieldModule.IsValidValue("test", null!));
        }

        #endregion

        #region GetValidationErrors Tests

        [Fact]
        public void GetValidationErrors_WithValidValue_ShouldReturnEmptyList()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Test Field",
                DataType = CustomFieldDataType.String,
                IsRequired = false
            };

            // Act
            var result = _customFieldModule.GetValidationErrors("test value", definition);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetValidationErrors_WithMissingRequiredValue_ShouldReturnError()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Required Field",
                DataType = CustomFieldDataType.String,
                IsRequired = true
            };

            // Act
            var result = _customFieldModule.GetValidationErrors(null, definition);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("Required Field", result[0]);
            Assert.Contains("required", result[0]);
        }

        [Fact]
        public void GetValidationErrors_WithStringTooShort_ShouldReturnError()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Test Field",
                DataType = CustomFieldDataType.String,
                IsRequired = false,
                MinLength = 5
            };

            // Act
            var result = _customFieldModule.GetValidationErrors("abc", definition);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("at least 5 characters", result[0]);
        }

        [Fact]
        public void GetValidationErrors_WithStringTooLong_ShouldReturnError()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Test Field",
                DataType = CustomFieldDataType.String,
                IsRequired = false,
                MaxLength = 5
            };

            // Act
            var result = _customFieldModule.GetValidationErrors("too long string", definition);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("no more than 5 characters", result[0]);
        }

        [Fact]
        public void GetValidationErrors_WithNumberTooSmall_ShouldReturnError()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Number Field",
                DataType = CustomFieldDataType.Number,
                IsRequired = false,
                MinValue = 10
            };

            // Act
            var result = _customFieldModule.GetValidationErrors(5.0, definition);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("at least 10", result[0]);
        }

        [Fact]
        public void GetValidationErrors_WithNumberTooLarge_ShouldReturnError()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Number Field",
                DataType = CustomFieldDataType.Number,
                IsRequired = false,
                MaxValue = 100
            };

            // Act
            var result = _customFieldModule.GetValidationErrors(150.0, definition);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("no more than 100", result[0]);
        }

        [Fact]
        public void GetValidationErrors_WithInvalidSingleOption_ShouldReturnError()
        {
            // Arrange
            var definition = new CustomField
            {
                Name = "Option Field",
                DataType = CustomFieldDataType.SingleOption,
                IsRequired = false,
                Options = new List<CustomFieldOption>
                {
                    new CustomFieldOption { Value = "option1", IsEnabled = true },
                    new CustomFieldOption { Value = "option2", IsEnabled = true }
                }
            };

            // Act
            var result = _customFieldModule.GetValidationErrors("invalid_option", definition);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("must be one of", result[0]);
            Assert.Contains("option1", result[0]);
            Assert.Contains("option2", result[0]);
        }

        [Fact]
        public void GetValidationErrors_WithNullDefinition_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _customFieldModule.GetValidationErrors("test", null!));
        }

        #endregion
    }
} 