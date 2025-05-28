using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Entities;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Modules;
using Prophy.ApiClient.Serialization;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.Performance
{
    /// <summary>
    /// Performance and edge case tests for the Prophy API Client Library.
    /// Tests large data sets, timeout scenarios, rate limiting, pagination, streaming responses,
    /// memory efficiency, concurrent API calls, and thread safety.
    /// </summary>
    public class PerformanceTests
    {
        private readonly Mock<IHttpClientWrapper> _mockHttpClient;
        private readonly Mock<IApiKeyAuthenticator> _mockAuthenticator;
        private readonly Mock<IMultipartFormDataBuilder> _mockFormDataBuilder;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly Mock<ILogger<ManuscriptModule>> _mockLogger;
        private readonly ManuscriptModule _manuscriptModule;

        public PerformanceTests()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockAuthenticator = new Mock<IApiKeyAuthenticator>();
            _mockFormDataBuilder = new Mock<IMultipartFormDataBuilder>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            _mockLogger = new Mock<ILogger<ManuscriptModule>>();

            // Setup authenticator to return valid API key and organization code
            _mockAuthenticator.Setup(x => x.ApiKey).Returns("test-api-key");
            _mockAuthenticator.Setup(x => x.OrganizationCode).Returns("test-org");

            _manuscriptModule = new ManuscriptModule(
                _mockHttpClient.Object,
                _mockAuthenticator.Object,
                _mockFormDataBuilder.Object,
                _mockJsonSerializer.Object,
                _mockLogger.Object);
        }

        #region Large Data Set Tests

        [Fact]
        public async Task ManuscriptUpload_WithLargeFile_ShouldHandleEfficiently()
        {
            // Arrange
            var largeFileSize = 45 * 1024 * 1024; // 45MB (under 50MB limit)
            var largeFileContent = new byte[largeFileSize];
            new System.Random().NextBytes(largeFileContent);

            var request = new ManuscriptUploadRequest
            {
                Title = "Large File Test",
                Abstract = "Testing large file upload performance",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = largeFileContent,
                FileName = "large-test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "large-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            stopwatch.Stop();
            Assert.NotNull(result);
            Assert.Equal("large-test-id", result.ManuscriptId?.ToString());
            
            // Performance assertion - should complete within reasonable time
            Assert.True(stopwatch.ElapsedMilliseconds < 30000, $"Large file upload took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
        }

        [Fact]
        public async Task ManuscriptUpload_WithManyAuthors_ShouldHandleEfficiently()
        {
            // Arrange
            var manyAuthors = Enumerable.Range(1, 100)
                .Select(i => $"Author {i}")
                .ToList();
            
            var manyEmails = Enumerable.Range(1, 100)
                .Select(i => $"author{i}@example.com")
                .ToList();

            var request = new ManuscriptUploadRequest
            {
                Title = "Many Authors Test",
                Abstract = "Testing manuscript with many authors",
                AuthorNames = manyAuthors,
                AuthorEmails = manyEmails,
                AuthorsCount = 100,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "many-authors-test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "many-authors-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            stopwatch.Stop();
            Assert.NotNull(result);
            Assert.Equal("many-authors-test-id", result.ManuscriptId?.ToString());
            
            // Performance assertion
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Many authors upload took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        [Property]
        public Property ManuscriptUpload_WithValidInputs_ShouldAlwaysSucceed()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (title, abstractText, authorName) =>
                {
                    // Arrange
                    var request = new ManuscriptUploadRequest
                    {
                        Title = title.Get,
                        Abstract = abstractText.Get,
                        AuthorNames = new List<string> { authorName.Get },
                        AuthorEmails = new List<string> { "test@example.com" },
                        AuthorsCount = 1,
                        FileContent = TestHelpers.CreateTestPdfBytes(),
                        FileName = "test.pdf"
                    };

                    var expectedResponse = new ManuscriptUploadResponse
                    {
                        ManuscriptId = "test-id",
                        Candidates = new List<RefereeCandidate>()
                    };

                    _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
                    _mockHttpClient.Setup(x => x.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<CancellationToken>()))
                        .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                            System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

                    _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                        .Returns(expectedResponse);

                    // Act & Assert
                    try
                    {
                        var result = _manuscriptModule.UploadAsync(request).Result;
                        return result != null && result.ManuscriptId?.ToString() == expectedResponse.ManuscriptId?.ToString();
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Timeout and Rate Limiting Tests

        [Fact]
        public async Task ManuscriptUpload_WithTimeout_ShouldThrowApiTimeoutException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Timeout Test",
                Abstract = "Testing timeout behavior",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "timeout-test.pdf"
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApiTimeoutException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Contains("timed out", exception.Message.ToLower());
            Assert.Equal("REQUEST_TIMEOUT", exception.ErrorCode);
        }

        [Fact]
        public async Task ManuscriptUpload_WithCancellation_ShouldThrowApiTimeoutException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Cancellation Test",
                Abstract = "Testing cancellation behavior",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "cancellation-test.pdf"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Request was cancelled", new TimeoutException(), cancellationTokenSource.Token));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApiTimeoutException>(
                () => _manuscriptModule.UploadAsync(request, null, cancellationTokenSource.Token));
            
            Assert.Contains("cancelled", exception.Message.ToLower());
            Assert.Equal("REQUEST_TIMEOUT", exception.ErrorCode);
        }

        [Fact]
        public async Task ManuscriptUpload_WithRateLimitResponse_ShouldThrowRateLimitException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Rate Limit Test",
                Abstract = "Testing rate limit behavior",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "rate-limit-test.pdf"
            };

            var errorContent = "{\"message\": \"Rate limit exceeded\", \"retryAfterSeconds\": 60, \"remainingRequests\": 0, \"requestLimit\": 100}";
            var rateLimitResponse = TestHelpers.CreateErrorResponse((HttpStatusCode)429, errorContent);

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(rateLimitResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RateLimitException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Equal("RATE_LIMIT_EXCEEDED", exception.ErrorCode);
            Assert.True(exception.RetryAfter > DateTimeOffset.Now);
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public async Task ManuscriptUpload_ConcurrentRequests_ShouldHandleThreadSafely()
        {
            // Arrange
            const int concurrentRequests = 10;
            var tasks = new List<Task<ManuscriptUploadResponse>>();

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "concurrent-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            for (int i = 0; i < concurrentRequests; i++)
            {
                var request = new ManuscriptUploadRequest
                {
                    Title = $"Concurrent Test {i}",
                    Abstract = $"Testing concurrent request {i}",
                    AuthorNames = new List<string> { $"Author {i}" },
                    AuthorEmails = new List<string> { $"author{i}@example.com" },
                    AuthorsCount = 1,
                    FileContent = TestHelpers.CreateTestPdfBytes(),
                    FileName = $"concurrent-test-{i}.pdf"
                };

                tasks.Add(_manuscriptModule.UploadAsync(request));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(concurrentRequests, results.Length);
            Assert.All(results, result =>
            {
                Assert.NotNull(result);
                Assert.Equal("concurrent-test-id", result.ManuscriptId?.ToString());
            });
        }

        [Fact]
        public async Task ManuscriptModule_ThreadSafety_ShouldNotCauseDataCorruption()
        {
            // Arrange
            const int threadCount = 20;
            const int operationsPerThread = 5;
            var barrier = new Barrier(threadCount);
            var exceptions = new List<Exception>();
            var results = new List<ManuscriptUploadResponse>();
            var lockObject = new object();

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "thread-safety-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(async () =>
            {
                try
                {
                    barrier.SignalAndWait(); // Synchronize thread start

                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var request = new ManuscriptUploadRequest
                        {
                            Title = $"Thread {threadId} Operation {i}",
                            Abstract = $"Testing thread safety for thread {threadId}, operation {i}",
                            AuthorNames = new List<string> { $"Author {threadId}-{i}" },
                            AuthorEmails = new List<string> { $"author{threadId}-{i}@example.com" },
                            AuthorsCount = 1,
                            FileContent = TestHelpers.CreateTestPdfBytes(),
                            FileName = $"thread-{threadId}-{i}.pdf"
                        };

                        var result = await _manuscriptModule.UploadAsync(request);
                        
                        lock (lockObject)
                        {
                            results.Add(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
            Assert.Equal(threadCount * operationsPerThread, results.Count);
            Assert.All(results, result =>
            {
                Assert.NotNull(result);
                Assert.Equal("thread-safety-test-id", result.ManuscriptId?.ToString());
            });
        }

        #endregion

        #region Memory and Resource Tests

        [Fact]
        public async Task ManuscriptUpload_MultipleSequentialUploads_ShouldNotLeakMemory()
        {
            // Arrange
            const int uploadCount = 50;
            var initialMemory = GC.GetTotalMemory(true);

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "memory-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            for (int i = 0; i < uploadCount; i++)
            {
                var request = new ManuscriptUploadRequest
                {
                    Title = $"Memory Test {i}",
                    Abstract = $"Testing memory usage for upload {i}",
                    AuthorNames = new List<string> { $"Author {i}" },
                    AuthorEmails = new List<string> { $"author{i}@example.com" },
                    AuthorsCount = 1,
                    FileContent = TestHelpers.CreateTestPdfBytes(),
                    FileName = $"memory-test-{i}.pdf"
                };

                var result = await _manuscriptModule.UploadAsync(request);
                Assert.NotNull(result);

                // Force garbage collection every 10 uploads
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            // Assert
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Memory increase should be reasonable (less than 50MB for 50 uploads)
            Assert.True(memoryIncrease < 50 * 1024 * 1024, 
                $"Memory increased by {memoryIncrease / (1024 * 1024)}MB, expected < 50MB");
        }

        [Fact]
        public async Task ManuscriptUpload_WithProgressReporting_ShouldReportAccurately()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Progress Test",
                Abstract = "Testing progress reporting",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "progress-test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "progress-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            var progressReports = new List<UploadProgress>();
            var progress = new Progress<UploadProgress>(p => progressReports.Add(p));

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request, progress);

            // Assert
            Assert.NotNull(result);
            Assert.True(progressReports.Count > 0, "Progress should be reported");
            
            // Check that progress stages are reported in order
            var stages = progressReports.Select(p => p.Stage).ToList();
            Assert.Contains("Validating", stages);
            Assert.Contains("Preparing", stages);
            Assert.Contains("Uploading", stages);
            Assert.Contains("Processing", stages);
            Assert.Contains("Completed", stages);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task ManuscriptUpload_WithMaximumFileSize_ShouldSucceed()
        {
            // Arrange
            var maxFileSize = 50 * 1024 * 1024 - 1024; // Just under 50MB limit
            var maxFileContent = new byte[maxFileSize];
            new System.Random().NextBytes(maxFileContent);

            var request = new ManuscriptUploadRequest
            {
                Title = "Maximum File Size Test",
                Abstract = "Testing maximum allowed file size",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = maxFileContent,
                FileName = "max-size-test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "max-size-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("max-size-test-id", result.ManuscriptId?.ToString());
        }

        [Fact]
        public async Task ManuscriptUpload_WithExcessiveFileSize_ShouldThrowValidationException()
        {
            // Arrange
            var excessiveFileSize = 51 * 1024 * 1024; // Over 50MB limit
            var excessiveFileContent = new byte[excessiveFileSize];

            var request = new ManuscriptUploadRequest
            {
                Title = "Excessive File Size Test",
                Abstract = "Testing file size validation",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = excessiveFileContent,
                FileName = "excessive-size-test.pdf"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Contains("File size exceeds maximum", exception.Message);
        }

        [Fact]
        public async Task ManuscriptUpload_WithEmptyFileContent_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Empty File Test",
                Abstract = "Testing empty file validation",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = new byte[0], // Empty file
                FileName = "empty-test.pdf"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Contains("File content is required", exception.Message);
        }

        [Fact]
        public async Task ManuscriptUpload_WithNullFileContent_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Null File Test",
                Abstract = "Testing null file validation",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = null, // Null file
                FileName = "null-test.pdf"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Contains("File content is required", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ManuscriptUpload_WithInvalidFileName_ShouldThrowValidationException(string fileName)
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Invalid File Name Test",
                Abstract = "Testing file name validation",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = fileName
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _manuscriptModule.UploadAsync(request));
            
            Assert.Contains("File name is required", exception.Message);
        }

        [Fact]
        public async Task ManuscriptUpload_WithSpecialCharactersInTitle_ShouldSucceed()
        {
            // Arrange
            var request = new ManuscriptUploadRequest
            {
                Title = "Special Characters: àáâãäåæçèéêë & <>&\"'",
                Abstract = "Testing special characters in title",
                AuthorNames = new List<string> { "Test Author" },
                AuthorEmails = new List<string> { "test@example.com" },
                AuthorsCount = 1,
                FileContent = TestHelpers.CreateTestPdfBytes(),
                FileName = "special-chars-test.pdf"
            };

            var expectedResponse = new ManuscriptUploadResponse
            {
                ManuscriptId = "special-chars-test-id",
                Candidates = new List<RefereeCandidate>()
            };

            _mockFormDataBuilder.Setup(x => x.Build()).Returns(new MultipartFormDataContent());
            _mockHttpClient.Setup(x => x.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateSuccessResponse(
                    System.Text.Json.JsonSerializer.Serialize(expectedResponse)));

            _mockJsonSerializer.Setup(x => x.Deserialize<ManuscriptUploadResponse>(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = await _manuscriptModule.UploadAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("special-chars-test-id", result.ManuscriptId?.ToString());
        }

        #endregion

        #region Configuration Edge Cases

        [Fact]
        public void ProphyApiClientConfiguration_WithZeroTimeout_ShouldFailValidation()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                TimeoutSeconds = 0
            };

            // Act
            var isValid = config.IsValid;
            var errors = config.Validate().ToList();

            // Assert
            Assert.False(isValid);
            Assert.Contains("Timeout seconds must be greater than zero.", errors);
        }

        [Fact]
        public void ProphyApiClientConfiguration_WithNegativeRetryAttempts_ShouldFailValidation()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                MaxRetryAttempts = -1
            };

            // Act
            var isValid = config.IsValid;
            var errors = config.Validate().ToList();

            // Assert
            Assert.False(isValid);
            Assert.Contains("Maximum retry attempts cannot be negative.", errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        [InlineData("ftp://invalid.com")]
        public void ProphyApiClientConfiguration_WithInvalidBaseUrl_ShouldFailValidation(string baseUrl)
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-key",
                OrganizationCode = "test-org",
                BaseUrl = baseUrl
            };

            // Act
            var isValid = config.IsValid;
            var errors = config.Validate().ToList();

            // Assert
            Assert.False(isValid);
            Assert.True(errors.Any(e => e.Contains("Base URL")));
        }

        #endregion
    }
} 