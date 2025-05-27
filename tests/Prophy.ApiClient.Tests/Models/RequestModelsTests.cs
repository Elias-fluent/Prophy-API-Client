using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Models
{
    public class RequestModelsTests
    {
        private readonly IJsonSerializer _serializer;

        public RequestModelsTests()
        {
            _serializer = SerializationFactory.CreateJsonSerializer();
        }

        [Fact]
        public void ManuscriptUploadRequest_DefaultConstructor_SetsRequiredProperties()
        {
            // Arrange & Act
            var request = new ManuscriptUploadRequest();

            // Assert
            Assert.Equal(string.Empty, request.Title);
            Assert.Null(request.Abstract);
            Assert.Null(request.Authors);
            Assert.Null(request.FileContent);
            Assert.Null(request.FileName);
        }

        [Fact]
        public void ManuscriptUploadRequest_WithValidData_SerializesCorrectly()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "A Novel Approach to Machine Learning",
                Abstract = "This paper presents a novel approach to machine learning...",
                Authors = new List<string> { "Dr. Jane Smith", "Dr. John Doe" },
                Keywords = new List<string> { "machine learning", "artificial intelligence", "data science" },
                Subject = "Computer Science",
                Type = "research article",
                Folder = "AI Research",
                OriginId = "MS-2024-001",
                Language = "en",
                CustomFields = new Dictionary<string, object>
                {
                    { "priority", "high" },
                    { "funding_source", "NSF Grant #12345" }
                },
                Metadata = new Dictionary<string, object>
                {
                    { "submission_date", DateTime.UtcNow },
                    { "version", 1 }
                }
            };

            // Act
            var json = _serializer.Serialize(request);
            var deserializedRequest = _serializer.Deserialize<ManuscriptUploadRequest>(json);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.Equal(request.Title, deserializedRequest.Title);
            Assert.Equal(request.Abstract, deserializedRequest.Abstract);
            Assert.Equal(request.Authors?.Count, deserializedRequest.Authors?.Count);
            Assert.Equal(request.Keywords?.Count, deserializedRequest.Keywords?.Count);
            Assert.Equal(request.Subject, deserializedRequest.Subject);
            Assert.Equal(request.Type, deserializedRequest.Type);
            Assert.Equal(request.Folder, deserializedRequest.Folder);
            Assert.Equal(request.OriginId, deserializedRequest.OriginId);
            Assert.Equal(request.Language, deserializedRequest.Language);
            Assert.Equal(request.CustomFields?.Count, deserializedRequest.CustomFields?.Count);
            Assert.Equal(request.Metadata?.Count, deserializedRequest.Metadata?.Count);
        }

        [Fact]
        public void ManuscriptUploadRequest_FileProperties_AreJsonIgnored()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Test Manuscript",
                FileContent = new byte[] { 1, 2, 3, 4, 5 },
                FileName = "test.pdf",
                MimeType = "application/pdf"
            };

            // Act
            var json = _serializer.Serialize(request);

            // Assert
            Assert.Contains("\"title\":", json);
            Assert.DoesNotContain("\"fileContent\":", json);
            Assert.DoesNotContain("\"fileName\":", json);
            Assert.DoesNotContain("\"mimeType\":", json);
        }

        [Fact]
        public void ManuscriptUploadRequest_RequiredValidation_WorksCorrectly()
        {
            // Arrange
            var request = new ManuscriptUploadRequest(); // Title is empty

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(validationResults);
            Assert.Contains("Title", validationResults[0].MemberNames);
        }

        [Fact]
        public void JournalRecommendationRequest_WithValidData_SerializesCorrectly()
        {
            // Arrange
            var request = new JournalRecommendationRequest
            {
                ManuscriptId = "ms-123",
                Limit = 10,
                MinRelevanceScore = 0.7,
                OpenAccessOnly = true,
                MinImpactFactor = 2.0,
                MaxImpactFactor = 10.0,
                SubjectAreas = new List<string> { "Computer Science", "Artificial Intelligence" },
                Publishers = new List<string> { "IEEE", "ACM" },
                ExcludePublishers = new List<string> { "Predatory Publisher" },
                ExcludeJournals = new List<string> { "Low Quality Journal" },
                IncludeRelatedArticles = true,
                MaxRelatedArticles = 5,
                Filters = new Dictionary<string, object>
                {
                    { "peer_review_type", "double_blind" },
                    { "publication_frequency", "monthly" }
                }
            };

            // Act
            var json = _serializer.Serialize(request);
            var deserializedRequest = _serializer.Deserialize<JournalRecommendationRequest>(json);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.Equal(request.ManuscriptId, deserializedRequest.ManuscriptId);
            Assert.Equal(request.Limit, deserializedRequest.Limit);
            Assert.Equal(request.MinRelevanceScore, deserializedRequest.MinRelevanceScore);
            Assert.Equal(request.OpenAccessOnly, deserializedRequest.OpenAccessOnly);
            Assert.Equal(request.MinImpactFactor, deserializedRequest.MinImpactFactor);
            Assert.Equal(request.MaxImpactFactor, deserializedRequest.MaxImpactFactor);
            Assert.Equal(request.SubjectAreas?.Count, deserializedRequest.SubjectAreas?.Count);
            Assert.Equal(request.Publishers?.Count, deserializedRequest.Publishers?.Count);
            Assert.Equal(request.ExcludePublishers?.Count, deserializedRequest.ExcludePublishers?.Count);
            Assert.Equal(request.ExcludeJournals?.Count, deserializedRequest.ExcludeJournals?.Count);
            Assert.Equal(request.IncludeRelatedArticles, deserializedRequest.IncludeRelatedArticles);
            Assert.Equal(request.MaxRelatedArticles, deserializedRequest.MaxRelatedArticles);
            Assert.Equal(request.Filters?.Count, deserializedRequest.Filters?.Count);
        }

        [Fact]
        public void RefereeRecommendationRequest_WithValidData_SerializesCorrectly()
        {
            // Arrange
            var request = new RefereeRecommendationRequest
            {
                ManuscriptId = "ms-456",
                Limit = 15,
                MinRelevanceScore = 0.8,
                MinExpertiseScore = 0.75,
                ExcludeConflicts = true,
                MinHIndex = 10,
                MinCitationCount = 100,
                MinPublicationCount = 20,
                Countries = new List<string> { "United States", "United Kingdom", "Canada" },
                ExcludeCountries = new List<string> { "Restricted Country" },
                Institutions = new List<string> { "MIT", "Stanford", "Harvard" },
                ExcludeInstitutions = new List<string> { "Conflicted Institution" },
                ExcludeAuthors = new List<string> { "Conflicted Author" },
                ExcludeOrcids = new List<string> { "0000-0000-0000-0001" },
                ExcludeEmails = new List<string> { "conflicted@email.com" },
                RequiredExpertise = new List<string> { "Machine Learning", "Deep Learning" },
                PreferredExpertise = new List<string> { "Computer Vision", "Natural Language Processing" },
                IncludeConflictAnalysis = true,
                IncludeRelevantPublications = true,
                MaxRelevantPublications = 3
            };

            // Act
            var json = _serializer.Serialize(request);
            var deserializedRequest = _serializer.Deserialize<RefereeRecommendationRequest>(json);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.Equal(request.ManuscriptId, deserializedRequest.ManuscriptId);
            Assert.Equal(request.Limit, deserializedRequest.Limit);
            Assert.Equal(request.MinRelevanceScore, deserializedRequest.MinRelevanceScore);
            Assert.Equal(request.MinExpertiseScore, deserializedRequest.MinExpertiseScore);
            Assert.Equal(request.ExcludeConflicts, deserializedRequest.ExcludeConflicts);
            Assert.Equal(request.MinHIndex, deserializedRequest.MinHIndex);
            Assert.Equal(request.MinCitationCount, deserializedRequest.MinCitationCount);
            Assert.Equal(request.MinPublicationCount, deserializedRequest.MinPublicationCount);
            Assert.Equal(request.Countries?.Count, deserializedRequest.Countries?.Count);
            Assert.Equal(request.ExcludeCountries?.Count, deserializedRequest.ExcludeCountries?.Count);
            Assert.Equal(request.Institutions?.Count, deserializedRequest.Institutions?.Count);
            Assert.Equal(request.ExcludeInstitutions?.Count, deserializedRequest.ExcludeInstitutions?.Count);
            Assert.Equal(request.RequiredExpertise?.Count, deserializedRequest.RequiredExpertise?.Count);
            Assert.Equal(request.PreferredExpertise?.Count, deserializedRequest.PreferredExpertise?.Count);
            Assert.Equal(request.IncludeConflictAnalysis, deserializedRequest.IncludeConflictAnalysis);
            Assert.Equal(request.IncludeRelevantPublications, deserializedRequest.IncludeRelevantPublications);
            Assert.Equal(request.MaxRelevantPublications, deserializedRequest.MaxRelevantPublications);
        }

        [Fact]
        public void RequestModels_SerializeToCamelCase()
        {
            // Arrange
            var manuscriptRequest = new ManuscriptUploadRequest
            {
                Title = "Test",
                OriginId = "test-123",
                CustomFields = new Dictionary<string, object> { { "test", "value" } }
            };

            var journalRequest = new JournalRecommendationRequest
            {
                ManuscriptId = "ms-123",
                MinRelevanceScore = 0.5,
                OpenAccessOnly = true,
                SubjectAreas = new List<string> { "CS" }
            };

            var refereeRequest = new RefereeRecommendationRequest
            {
                ManuscriptId = "ms-456",
                MinExpertiseScore = 0.7,
                ExcludeConflicts = true,
                RequiredExpertise = new List<string> { "AI" }
            };

            // Act
            var manuscriptJson = _serializer.Serialize(manuscriptRequest);
            var journalJson = _serializer.Serialize(journalRequest);
            var refereeJson = _serializer.Serialize(refereeRequest);

            // Assert - Manuscript Request
            Assert.Contains("\"origin_id\":", manuscriptJson);
            Assert.Contains("\"customFields\":", manuscriptJson);
            Assert.DoesNotContain("\"OriginId\":", manuscriptJson);

            // Assert - Journal Request
            Assert.Contains("\"manuscriptId\":", journalJson);
            Assert.Contains("\"minRelevanceScore\":", journalJson);
            Assert.Contains("\"openAccessOnly\":", journalJson);
            Assert.Contains("\"subjectAreas\":", journalJson);

            // Assert - Referee Request
            Assert.Contains("\"manuscriptId\":", refereeJson);
            Assert.Contains("\"minExpertiseScore\":", refereeJson);
            Assert.Contains("\"excludeConflicts\":", refereeJson);
            Assert.Contains("\"requiredExpertise\":", refereeJson);
        }

        [Fact]
        public void RequestModels_RequiredValidation_WorksCorrectly()
        {
            // Test Journal Recommendation Request
            var journalRequest = new JournalRecommendationRequest(); // ManuscriptId is empty
            var journalValidationResults = new List<ValidationResult>();
            var journalValidationContext = new ValidationContext(journalRequest);
            var journalIsValid = Validator.TryValidateObject(journalRequest, journalValidationContext, journalValidationResults, true);

            Assert.False(journalIsValid);
            Assert.Single(journalValidationResults);
            Assert.Contains("ManuscriptId", journalValidationResults[0].MemberNames);

            // Test Referee Recommendation Request
            var refereeRequest = new RefereeRecommendationRequest(); // ManuscriptId is empty
            var refereeValidationResults = new List<ValidationResult>();
            var refereeValidationContext = new ValidationContext(refereeRequest);
            var refereeIsValid = Validator.TryValidateObject(refereeRequest, refereeValidationContext, refereeValidationResults, true);

            Assert.False(refereeIsValid);
            Assert.Single(refereeValidationResults);
            Assert.Contains("ManuscriptId", refereeValidationResults[0].MemberNames);
        }
    }
} 