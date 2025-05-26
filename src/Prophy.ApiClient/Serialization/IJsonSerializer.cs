using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Prophy.ApiClient.Serialization
{
    /// <summary>
    /// Provides JSON serialization and deserialization functionality for the Prophy API client.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        string Serialize<T>(T value);

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T Deserialize<T>(string json);

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <returns>The deserialized object.</returns>
        object? Deserialize(string json, Type type);

        /// <summary>
        /// Asynchronously deserializes a JSON stream to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing JSON data.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The deserialized object.</returns>
        Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes a JSON stream to an object of the specified type.
        /// </summary>
        /// <param name="stream">The stream containing JSON data.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The deserialized object.</returns>
        Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously serializes an object to a JSON stream.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="stream">The stream to write JSON data to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);
    }
} 