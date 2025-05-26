using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Models
{
    public class AuthorTests
    {
        private readonly IJsonSerializer _serializer;

        public AuthorTests()
        {
            _serializer = SerializationFactory.CreateJsonSerializer();
        }

        [Fact]
        public void Author_DefaultConstructor_SetsRequiredProperties()
        {
            // Arrange & Act
            var author = new Author();

            // Assert
            Assert.Equal(string.Empty, author.Name);
            Assert.Null(author.Email);
            Assert.Null(author.Orcid);
            Assert.Null(author.Affiliation);
        }

        [Fact]
        public void Author_WithValidData_SerializesCorrectly()
        {
            // Arrange
            var author = new Author
            {
                Name = "Dr. Jane Smith",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@university.edu",
                Orcid = "0000-0000-0000-0000",
                Affiliation = "University of Science",
                Department = "Computer Science",
                Country = "United States",
                HIndex = 25,
                CitationCount = 1500,
                PublicationCount = 45,
                ResearchInterests = new List<string> { "Machine Learning", "Data Science" },
                Position = "Professor",
                Website = "https://janesmith.university.edu",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var json = _serializer.Serialize(author);
            var deserializedAuthor = _serializer.Deserialize<Author>(json);

            // Assert
            Assert.NotNull(deserializedAuthor);
            Assert.Equal(author.Name, deserializedAuthor.Name);
            Assert.Equal(author.FirstName, deserializedAuthor.FirstName);
            Assert.Equal(author.LastName, deserializedAuthor.LastName);
            Assert.Equal(author.Email, deserializedAuthor.Email);
            Assert.Equal(author.Orcid, deserializedAuthor.Orcid);
            Assert.Equal(author.Affiliation, deserializedAuthor.Affiliation);
            Assert.Equal(author.Department, deserializedAuthor.Department);
            Assert.Equal(author.Country, deserializedAuthor.Country);
            Assert.Equal(author.HIndex, deserializedAuthor.HIndex);
            Assert.Equal(author.CitationCount, deserializedAuthor.CitationCount);
            Assert.Equal(author.PublicationCount, deserializedAuthor.PublicationCount);
            Assert.Equal(author.Position, deserializedAuthor.Position);
            Assert.Equal(author.Website, deserializedAuthor.Website);
            Assert.Equal(author.ResearchInterests?.Count, deserializedAuthor.ResearchInterests?.Count);
        }

        [Fact]
        public void Author_SerializesToCamelCase()
        {
            // Arrange
            var author = new Author
            {
                Name = "Test Author",
                FirstName = "Test",
                LastName = "Author",
                ResearchInterests = new List<string> { "AI" }
            };

            // Act
            var json = _serializer.Serialize(author);

            // Assert
            Assert.Contains("\"name\":", json);
            Assert.Contains("\"firstName\":", json);
            Assert.Contains("\"lastName\":", json);
            Assert.Contains("\"researchInterests\":", json);
            Assert.DoesNotContain("\"Name\":", json);
            Assert.DoesNotContain("\"FirstName\":", json);
        }

        [Fact]
        public void Author_WithNullValues_SerializesCorrectly()
        {
            // Arrange
            var author = new Author
            {
                Name = "Minimal Author"
            };

            // Act
            var json = _serializer.Serialize(author);
            var deserializedAuthor = _serializer.Deserialize<Author>(json);

            // Assert
            Assert.NotNull(deserializedAuthor);
            Assert.Equal("Minimal Author", deserializedAuthor.Name);
            Assert.Null(deserializedAuthor.Email);
            Assert.Null(deserializedAuthor.Orcid);
            Assert.Null(deserializedAuthor.ResearchInterests);
        }

        [Fact]
        public void Author_WithMultipleEmails_SerializesCorrectly()
        {
            // Arrange
            var author = new Author
            {
                Name = "Multi Email Author",
                Email = "primary@university.edu",
                Emails = new List<string> { "primary@university.edu", "secondary@university.edu" }
            };

            // Act
            var json = _serializer.Serialize(author);
            var deserializedAuthor = _serializer.Deserialize<Author>(json);

            // Assert
            Assert.NotNull(deserializedAuthor);
            Assert.Equal(author.Email, deserializedAuthor.Email);
            Assert.Equal(2, deserializedAuthor.Emails?.Count);
            Assert.Contains("primary@university.edu", deserializedAuthor.Emails);
            Assert.Contains("secondary@university.edu", deserializedAuthor.Emails);
        }

        [Fact]
        public void Author_WithMetadata_SerializesCorrectly()
        {
            // Arrange
            var author = new Author
            {
                Name = "Metadata Author",
                Metadata = new Dictionary<string, object>
                {
                    { "customField1", "value1" },
                    { "customField2", 42 },
                    { "customField3", true }
                }
            };

            // Act
            var json = _serializer.Serialize(author);
            var deserializedAuthor = _serializer.Deserialize<Author>(json);

            // Assert
            Assert.NotNull(deserializedAuthor);
            Assert.NotNull(deserializedAuthor.Metadata);
            Assert.Equal(3, deserializedAuthor.Metadata.Count);
            Assert.True(deserializedAuthor.Metadata.ContainsKey("customField1"));
            Assert.True(deserializedAuthor.Metadata.ContainsKey("customField2"));
            Assert.True(deserializedAuthor.Metadata.ContainsKey("customField3"));
        }

        [Fact]
        public void Author_RequiredAttribute_ValidatesCorrectly()
        {
            // Arrange
            var author = new Author(); // Name is empty string by default

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(author);
            var isValid = Validator.TryValidateObject(author, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(validationResults);
            Assert.Contains("Name", validationResults[0].MemberNames);
        }

        [Fact]
        public void Author_EmailAttribute_ValidatesCorrectly()
        {
            // Arrange
            var author = new Author
            {
                Name = "Test Author",
                Email = "invalid-email"
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(author);
            var isValid = Validator.TryValidateObject(author, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(validationResults);
            Assert.Contains("Email", validationResults[0].MemberNames);
        }

        [Fact]
        public void Author_UrlAttribute_ValidatesCorrectly()
        {
            // Arrange
            var author = new Author
            {
                Name = "Test Author",
                Website = "not-a-valid-url"
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(author);
            var isValid = Validator.TryValidateObject(author, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(validationResults);
            Assert.Contains("Website", validationResults[0].MemberNames);
        }

        [Fact]
        public void Author_WithValidEmailAndUrl_PassesValidation()
        {
            // Arrange
            var author = new Author
            {
                Name = "Valid Author",
                Email = "valid@university.edu",
                Website = "https://valid.university.edu"
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(author);
            var isValid = Validator.TryValidateObject(author, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }
    }
} 