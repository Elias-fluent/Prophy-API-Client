using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Modules;
using Xunit;

namespace Prophy.ApiClient.Tests.Modules
{
    public class WebhookValidatorTests
    {
        private readonly Mock<ILogger<WebhookValidator>> _mockLogger;
        private readonly WebhookValidator _validator;

        public WebhookValidatorTests()
        {
            _mockLogger = new Mock<ILogger<WebhookValidator>>();
            _validator = new WebhookValidator(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WebhookValidator(null!));
        }

        [Fact]
        public void ValidateSignature_WithValidSignature_ReturnsTrue()
        {
            // Arrange
            var payload = "test payload";
            var secret = "test-secret";
            var expectedSignature = _validator.GenerateSignature(payload, secret);

            // Act
            var result = _validator.ValidateSignature(payload, expectedSignature, secret);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateSignature_WithInvalidSignature_ReturnsFalse()
        {
            // Arrange
            var payload = "test payload";
            var secret = "test-secret";
            var invalidSignature = "invalid-signature";

            // Act
            var result = _validator.ValidateSignature(payload, invalidSignature, secret);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSignature_WithDifferentSecret_ReturnsFalse()
        {
            // Arrange
            var payload = "test payload";
            var secret1 = "secret1";
            var secret2 = "secret2";
            var signature = _validator.GenerateSignature(payload, secret1);

            // Act
            var result = _validator.ValidateSignature(payload, signature, secret2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSignature_WithMultipleSecrets_ValidatesAgainstAll()
        {
            // Arrange
            var payload = "test payload";
            var secrets = new[] { "secret1", "secret2", "secret3" };
            var correctSecret = "secret2";
            var signature = _validator.GenerateSignature(payload, correctSecret);

            // Act
            var result = _validator.ValidateSignature(payload, signature, secrets);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateSignature_WithMultipleSecretsNoneMatch_ReturnsFalse()
        {
            // Arrange
            var payload = "test payload";
            var secrets = new[] { "secret1", "secret2", "secret3" };
            var wrongSecret = "wrong-secret";
            var signature = _validator.GenerateSignature(payload, wrongSecret);

            // Act
            var result = _validator.ValidateSignature(payload, signature, secrets);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null, "signature", "secret")]
        [InlineData("", "signature", "secret")]
        [InlineData("payload", null, "secret")]
        [InlineData("payload", "", "secret")]
        [InlineData("payload", "signature", null)]
        [InlineData("payload", "signature", "")]
        public void ValidateSignature_WithInvalidParameters_ThrowsArgumentException(
            string payload, string signature, string secret)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _validator.ValidateSignature(payload, signature, secret));
        }

        [Fact]
        public void ValidateSignature_WithMultipleSecretsNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _validator.ValidateSignature("payload", "signature", (IEnumerable<string>)null!));
        }

