using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Authentication;
using Prophy.ApiClient.Exceptions;
using Prophy.ApiClient.Http;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;
using Prophy.ApiClient.Serialization;
using ValidationException = Prophy.ApiClient.Exceptions.ValidationException;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Implementation of IManuscriptModule for manuscript operations in the Prophy API.
    /// </summary>
    public class ManuscriptModule : IManuscriptModule
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IApiKeyAuthenticator _authenticator;
        private readonly IMultipartFormDataBuilder _formDataBuilder;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<ManuscriptModule> _logger;

        /// <summary>
        /// Initializes a new instance of the ManuscriptModule class.
        /// </summary>
        /// <param name="httpClient">The HTTP client wrapper for making requests.</param>
        /// <param name="authenticator">The authenticator for adding authentication headers.</param>
        /// <param name="formDataBuilder">The builder for creating multipart form data.</param>
        /// <param name="jsonSerializer">The JSON serializer for response deserialization.</param>
        /// <param name="logger">The logger for recording operations.</param>
        public ManuscriptModule(
            IHttpClientWrapper httpClient,
            IApiKeyAuthenticator authenticator,
            IMultipartFormDataBuilder formDataBuilder,
            IJsonSerializer jsonSerializer,
            ILogger<ManuscriptModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _formDataBuilder = formDataBuilder ?? throw new ArgumentNullException(nameof(formDataBuilder));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ManuscriptUploadResponse> UploadAsync(ManuscriptUploadRequest request, IProgress<UploadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                _logger.LogInformation("Starting manuscript upload for title: {Title}", request.Title);

                // Report initial progress
                progress?.Report(new UploadProgress
                {
                    Stage = "Validating",
                    Message = "Validating request data"
                });

                // Validate the request
                ValidateRequest(request);

                // Report progress
                progress?.Report(new UploadProgress
                {
                    Stage = "Preparing",
                    Message = "Preparing multipart form data"
                });

                // Build multipart form data
                var formData = await BuildFormDataAsync(request, progress, cancellationToken);

                // Report progress
                var totalBytes = request.FileContent?.Length ?? 0;
                progress?.Report(new UploadProgress
                {
                    TotalBytes = totalBytes,
                    UploadedBytes = 0,
                    Stage = "Uploading",
                    Message = "Uploading manuscript to server"
                });

                // Create HTTP request
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "external/proposal/")
                {
                    Content = formData
                };

                // Note: Authentication is handled via form fields (api_key, organization) not headers

                _logger.LogDebug("Sending manuscript upload request to {Endpoint}", httpRequest.RequestUri);

                // Send request with progress tracking
                var response = await SendWithProgressAsync(httpRequest, progress, totalBytes, cancellationToken);

                // Report progress
                progress?.Report(new UploadProgress
                {
                    TotalBytes = totalBytes,
                    UploadedBytes = totalBytes,
                    Stage = "Processing",
                    Message = "Processing server response"
                });

                // Handle response
                await ErrorHandler.HandleResponseAsync(response, _logger);

                var responseContent = await response.Content.ReadAsStringAsync();
                var uploadResponse = _jsonSerializer.Deserialize<ManuscriptUploadResponse>(responseContent);

                _logger.LogInformation("Manuscript upload completed successfully. Manuscript ID: {ManuscriptId}", 
                    uploadResponse?.Manuscript?.Id ?? "Unknown");

                // Report completion
                progress?.Report(new UploadProgress
                {
                    TotalBytes = totalBytes,
                    UploadedBytes = totalBytes,
                    Stage = "Completed",
                    Message = "Upload completed successfully"
                });

                return uploadResponse ?? new ManuscriptUploadResponse { Message = "Invalid response format" };
            }
            catch (ValidationException)
            {
                _logger.LogError("Manuscript upload failed due to validation errors");
                throw;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Manuscript upload was cancelled");
                throw new ApiTimeoutException("Upload was cancelled", TimeSpan.Zero, ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Manuscript upload timed out");
                throw ErrorHandler.HandleTimeoutException(ex, TimeSpan.FromMinutes(5), _logger);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error during manuscript upload");
                throw new ProphyApiException("An unexpected error occurred during manuscript upload", "UPLOAD_ERROR", ex);
            }
        }

        /// <inheritdoc />
        public async Task<ManuscriptUploadResponse> GetStatusAsync(string manuscriptId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(manuscriptId))
                throw new ArgumentException("Manuscript ID cannot be null or empty.", nameof(manuscriptId));

            try
            {
                _logger.LogInformation("Retrieving status for manuscript: {ManuscriptId}", manuscriptId);

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"external/proposal/{manuscriptId}/status/");
                _authenticator.AuthenticateRequest(httpRequest);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                await ErrorHandler.HandleResponseAsync(response, _logger);

                var responseContent = await response.Content.ReadAsStringAsync();
                var statusResponse = _jsonSerializer.Deserialize<ManuscriptUploadResponse>(responseContent);

                _logger.LogDebug("Retrieved status for manuscript {ManuscriptId}: {Status}", 
                    manuscriptId, statusResponse?.ProcessingStatus ?? "Unknown");

                return statusResponse ?? new ManuscriptUploadResponse { Message = "Invalid response format" };
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Status retrieval was cancelled for manuscript: {ManuscriptId}", manuscriptId);
                throw new ApiTimeoutException("Status retrieval was cancelled", TimeSpan.Zero, ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Status retrieval timed out for manuscript: {ManuscriptId}", manuscriptId);
                throw ErrorHandler.HandleTimeoutException(ex, TimeSpan.FromMinutes(1), _logger);
            }
            catch (Exception ex) when (!(ex is ProphyApiException))
            {
                _logger.LogError(ex, "Unexpected error during status retrieval for manuscript: {ManuscriptId}", manuscriptId);
                throw new ProphyApiException($"An unexpected error occurred while retrieving status for manuscript {manuscriptId}", "STATUS_ERROR", ex);
            }
        }

        private void ValidateRequest(ManuscriptUploadRequest request)
        {
            var validationResults = new System.Collections.Generic.List<ValidationResult>();
            var validationContext = new ValidationContext(request);

            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
                throw new ValidationException("Request validation failed", errors);
            }

            // Additional validation for file content
            if (request.FileContent == null || request.FileContent.Length == 0)
            {
                throw new ValidationException("File content is required for manuscript upload", new[] { "FileContent is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                throw new ValidationException("File name is required for manuscript upload", new[] { "FileName is required" });
            }

            // Validate file size (e.g., max 50MB)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (request.FileContent.Length > maxFileSize)
            {
                throw new ValidationException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB", 
                    new[] { $"File size must be less than {maxFileSize / (1024 * 1024)}MB" });
            }

            _logger.LogDebug("Request validation passed for manuscript: {Title}", request.Title);
        }

        private async Task<MultipartFormDataContent> BuildFormDataAsync(ManuscriptUploadRequest request, IProgress<UploadProgress>? progress, CancellationToken cancellationToken)
        {
            _formDataBuilder.Clear();

            // Add authentication fields as form data (matching working example and API docs)
            _formDataBuilder.AddField("api_key", _authenticator.ApiKey ?? throw new InvalidOperationException("API key is required"));
            _formDataBuilder.AddField("organization", _authenticator.OrganizationCode ?? throw new InvalidOperationException("Organization code is required"));

            // Add required fields using the exact field names from API documentation
            _formDataBuilder.AddField("folder", request.Journal ?? request.Title); // API docs use "folder"
            _formDataBuilder.AddField("origin_id", request.OriginId ?? $"manuscript-{DateTime.Now.Ticks}");
            _formDataBuilder.AddField("title", request.Title);
            _formDataBuilder.AddField("abstract", request.Abstract ?? "");
            _formDataBuilder.AddField("authors_count", request.AuthorsCount.ToString());

            // Add author information using the exact format from working example
            if (request.AuthorNames?.Any() == true)
            {
                for (int i = 0; i < request.AuthorNames.Count; i++)
                {
                    _formDataBuilder.AddField($"author{i + 1}_name", request.AuthorNames[i]);
                }
            }

            if (request.AuthorEmails?.Any() == true)
            {
                for (int i = 0; i < request.AuthorEmails.Count; i++)
                {
                    _formDataBuilder.AddField($"author{i + 1}_email", request.AuthorEmails[i]);
                }
            }

            // Add optional filtering parameters if provided
            if (request.MinHIndex.HasValue)
                _formDataBuilder.AddField("min_h_index", request.MinHIndex.Value.ToString());
            
            if (request.MaxHIndex.HasValue)
                _formDataBuilder.AddField("max_h_index", request.MaxHIndex.Value.ToString());
            
            if (request.MinAcademicAge.HasValue)
                _formDataBuilder.AddField("min_academic_age", request.MinAcademicAge.Value.ToString());
            
            if (request.MaxAcademicAge.HasValue)
                _formDataBuilder.AddField("max_academic_age", request.MaxAcademicAge.Value.ToString());
            
            if (request.MinArticlesCount.HasValue)
                _formDataBuilder.AddField("min_articles_count", request.MinArticlesCount.Value.ToString());
            
            if (request.MaxArticlesCount.HasValue)
                _formDataBuilder.AddField("max_articles_count", request.MaxArticlesCount.Value.ToString());
            
            if (request.ExcludeCandidates)
                _formDataBuilder.AddField("exclude_candidates", "true");

            // Add file content - use "source_file" as the field name (matching API docs)
            var mimeType = request.MimeType ?? GetMimeTypeFromFileName(request.FileName!);
            _formDataBuilder.AddFile("source_file", request.FileName!, request.FileContent!, mimeType);

            _logger.LogDebug("Built multipart form data with file: {FileName} ({Size} bytes), Authors: {AuthorsCount}", 
                request.FileName, request.FileContent!.Length, request.AuthorsCount);

            return _formDataBuilder.Build();
        }

        private async Task<HttpResponseMessage> SendWithProgressAsync(HttpRequestMessage request, IProgress<UploadProgress>? progress, long totalBytes, CancellationToken cancellationToken)
        {
            // For now, we'll send the request directly since HttpClient doesn't provide built-in upload progress
            // In a future enhancement, we could implement a custom HttpContent that reports progress
            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Simulate progress reporting for the upload
            if (progress != null && totalBytes > 0)
            {
                // Report progress in chunks
                for (int i = 1; i <= 10; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var uploadedBytes = (long)(totalBytes * (i / 10.0));
                    progress.Report(new UploadProgress
                    {
                        TotalBytes = totalBytes,
                        UploadedBytes = uploadedBytes,
                        Stage = "Uploading",
                        Message = $"Uploading... {i * 10}%"
                    });

                    // Small delay to simulate upload progress
                    await Task.Delay(50, cancellationToken);
                }
            }

            return response;
        }

        private static string GetMimeTypeFromFileName(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                _ => "application/octet-stream"
            };
        }
    }
} 