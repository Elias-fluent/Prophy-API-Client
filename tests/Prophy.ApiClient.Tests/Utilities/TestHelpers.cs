using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Http;

namespace Prophy.ApiClient.Tests.Utilities
{
    /// <summary>
    /// Helper methods for creating test objects and mocks.
    /// </summary>
    public static class TestHelpers
    {
        #region Constants
        public const string DefaultApiKey = "test-api-key-12345";
        public const string DefaultOrgCode = "test-org";
        public const string DefaultBaseUrl = "https://api.test.com/";
        #endregion

        #region Mock Creation
        /// <summary>
        /// Creates a mock logger for the specified type.
        /// </summary>
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a mock HTTP client wrapper.
        /// </summary>
        public static Mock<IHttpClientWrapper> CreateMockHttpClient()
        {
            return new Mock<IHttpClientWrapper>();
        }

        /// <summary>
        /// Creates a mock API key authenticator.
        /// </summary>
        public static Mock<IApiKeyAuthenticator> CreateMockAuthenticator(string? apiKey = null)
        {
            var mock = new Mock<IApiKeyAuthenticator>();
            mock.Setup(x => x.ApiKey).Returns(apiKey ?? DefaultApiKey);
            return mock;
        }
        #endregion

        #region Test Data Generation
        /// <summary>
        /// Creates test PDF bytes with a proper PDF header.
        /// </summary>
        public static byte[] CreateTestPdfBytes(int size = 1024)
        {
            var bytes = new byte[size];
            var random = new Random(42); // Fixed seed for reproducible tests
            random.NextBytes(bytes);
            
            // Add PDF header to make it look like a real PDF
            bytes[0] = 0x25; // %
            bytes[1] = 0x50; // P
            bytes[2] = 0x44; // D
            bytes[3] = 0x46; // F
            
            return bytes;
        }

        /// <summary>
        /// Creates a random string of specified length.
        /// </summary>
        public static string CreateRandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random(42);
            var result = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(result);
        }

        /// <summary>
        /// Creates a random email address.
        /// </summary>
        public static string CreateRandomEmail(string? domain = null)
        {
            domain ??= "test.com";
            return $"{CreateRandomString(8).ToLower()}@{domain}";
        }

        /// <summary>
        /// Creates a random ORCID identifier.
        /// </summary>
        public static string CreateRandomOrcid()
        {
            var random = new Random(42);
            return $"0000-0000-0000-{random.Next(1000, 9999):D4}";
        }

        /// <summary>
        /// Creates test custom field values.
        /// </summary>
        public static Dictionary<string, object> CreateCustomFieldValues()
        {
            return new Dictionary<string, object>
            {
                ["test_string_field"] = "Test Value",
                ["test_number_field"] = 42,
                ["test_date_field"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["test_single_option"] = "Option 1",
                ["test_multi_option"] = new List<string> { "Option 1", "Option 2" }
            };
        }
        #endregion

        #region HTTP Response Helpers
        /// <summary>
        /// Creates a successful HTTP response message.
        /// </summary>
        public static HttpResponseMessage CreateSuccessResponse(string content = "{}")
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
        }

        /// <summary>
        /// Creates an error HTTP response message.
        /// </summary>
        public static HttpResponseMessage CreateErrorResponse(
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.BadRequest,
            string content = "{\"error\": \"Test error\"}")
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
        }
        #endregion

        #region Assertion Helpers
        /// <summary>
        /// Verifies that a mock logger was called with the expected log level.
        /// </summary>
        public static void VerifyLoggerCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, Times? times = null)
        {
            mockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times ?? Times.AtLeastOnce());
        }

        /// <summary>
        /// Verifies that a mock HTTP client was called with the expected method and URL.
        /// </summary>
        public static void VerifyHttpClientCalled(
            Mock<IHttpClientWrapper> mockHttpClient,
            HttpMethod method,
            string expectedUrl,
            Times? times = null)
        {
            mockHttpClient.Verify(
                x => x.SendAsync(
                    It.Is<HttpRequestMessage>(req => 
                        req.Method == method && 
                        req.RequestUri != null && 
                        req.RequestUri.ToString().Contains(expectedUrl)),
                    It.IsAny<System.Threading.CancellationToken>()),
                times ?? Times.Once());
        }
        #endregion
    }
} 