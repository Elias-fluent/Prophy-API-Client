using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Prophy.ApiClient.Security;
using Xunit;

namespace Prophy.ApiClient.Tests.Security
{
    /// <summary>
    /// Unit tests for the IpWhitelistValidator class.
    /// </summary>
    public class IpWhitelistValidatorTests
    {
        private readonly Mock<ILogger<IpWhitelistValidator>> _mockLogger;
        private readonly Mock<ISecurityAuditLogger> _mockAuditLogger;

        public IpWhitelistValidatorTests()
        {
            _mockLogger = new Mock<ILogger<IpWhitelistValidator>>();
            _mockAuditLogger = new Mock<ISecurityAuditLogger>();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new IpWhitelistValidator(null!, _mockAuditLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullAuditLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new IpWhitelistValidator(_mockLogger.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Assert
            Assert.NotNull(validator);
            var allowedIps = validator.GetAllowedIps().ToList();
            Assert.Contains("127.0.0.1", allowedIps);
            Assert.Contains("::1", allowedIps);
        }

        [Theory]
        [InlineData("127.0.0.1", true)]
        [InlineData("::1", true)]
        [InlineData("10.5.10.20", true)] // Should match 10.0.0.0/8
        [InlineData("172.16.100.50", true)] // Should match 172.16.0.0/12
        [InlineData("192.168.1.100", true)] // Should match 192.168.0.0/16
        [InlineData("203.0.113.1", false)] // External IP
        [InlineData("8.8.8.8", false)] // Public DNS
        public void IsIpAllowed_WithDefaultWhitelist_ReturnsExpectedResult(string ipAddress, bool expected)
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            var result = validator.IsIpAllowed(ipAddress);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsIpAllowed_WithNullOrEmptyIp_ReturnsFalse(string ipAddress)
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            var result = validator.IsIpAllowed(ipAddress);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("invalid-ip")]
        [InlineData("256.256.256.256")]
        [InlineData("192.168.1")]
        [InlineData("192.168.1.1.1")]
        public void IsIpAllowed_WithInvalidIpFormat_ReturnsFalse(string ipAddress)
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            var result = validator.IsIpAllowed(ipAddress);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsIpAllowed_WithWhitelistDisabled_ReturnsTrue()
        {
            // Arrange
            var options = new IpWhitelistOptions { EnableWhitelist = false };
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object, options);

            // Act
            var result = validator.IsIpAllowed("203.0.113.1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AddAllowedIp_WithValidIp_AddsToWhitelist()
        {
            // Arrange
            var options = new IpWhitelistOptions { DefaultAllowedIps = new List<string>() };
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object, options);

            // Act
            validator.AddAllowedIp("203.0.113.100");

            // Assert
            Assert.True(validator.IsIpAllowed("203.0.113.100"));
            Assert.Contains("203.0.113.100", validator.GetAllowedIps());
        }

        [Fact]
        public void AddAllowedIp_WithCidrRange_AddsToWhitelist()
        {
            // Arrange
            var options = new IpWhitelistOptions { DefaultAllowedIps = new List<string>() };
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object, options);

            // Act
            validator.AddAllowedIp("203.0.113.0/24");

            // Assert
            Assert.True(validator.IsIpAllowed("203.0.113.50"));
            Assert.True(validator.IsIpAllowed("203.0.113.255"));
            Assert.False(validator.IsIpAllowed("203.0.114.1"));
            Assert.Contains("203.0.113.0/24", validator.GetAllowedIps());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddAllowedIp_WithNullOrEmptyIp_ThrowsArgumentException(string ipAddress)
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.AddAllowedIp(ipAddress));
        }

