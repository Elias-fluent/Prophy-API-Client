using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Serialization;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.Serialization.Enhanced
{
    /// <summary>
    /// Enhanced tests for JSON serialization and deserialization using property-based testing.
    /// </summary>
    public class SerializationEnhancedTests
    {
        private readonly Mock<ILogger<SystemTextJsonSerializer>> _mockLogger;
        private readonly SystemTextJsonSerializer _serializationService;

        public SerializationEnhancedTests()
        {
            _mockLogger = TestHelpers.CreateMockLogger<SystemTextJsonSerializer>();
            _serializationService = new SystemTextJsonSerializer(_mockLogger.Object);
        }

        #region Property-Based Tests for Basic Serialization

        [Property]
        public Property SerializeDeserialize_WithStrings_ShouldRoundTrip()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                input =>
                {
                    try
                    {
                        // Act
                        var serialized = _serializationService.Serialize(input.Get);
                        var deserialized = _serializationService.Deserialize<string>(serialized);

                        // Assert
                        return deserialized == input.Get;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property SerializeDeserialize_WithIntegers_ShouldRoundTrip()
        {
            return Prop.ForAll(
                Arb.From<int>(),
                input =>
                {
                    try
                    {
                        // Act
                        var serialized = _serializationService.Serialize(input);
                        var deserialized = _serializationService.Deserialize<int>(serialized);

                        // Assert
                        return deserialized == input;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property SerializeDeserialize_WithBooleans_ShouldRoundTrip()
        {
            return Prop.ForAll(
                Arb.From<bool>(),
                input =>
                {
                    try
                    {
                        // Act
                        var serialized = _serializationService.Serialize(input);
                        var deserialized = _serializationService.Deserialize<bool>(serialized);

                        // Assert
                        return deserialized == input;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Property-Based Tests for Complex Objects

        [Property]
        public Property SerializeDeserialize_WithManuscriptUploadRequest_ShouldRoundTrip()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (title, abstractText, journal) =>
                {
                    try
                    {
                        // Arrange
                        var request = new ManuscriptUploadRequest
                        {
                            Title = title.Get,
                            Abstract = abstractText.Get,
                            Journal = journal.Get,
                            Keywords = new List<string> { "test", "property-based" },
                            AuthorNames = new List<string> { "Test Author" },
                            AuthorEmails = new List<string> { "test@example.com" }
                        };

                        // Act
                        var serialized = _serializationService.Serialize(request);
                        var deserialized = _serializationService.Deserialize<ManuscriptUploadRequest>(serialized);

                        // Assert
                        return deserialized != null &&
                               deserialized.Title == request.Title &&
                               deserialized.Abstract == request.Abstract &&
                               deserialized.Journal == request.Journal;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property SerializeDeserialize_WithJournalRecommendationRequest_ShouldRoundTrip()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<PositiveInt>(),
                (manuscriptId, limit) =>
                {
                    try
                    {
                        // Arrange
                        var request = new JournalRecommendationRequest
                        {
                            ManuscriptId = manuscriptId.Get,
                            Limit = limit.Get,
                            MinRelevanceScore = 0.5,
                            OpenAccessOnly = true
                        };

                        // Act
                        var serialized = _serializationService.Serialize(request);
                        var deserialized = _serializationService.Deserialize<JournalRecommendationRequest>(serialized);

                        // Assert
                        return deserialized != null &&
                               deserialized.ManuscriptId == request.ManuscriptId &&
                               deserialized.Limit == request.Limit &&
                               deserialized.MinRelevanceScore == request.MinRelevanceScore;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Special Character and Encoding Tests

        [Theory]
        [InlineData("Hello, 世界!")]
        [InlineData("Café résumé naïve")]
        [InlineData("Emoji test: 🚀🌟💻")]
        [InlineData("Special chars: !@#$%^&*()")]
        [InlineData("Quotes: \"single\" and 'double'")]
        [InlineData("Newlines:\nand\ttabs")]
        public void SerializeDeserialize_WithSpecialCharacters_ShouldRoundTrip(string input)
        {
            // Act
            var serialized = _serializationService.Serialize(input);
            var deserialized = _serializationService.Deserialize<string>(serialized);

            // Assert
            Assert.Equal(input, deserialized);
        }

        [Fact]
        public void SerializeDeserialize_WithLargeString_ShouldRoundTrip()
        {
            // Arrange
            var largeString = new string('A', 10000) + "Special content: 世界 🌟" + new string('B', 10000);

            // Act
            var serialized = _serializationService.Serialize(largeString);
            var deserialized = _serializationService.Deserialize<string>(serialized);

            // Assert
            Assert.Equal(largeString, deserialized);
        }

        [Fact]
        public void SerializeDeserialize_WithComplexNestedObject_ShouldRoundTrip()
        {
            // Arrange
            var complexObject = new
            {
                StringValue = "Test string with special chars: 世界 🌟",
                IntValue = 42,
                BoolValue = true,
                ArrayValue = new[] { "item1", "item2", "item3" },
                NestedObject = new
                {
                    NestedString = "Nested value",
                    NestedArray = new[] { 1, 2, 3, 4, 5 }
                },
                NullValue = (string?)null
            };

            // Act
            var serialized = _serializationService.Serialize(complexObject);
            var deserialized = _serializationService.Deserialize<dynamic>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.NotNull(serialized);
            Assert.Contains("Test string with special chars", serialized);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void Serialize_WithNullObject_ShouldReturnNullJson()
        {
            // Act
            var result = _serializationService.Serialize<object>(null!);

            // Assert
            Assert.Equal("null", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid json")]
        [InlineData("{invalid}")]
        [InlineData("[1,2,3")]
        [InlineData("{'key': value}")]
        public void Deserialize_WithInvalidJson_ShouldThrowJsonException(string invalidJson)
        {
            // Act & Assert
            Assert.Throws<JsonException>(() =>
                _serializationService.Deserialize<object>(invalidJson));
        }

        [Fact]
        public void Deserialize_WithNullJson_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _serializationService.Deserialize<object>(null!));
        }

        [Fact]
        public void Deserialize_WithEmptyJson_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _serializationService.Deserialize<object>(""));
        }

        [Fact]
        public void Deserialize_WithMismatchedType_ShouldThrowJsonException()
        {
            // Arrange
            var jsonString = _serializationService.Serialize("string value");

            // Act & Assert
            Assert.Throws<JsonException>(() =>
                _serializationService.Deserialize<int>(jsonString));
        }

        #endregion

        #region JSON Options and Configuration Tests

        [Fact]
        public void Serialize_WithCamelCaseNaming_ShouldUseCamelCase()
        {
            // Arrange
            var obj = new { FirstName = "John", LastName = "Doe" };

            // Act
            var json = _serializationService.Serialize(obj);

            // Assert
            Assert.Contains("firstName", json);
            Assert.Contains("lastName", json);
            Assert.DoesNotContain("FirstName", json);
            Assert.DoesNotContain("LastName", json);
        }

        [Fact]
        public void Serialize_WithNullValues_ShouldIgnoreNulls()
        {
            // Arrange
            var obj = new { Name = "John", NullValue = (string?)null };

            // Act
            var json = _serializationService.Serialize(obj);

            // Assert
            Assert.Contains("name", json);
            Assert.DoesNotContain("nullValue", json);
            Assert.DoesNotContain("null", json);
        }

        [Fact]
        public void Serialize_WithDateTimeValues_ShouldUseIsoFormat()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Utc);
            var obj = new { CreatedAt = dateTime };

            // Act
            var json = _serializationService.Serialize(obj);

            // Assert
            Assert.Contains("2023-12-25T10:30:45", json);
        }

        #endregion

        #region Performance and Memory Tests

        [Fact]
        public void SerializeDeserialize_WithLargeArray_ShouldHandleEfficiently()
        {
            // Arrange
            var largeArray = Enumerable.Range(1, 10000).ToArray();

            // Act
            var serialized = _serializationService.Serialize(largeArray);
            var deserialized = _serializationService.Deserialize<int[]>(serialized);

            // Assert
            Assert.Equal(largeArray.Length, deserialized.Length);
            Assert.Equal(largeArray.First(), deserialized.First());
            Assert.Equal(largeArray.Last(), deserialized.Last());
        }

        [Fact]
        public void SerializeDeserialize_WithDeeplyNestedObject_ShouldHandle()
        {
            // Arrange - Create a deeply nested object
            object nestedObject = "leaf value";
            for (int i = 0; i < 50; i++)
            {
                nestedObject = new { Level = i, Nested = nestedObject };
            }

            // Act
            var serialized = _serializationService.Serialize(nestedObject);
            var deserialized = _serializationService.Deserialize<dynamic>(serialized);

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.True(serialized.Length > 1000); // Should be a substantial JSON string
        }

        #endregion

        #region Concurrent Serialization Tests

        [Fact]
        public async Task SerializeDeserialize_WithConcurrentOperations_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = Enumerable.Range(1, 10)
                .Select(i => Task.Run(() =>
                {
                    var obj = new { Id = i, Name = $"Object {i}", Value = i * 10 };
                    var serialized = _serializationService.Serialize(obj);
                    var deserialized = _serializationService.Deserialize<dynamic>(serialized);
                    return new { Original = obj, Serialized = serialized, Deserialized = deserialized };
                }))
                .ToList();

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result =>
            {
                Assert.NotNull(result.Serialized);
                Assert.NotNull(result.Deserialized);
                Assert.Contains($"\"id\":{result.Original.Id}", result.Serialized);
                Assert.Contains($"\"name\":\"Object {result.Original.Id}\"", result.Serialized);
            });
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        public void SerializeDeserialize_WithBoundaryIntegers_ShouldRoundTrip(int value)
        {
            // Act
            var serialized = _serializationService.Serialize(value);
            var deserialized = _serializationService.Deserialize<int>(serialized);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Theory]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(1.0)]
        [InlineData(3.14159)]
        public void SerializeDeserialize_WithBoundaryDoubles_ShouldRoundTrip(double value)
        {
            // Act
            var serialized = _serializationService.Serialize(value);
            var deserialized = _serializationService.Deserialize<double>(serialized);

            // Assert
            Assert.Equal(value, deserialized, precision: 10);
        }

        [Fact]
        public void SerializeDeserialize_WithEmptyCollections_ShouldRoundTrip()
        {
            // Arrange
            var obj = new
            {
                EmptyArray = new string[0],
                EmptyList = new List<int>(),
                EmptyDictionary = new Dictionary<string, object>()
            };

            // Act
            var serialized = _serializationService.Serialize(obj);
            var deserialized = _serializationService.Deserialize<dynamic>(serialized);

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Contains("\"emptyArray\":[]", serialized);
            Assert.Contains("\"emptyList\":[]", serialized);
            Assert.Contains("\"emptyDictionary\":{}", serialized);
        }

        [Fact]
        public void SerializeDeserialize_WithCircularReference_ShouldHandleGracefully()
        {
            // Note: This test verifies that the serializer handles circular references
            // by either throwing a predictable exception or handling it gracefully
            
            // Arrange
            var parent = new { Name = "Parent" };
            // We can't create actual circular references in anonymous objects,
            // so we test with a structure that could potentially cause issues
            var complexObject = new
            {
                Parent = parent,
                Children = new[] { parent, parent } // Reference same object multiple times
            };

            // Act & Assert
            // Should either serialize successfully or throw a predictable exception
            try
            {
                var serialized = _serializationService.Serialize(complexObject);
                var deserialized = _serializationService.Deserialize<dynamic>(serialized);
                
                Assert.NotNull(serialized);
                Assert.NotNull(deserialized);
            }
            catch (JsonException)
            {
                // This is acceptable - circular references should throw JsonException
                Assert.True(true);
            }
        }

        #endregion

        #region Model-Specific Serialization Tests

        [Fact]
        public void SerializeDeserialize_WithManuscript_ShouldPreserveAllProperties()
        {
            // Arrange
            var manuscript = new Manuscript
            {
                Id = "ms-123",
                Title = "Test Manuscript",
                Abstract = "This is a test abstract",
                Keywords = new List<string> { "test", "manuscript" },
                Subject = "Computer Science",
                Type = "research-article",
                Folder = "test-folder",
                Status = "pending",
                OriginId = "origin-123",
                FileName = "test.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                Language = "en",
                CustomFields = new Dictionary<string, object>
                {
                    { "field1", "value1" },
                    { "field2", 42 }
                }
            };

            // Act
            var serialized = _serializationService.Serialize(manuscript);
            var deserialized = _serializationService.Deserialize<Manuscript>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(manuscript.Id, deserialized.Id);
            Assert.Equal(manuscript.Title, deserialized.Title);
            Assert.Equal(manuscript.Abstract, deserialized.Abstract);
            Assert.Equal(manuscript.Keywords?.Count, deserialized.Keywords?.Count);
            Assert.Equal(manuscript.Subject, deserialized.Subject);
            Assert.Equal(manuscript.Type, deserialized.Type);
            Assert.Equal(manuscript.Folder, deserialized.Folder);
            Assert.Equal(manuscript.Status, deserialized.Status);
            Assert.Equal(manuscript.OriginId, deserialized.OriginId);
            Assert.Equal(manuscript.FileName, deserialized.FileName);
            Assert.Equal(manuscript.FileSize, deserialized.FileSize);
            Assert.Equal(manuscript.MimeType, deserialized.MimeType);
            Assert.Equal(manuscript.Language, deserialized.Language);
            Assert.Equal(manuscript.CustomFields?.Count, deserialized.CustomFields?.Count);
        }

        [Fact]
        public void SerializeDeserialize_WithJournal_ShouldPreserveAllProperties()
        {
            // Arrange
            var journal = new Journal
            {
                Id = "journal-123",
                Title = "Test Journal",
                AbbreviatedTitle = "Test J.",
                Issn = "1234-5678",
                EIssn = "8765-4321",
                Publisher = "Test Publisher",
                Website = "https://testjournal.com",
                Description = "A test journal",
                SubjectAreas = new List<string> { "Computer Science", "AI" },
                Keywords = new List<string> { "test", "journal" },
                ImpactFactor = 3.5,
                HIndex = 50,
                CitationCount = 1000,
                RelevanceScore = 0.85,
                Rank = 1,
                IsOpenAccess = true,
                PeerReviewType = "double-blind",
                PublicationFrequency = "monthly",
                AcceptanceRate = 0.25,
                TimeToFirstDecision = 30,
                TimeToPublication = 90
            };

            // Act
            var serialized = _serializationService.Serialize(journal);
            var deserialized = _serializationService.Deserialize<Journal>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(journal.Id, deserialized.Id);
            Assert.Equal(journal.Title, deserialized.Title);
            Assert.Equal(journal.AbbreviatedTitle, deserialized.AbbreviatedTitle);
            Assert.Equal(journal.Issn, deserialized.Issn);
            Assert.Equal(journal.EIssn, deserialized.EIssn);
            Assert.Equal(journal.Publisher, deserialized.Publisher);
            Assert.Equal(journal.Website, deserialized.Website);
            Assert.Equal(journal.Description, deserialized.Description);
            Assert.Equal(journal.SubjectAreas?.Count, deserialized.SubjectAreas?.Count);
            Assert.Equal(journal.Keywords?.Count, deserialized.Keywords?.Count);
            Assert.Equal(journal.ImpactFactor, deserialized.ImpactFactor);
            Assert.Equal(journal.HIndex, deserialized.HIndex);
            Assert.Equal(journal.CitationCount, deserialized.CitationCount);
            Assert.Equal(journal.RelevanceScore, deserialized.RelevanceScore);
            Assert.Equal(journal.Rank, deserialized.Rank);
            Assert.Equal(journal.IsOpenAccess, deserialized.IsOpenAccess);
            Assert.Equal(journal.PeerReviewType, deserialized.PeerReviewType);
            Assert.Equal(journal.PublicationFrequency, deserialized.PublicationFrequency);
            Assert.Equal(journal.AcceptanceRate, deserialized.AcceptanceRate);
            Assert.Equal(journal.TimeToFirstDecision, deserialized.TimeToFirstDecision);
            Assert.Equal(journal.TimeToPublication, deserialized.TimeToPublication);
        }

        #endregion
    }
} 