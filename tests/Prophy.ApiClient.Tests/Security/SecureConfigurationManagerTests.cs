using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Security;
using Prophy.ApiClient.Security.Providers;

namespace Prophy.ApiClient.Tests.Security
{
    public class SecureConfigurationManagerTests
    {
        private readonly Mock<ILogger<SecureConfigurationManager>> _mockLogger;
        private readonly Mock<ISecureConfigurationProvider> _mockProvider1;
        private readonly Mock<ISecureConfigurationProvider> _mockProvider2;

        public SecureConfigurationManagerTests()
        {
            _mockLogger = new Mock<ILogger<SecureConfigurationManager>>();
            _mockProvider1 = new Mock<ISecureConfigurationProvider>();
            _mockProvider2 = new Mock<ISecureConfigurationProvider>();

            _mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
            _mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");
        }

        [Fact]
        public void Constructor_WithNullProviders_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SecureConfigurationManager(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SecureConfigurationManager(providers, null));
        }

        [Fact]
        public void Constructor_WithEmptyProviders_ThrowsArgumentException()
        {
            // Arrange
            var providers = new ISecureConfigurationProvider[0];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new SecureConfigurationManager(providers, _mockLogger.Object));
        }

        [Fact]
        public async Task GetSecretAsync_WithNullSecretName_ThrowsArgumentException()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                manager.GetSecretAsync(null));
        }

        [Fact]
        public async Task GetSecretAsync_WithEmptySecretName_ThrowsArgumentException()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                manager.GetSecretAsync(""));
        }

        [Fact]
        public async Task GetSecretAsync_WithAvailableProvider_ReturnsSecret()
        {
            // Arrange
            const string secretName = "test-secret";
            const string secretValue = "test-value";

            _mockProvider1.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider1.Setup(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretValue);

            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.GetSecretAsync(secretName);

            // Assert
            Assert.Equal(secretValue, result);
            _mockProvider1.Verify(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_WithUnavailableProvider_ReturnsNull()
        {
            // Arrange
            const string secretName = "test-secret";

            _mockProvider1.Setup(p => p.IsAvailable).Returns(false);

            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.GetSecretAsync(secretName);

            // Assert
            Assert.Null(result);
            _mockProvider1.Verify(p => p.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetSecretAsync_WithMultipleProviders_UsesFirstAvailable()
        {
            // Arrange
            const string secretName = "test-secret";
            const string secretValue = "test-value";

            _mockProvider1.Setup(p => p.IsAvailable).Returns(false);
            _mockProvider2.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider2.Setup(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretValue);

            var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.GetSecretAsync(secretName);

            // Assert
            Assert.Equal(secretValue, result);
            _mockProvider1.Verify(p => p.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockProvider2.Verify(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_WithProviderException_ContinuesToNextProvider()
        {
            // Arrange
            const string secretName = "test-secret";
            const string secretValue = "test-value";

            _mockProvider1.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider1.Setup(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Provider1 failed"));

            _mockProvider2.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider2.Setup(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretValue);

            var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.GetSecretAsync(secretName);

            // Assert
            Assert.Equal(secretValue, result);
            _mockProvider1.Verify(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()), Times.Once);
            _mockProvider2.Verify(p => p.GetSecretAsync(secretName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSecretsAsync_WithNullSecretNames_ThrowsArgumentNullException()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                manager.GetSecretsAsync(null));
        }

        [Fact]
        public async Task GetSecretsAsync_WithEmptySecretNames_ReturnsEmptyDictionary()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.GetSecretsAsync(new string[0]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSecretsAsync_WithMultipleSecrets_ReturnsAllFound()
        {
            // Arrange
            var secretNames = new[] { "secret1", "secret2", "secret3" };
            var expectedSecrets = new Dictionary<string, string>
            {
                ["secret1"] = "value1",
                ["secret2"] = "value2"
            };

            _mockProvider1.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider1.Setup(p => p.GetSecretsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSecrets);

            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.GetSecretsAsync(secretNames);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("value1", result["secret1"]);
            Assert.Equal("value2", result["secret2"]);
            Assert.False(result.ContainsKey("secret3"));
        }

        [Fact]
        public async Task SetSecretAsync_WithNullSecretName_ThrowsArgumentException()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                manager.SetSecretAsync(null, "value"));
        }

        [Fact]
        public async Task SetSecretAsync_WithNullSecretValue_ThrowsArgumentException()
        {
            // Arrange
            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                manager.SetSecretAsync("secret", null));
        }

        [Fact]
        public async Task SetSecretAsync_WithNoAvailableProviders_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockProvider1.Setup(p => p.IsAvailable).Returns(false);

            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                manager.SetSecretAsync("secret", "value"));
        }

        [Fact]
        public async Task SetSecretAsync_WithAvailableProvider_CallsProvider()
        {
            // Arrange
            const string secretName = "test-secret";
            const string secretValue = "test-value";

            _mockProvider1.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider1.Setup(p => p.SetSecretAsync(secretName, secretValue, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var providers = new[] { _mockProvider1.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            await manager.SetSecretAsync(secretName, secretValue);

            // Assert
            _mockProvider1.Verify(p => p.SetSecretAsync(secretName, secretValue, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TestConnectionAsync_WithAllProvidersSuccessful_ReturnsTrue()
        {
            // Arrange
            _mockProvider1.Setup(p => p.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockProvider2.Setup(p => p.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.TestConnectionAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestConnectionAsync_WithSomeProvidersSuccessful_ReturnsTrue()
        {
            // Arrange
            _mockProvider1.Setup(p => p.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockProvider2.Setup(p => p.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.TestConnectionAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestConnectionAsync_WithAllProvidersFailed_ReturnsFalse()
        {
            // Arrange
            _mockProvider1.Setup(p => p.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockProvider2.Setup(p => p.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = await manager.TestConnectionAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAvailableProviders_ReturnsOnlyAvailableProviders()
        {
            // Arrange
            _mockProvider1.Setup(p => p.IsAvailable).Returns(true);
            _mockProvider2.Setup(p => p.IsAvailable).Returns(false);

            var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
            var manager = new SecureConfigurationManager(providers, _mockLogger.Object);

            // Act
            var result = manager.GetAvailableProviders().ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains("Provider1", result);
            Assert.DoesNotContain("Provider2", result);
        }
    }

    public class InMemorySecureConfigurationProviderTests
    {
        [Fact]
        public void Constructor_WithDefaultParameters_InitializesCorrectly()
        {
            // Act
            var provider = new InMemorySecureConfigurationProvider();

            // Assert
            Assert.Equal("InMemory", provider.ProviderName);
            Assert.True(provider.IsAvailable);
            Assert.Equal(0, provider.SecretCount);
        }

        [Fact]
        public void Constructor_WithInitialSecrets_PopulatesSecrets()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string>
            {
                ["secret1"] = "value1",
                ["secret2"] = "value2"
            };

            // Act
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);

            // Assert
            Assert.Equal(2, provider.SecretCount);
            Assert.True(provider.ContainsSecret("secret1"));
            Assert.True(provider.ContainsSecret("secret2"));
        }

        [Fact]
        public void Constructor_WithIsAvailableFalse_ReportsUnavailable()
        {
            // Act
            var provider = new InMemorySecureConfigurationProvider(isAvailable: false);

            // Assert
            Assert.False(provider.IsAvailable);
        }

        [Fact]
        public async Task GetSecretAsync_WithNullSecretName_ThrowsArgumentException()
        {
            // Arrange
            var provider = new InMemorySecureConfigurationProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                provider.GetSecretAsync(null));
        }

        [Fact]
        public async Task GetSecretAsync_WithExistingSecret_ReturnsValue()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string> { ["test"] = "value" };
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);

            // Act
            var result = await provider.GetSecretAsync("test");

            // Assert
            Assert.Equal("value", result);
        }

        [Fact]
        public async Task GetSecretAsync_WithNonExistingSecret_ReturnsNull()
        {
            // Arrange
            var provider = new InMemorySecureConfigurationProvider();

            // Act
            var result = await provider.GetSecretAsync("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetSecretAsync_WithValidInput_StoresSecret()
        {
            // Arrange
            var provider = new InMemorySecureConfigurationProvider();

            // Act
            await provider.SetSecretAsync("test", "value");

            // Assert
            Assert.Equal(1, provider.SecretCount);
            Assert.True(provider.ContainsSecret("test"));
            
            var retrievedValue = await provider.GetSecretAsync("test");
            Assert.Equal("value", retrievedValue);
        }

        [Fact]
        public async Task SetSecretAsync_WithExistingSecret_UpdatesValue()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string> { ["test"] = "oldvalue" };
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);

            // Act
            await provider.SetSecretAsync("test", "newvalue");

            // Assert
            Assert.Equal(1, provider.SecretCount);
            
            var retrievedValue = await provider.GetSecretAsync("test");
            Assert.Equal("newvalue", retrievedValue);
        }

        [Fact]
        public async Task DeleteSecretAsync_WithExistingSecret_RemovesSecret()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string> { ["test"] = "value" };
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);

            // Act
            await provider.DeleteSecretAsync("test");

            // Assert
            Assert.Equal(0, provider.SecretCount);
            Assert.False(provider.ContainsSecret("test"));
        }

        [Fact]
        public async Task DeleteSecretAsync_WithNonExistingSecret_DoesNotThrow()
        {
            // Arrange
            var provider = new InMemorySecureConfigurationProvider();

            // Act & Assert (should not throw)
            await provider.DeleteSecretAsync("nonexistent");
            Assert.Equal(0, provider.SecretCount);
        }

        [Fact]
        public async Task GetSecretsAsync_WithMultipleSecrets_ReturnsFoundSecrets()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string>
            {
                ["secret1"] = "value1",
                ["secret2"] = "value2"
            };
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);
            var requestedSecrets = new[] { "secret1", "secret2", "secret3" };

            // Act
            var result = await provider.GetSecretsAsync(requestedSecrets);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("value1", result["secret1"]);
            Assert.Equal("value2", result["secret2"]);
            Assert.False(result.ContainsKey("secret3"));
        }

        [Fact]
        public async Task TestConnectionAsync_ReturnsIsAvailableValue()
        {
            // Arrange
            var availableProvider = new InMemorySecureConfigurationProvider(isAvailable: true);
            var unavailableProvider = new InMemorySecureConfigurationProvider(isAvailable: false);

            // Act
            var availableResult = await availableProvider.TestConnectionAsync();
            var unavailableResult = await unavailableProvider.TestConnectionAsync();

            // Assert
            Assert.True(availableResult);
            Assert.False(unavailableResult);
        }

        [Fact]
        public void Clear_RemovesAllSecrets()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string>
            {
                ["secret1"] = "value1",
                ["secret2"] = "value2"
            };
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);

            // Act
            provider.Clear();

            // Assert
            Assert.Equal(0, provider.SecretCount);
            Assert.False(provider.ContainsSecret("secret1"));
            Assert.False(provider.ContainsSecret("secret2"));
        }

        [Fact]
        public void GetSecretNames_ReturnsAllSecretNames()
        {
            // Arrange
            var initialSecrets = new Dictionary<string, string>
            {
                ["secret1"] = "value1",
                ["secret2"] = "value2"
            };
            var provider = new InMemorySecureConfigurationProvider(initialSecrets);

            // Act
            var secretNames = provider.GetSecretNames().ToList();

            // Assert
            Assert.Equal(2, secretNames.Count);
            Assert.Contains("secret1", secretNames);
            Assert.Contains("secret2", secretNames);
        }
    }
} 