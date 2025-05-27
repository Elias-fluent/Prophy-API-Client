using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Security;
using Prophy.ApiClient.Security.Providers;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates the comprehensive security features of the Prophy API Client.
    /// </summary>
    public class SecurityDemo
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityDemo> _logger;

        public SecurityDemo()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<SecurityDemo>>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Register security services
            services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();
            services.AddSingleton<SecurityAuditOptions>();

            // Register secure configuration providers
            services.AddSingleton<ISecureConfigurationProvider>(provider =>
                new InMemorySecureConfigurationProvider(new Dictionary<string, string>
                {
                    ["ApiKey"] = "demo-api-key-12345678901234567890",
                    ["OrganizationCode"] = "demo-org",
                    ["DatabaseConnectionString"] = "Server=localhost;Database=demo;Integrated Security=true;",
                    ["EncryptionKey"] = "demo-encryption-key-32-chars-long"
                }));

            services.AddSingleton<ISecureConfigurationManager, SecureConfigurationManager>();
        }

        /// <summary>
        /// Runs the complete security demonstration.
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("=== Prophy API Client Security Features Demo ===\n");

            await DemonstrateInputValidation();
            await DemonstrateSecureConfiguration();
            await DemonstrateAuditLogging();
            await DemonstrateSecurityViolationDetection();

            Console.WriteLine("\n=== Security Demo Complete ===");
        }

        private async Task DemonstrateInputValidation()
        {
            Console.WriteLine("1. INPUT VALIDATION & SANITIZATION");
            Console.WriteLine("=====================================");

            // Email validation
            Console.WriteLine("\n📧 Email Validation:");
            await TestEmailValidation("user@example.com", "Valid email");
            await TestEmailValidation("invalid-email", "Invalid email");
            await TestEmailValidation("<script>alert('xss')</script>@evil.com", "Malicious email");

            // Organization code validation
            Console.WriteLine("\n🏢 Organization Code Validation:");
            await TestOrganizationValidation("valid-org-123", "Valid organization code");
            await TestOrganizationValidation("org with spaces", "Invalid organization code");
            await TestOrganizationValidation("a", "Too short organization code");

            // API key validation
            Console.WriteLine("\n🔑 API Key Validation:");
            await TestApiKeyValidation("valid-api-key-1234567890123456", "Valid API key");
            await TestApiKeyValidation("short", "Too short API key");
            await TestApiKeyValidation("key-with-@-symbols", "Invalid API key characters");

            // URL validation
            Console.WriteLine("\n🌐 URL Validation:");
            await TestUrlValidation("https://api.example.com/v1", "Valid HTTPS URL");
            await TestUrlValidation("javascript:alert('xss')", "Malicious JavaScript URL");
            await TestUrlValidation("ftp://files.example.com", "Invalid scheme");

            // Safe string validation
            Console.WriteLine("\n🛡️ Safe String Validation:");
            await TestSafeStringValidation("Normal text 123", "Safe text");
            await TestSafeStringValidation("<script>alert('hack')</script>", "XSS attempt");
            await TestSafeStringValidation("'; DROP TABLE users; --", "SQL injection attempt");

            Console.WriteLine("\n✅ Input validation demonstration complete.\n");
        }

        private async Task DemonstrateSecureConfiguration()
        {
            Console.WriteLine("2. SECURE CONFIGURATION MANAGEMENT");
            Console.WriteLine("===================================");

            var configManager = _serviceProvider.GetRequiredService<ISecureConfigurationManager>();

            // Test connection
            Console.WriteLine("\n🔗 Testing provider connections...");
            var connectionResult = await configManager.TestConnectionAsync();
            Console.WriteLine($"Connection test result: {(connectionResult ? "✅ Success" : "❌ Failed")}");

            // Retrieve individual secrets
            Console.WriteLine("\n🔐 Retrieving individual secrets:");
            await RetrieveSecret(configManager, "ApiKey");
            await RetrieveSecret(configManager, "OrganizationCode");
            await RetrieveSecret(configManager, "NonExistentSecret");

            // Retrieve multiple secrets
            Console.WriteLine("\n📦 Retrieving multiple secrets:");
            var secretNames = new[] { "ApiKey", "OrganizationCode", "DatabaseConnectionString", "MissingSecret" };
            var secrets = await configManager.GetSecretsAsync(secretNames);
            
            Console.WriteLine($"Requested {secretNames.Length} secrets, retrieved {secrets.Count}:");
            foreach (var secret in secrets)
            {
                Console.WriteLine($"  ✅ {secret.Key}: {MaskSecret(secret.Value)}");
            }

            // Store a new secret
            Console.WriteLine("\n💾 Storing new secret:");
            await configManager.SetSecretAsync("NewSecret", "new-secret-value-123");
            var retrievedNewSecret = await configManager.GetSecretAsync("NewSecret");
            Console.WriteLine($"Stored and retrieved new secret: {MaskSecret(retrievedNewSecret)}");

            // List available providers
            Console.WriteLine("\n🏪 Available providers:");
            var providers = configManager.GetAvailableProviders();
            foreach (var provider in providers)
            {
                Console.WriteLine($"  ✅ {provider}");
            }

            Console.WriteLine("\n✅ Secure configuration demonstration complete.\n");
        }

        private async Task DemonstrateAuditLogging()
        {
            Console.WriteLine("3. SECURITY AUDIT LOGGING");
            Console.WriteLine("==========================");

            var auditLogger = _serviceProvider.GetRequiredService<ISecurityAuditLogger>();

            Console.WriteLine("\n📝 Logging various security events:");

            // Authentication events
            Console.WriteLine("\n🔐 Authentication Events:");
            auditLogger.LogAuthenticationAttempt("demo-org", "user@example.com", true, "192.168.1.100", "Mozilla/5.0");
            Console.WriteLine("  ✅ Successful authentication logged");

            auditLogger.LogAuthenticationAttempt("demo-org", "hacker@evil.com", false, "10.0.0.1", "curl/7.68.0");
            Console.WriteLine("  ❌ Failed authentication logged");

            // Configuration access
            Console.WriteLine("\n⚙️ Configuration Access Events:");
            auditLogger.LogConfigurationAccess("ApiKey", "Read", "admin@example.com");
            Console.WriteLine("  📖 Configuration read logged");

            auditLogger.LogConfigurationAccess("DatabasePassword", "Write", "admin@example.com");
            Console.WriteLine("  ✏️ Configuration write logged");

            // Secret access
            Console.WriteLine("\n🔒 Secret Access Events:");
            auditLogger.LogSecretAccess("ApiKey", "Read", "service-account", true, "InMemory");
            Console.WriteLine("  🔍 Secret read logged");

            auditLogger.LogSecretAccess("EncryptionKey", "Write", "admin@example.com", true, "InMemory");
            Console.WriteLine("  💾 Secret write logged");

            // API access
            Console.WriteLine("\n🌐 API Access Events:");
            auditLogger.LogApiAccess("/api/v1/users", "GET", 200, "user@example.com", "192.168.1.100", TimeSpan.FromMilliseconds(150));
            Console.WriteLine("  ✅ Successful API access logged");

            auditLogger.LogApiAccess("/api/v1/admin", "POST", 403, "user@example.com", "192.168.1.100", TimeSpan.FromMilliseconds(50));
            Console.WriteLine("  🚫 Forbidden API access logged");

            // Permission checks
            Console.WriteLine("\n🛡️ Permission Check Events:");
            auditLogger.LogPermissionCheck("admin.users.read", "user:123", true, "admin@example.com");
            Console.WriteLine("  ✅ Permission granted logged");

            auditLogger.LogPermissionCheck("admin.system.delete", "system", false, "user@example.com");
            Console.WriteLine("  ❌ Permission denied logged");

            // Data access
            Console.WriteLine("\n📊 Data Access Events:");
            auditLogger.LogDataAccess("User", "user:123", "Read", "service@example.com");
            Console.WriteLine("  📖 Data read logged");

            auditLogger.LogDataAccess("PaymentInfo", "payment:456", "Update", "admin@example.com");
            Console.WriteLine("  ✏️ Data update logged");

            Console.WriteLine("\n✅ Audit logging demonstration complete.\n");
        }

        private async Task DemonstrateSecurityViolationDetection()
        {
            Console.WriteLine("4. SECURITY VIOLATION DETECTION");
            Console.WriteLine("================================");

            var auditLogger = _serviceProvider.GetRequiredService<ISecurityAuditLogger>();

            Console.WriteLine("\n🚨 Detecting and logging security violations:");

            // XSS attempt
            Console.WriteLine("\n🕷️ Cross-Site Scripting (XSS) Attempts:");
            var xssAttempt = "<script>alert('XSS')</script>";
            if (InputValidator.ContainsDangerousPatterns(xssAttempt))
            {
                auditLogger.LogSecurityViolation("XSS_ATTEMPT", "Malicious script detected in user input", 
                    "attacker@evil.com", "10.0.0.1", new Dictionary<string, object>
                    {
                        ["InputValue"] = InputValidator.SanitizeInput(xssAttempt),
                        ["DetectionMethod"] = "Pattern matching"
                    });
                Console.WriteLine("  🚨 XSS attempt detected and logged");
            }

            // SQL injection attempt
            Console.WriteLine("\n💉 SQL Injection Attempts:");
            var sqlInjection = "'; DROP TABLE users; --";
            if (InputValidator.ContainsDangerousPatterns(sqlInjection))
            {
                auditLogger.LogSecurityViolation("SQL_INJECTION", "SQL injection pattern detected", 
                    "attacker@evil.com", "10.0.0.1", new Dictionary<string, object>
                    {
                        ["InputValue"] = InputValidator.SanitizeInput(sqlInjection),
                        ["DetectionMethod"] = "Pattern matching"
                    });
                Console.WriteLine("  🚨 SQL injection attempt detected and logged");
            }

            // Path traversal attempt
            Console.WriteLine("\n📁 Path Traversal Attempts:");
            var pathTraversal = "../../../etc/passwd";
            if (InputValidator.ContainsDangerousPatterns(pathTraversal))
            {
                auditLogger.LogSecurityViolation("PATH_TRAVERSAL", "Directory traversal attempt detected", 
                    "attacker@evil.com", "10.0.0.1", new Dictionary<string, object>
                    {
                        ["InputValue"] = pathTraversal,
                        ["DetectionMethod"] = "Pattern matching"
                    });
                Console.WriteLine("  🚨 Path traversal attempt detected and logged");
            }

            // Multiple failed authentication attempts
            Console.WriteLine("\n🔐 Brute Force Detection:");
            for (int i = 1; i <= 5; i++)
            {
                auditLogger.LogAuthenticationAttempt("demo-org", "victim@example.com", false, "10.0.0.1", "curl/7.68.0");
            }
            
            auditLogger.LogSecurityViolation("BRUTE_FORCE", "Multiple failed authentication attempts detected", 
                "victim@example.com", "10.0.0.1", new Dictionary<string, object>
                {
                    ["FailedAttempts"] = 5,
                    ["TimeWindow"] = "5 minutes",
                    ["DetectionMethod"] = "Rate limiting"
                });
            Console.WriteLine("  🚨 Brute force attack detected and logged");

            // Invalid API key usage
            Console.WriteLine("\n🔑 Invalid API Key Usage:");
            auditLogger.LogSecurityViolation("INVALID_API_KEY", "Repeated use of invalid API key", 
                null, "10.0.0.1", new Dictionary<string, object>
                {
                    ["ApiKeyPrefix"] = "invalid_key_***",
                    ["Attempts"] = 10,
                    ["DetectionMethod"] = "API key validation"
                });
            Console.WriteLine("  🚨 Invalid API key usage detected and logged");

            Console.WriteLine("\n✅ Security violation detection demonstration complete.\n");
        }

        private async Task TestEmailValidation(string email, string description)
        {
            var result = InputValidator.ValidateEmail(email);
            var status = result.IsValid ? "✅ Valid" : "❌ Invalid";
            var sanitized = InputValidator.SanitizeInput(email);
            
            Console.WriteLine($"  {description}: {status}");
            Console.WriteLine($"    Input: '{email}'");
            Console.WriteLine($"    Sanitized: '{sanitized}'");
            if (!result.IsValid)
            {
                Console.WriteLine($"    Errors: {string.Join(", ", result.Errors)}");
            }
            await Task.Delay(100); // Simulate processing time
        }

        private async Task TestOrganizationValidation(string orgCode, string description)
        {
            var result = InputValidator.ValidateOrganizationCode(orgCode);
            var status = result.IsValid ? "✅ Valid" : "❌ Invalid";
            
            Console.WriteLine($"  {description}: {status}");
            Console.WriteLine($"    Input: '{orgCode}'");
            if (!result.IsValid)
            {
                Console.WriteLine($"    Errors: {string.Join(", ", result.Errors)}");
            }
            await Task.Delay(100);
        }

        private async Task TestApiKeyValidation(string apiKey, string description)
        {
            var result = InputValidator.ValidateApiKey(apiKey);
            var status = result.IsValid ? "✅ Valid" : "❌ Invalid";
            
            Console.WriteLine($"  {description}: {status}");
            Console.WriteLine($"    Input: '{MaskSecret(apiKey)}'");
            if (!result.IsValid)
            {
                Console.WriteLine($"    Errors: {string.Join(", ", result.Errors)}");
            }
            await Task.Delay(100);
        }

        private async Task TestUrlValidation(string url, string description)
        {
            var result = InputValidator.ValidateUrl(url);
            var status = result.IsValid ? "✅ Valid" : "❌ Invalid";
            
            Console.WriteLine($"  {description}: {status}");
            Console.WriteLine($"    Input: '{url}'");
            if (!result.IsValid)
            {
                Console.WriteLine($"    Errors: {string.Join(", ", result.Errors)}");
            }
            await Task.Delay(100);
        }

        private async Task TestSafeStringValidation(string input, string description)
        {
            var result = InputValidator.ValidateSafeString(input);
            var status = result.IsValid ? "✅ Safe" : "🚨 Dangerous";
            var sanitized = InputValidator.SanitizeInput(input);
            var hasDangerousPatterns = InputValidator.ContainsDangerousPatterns(input);
            
            Console.WriteLine($"  {description}: {status}");
            Console.WriteLine($"    Input: '{input}'");
            Console.WriteLine($"    Sanitized: '{sanitized}'");
            Console.WriteLine($"    Contains dangerous patterns: {(hasDangerousPatterns ? "🚨 Yes" : "✅ No")}");
            if (!result.IsValid)
            {
                Console.WriteLine($"    Errors: {string.Join(", ", result.Errors)}");
            }
            await Task.Delay(100);
        }

        private async Task RetrieveSecret(ISecureConfigurationManager configManager, string secretName)
        {
            try
            {
                var secret = await configManager.GetSecretAsync(secretName);
                if (secret != null)
                {
                    Console.WriteLine($"  ✅ {secretName}: {MaskSecret(secret)}");
                }
                else
                {
                    Console.WriteLine($"  ❌ {secretName}: Not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  🚨 {secretName}: Error - {ex.Message}");
            }
        }

        private string MaskSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "null";
            
            if (secret.Length <= 8)
                return "***";
            
            return secret.Substring(0, 4) + "***" + secret.Substring(secret.Length - 4);
        }

        /// <summary>
        /// Entry point for running the security demo.
        /// </summary>
        public static async Task RunSecurityDemoAsync()
        {
            try
            {
                var demo = new SecurityDemo();
                await demo.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 