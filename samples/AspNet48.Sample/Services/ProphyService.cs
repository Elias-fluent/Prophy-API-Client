using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AspNet48.Sample.Models;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace AspNet48.Sample.Services
{
    /// <summary>
    /// Simplified service for Prophy API operations in ASP.NET Framework 4.8.
    /// This version focuses on core functionality without resilience features.
    /// </summary>
    public class ProphyService
    {
        private readonly ILogger<ProphyService> _logger;

        /// <summary>
        /// Initializes a new instance of the ProphyService class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ProphyService(ILogger<ProphyService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the ProphyService class with additional parameters for DI compatibility.
        /// </summary>
        /// <param name="apiKey">The API key (not used, gets from config).</param>
        /// <param name="organizationCode">The organization code (not used, gets from config).</param>
        /// <param name="logger">The logger instance.</param>
        public ProphyService(string apiKey, string organizationCode, ILogger<ProphyService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a configured Prophy API client instance.
        /// </summary>
        /// <returns>A configured ProphyApiClient instance.</returns>
        private ProphyApiClient CreateClient()
        {
            var apiKey = ConfigurationManager.AppSettings["Prophy:ApiKey"];
            var organizationCode = ConfigurationManager.AppSettings["Prophy:OrganizationCode"];
            var baseUrl = ConfigurationManager.AppSettings["Prophy:BaseUrl"] ?? "https://www.prophy.ai/api/";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Prophy API key is not configured. Please set 'Prophy:ApiKey' in appSettings.");

            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new InvalidOperationException("Prophy organization code is not configured. Please set 'Prophy:OrganizationCode' in appSettings.");

            _logger.LogDebug("Creating Prophy API client for organization: {OrganizationCode}", organizationCode);

            // Create a simple console logger factory for the client
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            
            var clientLogger = loggerFactory.CreateLogger<ProphyApiClient>();

            return new ProphyApiClient(apiKey, organizationCode, baseUrl, clientLogger);
        }

        /// <summary>
        /// Checks if the Prophy API is healthy and accessible.
        /// </summary>
        /// <returns>True if the API is healthy, false otherwise.</returns>
        public async Task<bool> IsHealthyAsync()
        {
            return await TestConnectionAsync();
        }

        /// <summary>
        /// Gets the list of author groups for the organization.
        /// </summary>
        /// <returns>The list of author groups.</returns>
        public async Task<AuthorGroupListResponse> GetAuthorGroupsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving author groups");

                using (var client = CreateClient())
                {
                    var response = await client.AuthorGroups.GetAllAsync();
                    _logger.LogDebug("Retrieved {Count} author groups", response.Data?.Count ?? 0);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get author groups");
                // Return empty response on error
                return new AuthorGroupListResponse { Data = new List<Prophy.ApiClient.Models.Entities.AuthorGroup>() };
            }
        }

        /// <summary>
        /// Gets the custom fields defined for the organization.
        /// </summary>
        /// <returns>The custom fields definitions.</returns>
        public async Task<CustomFieldDefinitionsResponse> GetCustomFieldsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving custom fields");

                using (var client = CreateClient())
                {
                    var response = await client.CustomFields.GetDefinitionsAsync();
                    _logger.LogDebug("Retrieved {Count} custom field definitions", response.CustomFields?.Count ?? 0);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get custom fields");
                // Return empty response on error
                return new CustomFieldDefinitionsResponse { CustomFields = new List<Prophy.ApiClient.Models.Entities.CustomField>() };
            }
        }

        /// <summary>
        /// Generates a JWT login URL for the specified manuscript.
        /// </summary>
        /// <param name="manuscriptId">The manuscript ID.</param>
        /// <param name="userEmail">The user email.</param>
        /// <param name="folder">The folder name.</param>
        /// <returns>The JWT login URL.</returns>
        public async Task<string> GenerateJwtLoginUrlAsync(string manuscriptId, string userEmail, string folder = null)
        {
            try
            {
                _logger.LogInformation("Generating JWT login URL for manuscript: {ManuscriptId}", manuscriptId);

                // Create JWT token generator manually since Authentication module is not exposed
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                });
                
                var jwtLogger = loggerFactory.CreateLogger<JwtTokenGenerator>();
                var jwtGenerator = new JwtTokenGenerator(jwtLogger);

                var claims = new JwtLoginClaims
                {
                    Subject = ConfigurationManager.AppSettings["Prophy:OrganizationCode"] ?? "Demo Organization",
                    Organization = ConfigurationManager.AppSettings["Prophy:OrganizationCode"] ?? "demo-org",
                    Email = userEmail,
                    Folder = folder ?? "demo-folder",
                    OriginId = manuscriptId
                };

                var jwtSecret = ConfigurationManager.AppSettings["Prophy:JwtSecret"] ?? "demo-secret-key-for-testing";
                var loginUrl = jwtGenerator.GenerateLoginUrl(claims, jwtSecret);
                
                _logger.LogDebug("Generated JWT login URL for manuscript: {ManuscriptId}", manuscriptId);
                return loginUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate JWT login URL for manuscript: {ManuscriptId}", manuscriptId);
                throw;
            }
        }

        /// <summary>
        /// Gets the manuscript analysis for the specified manuscript.
        /// </summary>
        /// <param name="manuscriptId">The manuscript ID.</param>
        /// <returns>The manuscript analysis response.</returns>
        public async Task<ManuscriptUploadResponse> GetManuscriptAnalysisAsync(string manuscriptId)
        {
            return await GetManuscriptStatusAsync(manuscriptId);
        }

        /// <summary>
        /// Gets the referee candidates for the specified manuscript.
        /// </summary>
        /// <param name="manuscriptId">The manuscript ID.</param>
        /// <returns>The referee candidates response.</returns>
        public async Task<RefereeRecommendationResponse> GetRefereeCandidatesAsync(string manuscriptId)
        {
            try
            {
                _logger.LogInformation("Retrieving referee candidates for manuscript: {ManuscriptId}", manuscriptId);

                // Get the manuscript details first
                var manuscriptResponse = await GetManuscriptStatusAsync(manuscriptId);
                
                // Return the referee candidates from the manuscript response
                var response = new RefereeRecommendationResponse
                {
                    ManuscriptId = manuscriptResponse.ManuscriptIdString,
                    Recommendations = manuscriptResponse.Candidates ?? new List<Prophy.ApiClient.Models.Entities.RefereeCandidate>()
                };

                _logger.LogDebug("Retrieved {Count} referee candidates for manuscript: {ManuscriptId}", 
                    response.Recommendations.Count, manuscriptId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get referee candidates for manuscript: {ManuscriptId}", manuscriptId);
                // Return empty response on error
                return new RefereeRecommendationResponse 
                { 
                    ManuscriptId = manuscriptId,
                    Recommendations = new List<Prophy.ApiClient.Models.Entities.RefereeCandidate>() 
                };
            }
        }

        /// <summary>
        /// Gets the journal recommendations for the specified manuscript.
        /// </summary>
        /// <param name="manuscriptId">The manuscript ID.</param>
        /// <returns>The journal recommendations response.</returns>
        public async Task<JournalRecommendationResponse> GetJournalRecommendationsAsync(string manuscriptId)
        {
            try
            {
                _logger.LogInformation("Retrieving journal recommendations for manuscript: {ManuscriptId}", manuscriptId);

                using (var client = CreateClient())
                {
                    var response = await client.Journals.GetRecommendationsAsync(manuscriptId, default);
                    _logger.LogDebug("Retrieved {Count} journal recommendations for manuscript: {ManuscriptId}", 
                        response.Recommendations?.Count ?? 0, manuscriptId);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get journal recommendations for manuscript: {ManuscriptId}", manuscriptId);
                // Return empty response on error
                return new JournalRecommendationResponse 
                { 
                    ManuscriptId = manuscriptId,
                    Recommendations = new List<Prophy.ApiClient.Models.Entities.Journal>() 
                };
            }
        }

        /// <summary>
        /// Uploads a manuscript to Prophy and returns referee candidates.
        /// </summary>
        /// <param name="viewModel">The manuscript upload view model containing file and metadata.</param>
        /// <returns>The upload response with referee candidates.</returns>
        public async Task<ManuscriptUploadResponse> UploadManuscriptAsync(ManuscriptUploadViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            try
            {
                _logger.LogInformation("Starting manuscript upload for title: {Title}", viewModel.Title);

                // Convert authors list to string list for compatibility
                var authorNames = new List<string>();
                if (viewModel.Authors != null)
                {
                    authorNames.AddRange(viewModel.Authors.Select(a => a.Name ?? "Unknown Author"));
                }

                // Create the upload request
                var request = new ManuscriptUploadRequest
                {
                    Title = viewModel.Title,
                    Abstract = viewModel.Abstract,
                    Subject = viewModel.Subject,
                    Language = viewModel.Language ?? "English",
                    AuthorNames = authorNames,
                    OriginId = viewModel.OriginId ?? Guid.NewGuid().ToString(),
                };

                // Convert uploaded file to byte array
                if (viewModel.ManuscriptFile != null && viewModel.ManuscriptFile.ContentLength > 0)
                {
                    using (var stream = viewModel.ManuscriptFile.InputStream)
                    {
                        request.FileContent = ReadStreamToBytes(stream);
                        request.FileName = viewModel.ManuscriptFile.FileName;
                        request.MimeType = viewModel.ManuscriptFile.ContentType;
                    }
                }
                else
                {
                    throw new ArgumentException("Manuscript file is required");
                }

                // Upload the manuscript using the client
                using (var client = CreateClient())
                {
                    var response = await client.Manuscripts.UploadAsync(request);

                    _logger.LogInformation("Manuscript upload completed successfully for title: {Title}, ManuscriptId: {ManuscriptId}", 
                        viewModel.Title, response.ManuscriptIdString);

                    return response;
                }
            }
            catch (ProphyApiException ex)
            {
                _logger.LogError(ex, "Failed to upload manuscript: {ErrorCode} - {Message}", 
                    ex.ErrorCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during manuscript upload");
                throw new ProphyApiException("An unexpected error occurred during manuscript upload", "UPLOAD_ERROR", ex);
            }
        }

        /// <summary>
        /// Reads a stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The stream contents as a byte array.</returns>
        private static byte[] ReadStreamToBytes(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Gets the status of a previously uploaded manuscript.
        /// </summary>
        /// <param name="manuscriptId">The manuscript ID to check.</param>
        /// <returns>The manuscript status response.</returns>
        public async Task<ManuscriptUploadResponse> GetManuscriptStatusAsync(string manuscriptId)
        {
            if (string.IsNullOrWhiteSpace(manuscriptId))
                throw new ArgumentException("Manuscript ID cannot be null or empty.", nameof(manuscriptId));

            try
            {
                _logger.LogInformation("Retrieving status for manuscript: {ManuscriptId}", manuscriptId);

                using (var client = CreateClient())
                {
                    var response = await client.Manuscripts.GetStatusAsync(manuscriptId);

                    _logger.LogDebug("Retrieved status for manuscript {ManuscriptId}: {Status}", 
                        manuscriptId, response.ProcessingStatus ?? "Unknown");

                    return response;
                }
            }
            catch (ProphyApiException ex)
            {
                _logger.LogError(ex, "Failed to get manuscript status: {ErrorCode} - {Message}", 
                    ex.ErrorCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during status retrieval for manuscript: {ManuscriptId}", manuscriptId);
                throw new ProphyApiException($"An unexpected error occurred while retrieving status for manuscript {manuscriptId}", "STATUS_ERROR", ex);
            }
        }

        /// <summary>
        /// Tests the connection to the Prophy API.
        /// </summary>
        /// <returns>True if the connection is successful, false otherwise.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing connection to Prophy API");

                // Try to create a client to validate configuration
                using (var client = CreateClient())
                {
                    // For a real health check, we could try to get custom fields as a lightweight test
                    await client.CustomFields.GetDefinitionsAsync();
                }
                
                _logger.LogInformation("Prophy API connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prophy API connection test failed");
                return false;
            }
        }
    }
} 