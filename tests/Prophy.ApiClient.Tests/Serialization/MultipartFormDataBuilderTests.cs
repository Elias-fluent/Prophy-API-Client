using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Serialization;
using Xunit;

namespace Prophy.ApiClient.Tests.Serialization
{
    public class MultipartFormDataBuilderTests : IDisposable
    {
        private readonly Mock<ILogger<MultipartFormDataBuilder>> _mockLogger;
        private readonly MultipartFormDataBuilder _builder;

        public MultipartFormDataBuilderTests()
        {
            _mockLogger = new Mock<ILogger<MultipartFormDataBuilder>>();
            _builder = new MultipartFormDataBuilder(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithLogger_InitializesCorrectly()
        {
            // Act & Assert - Should not throw
            var builder = new MultipartFormDataBuilder(_mockLogger.Object);
            Assert.NotNull(builder);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MultipartFormDataBuilder(null!));
        }

        [Fact]
        public void AddField_WithValidNameAndValue_ReturnsBuilder()
        {
            // Act
            var result = _builder.AddField("testField", "testValue");

            // Assert
            Assert.Same(_builder, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddField_WithInvalidName_ThrowsArgumentException(string? invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.AddField(invalidName!, "value"));
        }

        [Fact]
        public void AddField_WithNullValue_AcceptsNullValue()
        {
            // Act & Assert - Should not throw
            var result = _builder.AddField("testField", null!);
            Assert.Same(_builder, result);
        }

        [Fact]
        public void AddFile_WithByteArray_ReturnsBuilder()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test file content");

            // Act
            var result = _builder.AddFile("file", "test.txt", content, "text/plain");

            // Assert
            Assert.Same(_builder, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddFile_WithInvalidName_ThrowsArgumentException(string? invalidName)
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _builder.AddFile(invalidName!, "test.txt", content, "text/plain"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddFile_WithInvalidFileName_ThrowsArgumentException(string? invalidFileName)
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _builder.AddFile("file", invalidFileName!, content, "text/plain"));
        }

        [Fact]
        public void AddFile_WithNullContent_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _builder.AddFile("file", "test.txt", (byte[])null!, "text/plain"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddFile_WithInvalidContentType_ThrowsArgumentException(string? invalidContentType)
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _builder.AddFile("file", "test.txt", content, invalidContentType!));
        }

        [Fact]
        public void AddFile_WithStream_ReturnsBuilder()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test file content"));

            // Act
            var result = _builder.AddFile("file", "test.txt", stream, "text/plain");

            // Assert
            Assert.Same(_builder, result);
        }

        [Fact]
        public void AddFile_WithNullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _builder.AddFile("file", "test.txt", (Stream)null!, "text/plain"));
        }

        [Fact]
        public void AddFields_WithValidDictionary_ReturnsBuilder()
        {
            // Arrange
            var fields = new Dictionary<string, string>
            {
                { "field1", "value1" },
                { "field2", "value2" }
            };

            // Act
            var result = _builder.AddFields(fields);

            // Assert
            Assert.Same(_builder, result);
        }

        [Fact]
        public void AddFields_WithNullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.AddFields(null!));
        }

        [Fact]
        public void Build_WithNoContent_ReturnsEmptyMultipartContent()
        {
            // Act
            using var content = _builder.Build();

            // Assert
            Assert.NotNull(content);
            Assert.Empty(content);
        }

        [Fact]
        public void Build_WithFields_ReturnsMultipartContentWithFields()
        {
            // Arrange
            _builder.AddField("field1", "value1")
                   .AddField("field2", "value2");

            // Act
            using var content = _builder.Build();

            // Assert
            Assert.NotNull(content);
            Assert.Equal(2, content.Count());
        }

        [Fact]
        public void Build_WithFile_ReturnsMultipartContentWithFile()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            _builder.AddFile("file", "test.txt", fileContent, "text/plain");

            // Act
            using var content = _builder.Build();

            // Assert
            Assert.NotNull(content);
            Assert.Single(content);
        }

        [Fact]
        public void Build_WithMixedContent_ReturnsMultipartContentWithAllParts()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            _builder.AddField("field1", "value1")
                   .AddFile("file", "test.txt", fileContent, "text/plain")
                   .AddField("field2", "value2");

            // Act
            using var content = _builder.Build();

            // Assert
            Assert.NotNull(content);
            Assert.Equal(3, content.Count());
        }

        [Fact]
        public void Clear_RemovesAllContent()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            _builder.AddField("field1", "value1")
                   .AddFile("file", "test.txt", fileContent, "text/plain");

            // Act
            var result = _builder.Clear();

            // Assert
            Assert.Same(_builder, result);
            
            using var content = _builder.Build();
            Assert.Empty(content);
        }

        [Fact]
        public void Build_CalledMultipleTimes_ReturnsNewInstancesWithSameContent()
        {
            // Arrange
            _builder.AddField("field1", "value1");

            // Act
            using var content1 = _builder.Build();
            using var content2 = _builder.Build();

            // Assert
            Assert.NotSame(content1, content2);
            Assert.Single(content1);
            Assert.Single(content2);
        }

        [Fact]
        public void FluentInterface_AllowsMethodChaining()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            var fields = new Dictionary<string, string> { { "field3", "value3" } };

            // Act & Assert - Should not throw and should return builder for chaining
            var result = _builder
                .AddField("field1", "value1")
                .AddFile("file", "test.txt", fileContent, "text/plain")
                .AddFields(fields)
                .Clear()
                .AddField("field2", "value2");

            Assert.Same(_builder, result);
        }

        public void Dispose()
        {
            // Cleanup any resources if needed
        }
    }
} 