        [Theory]
        [InlineData("invalid-ip")]
        [InlineData("256.256.256.256")]
        public void AddAllowedIp_WithInvalidIp_ThrowsArgumentException(string ipAddress)
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => validator.AddAllowedIp(ipAddress));
        }

        [Fact]
        public void RemoveAllowedIp_WithExistingIp_RemovesFromWhitelist()
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);
            validator.AddAllowedIp("203.0.113.100");

            // Act
            validator.RemoveAllowedIp("203.0.113.100");

            // Assert
            Assert.False(validator.IsIpAllowed("203.0.113.100"));
            Assert.DoesNotContain("203.0.113.100", validator.GetAllowedIps());
        }

        [Fact]
        public void RemoveAllowedIp_WithCidrRange_RemovesFromWhitelist()
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);
            validator.AddAllowedIp("203.0.113.0/24");

            // Act
            validator.RemoveAllowedIp("203.0.113.0/24");

            // Assert
            Assert.False(validator.IsIpAllowed("203.0.113.50"));
            Assert.DoesNotContain("203.0.113.0/24", validator.GetAllowedIps());
        }

        [Fact]
        public void ClearWhitelist_RemovesAllEntries()
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);
            validator.AddAllowedIp("203.0.113.100");
            validator.AddAllowedIp("203.0.113.0/24");

            // Act
            validator.ClearWhitelist();

            // Assert
            Assert.Empty(validator.GetAllowedIps());
            Assert.False(validator.IsIpAllowed("127.0.0.1")); // Even default IPs should be removed
        }

        [Fact]
        public void ValidateRequest_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            var result = validator.ValidateRequest("127.0.0.1", "Mozilla/5.0");

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateRequest_WithBlockedIp_ReturnsFailure()
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            var result = validator.ValidateRequest("203.0.113.1", "Mozilla/5.0");

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("IP address 203.0.113.1 is not allowed", result.Errors);
        }

        [Fact]
        public void ValidateRequest_WithMissingUserAgent_WhenRequired_ReturnsFailure()
        {
            // Arrange
            var options = new IpWhitelistOptions { RequireUserAgent = true };
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object, options);

            // Act
            var result = validator.ValidateRequest("127.0.0.1", null);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("User-Agent header is required", result.Errors);
        }

        [Theory]
        [InlineData("sqlmap/1.0")]
        [InlineData("nmap scanner")]
        [InlineData("Burp Suite")]
        [InlineData("nikto/2.1.6")]
        public void ValidateRequest_WithSuspiciousUserAgent_ReturnsFailure(string userAgent)
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            var result = validator.ValidateRequest("127.0.0.1", userAgent);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("User-Agent contains suspicious patterns", result.Errors);
        }

        [Fact]
        public void ValidateRequest_WithMultipleViolations_ReturnsAllErrors()
        {
            // Arrange
            var options = new IpWhitelistOptions { RequireUserAgent = true };
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object, options);

            // Act
            var result = validator.ValidateRequest("203.0.113.1", null);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("IP address 203.0.113.1 is not allowed", result.Errors);
            Assert.Contains("User-Agent header is required", result.Errors);
        }

        [Fact]
        public void ValidateRequest_LogsSecurityViolations()
        {
            // Arrange
            var validator = new IpWhitelistValidator(_mockLogger.Object, _mockAuditLogger.Object);

            // Act
            validator.ValidateRequest("203.0.113.1", "sqlmap/1.0", "test-user");

            // Assert
            _mockAuditLogger.Verify(
                x => x.LogSecurityViolation(
                    "IP_NOT_WHITELISTED",
                    It.IsAny<string>(),
                    "test-user",
                    "203.0.113.1",
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);

            _mockAuditLogger.Verify(
                x => x.LogSecurityViolation(
                    "SUSPICIOUS_USER_AGENT",
                    It.IsAny<string>(),
                    "test-user",
                    "203.0.113.1",
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
    }

    /// <summary>
    /// Unit tests for the IpRange class.
    /// </summary>
    public class IpRangeTests
    {
        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.50", true)]
        [InlineData("192.168.1.0/24", "192.168.1.255", true)]
        [InlineData("192.168.1.0/24", "192.168.2.1", false)]
        [InlineData("10.0.0.0/8", "10.255.255.255", true)]
        [InlineData("10.0.0.0/8", "11.0.0.1", false)]
        [InlineData("172.16.0.0/12", "172.31.255.255", true)]
        [InlineData("172.16.0.0/12", "172.32.0.1", false)]
        public void Contains_WithVariousCidrRanges_ReturnsExpectedResult(string cidr, string testIp, bool expected)
        {
            // Arrange
            var range = IpRange.Parse(cidr);
            var ipAddress = System.Net.IPAddress.Parse(testIp);

            // Act
            var result = range.Contains(ipAddress);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("192.168.1.0/24")]
        [InlineData("10.0.0.0/8")]
        [InlineData("172.16.0.0/12")]
        [InlineData("203.0.113.0/24")]
        public void Parse_WithValidCidr_ReturnsIpRange(string cidr)
        {
            // Act
            var range = IpRange.Parse(cidr);

            // Assert
            Assert.NotNull(range);
            Assert.Equal(cidr, range.CidrNotation);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/")]
        [InlineData("192.168.1.0/abc")]
        [InlineData("invalid-ip/24")]
        public void Parse_WithInvalidCidr_ThrowsArgumentException(string cidr)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => IpRange.Parse(cidr));
        }

        [Theory]
        [InlineData("192.168.1.0/33")]
        [InlineData("10.0.0.0/129")]
        public void Parse_WithInvalidPrefixLength_ThrowsArgumentOutOfRangeException(string cidr)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => IpRange.Parse(cidr));
        }

        [Fact]
        public void Contains_WithNullIpAddress_ReturnsFalse()
        {
            // Arrange
            var range = IpRange.Parse("192.168.1.0/24");

            // Act
            var result = range.Contains(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Contains_WithDifferentAddressFamily_ReturnsFalse()
        {
            // Arrange
            var ipv4Range = IpRange.Parse("192.168.1.0/24");
            var ipv6Address = System.Net.IPAddress.Parse("2001:db8::1");

            // Act
            var result = ipv4Range.Contains(ipv6Address);

            // Assert
            Assert.False(result);
        }
    }

    /// <summary>
    /// Unit tests for the IpWhitelistOptions class.
    /// </summary>
    public class IpWhitelistOptionsTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            // Act
            var options = new IpWhitelistOptions();

            // Assert
            Assert.True(options.EnableWhitelist);
            Assert.False(options.RequireUserAgent);
            Assert.False(options.EnableRateLimiting);
            Assert.Equal(60, options.MaxRequestsPerMinute);
            Assert.Contains("127.0.0.1", options.DefaultAllowedIps);
            Assert.Contains("::1", options.DefaultAllowedIps);
            Assert.Contains("10.0.0.0/8", options.DefaultAllowedIps);
            Assert.Contains("172.16.0.0/12", options.DefaultAllowedIps);
            Assert.Contains("192.168.0.0/16", options.DefaultAllowedIps);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var options = new IpWhitelistOptions();

            // Act
            options.EnableWhitelist = false;
            options.RequireUserAgent = true;
            options.EnableRateLimiting = true;
            options.MaxRequestsPerMinute = 120;
            options.DefaultAllowedIps = new List<string> { "203.0.113.0/24" };

            // Assert
            Assert.False(options.EnableWhitelist);
            Assert.True(options.RequireUserAgent);
            Assert.True(options.EnableRateLimiting);
            Assert.Equal(120, options.MaxRequestsPerMinute);
            Assert.Single(options.DefaultAllowedIps);
            Assert.Contains("203.0.113.0/24", options.DefaultAllowedIps);
        }
    }
} 