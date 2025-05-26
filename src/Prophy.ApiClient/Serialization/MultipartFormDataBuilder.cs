using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// Concrete implementation of IMultipartFormDataBuilder for building multipart form data content.
    /// </summary>
    public class MultipartFormDataBuilder : IMultipartFormDataBuilder
    {
        private readonly ILogger<MultipartFormDataBuilder> _logger;
        private readonly List<Action<MultipartFormDataContent>> _contentBuilders;

        /// <summary>
        /// Initializes a new instance of the MultipartFormDataBuilder class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operations.</param>
        public MultipartFormDataBuilder(ILogger<MultipartFormDataBuilder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contentBuilders = new List<Action<MultipartFormDataContent>>();
        }

        /// <inheritdoc />
        public IMultipartFormDataBuilder AddField(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));

            _contentBuilders.Add(content =>
            {
                var stringContent = new StringContent(value ?? string.Empty, Encoding.UTF8);
                content.Add(stringContent, name);
                _logger.LogDebug("Added field '{Name}' with value length {Length}", name, (value ?? string.Empty).Length);
            });

            return this;
        }

        /// <inheritdoc />
        public IMultipartFormDataBuilder AddFile(string name, string fileName, byte[] content, string contentType)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type cannot be null or empty.", nameof(contentType));

            _contentBuilders.Add(multipartContent =>
            {
                var byteArrayContent = new ByteArrayContent(content);
                byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                multipartContent.Add(byteArrayContent, name, fileName);
                _logger.LogDebug("Added file '{FileName}' to field '{Name}' ({Size} bytes, {ContentType})", 
                    fileName, name, content.Length, contentType);
            });

            return this;
        }

        /// <inheritdoc />
        public IMultipartFormDataBuilder AddFile(string name, string fileName, Stream stream, string contentType)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type cannot be null or empty.", nameof(contentType));

            _contentBuilders.Add(multipartContent =>
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                multipartContent.Add(streamContent, name, fileName);
                _logger.LogDebug("Added file '{FileName}' to field '{Name}' (stream, {ContentType})", 
                    fileName, name, contentType);
            });

            return this;
        }

        /// <inheritdoc />
        public IMultipartFormDataBuilder AddFields(IDictionary<string, string> fields)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));

            foreach (var field in fields)
            {
                AddField(field.Key, field.Value);
            }

            _logger.LogDebug("Added {Count} fields to multipart form data", fields.Count);
            return this;
        }

        /// <inheritdoc />
        public MultipartFormDataContent Build()
        {
            var content = new MultipartFormDataContent();

            try
            {
                foreach (var builder in _contentBuilders)
                {
                    builder(content);
                }

                _logger.LogDebug("Built multipart form data content with {Count} parts", _contentBuilders.Count);
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build multipart form data content");
                content.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public IMultipartFormDataBuilder Clear()
        {
            _contentBuilders.Clear();
            _logger.LogDebug("Cleared all content builders");
            return this;
        }
    }
} 