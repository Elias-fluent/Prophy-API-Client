using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Serialization
{
    public class SystemTextJsonSerializerTests
    {
        private readonly Mock<ILogger<SystemTextJsonSerializer>> _mockLogger;
        private readonly SystemTextJsonSerializer _serializer;

        public SystemTextJsonSerializerTests()
        {
            _mockLogger = new Mock<ILogger<SystemTextJsonSerializer>>();
            _serializer = new SystemTextJsonSerializer(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithLogger_InitializesCorrectly()
        {
            // Act & Assert - Should not throw
            var serializer = new SystemTextJsonSerializer(_mockLogger.Object);
            Assert.NotNull(serializer);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemTextJsonSerializer(null!));
        }

        [Fact]
        public void Constructor_WithCustomOptions_InitializesCorrectly()
        {
            // Arrange
            var options = new JsonSerializerOptions();

            // Act & Assert - Should not throw
            var serializer = new SystemTextJsonSerializer(options, _mockLogger.Object);
            Assert.NotNull(serializer);
        }

        [Fact]
        public void Serialize_WithSimpleObject_ReturnsJsonString()
        {
            // Arrange
            var testObject = new { Name = "Test", Value = 42 };

            // Act
            var json = _serializer.Serialize(testObject);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"name\":", json); // camelCase naming
            Assert.Contains("\"value\":", json);
            Assert.Contains("\"Test\"", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void Serialize_WithNullObject_ReturnsNullJson()
        {
            // Act
            var json = _serializer.Serialize<object?>(null);

            // Assert
            Assert.Equal("null", json);
        }

        [Fact]
        public void Deserialize_WithValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";

            // Act
            var result = _serializer.Deserialize<TestClass>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Deserialize_WithInvalidJson_ThrowsArgumentException(string? invalidJson)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _serializer.Deserialize<TestClass>(invalidJson!));
        }

        [Fact]
        public void Deserialize_WithMalformedJson_ThrowsJsonException()
        {
            // Arrange
            var malformedJson = "{\"name\":\"Test\",\"value\":}";

            // Act & Assert
            Assert.Throws<JsonException>(() => _serializer.Deserialize<TestClass>(malformedJson));
        }

        [Fact]
        public void Deserialize_WithType_ReturnsObject()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";

            // Act
            var result = _serializer.Deserialize(json, typeof(TestClass));

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TestClass>(result);
            var testClass = (TestClass)result;
            Assert.Equal("Test", testClass.Name);
            Assert.Equal(42, testClass.Value);
        }

        [Fact]
        public void Deserialize_WithNullType_ThrowsArgumentNullException()
        {
            // Arrange
            var json = "{\"name\":\"Test\"}";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(json, null!));
        }

        [Fact]
        public async Task DeserializeAsync_WithValidStream_ReturnsObject()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await _serializer.DeserializeAsync<TestClass>(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public async Task DeserializeAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _serializer.DeserializeAsync<TestClass>(null!));
        }

        [Fact]
        public async Task DeserializeAsync_WithType_ReturnsObject()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await _serializer.DeserializeAsync(stream, typeof(TestClass));

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TestClass>(result);
            var testClass = (TestClass)result;
            Assert.Equal("Test", testClass.Name);
            Assert.Equal(42, testClass.Value);
        }

        [Fact]
        public async Task SerializeAsync_WithObject_WritesToStream()
        {
            // Arrange
            var testObject = new TestClass { Name = "Test", Value = 42 };
            using var stream = new MemoryStream();

            // Act
            await _serializer.SerializeAsync(stream, testObject);

            // Assert
            stream.Position = 0;
            var json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("\"name\":", json);
            Assert.Contains("\"value\":", json);
            Assert.Contains("\"Test\"", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public async Task SerializeAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var testObject = new TestClass { Name = "Test", Value = 42 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _serializer.SerializeAsync(null!, testObject));
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip_PreservesData()
        {
            // Arrange
            var original = new TestClass { Name = "Test", Value = 42 };

            // Act
            var json = _serializer.Serialize(original);
            var deserialized = _serializer.Deserialize<TestClass>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.Value, deserialized.Value);
        }

        public class TestClass
        {
            public string? Name { get; set; }
            public int Value { get; set; }
        }
    }
} 