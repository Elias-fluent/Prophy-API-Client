using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Models
{
    public class JwtLoginClaimsTests
    {
        private readonly SystemTextJsonSerializer _serializer;

        public JwtLoginClaimsTests()
        {
            var mockLogger = new Mock<ILogger<SystemTextJsonSerializer>>();
            _serializer = new SystemTextJsonSerializer(mockLogger.Object);
        }

        [Fact]
        public void JwtLoginClaims_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var claims = new JwtLoginClaims();

            // Assert
            Assert.Equal(string.Empty, claims.Subject);
            Assert.Equal(string.Empty, claims.Organization);
            Assert.Equal(string.Empty, claims.Email);
            Assert.Null(claims.FirstName);
            Assert.Null(claims.LastName);
            Assert.Null(claims.Name);
            Assert.Null(claims.Role);
            Assert.Null(claims.Folder);
            Assert.Null(claims.OriginId);
            Assert.Equal(3600, claims.ExpirationSeconds);
            Assert.Equal("Prophy", claims.Issuer);
            Assert.Equal("Prophy", claims.Audience);
        }

        [Fact]
        public void JwtLoginClaims_Serialization_ShouldUseCamelCasePropertyNames()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                Role = "Admin",
                Folder = "TestFolder",
                OriginId = "TestOriginId",
                ExpirationSeconds = 7200,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };

            // Act
            var json = _serializer.Serialize(claims);

            // Assert
            Assert.Contains("\"sub\":", json);
            Assert.Contains("\"organization\":", json);
            Assert.Contains("\"email\":", json);
            Assert.Contains("\"firstName\":", json);
            Assert.Contains("\"lastName\":", json);
            Assert.Contains("\"name\":", json);
            Assert.Contains("\"role\":", json);
            Assert.Contains("\"folder\":", json);
            Assert.Contains("\"originId\":", json);
            Assert.Contains("\"expirationSeconds\":", json);
            Assert.Contains("\"issuer\":", json);
            Assert.Contains("\"audience\":", json);
        }

        [Fact]
        public void JwtLoginClaims_Deserialization_ShouldWorkCorrectly()
        {
            // Arrange
            var json = """
            {
                "sub": "TestSubject",
                "organization": "TestOrganization",
                "email": "test@example.com",
                "firstName": "John",
                "lastName": "Doe",
                "name": "John Doe",
                "role": "Admin",
                "folder": "TestFolder",
                "originId": "TestOriginId",
                "expirationSeconds": 7200,
                "issuer": "TestIssuer",
                "audience": "TestAudience"
            }
            """;

            // Act
            var claims = _serializer.Deserialize<JwtLoginClaims>(json);

            // Assert
            Assert.NotNull(claims);
            Assert.Equal("TestSubject", claims.Subject);
            Assert.Equal("TestOrganization", claims.Organization);
            Assert.Equal("test@example.com", claims.Email);
            Assert.Equal("John", claims.FirstName);
            Assert.Equal("Doe", claims.LastName);
            Assert.Equal("John Doe", claims.Name);
            Assert.Equal("Admin", claims.Role);
            Assert.Equal("TestFolder", claims.Folder);
            Assert.Equal("TestOriginId", claims.OriginId);
            Assert.Equal(7200, claims.ExpirationSeconds);
            Assert.Equal("TestIssuer", claims.Issuer);
            Assert.Equal("TestAudience", claims.Audience);
        }

        [Fact]
        public void JwtLoginClaims_SerializationRoundTrip_ShouldPreserveAllValues()
        {
            // Arrange
            var originalClaims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                Role = "Admin",
                Folder = "TestFolder",
                OriginId = "TestOriginId",
                ExpirationSeconds = 7200,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };

            // Act
            var json = _serializer.Serialize(originalClaims);
            var deserializedClaims = _serializer.Deserialize<JwtLoginClaims>(json);

            // Assert
            Assert.NotNull(deserializedClaims);
            Assert.Equal(originalClaims.Subject, deserializedClaims.Subject);
            Assert.Equal(originalClaims.Organization, deserializedClaims.Organization);
            Assert.Equal(originalClaims.Email, deserializedClaims.Email);
            Assert.Equal(originalClaims.FirstName, deserializedClaims.FirstName);
            Assert.Equal(originalClaims.LastName, deserializedClaims.LastName);
            Assert.Equal(originalClaims.Name, deserializedClaims.Name);
            Assert.Equal(originalClaims.Role, deserializedClaims.Role);
            Assert.Equal(originalClaims.Folder, deserializedClaims.Folder);
            Assert.Equal(originalClaims.OriginId, deserializedClaims.OriginId);
            Assert.Equal(originalClaims.ExpirationSeconds, deserializedClaims.ExpirationSeconds);
            Assert.Equal(originalClaims.Issuer, deserializedClaims.Issuer);
            Assert.Equal(originalClaims.Audience, deserializedClaims.Audience);
        }

        [Fact]
        public void JwtLoginClaims_WithNullOptionalFields_ShouldSerializeCorrectly()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com"
                // Optional fields left as null
            };

            // Act
            var json = _serializer.Serialize(claims);
            var deserializedClaims = _serializer.Deserialize<JwtLoginClaims>(json);

            // Assert
            Assert.NotNull(deserializedClaims);
            Assert.Equal("TestSubject", deserializedClaims.Subject);
            Assert.Equal("TestOrganization", deserializedClaims.Organization);
            Assert.Equal("test@example.com", deserializedClaims.Email);
            Assert.Null(deserializedClaims.FirstName);
            Assert.Null(deserializedClaims.LastName);
            Assert.Null(deserializedClaims.Name);
            Assert.Null(deserializedClaims.Role);
            Assert.Null(deserializedClaims.Folder);
            Assert.Null(deserializedClaims.OriginId);
        }

        [Fact]
        public void JwtLoginClaims_RequiredFieldValidation_ShouldWork()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                // Subject is required but not set (empty string)
                Organization = "TestOrganization",
                Email = "test@example.com"
            };

            var validationContext = new ValidationContext(claims);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(claims, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(JwtLoginClaims.Subject)));
        }

        [Fact]
        public void JwtLoginClaims_EmailValidation_ShouldWork()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "invalid-email-format"
            };

            var validationContext = new ValidationContext(claims);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(claims, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(JwtLoginClaims.Email)));
        }

        [Fact]
        public void JwtLoginClaims_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com"
            };

            var validationContext = new ValidationContext(claims);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(claims, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void JwtLoginClaims_ExpirationSeconds_ShouldHaveCorrectRange()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com",
                ExpirationSeconds = -1 // Invalid value
            };

            var validationContext = new ValidationContext(claims);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(claims, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(JwtLoginClaims.ExpirationSeconds)));
        }

        [Fact]
        public void JwtLoginClaims_ExpirationSeconds_WithValidValue_ShouldPassValidation()
        {
            // Arrange
            var claims = new JwtLoginClaims
            {
                Subject = "TestSubject",
                Organization = "TestOrganization",
                Email = "test@example.com",
                ExpirationSeconds = 7200
            };

            var validationContext = new ValidationContext(claims);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(claims, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }
    }
} 