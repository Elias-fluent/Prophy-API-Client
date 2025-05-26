using System;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Provides functionality for manuscript operations in the Prophy API.
    /// </summary>
    public interface IManuscriptModule
    {
        /// <summary>
        /// Uploads a manuscript to the Prophy API with file content and metadata.
        /// </summary>
        /// <param name="request">The manuscript upload request containing metadata and file information.</param>
        /// <param name="progress">Optional progress reporter for upload progress tracking.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous upload operation. The task result contains the upload response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="ValidationException">Thrown when the request validation fails.</exception>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ApiTimeoutException">Thrown when the request times out.</exception>
        /// <exception cref="ProphyApiException">Thrown when other API errors occur.</exception>
        Task<ManuscriptUploadResponse> UploadAsync(ManuscriptUploadRequest request, IProgress<UploadProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the status of a previously uploaded manuscript.
        /// </summary>
        /// <param name="manuscriptId">The ID of the manuscript to retrieve.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the manuscript status.</returns>
        /// <exception cref="ArgumentException">Thrown when the manuscript ID is null or empty.</exception>
        /// <exception cref="AuthenticationException">Thrown when authentication fails.</exception>
        /// <exception cref="ProphyApiException">Thrown when API errors occur.</exception>
        Task<ManuscriptUploadResponse> GetStatusAsync(string manuscriptId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the progress of a manuscript upload operation.
    /// </summary>
    public class UploadProgress
    {
        /// <summary>
        /// Gets or sets the total number of bytes to upload.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes uploaded so far.
        /// </summary>
        public long UploadedBytes { get; set; }

        /// <summary>
        /// Gets the upload progress as a percentage (0-100).
        /// </summary>
        public double PercentageComplete => TotalBytes > 0 ? (double)UploadedBytes / TotalBytes * 100 : 0;

        /// <summary>
        /// Gets or sets the current stage of the upload process.
        /// </summary>
        public string Stage { get; set; } = "Preparing";

        /// <summary>
        /// Gets or sets additional information about the current upload stage.
        /// </summary>
        public string? Message { get; set; }
    }
} 