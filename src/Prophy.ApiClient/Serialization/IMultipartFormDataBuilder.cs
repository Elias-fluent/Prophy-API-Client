using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// Provides functionality for building multipart form data content for file uploads.
    /// </summary>
    public interface IMultipartFormDataBuilder
    {
        /// <summary>
        /// Adds a string field to the multipart form data.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IMultipartFormDataBuilder AddField(string name, string value);

        /// <summary>
        /// Adds a file to the multipart form data.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="content">The file content as a byte array.</param>
        /// <param name="contentType">The MIME content type of the file.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IMultipartFormDataBuilder AddFile(string name, string fileName, byte[] content, string contentType);

        /// <summary>
        /// Adds a file to the multipart form data.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="stream">The file content as a stream.</param>
        /// <param name="contentType">The MIME content type of the file.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IMultipartFormDataBuilder AddFile(string name, string fileName, Stream stream, string contentType);

        /// <summary>
        /// Adds multiple fields to the multipart form data.
        /// </summary>
        /// <param name="fields">A dictionary of field names and values.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IMultipartFormDataBuilder AddFields(IDictionary<string, string> fields);

        /// <summary>
        /// Builds the multipart form data content.
        /// </summary>
        /// <returns>The MultipartFormDataContent instance ready for HTTP requests.</returns>
        MultipartFormDataContent Build();

        /// <summary>
        /// Clears all added fields and files from the builder.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        IMultipartFormDataBuilder Clear();
    }
} 