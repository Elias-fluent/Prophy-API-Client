using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Security;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates IP whitelisting and request validation functionality.
    /// </summary>
    public static class IpWhitelistDemo
    {
        /// <summary>
        /// Runs the IP whitelist demonstration.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static async Task RunAsync(ILogger logger)
        {
            logger.LogInformation("=== IP Whitelisting and Request Validation Demo ===");
            logger.LogInformation("");

            // Create audit logger
            var auditLogger = new SecurityAuditLogger(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SecurityAuditLogger>());

            // Demo 1: Basic IP whitelisting
            await DemoBasicIpWhitelisting(logger, auditLogger);
            logger.LogInformation("");

            // Demo 2: CIDR range support
            await DemoCidrRangeSupport(logger, auditLogger);
            logger.LogInformation("");

            // Demo 3: Request validation
            await DemoRequestValidation(logger, auditLogger);
            logger.LogInformation("");

            // Demo 4: Security violation detection
            await DemoSecurityViolationDetection(logger, auditLogger);
            logger.LogInformation("");

            logger.LogInformation("✅ IP whitelisting demonstration completed successfully!");
        }

        private static async Task DemoBasicIpWhitelisting(ILogger logger, ISecurityAuditLogger auditLogger)
        {
            logger.LogInformation("1. BASIC IP WHITELISTING");
            logger.LogInformation("========================");

            // Create IP whitelist validator with custom options
            var options = new IpWhitelistOptions
            {
                EnableWhitelist = true,
                DefaultAllowedIps = new System.Collections.Generic.List<string>
                {
                    "127.0.0.1",
                    "192.168.1.100",
                    "10.0.0.0/8"
                }
            };

            var validator = new IpWhitelistValidator(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<IpWhitelistValidator>(),
                auditLogger,
                options);

            // Test allowed IPs
            logger.LogInformation("🔍 Testing allowed IP addresses:");
            
            var testIps = new[]
            {
                ("127.0.0.1", "Localhost"),
                ("192.168.1.100", "Specific allowed IP"),
                ("10.5.10.20", "IP in allowed CIDR range"),
                ("203.0.113.1", "External IP (should be blocked)"),
                ("invalid-ip", "Invalid IP format")
            };

            foreach (var (ip, description) in testIps)
            {
                var isAllowed = validator.IsIpAllowed(ip, "demo-user");
                var status = isAllowed ? "✅ ALLOWED" : "❌ BLOCKED";
                logger.LogInformation("  {Status}: {IpAddress} - {Description}", status, ip, description);
            }

            // Show current whitelist
            logger.LogInformation("");
            logger.LogInformation("📋 Current whitelist entries:");
            foreach (var allowedIp in validator.GetAllowedIps())
            {
                logger.LogInformation("  - {AllowedIp}", allowedIp);
            }

            await Task.Delay(100); // Small delay for demo purposes
        }

        private static async Task DemoCidrRangeSupport(ILogger logger, ISecurityAuditLogger auditLogger)
        {
            logger.LogInformation("2. CIDR RANGE SUPPORT");
            logger.LogInformation("====================");

            var validator = new IpWhitelistValidator(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<IpWhitelistValidator>(),
                auditLogger,
                new IpWhitelistOptions { DefaultAllowedIps = new System.Collections.Generic.List<string>() });

            // Add various CIDR ranges
            logger.LogInformation("🔧 Adding CIDR ranges to whitelist:");
            
            var cidrRanges = new[]
            {
                "192.168.0.0/16",   // Private network
                "172.16.0.0/12",    // Private network
                "203.0.113.0/24",   // Test network
                "2001:db8::/32"     // IPv6 test network
            };

            foreach (var cidr in cidrRanges)
            {
                try
                {
                    validator.AddAllowedIp(cidr);
                    logger.LogInformation("  ✅ Added: {CidrRange}", cidr);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("  ❌ Failed to add {CidrRange}: {Error}", cidr, ex.Message);
                }
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 Testing IPs against CIDR ranges:");

            var testCidrIps = new[]
            {
                ("192.168.1.50", "Should match 192.168.0.0/16"),
                ("172.16.255.1", "Should match 172.16.0.0/12"),
                ("203.0.113.100", "Should match 203.0.113.0/24"),
                ("203.0.114.1", "Should NOT match any range"),
                ("8.8.8.8", "Public DNS (should be blocked)")
            };

            foreach (var (ip, description) in testCidrIps)
            {
                var isAllowed = validator.IsIpAllowed(ip, "cidr-test-user");
                var status = isAllowed ? "✅ ALLOWED" : "❌ BLOCKED";
                logger.LogInformation("  {Status}: {IpAddress} - {Description}", status, ip, description);
            }

            await Task.Delay(100);
        }

        private static async Task DemoRequestValidation(ILogger logger, ISecurityAuditLogger auditLogger)
        {
            logger.LogInformation("3. REQUEST VALIDATION");
            logger.LogInformation("====================");

            // Create validator with strict options
            var options = new IpWhitelistOptions
            {
                EnableWhitelist = true,
                RequireUserAgent = true,
                EnableRateLimiting = false, // Disabled for demo
                DefaultAllowedIps = new System.Collections.Generic.List<string>
                {
                    "127.0.0.1",
                    "192.168.1.0/24"
                }
            };

            var validator = new IpWhitelistValidator(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<IpWhitelistValidator>(),
                auditLogger,
                options);

            logger.LogInformation("🔍 Testing request validation scenarios:");

            var testRequests = new[]
            {
                ("127.0.0.1", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Valid request"),
                ("127.0.0.1", null, "Missing User-Agent"),
                ("127.0.0.1", "sqlmap/1.0", "Suspicious User-Agent"),
                ("192.168.1.50", "Chrome/91.0", "Valid IP and User-Agent"),
                ("203.0.113.1", "Mozilla/5.0", "Blocked IP"),
                ("192.168.1.100", "nmap scanner", "Allowed IP but suspicious User-Agent")
            };

            foreach (var (ip, userAgent, description) in testRequests)
            {
                var result = validator.ValidateRequest(ip, userAgent, "validation-test-user");
                var status = result.IsValid ? "✅ VALID" : "❌ INVALID";
                
                logger.LogInformation("  {Status}: {Description}", status, description);
                logger.LogInformation("    IP: {IpAddress}, User-Agent: {UserAgent}", ip, userAgent ?? "null");
                
                if (!result.IsValid)
                {
                    logger.LogInformation("    Errors: {Errors}", string.Join(", ", result.Errors));
                }
                
                logger.LogInformation("");
            }

            await Task.Delay(100);
        }

        private static async Task DemoSecurityViolationDetection(ILogger logger, ISecurityAuditLogger auditLogger)
        {
            logger.LogInformation("4. SECURITY VIOLATION DETECTION");
            logger.LogInformation("===============================");

            var validator = new IpWhitelistValidator(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<IpWhitelistValidator>(),
                auditLogger,
                new IpWhitelistOptions
                {
                    EnableWhitelist = true,
                    RequireUserAgent = true,
                    DefaultAllowedIps = new System.Collections.Generic.List<string> { "127.0.0.1" }
                });

            logger.LogInformation("🚨 Simulating security violations (these will be logged):");

            // Simulate various security violations
            var violations = new[]
            {
                ("10.0.0.1", "Burp Suite Professional", "Penetration testing tool"),
                ("203.0.113.50", "nikto/2.1.6", "Web vulnerability scanner"),
                ("192.168.1.200", "sqlmap/1.4.12", "SQL injection tool"),
                ("", "Mozilla/5.0", "Empty IP address"),
                ("invalid.ip.format", "Chrome/91.0", "Invalid IP format"),
                ("172.16.0.100", null, "Missing User-Agent header")
            };

            foreach (var (ip, userAgent, description) in violations)
            {
                logger.LogInformation("  🔍 Testing: {Description}", description);
                
                try
                {
                    var result = validator.ValidateRequest(ip, userAgent, "security-test-user");
                    var status = result.IsValid ? "✅ PASSED" : "🚨 VIOLATION DETECTED";
                    logger.LogInformation("    Result: {Status}", status);
                    
                    if (!result.IsValid)
                    {
                        logger.LogInformation("    Violations: {Errors}", string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("    ❌ Exception: {Error}", ex.Message);
                }
                
                logger.LogInformation("");
            }

            // Demonstrate whitelist management
            logger.LogInformation("🔧 Demonstrating whitelist management:");
            
            logger.LogInformation("  📋 Current whitelist: {Count} entries", validator.GetAllowedIps().Count());
            
            // Add a new IP
            validator.AddAllowedIp("203.0.113.100");
            logger.LogInformation("  ➕ Added IP: 203.0.113.100");
            
            // Test the newly added IP
            var newIpResult = validator.IsIpAllowed("203.0.113.100", "whitelist-test-user");
            logger.LogInformation("  🔍 Testing newly added IP: {Result}", newIpResult ? "✅ ALLOWED" : "❌ BLOCKED");
            
            // Remove the IP
            validator.RemoveAllowedIp("203.0.113.100");
            logger.LogInformation("  ➖ Removed IP: 203.0.113.100");
            
            // Test the removed IP
            var removedIpResult = validator.IsIpAllowed("203.0.113.100", "whitelist-test-user");
            logger.LogInformation("  🔍 Testing removed IP: {Result}", removedIpResult ? "✅ ALLOWED" : "❌ BLOCKED");

            await Task.Delay(100);
        }
    }
} 