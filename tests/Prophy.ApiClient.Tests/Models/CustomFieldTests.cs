using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Models
{
    public class CustomFieldTests
    {
        private readonly IJsonSerializer _serializer;

        public CustomFieldTests()
        {
            _serializer = SerializationFactory.CreateJsonSerializer();
        }

        [Fact]
        public void CustomField_DefaultConstructor_SetsRequiredProperties()
        {
            // Arrange & Act
            var customField = new CustomField();

            // Assert
            Assert.Equal(string.Empty, customField.ApiId);
            Assert.Equal(string.Empty, customField.Name);
            Assert.Equal(CustomFieldDataType.String, customField.DataType);
            Assert.False(customField.IsRequired);
            Assert.False(customField.IsMultiple);
            Assert.True(customField.IsVisible);
            Assert.True(customField.IsEnabled);
        }

        [Fact]
        public void CustomField_WithValidData_SerializesCorrectly()
        {
            // Arrange
            var customField = new CustomField
            {
                ApiId = "custom_field_1",
                Name = "Research Area",
                Description = "Primary research area of the author",
                DataType = CustomFieldDataType.SingleOption,
                IsRequired = true,
                IsMultiple = false,
                DefaultValue = "Computer Science",
                Options = new List<CustomFieldOption>
                {
                    new CustomFieldOption { Value = "Computer Science", Label = "Computer Science", IsDefault = true },
                    new CustomFieldOption { Value = "Biology", Label = "Biology", IsDefault = false }
                },
                DisplayOrder = 1,
                IsVisible = true,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var json = _serializer.Serialize(customField);
            var deserializedField = _serializer.Deserialize<CustomField>(json);

            // Assert
            Assert.NotNull(deserializedField);
            Assert.Equal(customField.ApiId, deserializedField.ApiId);
            Assert.Equal(customField.Name, deserializedField.Name);
            Assert.Equal(customField.Description, deserializedField.Description);
            Assert.Equal(customField.DataType, deserializedField.DataType);
            Assert.Equal(customField.IsRequired, deserializedField.IsRequired);
            Assert.Equal(customField.IsMultiple, deserializedField.IsMultiple);
            Assert.Equal(customField.DisplayOrder, deserializedField.DisplayOrder);
            Assert.Equal(customField.Options?.Count, deserializedField.Options?.Count);
        }

        [Fact]
        public void CustomFieldDataType_SerializesToStringValues()
        {
            // Arrange
            var testCases = new[]
            {
                (CustomFieldDataType.String, "string"),
                (CustomFieldDataType.Number, "number"),
                (CustomFieldDataType.Date, "date"),
                (CustomFieldDataType.Boolean, "boolean"),
                (CustomFieldDataType.SingleOption, "single-option"),
                (CustomFieldDataType.MultiOption, "multi-option"),
                (CustomFieldDataType.Array, "array"),
                (CustomFieldDataType.Object, "object")
            };

            foreach (var (dataType, expectedString) in testCases)
            {
                // Arrange
                var customField = new CustomField
                {
                    ApiId = "test_field",
                    Name = "Test Field",
                    DataType = dataType
                };

                // Act
                var json = _serializer.Serialize(customField);

                // Assert
                Assert.Contains($"\"dataType\":\"{expectedString}\"", json);
            }
        }

        [Fact]
        public void CustomFieldDataType_DeserializesFromStringValues()
        {
            // Arrange
            var json = """
                {
                    "apiId": "test_field",
                    "name": "Test Field",
                    "dataType": "single-option"
                }
                """;

            // Act
            var customField = _serializer.Deserialize<CustomField>(json);

            // Assert
            Assert.NotNull(customField);
            Assert.Equal(CustomFieldDataType.SingleOption, customField.DataType);
        }

        [Fact]
        public void CustomField_WithOptions_SerializesCorrectly()
        {
            // Arrange
            var customField = new CustomField
            {
                ApiId = "priority_field",
                Name = "Priority",
                DataType = CustomFieldDataType.SingleOption,
                Options = new List<CustomFieldOption>
                {
                    new CustomFieldOption
                    {
                        Id = "1",
                        Value = "high",
                        Label = "High Priority",
                        IsDefault = false,
                        IsEnabled = true,
                        DisplayOrder = 1
                    },
                    new CustomFieldOption
                    {
                        Id = "2",
                        Value = "medium",
                        Label = "Medium Priority",
                        IsDefault = true,
                        IsEnabled = true,
                        DisplayOrder = 2
                    }
                }
            };

            // Act
            var json = _serializer.Serialize(customField);
            var deserializedField = _serializer.Deserialize<CustomField>(json);

            // Assert
            Assert.NotNull(deserializedField);
            Assert.NotNull(deserializedField.Options);
            Assert.Equal(2, deserializedField.Options.Count);
            
            var highOption = deserializedField.Options.Find(o => o.Value == "high");
            Assert.NotNull(highOption);
            Assert.Equal("High Priority", highOption.Label);
            Assert.False(highOption.IsDefault);
            
            var mediumOption = deserializedField.Options.Find(o => o.Value == "medium");
            Assert.NotNull(mediumOption);
            Assert.Equal("Medium Priority", mediumOption.Label);
            Assert.True(mediumOption.IsDefault);
        }

        [Fact]
        public void CustomField_WithValidationRules_SerializesCorrectly()
        {
            // Arrange
            var customField = new CustomField
            {
                ApiId = "text_field",
                Name = "Description",
                DataType = CustomFieldDataType.String,
                MinLength = 10,
                MaxLength = 500,
                ValidationPattern = @"^[a-zA-Z0-9\s]+$",
                ValidationMessage = "Only alphanumeric characters and spaces are allowed"
            };

            // Act
            var json = _serializer.Serialize(customField);
            var deserializedField = _serializer.Deserialize<CustomField>(json);

            // Assert
            Assert.NotNull(deserializedField);
            Assert.Equal(customField.MinLength, deserializedField.MinLength);
            Assert.Equal(customField.MaxLength, deserializedField.MaxLength);
            Assert.Equal(customField.ValidationPattern, deserializedField.ValidationPattern);
            Assert.Equal(customField.ValidationMessage, deserializedField.ValidationMessage);
        }

        [Fact]
        public void CustomField_WithNumberValidation_SerializesCorrectly()
        {
            // Arrange
            var customField = new CustomField
            {
                ApiId = "score_field",
                Name = "Score",
                DataType = CustomFieldDataType.Number,
                MinValue = 0.0,
                MaxValue = 100.0,
                DefaultValue = 50.0
            };

            // Act
            var json = _serializer.Serialize(customField);
            var deserializedField = _serializer.Deserialize<CustomField>(json);

            // Assert
            Assert.NotNull(deserializedField);
            Assert.Equal(customField.MinValue, deserializedField.MinValue);
            Assert.Equal(customField.MaxValue, deserializedField.MaxValue);
            
            // DefaultValue is deserialized as JsonElement, so we need to convert it
            if (deserializedField.DefaultValue is JsonElement jsonElement)
            {
                Assert.Equal(50.0, jsonElement.GetDouble());
            }
            else
            {
                Assert.Equal(customField.DefaultValue, deserializedField.DefaultValue);
            }
        }

        [Fact]
        public void CustomField_RequiredValidation_WorksCorrectly()
        {
            // Arrange
            var customField = new CustomField(); // ApiId and Name are empty

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(customField);
            var isValid = Validator.TryValidateObject(customField, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Equal(2, validationResults.Count); // ApiId and Name are both required
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("ApiId"));
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Name"));
        }

        [Fact]
        public void CustomField_WithValidRequiredFields_PassesValidation()
        {
            // Arrange
            var customField = new CustomField
            {
                ApiId = "valid_field",
                Name = "Valid Field",
                DataType = CustomFieldDataType.String
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(customField);
            var isValid = Validator.TryValidateObject(customField, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CustomFieldOption_RequiredValidation_WorksCorrectly()
        {
            // Arrange
            var option = new CustomFieldOption(); // Value is empty

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(option);
            var isValid = Validator.TryValidateObject(option, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(validationResults);
            Assert.Contains("Value", validationResults[0].MemberNames);
        }

        [Fact]
        public void CustomField_SerializesToCamelCase()
        {
            // Arrange
            var customField = new CustomField
            {
                ApiId = "test_field",
                Name = "Test Field",
                DataType = CustomFieldDataType.MultiOption,
                IsRequired = true,
                IsMultiple = true,
                DefaultValue = "test",
                MinLength = 5,
                MaxLength = 100,
                ValidationPattern = ".*",
                ValidationMessage = "Test message",
                DisplayOrder = 1,
                IsVisible = true,
                IsEnabled = true
            };

            // Act
            var json = _serializer.Serialize(customField);

            // Assert
            Assert.Contains("\"apiId\":", json);
            Assert.Contains("\"dataType\":", json);
            Assert.Contains("\"isRequired\":", json);
            Assert.Contains("\"isMultiple\":", json);
            Assert.Contains("\"defaultValue\":", json);
            Assert.Contains("\"minLength\":", json);
            Assert.Contains("\"maxLength\":", json);
            Assert.Contains("\"validationPattern\":", json);
            Assert.Contains("\"validationMessage\":", json);
            Assert.Contains("\"displayOrder\":", json);
            Assert.Contains("\"isVisible\":", json);
            Assert.Contains("\"isEnabled\":", json);
        }
    }
} 