        [Fact]
        public void ValidateSignature_WithEmptySecrets_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _validator.ValidateSignature("payload", "signature", new string[0]));
        }

        [Fact]
        public void GenerateSignature_WithValidInputs_ReturnsConsistentSignature()
        {
            // Arrange
            var payload = "test payload";
            var secret = "test-secret";

            // Act
            var signature1 = _validator.GenerateSignature(payload, secret);
            var signature2 = _validator.GenerateSignature(payload, secret);

            // Assert
            Assert.Equal(signature1, signature2);
            Assert.NotEmpty(signature1);
        }

        [Fact]
        public void GenerateSignature_WithDifferentPayloads_ReturnsDifferentSignatures()
        {
            // Arrange
            var payload1 = "test payload 1";
            var payload2 = "test payload 2";
            var secret = "test-secret";

            // Act
            var signature1 = _validator.GenerateSignature(payload1, secret);
            var signature2 = _validator.GenerateSignature(payload2, secret);

            // Assert
            Assert.NotEqual(signature1, signature2);
        }

        [Fact]
        public void GenerateSignature_WithDifferentSecrets_ReturnsDifferentSignatures()
        {
            // Arrange
            var payload = "test payload";
            var secret1 = "secret1";
            var secret2 = "secret2";

            // Act
            var signature1 = _validator.GenerateSignature(payload, secret1);
            var signature2 = _validator.GenerateSignature(payload, secret2);

            // Assert
            Assert.NotEqual(signature1, signature2);
        }

        [Theory]
        [InlineData(null, "secret")]
        [InlineData("", "secret")]
        [InlineData("payload", null)]
        [InlineData("payload", "")]
        public void GenerateSignature_WithInvalidParameters_ThrowsArgumentException(
            string payload, string secret)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _validator.GenerateSignature(payload, secret));
        }

        [Theory]
        [InlineData("sha256=abc123def456", "abc123def456")]
        [InlineData("sha1=xyz789", "xyz789")]
        [InlineData("abc123def456", "abc123def456")]
        [InlineData("  sha256=abc123def456  ", "abc123def456")]
        public void ExtractSignature_WithVariousFormats_ExtractsCorrectly(
            string signatureHeader, string expectedSignature)
        {
            // Act
            var result = _validator.ExtractSignature(signatureHeader);

            // Assert
            Assert.Equal(expectedSignature, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ExtractSignature_WithInvalidInput_ThrowsArgumentException(string signatureHeader)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _validator.ExtractSignature(signatureHeader));
        }

        [Fact]
        public void ValidatePayloadStructure_WithValidPayload_ReturnsTrue()
        {
            // Arrange
            var payload = @"{
                ""id"": ""test-id"",
                ""event_type"": ""MarkAsProposalReferee"",
                ""timestamp"": ""2023-01-01T00:00:00Z"",
                ""organization"": ""test-org"",
                ""data"": {}
            }";

            // Act
            var result = _validator.ValidatePayloadStructure(payload);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidatePayloadStructure_WithMissingRequiredField_ReturnsFalse()
        {
            // Arrange
            var payload = @"{
                ""event_type"": ""MarkAsProposalReferee"",
                ""timestamp"": ""2023-01-01T00:00:00Z"",
                ""organization"": ""test-org""
            }"; // Missing "id" field

            // Act
            var result = _validator.ValidatePayloadStructure(payload);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("id")]
        [InlineData("event_type")]
        [InlineData("timestamp")]
        [InlineData("organization")]
        public void ValidatePayloadStructure_WithMissingSpecificField_ReturnsFalse(string missingField)
        {
            // Arrange
            var basePayload = new Dictionary<string, object>
            {
                ["id"] = "test-id",
                ["event_type"] = "MarkAsProposalReferee",
                ["timestamp"] = "2023-01-01T00:00:00Z",
                ["organization"] = "test-org"
            };

            basePayload.Remove(missingField);
            var payload = System.Text.Json.JsonSerializer.Serialize(basePayload);

            // Act
            var result = _validator.ValidatePayloadStructure(payload);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidatePayloadStructure_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            var payload = "{ invalid json }";

            // Act
            var result = _validator.ValidatePayloadStructure(payload);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidatePayloadStructure_WithUnknownEventType_ReturnsTrue()
        {
            // Arrange - Unknown event types should not fail validation for extensibility
            var payload = @"{
                ""id"": ""test-id"",
                ""event_type"": ""UnknownEventType"",
                ""timestamp"": ""2023-01-01T00:00:00Z"",
                ""organization"": ""test-org"",
                ""data"": {}
            }";

            // Act
            var result = _validator.ValidatePayloadStructure(payload);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidatePayloadStructure_WithInvalidInput_ReturnsFalse(string payload)
        {
            // Act
            var result = _validator.ValidatePayloadStructure(payload);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSignature_WithMultipleSecretsContainingNulls_SkipsNullSecrets()
        {
            // Arrange
            var payload = "test payload";
            var secrets = new[] { null!, "secret1", "", "secret2", null! };
            var correctSecret = "secret2";
            var signature = _validator.GenerateSignature(payload, correctSecret);

            // Act
            var result = _validator.ValidateSignature(payload, signature, secrets);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GenerateSignature_ProducesLowercaseHexString()
        {
            // Arrange
            var payload = "test payload";
            var secret = "test-secret";

            // Act
            var signature = _validator.GenerateSignature(payload, secret);

            // Assert
            Assert.True(signature.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f')));
            Assert.Equal(64, signature.Length); // SHA-256 produces 64 character hex string
        }

        [Fact]
        public void ValidateSignature_IsCaseInsensitive()
        {
            // Arrange
            var payload = "test payload";
            var secret = "test-secret";
            var signature = _validator.GenerateSignature(payload, secret);
            var uppercaseSignature = signature.ToUpperInvariant();

            // Act
            var result = _validator.ValidateSignature(payload, uppercaseSignature, secret);

            // Assert
            Assert.True(result);
        }
    }
} 