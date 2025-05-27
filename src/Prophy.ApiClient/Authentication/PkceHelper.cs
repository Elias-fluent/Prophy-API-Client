using System;
using System.Security.Cryptography;
using System.Text;

namespace Prophy.ApiClient.Authentication
{
    /// <summary>
    /// Helper class for generating PKCE (Proof Key for Code Exchange) parameters for OAuth 2.0.
    /// </summary>
    public static class PkceHelper
    {
        /// <summary>
        /// Generates a cryptographically secure code verifier for PKCE.
        /// </summary>
        /// <returns>A base64url-encoded code verifier string.</returns>
        public static string GenerateCodeVerifier()
        {
            // Generate 32 random bytes (256 bits) for high entropy
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Convert to base64url encoding (RFC 7636 compliant)
            return Base64UrlEncode(randomBytes);
        }

        /// <summary>
        /// Generates a code challenge from a code verifier using SHA256.
        /// </summary>
        /// <param name="codeVerifier">The code verifier to generate a challenge for.</param>
        /// <returns>A base64url-encoded code challenge string.</returns>
        /// <exception cref="ArgumentException">Thrown when codeVerifier is null or empty.</exception>
        public static string GenerateCodeChallenge(string codeVerifier)
        {
            if (string.IsNullOrEmpty(codeVerifier))
                throw new ArgumentException("Code verifier cannot be null or empty.", nameof(codeVerifier));

            // Hash the code verifier using SHA256
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Base64UrlEncode(challengeBytes);
            }
        }

        /// <summary>
        /// Generates both a code verifier and its corresponding code challenge.
        /// </summary>
        /// <returns>A tuple containing the code verifier and code challenge.</returns>
        public static (string CodeVerifier, string CodeChallenge) GeneratePkceParameters()
        {
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            return (codeVerifier, codeChallenge);
        }

        /// <summary>
        /// Generates a cryptographically secure state parameter for OAuth requests.
        /// </summary>
        /// <returns>A base64url-encoded state string.</returns>
        public static string GenerateState()
        {
            var randomBytes = new byte[16]; // 128 bits should be sufficient for state
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return Base64UrlEncode(randomBytes);
        }

        /// <summary>
        /// Validates that a code verifier matches the expected format and length requirements.
        /// </summary>
        /// <param name="codeVerifier">The code verifier to validate.</param>
        /// <returns>True if the code verifier is valid, false otherwise.</returns>
        public static bool IsValidCodeVerifier(string codeVerifier)
        {
            if (string.IsNullOrEmpty(codeVerifier))
                return false;

            // RFC 7636: code verifier must be 43-128 characters long
            if (codeVerifier.Length < 43 || codeVerifier.Length > 128)
                return false;

            // Must contain only unreserved characters: [A-Z] / [a-z] / [0-9] / "-" / "." / "_" / "~"
            foreach (char c in codeVerifier)
            {
                if (!IsUnreservedCharacter(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Encodes a byte array using base64url encoding (RFC 4648 Section 5).
        /// </summary>
        /// <param name="input">The byte array to encode.</param>
        /// <returns>A base64url-encoded string.</returns>
        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            
            // Convert base64 to base64url by replacing characters and removing padding
            return base64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Checks if a character is an unreserved character according to RFC 3986.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is unreserved, false otherwise.</returns>
        private static bool IsUnreservedCharacter(char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '-' || c == '.' || c == '_' || c == '~';
        }
    }
} 