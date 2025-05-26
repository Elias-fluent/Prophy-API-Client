using System;
using System.Net;

namespace Prophy.ApiClient.Exceptions
{
    /// <summary>
    /// Exception thrown when JSON serialization or deserialization fails.
    /// </summary>
    public class SerializationException : ProphyApiException
    {
        /// <summary>
        /// Gets the type that was being serialized or deserialized.
        /// </summary>
        public Type? TargetType { get; }

        /// <summary>
        /// Gets the JSON content that caused the serialization error.
        /// </summary>
        public string? JsonContent { get; }

        /// <summary>
        /// Initializes a new instance of the SerializationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="targetType">The type that was being serialized or deserialized.</param>
        public SerializationException(string message, Type? targetType = null) 
            : base(message, "SERIALIZATION_ERROR")
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Initializes a new instance of the SerializationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="targetType">The type that was being serialized or deserialized.</param>
        /// <param name="innerException">The inner exception.</param>
        public SerializationException(string message, Type? targetType, Exception innerException) 
            : base(message, "SERIALIZATION_ERROR", innerException)
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Initializes a new instance of the SerializationException class with JSON content.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="targetType">The type that was being serialized or deserialized.</param>
        /// <param name="jsonContent">The JSON content that caused the error.</param>
        /// <param name="innerException">The inner exception.</param>
        public SerializationException(string message, Type? targetType, string? jsonContent, Exception innerException) 
            : base(message, "SERIALIZATION_ERROR", innerException)
        {
            TargetType = targetType;
            JsonContent = jsonContent;
        }

        /// <summary>
        /// Initializes a new instance of the SerializationException class with HTTP details.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="targetType">The type that was being serialized or deserialized.</param>
        /// <param name="jsonContent">The JSON content that caused the error.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="errorDetails">Additional error details.</param>
        /// <param name="requestId">The request ID for tracking.</param>
        public SerializationException(string message, Type? targetType, string? jsonContent, HttpStatusCode httpStatusCode, string? errorDetails = null, string? requestId = null) 
            : base(message, "SERIALIZATION_ERROR", httpStatusCode, errorDetails, requestId)
        {
            TargetType = targetType;
            JsonContent = jsonContent;
        }

        /// <summary>
        /// Returns a string representation of the exception with serialization details.
        /// </summary>
        /// <returns>A formatted string containing exception and serialization details.</returns>
        public override string ToString()
        {
            var result = base.ToString();
            
            if (TargetType != null)
                result += $"\nTarget Type: {TargetType.FullName}";
            
            if (!string.IsNullOrEmpty(JsonContent))
            {
                var truncatedJson = JsonContent.Length > 500 ? JsonContent.Substring(0, 500) + "..." : JsonContent;
                result += $"\nJSON Content: {truncatedJson}";
            }
            
            return result;
        }
    }
} 