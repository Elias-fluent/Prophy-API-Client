using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Models.Responses;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Secure storage for OAuth tokens with encryption and expiration handling.
    /// </summary>
    public class SecureTokenStorage : ISecureTokenStorage
    {
        private readonly ILogger<SecureTokenStorage> _logger;
        private readonly ConcurrentDictionary<string, EncryptedTokenEntry> _tokenCache;
        private readonly byte[] _encryptionKey;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the SecureTokenStorage class.
        /// </summary>
        /// <param name="logger">The logger instance for logging storage operations.</param>
        /// <param name="encryptionKey">The encryption key for securing stored tokens. If null, a random key is generated.</param>
        public SecureTokenStorage(ILogger<SecureTokenStorage> logger, byte[]? encryptionKey = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenCache = new ConcurrentDictionary<string, EncryptedTokenEntry>();
            _encryptionKey = encryptionKey ?? GenerateEncryptionKey();
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _logger.LogDebug("Initialized secure token storage with {KeyLength}-bit encryption", _encryptionKey.Length * 8);
        }

        /// <inheritdoc />
        public void StoreToken(string key, OAuthTokenResponse token)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (token == null)
                throw new ArgumentNullException(nameof(token));

            try
            {
                _logger.LogDebug("Storing OAuth token for key: {Key}", key);

                var tokenJson = JsonSerializer.Serialize(token, _jsonOptions);
                var encryptedData = EncryptData(tokenJson);
                var entry = new EncryptedTokenEntry
                {
                    EncryptedData = encryptedData,
                    ExpiresAt = token.ExpiresAt,
                    StoredAt = DateTime.UtcNow
                };

                _tokenCache.AddOrUpdate(key, entry, (k, v) => entry);

                _logger.LogDebug("Successfully stored encrypted OAuth token for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store OAuth token for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public OAuthTokenResponse? GetToken(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (!_tokenCache.TryGetValue(key, out var entry))
                {
                    _logger.LogDebug("No token found for key: {Key}", key);
                    return null;
                }

                // Check if token is expired
                if (entry.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogDebug("Token expired for key: {Key}, removing from storage", key);
                    _tokenCache.TryRemove(key, out _);
                    return null;
                }

                var decryptedJson = DecryptData(entry.EncryptedData);
                var token = JsonSerializer.Deserialize<OAuthTokenResponse>(decryptedJson, _jsonOptions);

                _logger.LogDebug("Successfully retrieved OAuth token for key: {Key}", key);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve OAuth token for key: {Key}", key);
                return null;
            }
        }

        /// <inheritdoc />
        public bool IsTokenValid(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (!_tokenCache.TryGetValue(key, out var entry))
                return false;

            var isValid = entry.ExpiresAt > DateTime.UtcNow;
            
            if (!isValid)
            {
                _logger.LogDebug("Token expired for key: {Key}, removing from storage", key);
                _tokenCache.TryRemove(key, out _);
            }

            return isValid;
        }

        /// <inheritdoc />
        public bool IsTokenExpiringSoon(string key, TimeSpan threshold)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (!_tokenCache.TryGetValue(key, out var entry))
                return false;

            return entry.ExpiresAt <= DateTime.UtcNow.Add(threshold);
        }

        /// <inheritdoc />
        public void RemoveToken(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                var removed = _tokenCache.TryRemove(key, out _);
                
                if (removed)
                {
                    _logger.LogDebug("Successfully removed OAuth token for key: {Key}", key);
                }
                else
                {
                    _logger.LogDebug("No token found to remove for key: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove OAuth token for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public void ClearAllTokens()
        {
            try
            {
                var count = _tokenCache.Count;
                _tokenCache.Clear();
                
                _logger.LogDebug("Cleared {TokenCount} OAuth tokens from storage", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear OAuth tokens from storage");
                throw;
            }
        }

        /// <inheritdoc />
        public void CleanupExpiredTokens()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                foreach (var kvp in _tokenCache)
                {
                    if (kvp.Value.ExpiresAt <= now)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _tokenCache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {ExpiredCount} expired OAuth tokens", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired OAuth tokens");
                throw;
            }
        }

        /// <summary>
        /// Encrypts data using AES encryption.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The encrypted data including IV.</returns>
        private byte[] EncryptData(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    
                    // Combine IV and encrypted data
                    var result = new byte[aes.IV.Length + encryptedBytes.Length];
                    Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                    Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
                    
                    return result;
                }
            }
        }

        /// <summary>
        /// Decrypts data using AES encryption.
        /// </summary>
        /// <param name="encryptedData">The encrypted data including IV.</param>
        /// <returns>The decrypted plain text.</returns>
        private string DecryptData(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                
                // Extract IV from the beginning of the encrypted data
                var iv = new byte[aes.IV.Length];
                var encrypted = new byte[encryptedData.Length - iv.Length];
                
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                Array.Copy(encryptedData, iv.Length, encrypted, 0, encrypted.Length);
                
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        /// <summary>
        /// Generates a random encryption key for AES.
        /// </summary>
        /// <returns>A 256-bit encryption key.</returns>
        private static byte[] GenerateEncryptionKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new byte[32]; // 256 bits
                rng.GetBytes(key);
                return key;
            }
        }

        /// <summary>
        /// Represents an encrypted token entry in storage.
        /// </summary>
        private class EncryptedTokenEntry
        {
            public byte[] EncryptedData { get; set; } = Array.Empty<byte>();
            public DateTime ExpiresAt { get; set; }
            public DateTime StoredAt { get; set; }
        }
    }
} 