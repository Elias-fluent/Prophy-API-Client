using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Provides IP address whitelisting and request validation functionality with CIDR notation support.
    /// </summary>
    public class IpWhitelistValidator : IIpWhitelistValidator
    {
        private readonly ILogger<IpWhitelistValidator> _logger;
        private readonly ISecurityAuditLogger _auditLogger;
        private readonly IpWhitelistOptions _options;
        private readonly List<IpRange> _allowedRanges;
        private readonly HashSet<IPAddress> _allowedAddresses;

        /// <summary>
        /// Initializes a new instance of the IpWhitelistValidator class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="auditLogger">The security audit logger.</param>
        /// <param name="options">Configuration options for IP whitelisting.</param>
        public IpWhitelistValidator(
            ILogger<IpWhitelistValidator> logger,
            ISecurityAuditLogger auditLogger,
            IpWhitelistOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _options = options ?? new IpWhitelistOptions();
            
            _allowedRanges = new List<IpRange>();
            _allowedAddresses = new HashSet<IPAddress>();
            
            InitializeWhitelist();
        }

        /// <inheritdoc />
        public bool IsIpAllowed(string ipAddress, string? userIdentity = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                _logger.LogWarning("IP address validation failed: null or empty IP address provided");
                LogSecurityViolation("INVALID_IP_ADDRESS", "Null or empty IP address provided", userIdentity, ipAddress);
                return false;
            }

            // If whitelist is disabled, allow all IPs
            if (!_options.EnableWhitelist)
            {
                _logger.LogDebug("IP whitelist is disabled, allowing IP: {IpAddress}", ipAddress);
                return true;
            }

            // Parse the IP address with strict validation
            if (!IsValidIpAddressFormat(ipAddress))
            {
                _logger.LogWarning("IP address validation failed: invalid IP format: {IpAddress}", ipAddress);
                LogSecurityViolation("INVALID_IP_FORMAT", $"Invalid IP address format: {ipAddress}", userIdentity, ipAddress);
                return false;
            }

            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                _logger.LogWarning("IP address validation failed: invalid IP format: {IpAddress}", ipAddress);
                LogSecurityViolation("INVALID_IP_FORMAT", $"Invalid IP address format: {ipAddress}", userIdentity, ipAddress);
                return false;
            }

            // Check against individual allowed addresses
            if (_allowedAddresses.Contains(ip))
            {
                _logger.LogDebug("IP address {IpAddress} found in allowed addresses list", ipAddress);
                return true;
            }

            // Check against CIDR ranges
            foreach (var range in _allowedRanges)
            {
                if (range.Contains(ip))
                {
                    _logger.LogDebug("IP address {IpAddress} matches allowed CIDR range: {CidrRange}", ipAddress, range.CidrNotation);
                    return true;
                }
            }

            // IP not found in whitelist
            _logger.LogWarning("IP address {IpAddress} not found in whitelist", ipAddress);
            LogSecurityViolation("IP_NOT_WHITELISTED", $"IP address {ipAddress} is not in the whitelist", userIdentity, ipAddress);
            
            return false;
        }

        /// <inheritdoc />
        public ValidationResult ValidateRequest(string ipAddress, string? userAgent = null, string? userIdentity = null)
        {
            var errors = new List<string>();

            // Validate IP address
            if (!IsIpAllowed(ipAddress, userIdentity))
            {
                errors.Add($"IP address {ipAddress} is not allowed");
            }

            // Validate User-Agent if required
            if (_options.RequireUserAgent && string.IsNullOrWhiteSpace(userAgent))
            {
                errors.Add("User-Agent header is required");
                LogSecurityViolation("MISSING_USER_AGENT", "Request missing required User-Agent header", userIdentity, ipAddress);
            }

            // Check for suspicious User-Agent patterns
            if (!string.IsNullOrWhiteSpace(userAgent) && ContainsSuspiciousPatterns(userAgent))
            {
                errors.Add("User-Agent contains suspicious patterns");
                LogSecurityViolation("SUSPICIOUS_USER_AGENT", $"Suspicious User-Agent detected: {userAgent}", userIdentity, ipAddress);
            }

            // Check for rate limiting violations (if enabled)
            if (_options.EnableRateLimiting && IsRateLimited(ipAddress))
            {
                errors.Add($"Rate limit exceeded for IP address {ipAddress}");
                LogSecurityViolation("RATE_LIMIT_EXCEEDED", $"Rate limit exceeded for IP {ipAddress}", userIdentity, ipAddress);
            }

            var isValid = !errors.Any();
            
            if (isValid)
            {
                _logger.LogDebug("Request validation passed for IP: {IpAddress}, User-Agent: {UserAgent}", ipAddress, userAgent);
            }
            else
            {
                _logger.LogWarning("Request validation failed for IP: {IpAddress}, Errors: {Errors}", ipAddress, string.Join(", ", errors));
            }

            return isValid ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
        }

        /// <inheritdoc />
        public void AddAllowedIp(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));
            }

            if (ipAddress.Contains('/'))
            {
                // CIDR notation
                var range = IpRange.Parse(ipAddress);
                _allowedRanges.Add(range);
                _logger.LogInformation("Added CIDR range to whitelist: {CidrRange}", ipAddress);
            }
            else
            {
                // Individual IP address
                if (IPAddress.TryParse(ipAddress, out var ip))
                {
                    _allowedAddresses.Add(ip);
                    _logger.LogInformation("Added IP address to whitelist: {IpAddress}", ipAddress);
                }
                else
                {
                    throw new ArgumentException($"Invalid IP address format: {ipAddress}", nameof(ipAddress));
                }
            }
        }

        /// <inheritdoc />
        public void RemoveAllowedIp(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));
            }

            if (ipAddress.Contains('/'))
            {
                // CIDR notation
                var range = IpRange.Parse(ipAddress);
                _allowedRanges.RemoveAll(r => r.CidrNotation.Equals(ipAddress, StringComparison.OrdinalIgnoreCase));
                _logger.LogInformation("Removed CIDR range from whitelist: {CidrRange}", ipAddress);
            }
            else
            {
                // Individual IP address
                if (IPAddress.TryParse(ipAddress, out var ip))
                {
                    _allowedAddresses.Remove(ip);
                    _logger.LogInformation("Removed IP address from whitelist: {IpAddress}", ipAddress);
                }
                else
                {
                    throw new ArgumentException($"Invalid IP address format: {ipAddress}", nameof(ipAddress));
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllowedIps()
        {
            var result = new List<string>();
            
            // Add individual IP addresses
            result.AddRange(_allowedAddresses.Select(ip => ip.ToString()));
            
            // Add CIDR ranges
            result.AddRange(_allowedRanges.Select(range => range.CidrNotation));
            
            return result;
        }

        /// <inheritdoc />
        public void ClearWhitelist()
        {
            _allowedAddresses.Clear();
            _allowedRanges.Clear();
            _logger.LogWarning("IP whitelist has been cleared");
        }

        private void InitializeWhitelist()
        {
            // Add default allowed IPs from configuration
            foreach (var ip in _options.DefaultAllowedIps)
            {
                try
                {
                    AddAllowedIp(ip);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add default IP to whitelist: {IpAddress}", ip);
                }
            }

            _logger.LogInformation("IP whitelist initialized with {AddressCount} individual addresses and {RangeCount} CIDR ranges",
                _allowedAddresses.Count, _allowedRanges.Count);
        }

        private bool ContainsSuspiciousPatterns(string userAgent)
        {
            // Check for common bot/scanner patterns
            var suspiciousPatterns = new[]
            {
                "sqlmap", "nmap", "nikto", "dirb", "gobuster", "wfuzz", "burp",
                "scanner", "crawler", "bot", "spider", "scraper", "harvester",
                "exploit", "hack", "attack", "injection", "payload"
            };

            var lowerUserAgent = userAgent.ToLowerInvariant();
            return suspiciousPatterns.Any(pattern => lowerUserAgent.Contains(pattern));
        }

        private bool IsRateLimited(string ipAddress)
        {
            // Simple rate limiting implementation
            // In a real-world scenario, this would use a more sophisticated rate limiting mechanism
            // such as Redis or in-memory cache with sliding windows
            
            if (!_options.EnableRateLimiting)
            {
                return false;
            }

            // For demo purposes, always return false
            // Real implementation would track request counts per IP
            return false;
        }

        private void LogSecurityViolation(string violationType, string description, string? userIdentity, string? ipAddress)
        {
            _auditLogger.LogSecurityViolation(violationType, description, userIdentity, ipAddress, new Dictionary<string, object>
            {
                ["ViolationType"] = violationType,
                ["IpAddress"] = ipAddress ?? "Unknown",
                ["Timestamp"] = DateTimeOffset.UtcNow,
                ["DetectionMethod"] = "IP Whitelist Validation"
            });
        }

        private static bool IsValidIpAddressFormat(string ipAddress)
        {
            // Check for IPv4 format: exactly 4 octets separated by dots
            var parts = ipAddress.Split('.');
            if (parts.Length == 4)
            {
                // Validate each octet
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part) || part.Length > 3)
                        return false;
                    
                    if (!int.TryParse(part, out var octet) || octet < 0 || octet > 255)
                        return false;
                    
                    // Reject leading zeros (except for "0" itself)
                    if (part.Length > 1 && part[0] == '0')
                        return false;
                }
                return true;
            }

            // Check for IPv6 format: contains colons
            if (ipAddress.Contains(':'))
            {
                // For IPv6, we'll rely on IPAddress.TryParse for detailed validation
                // but ensure it has the basic IPv6 structure
                return ipAddress.Split(':').Length >= 3; // Minimum valid IPv6 has at least 3 parts
            }

            return false;
        }
    }

    /// <summary>
    /// Interface for IP whitelist validation functionality.
    /// </summary>
    public interface IIpWhitelistValidator
    {
        /// <summary>
        /// Checks if the specified IP address is allowed by the whitelist.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <param name="userIdentity">Optional user identity for audit logging.</param>
        /// <returns>True if the IP is allowed, false otherwise.</returns>
        bool IsIpAllowed(string ipAddress, string? userIdentity = null);

        /// <summary>
        /// Validates a request including IP address and other security checks.
        /// </summary>
        /// <param name="ipAddress">The IP address of the request.</param>
        /// <param name="userAgent">The User-Agent header of the request.</param>
        /// <param name="userIdentity">Optional user identity for audit logging.</param>
        /// <returns>A validation result indicating success or failure with error details.</returns>
        ValidationResult ValidateRequest(string ipAddress, string? userAgent = null, string? userIdentity = null);

        /// <summary>
        /// Adds an IP address or CIDR range to the whitelist.
        /// </summary>
        /// <param name="ipAddress">The IP address or CIDR notation to add.</param>
        void AddAllowedIp(string ipAddress);

        /// <summary>
        /// Removes an IP address or CIDR range from the whitelist.
        /// </summary>
        /// <param name="ipAddress">The IP address or CIDR notation to remove.</param>
        void RemoveAllowedIp(string ipAddress);

        /// <summary>
        /// Gets all allowed IP addresses and CIDR ranges.
        /// </summary>
        /// <returns>A collection of allowed IP addresses and CIDR ranges.</returns>
        IEnumerable<string> GetAllowedIps();

        /// <summary>
        /// Clears all entries from the whitelist.
        /// </summary>
        void ClearWhitelist();
    }

    /// <summary>
    /// Configuration options for IP whitelisting.
    /// </summary>
    public class IpWhitelistOptions
    {
        /// <summary>
        /// Gets or sets whether IP whitelisting is enabled.
        /// Default is true.
        /// </summary>
        public bool EnableWhitelist { get; set; } = true;

        /// <summary>
        /// Gets or sets whether User-Agent header is required.
        /// Default is false.
        /// </summary>
        public bool RequireUserAgent { get; set; } = false;

        /// <summary>
        /// Gets or sets whether rate limiting is enabled.
        /// Default is false.
        /// </summary>
        public bool EnableRateLimiting { get; set; } = false;

        /// <summary>
        /// Gets or sets the default allowed IP addresses and CIDR ranges.
        /// </summary>
        public List<string> DefaultAllowedIps { get; set; } = new List<string>
        {
            "127.0.0.1",      // Localhost IPv4
            "::1",            // Localhost IPv6
            "10.0.0.0/8",     // Private network Class A
            "172.16.0.0/12",  // Private network Class B
            "192.168.0.0/16"  // Private network Class C
        };

        /// <summary>
        /// Gets or sets the maximum number of requests per minute per IP.
        /// Default is 60.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; } = 60;
    }

    /// <summary>
    /// Represents an IP address range with CIDR notation support.
    /// </summary>
    public class IpRange
    {
        private readonly IPAddress _network;
        private readonly IPAddress _mask;

        /// <summary>
        /// Gets the CIDR notation string for this range.
        /// </summary>
        public string CidrNotation { get; }

        /// <summary>
        /// Initializes a new instance of the IpRange class.
        /// </summary>
        /// <param name="network">The network address.</param>
        /// <param name="prefixLength">The prefix length (subnet mask bits).</param>
        public IpRange(IPAddress network, int prefixLength)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            
            if (prefixLength < 0 || prefixLength > (network.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength));
            }

            _mask = CreateMask(prefixLength, network.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            CidrNotation = $"{network}/{prefixLength}";
        }

        /// <summary>
        /// Parses a CIDR notation string into an IpRange.
        /// </summary>
        /// <param name="cidr">The CIDR notation string (e.g., "192.168.1.0/24").</param>
        /// <returns>An IpRange representing the parsed CIDR range.</returns>
        public static IpRange Parse(string cidr)
        {
            if (string.IsNullOrWhiteSpace(cidr))
            {
                throw new ArgumentException("CIDR notation cannot be null or empty", nameof(cidr));
            }

            var parts = cidr.Split('/');
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid CIDR notation: {cidr}", nameof(cidr));
            }

            if (!IPAddress.TryParse(parts[0], out var network))
            {
                throw new ArgumentException($"Invalid IP address in CIDR notation: {parts[0]}", nameof(cidr));
            }

            if (!int.TryParse(parts[1], out var prefixLength))
            {
                throw new ArgumentException($"Invalid prefix length in CIDR notation: {parts[1]}", nameof(cidr));
            }

            return new IpRange(network, prefixLength);
        }

        /// <summary>
        /// Checks if the specified IP address is within this range.
        /// </summary>
        /// <param name="address">The IP address to check.</param>
        /// <returns>True if the address is within the range, false otherwise.</returns>
        public bool Contains(IPAddress address)
        {
            if (address == null)
            {
                return false;
            }

            // Ensure both addresses are the same family
            if (address.AddressFamily != _network.AddressFamily)
            {
                return false;
            }

            var addressBytes = address.GetAddressBytes();
            var networkBytes = _network.GetAddressBytes();
            var maskBytes = _mask.GetAddressBytes();

            for (int i = 0; i < addressBytes.Length; i++)
            {
                if ((addressBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static IPAddress CreateMask(int prefixLength, bool isIPv4)
        {
            var totalBits = isIPv4 ? 32 : 128;
            var bytes = new byte[isIPv4 ? 4 : 16];

            for (int i = 0; i < totalBits; i++)
            {
                if (i < prefixLength)
                {
                    bytes[i / 8] |= (byte)(0x80 >> (i % 8));
                }
            }

            return new IPAddress(bytes);
        }
    }
} 