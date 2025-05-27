using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Security;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates the Security Policy Enforcement Module functionality.
    /// </summary>
    public static class SecurityPolicyDemo
    {
        /// <summary>
        /// Runs the security policy enforcement demonstration.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static async Task RunAsync(ILogger logger)
        {
            logger.LogInformation("=== Security Policy Enforcement Module Demo ===");
            logger.LogInformation("");

            // Create audit logger
            var auditLogger = new SecurityAuditLogger(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SecurityAuditLogger>());

            // Create security policy options
            var policyOptions = new SecurityPolicyOptions
            {
                EnableDefaultPolicies = true,
                TlsOptions = new TlsEnforcementOptions
                {
                    IsEnabled = true,
                    MinimumTlsVersion = TlsVersion.Tls12,
                    RequireValidCertificate = true
                },
                ThrottlingOptions = new RequestThrottlingOptions
                {
                    IsEnabled = true,
                    MaxRequestsPerMinute = 10,
                    MaxRequestsPerHour = 100,
                    MaxConcurrentRequests = 3
                },
                TokenValidationOptions = new TokenValidationOptions
                {
                    IsEnabled = true,
                    ValidateApiKeyFormat = true,
                    ValidateJwtTokens = true,
                    MaxTokenAgeMinutes = 60
                }
            };

            // Create security policy engine
            var policyEngine = new SecurityPolicyEngine(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SecurityPolicyEngine>(),
                auditLogger,
                policyOptions);

            logger.LogInformation("✅ Security Policy Engine initialized with {PolicyCount} default policies",
                policyEngine.GetRegisteredPolicies().Count);

            // Demonstrate policy registration
            await DemonstratePolicyRegistration(logger, policyEngine);

            // Demonstrate request validation
            await DemonstrateRequestValidation(logger, policyEngine);

            // Demonstrate response validation
            await DemonstrateResponseValidation(logger, policyEngine);

            // Demonstrate violation handling
            await DemonstrateViolationHandling(logger, policyEngine);

            logger.LogInformation("");
            logger.LogInformation("✅ Security Policy Enforcement Module demonstration completed successfully!");
        }

        private static async Task DemonstratePolicyRegistration(ILogger logger, SecurityPolicyEngine policyEngine)
        {
            logger.LogInformation("");
            logger.LogInformation("--- Policy Registration Demo ---");

            // Show registered policies
            var policies = policyEngine.GetRegisteredPolicies();
            logger.LogInformation("📋 Currently registered policies:");
            foreach (var policy in policies)
            {
                logger.LogInformation("  • {PolicyName} (Priority: {Priority}, Enabled: {IsEnabled})",
                    policy.Name, policy.Priority, policy.IsEnabled);
            }

            // Create a custom policy
            var customPolicy = new CustomSecurityPolicy();
            policyEngine.RegisterPolicy(customPolicy);

            logger.LogInformation("✅ Registered custom security policy: {PolicyName}", customPolicy.Name);

            // Show updated policy list
            var updatedPolicies = policyEngine.GetRegisteredPolicies();
            logger.LogInformation("📋 Updated policy count: {PolicyCount}", updatedPolicies.Count);

            // Unregister the custom policy
            policyEngine.UnregisterPolicy(customPolicy.Name);
            logger.LogInformation("🗑️ Unregistered custom policy: {PolicyName}", customPolicy.Name);
        }

        private static async Task DemonstrateRequestValidation(ILogger logger, SecurityPolicyEngine policyEngine)
        {
            logger.LogInformation("");
            logger.LogInformation("--- Request Validation Demo ---");

            // Create test requests
            var validRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.prophy.ai/test");
            validRequest.Headers.Add("X-ApiKey", "pk_live_1234567890abcdef1234567890abcdef");
            validRequest.Headers.Add("User-Agent", "Prophy.ApiClient/1.0.0");

            var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "http://api.prophy.ai/test"); // HTTP instead of HTTPS
            invalidRequest.Headers.Add("X-ApiKey", "test123"); // Invalid API key format

            var securityContext = new SecurityContext
            {
                UserIdentity = "demo-user",
                ClientIpAddress = "192.168.1.100",
                OrganizationCode = "demo-org",
                ApiKey = "pk_live_1234567890abcdef1234567890abcdef",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Test valid request
            logger.LogInformation("🔍 Testing valid HTTPS request...");
            var validResult = await policyEngine.EnforceRequestPoliciesAsync(validRequest, securityContext);
            logger.LogInformation("✅ Valid request result: {IsAllowed} (Violations: {ViolationCount})",
                validResult.IsAllowed, validResult.Violations.Count);

            // Test invalid request
            logger.LogInformation("🔍 Testing invalid HTTP request...");
            var invalidResult = await policyEngine.EnforceRequestPoliciesAsync(invalidRequest, securityContext);
            logger.LogInformation("❌ Invalid request result: {IsAllowed} (Violations: {ViolationCount})",
                invalidResult.IsAllowed, invalidResult.Violations.Count);

            if (invalidResult.Violations.Count > 0)
            {
                logger.LogInformation("📋 Policy violations found:");
                foreach (var violation in invalidResult.Violations)
                {
                    logger.LogInformation("  • {PolicyName}: {Code} - {Message} (Severity: {Severity})",
                        violation.PolicyName, violation.Code, violation.Message, violation.Severity);
                }
            }
        }

        private static async Task DemonstrateResponseValidation(ILogger logger, SecurityPolicyEngine policyEngine)
        {
            logger.LogInformation("");
            logger.LogInformation("--- Response Validation Demo ---");

            // Create test responses
            var validResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            validResponse.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            validResponse.Headers.Add("X-Content-Type-Options", "nosniff");

            var invalidResponse = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            // Missing WWW-Authenticate header

            var securityContext = new SecurityContext
            {
                UserIdentity = "demo-user",
                ClientIpAddress = "192.168.1.100",
                OrganizationCode = "demo-org",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Test valid response
            logger.LogInformation("🔍 Testing valid response with security headers...");
            var validResult = await policyEngine.EnforceResponsePoliciesAsync(validResponse, securityContext);
            logger.LogInformation("✅ Valid response result: {IsAllowed} (Violations: {ViolationCount})",
                validResult.IsAllowed, validResult.Violations.Count);

            // Test invalid response
            logger.LogInformation("🔍 Testing 401 response without WWW-Authenticate header...");
            var invalidResult = await policyEngine.EnforceResponsePoliciesAsync(invalidResponse, securityContext);
            logger.LogInformation("⚠️ Response validation result: {IsAllowed} (Violations: {ViolationCount})",
                invalidResult.IsAllowed, invalidResult.Violations.Count);

            if (invalidResult.Violations.Count > 0)
            {
                logger.LogInformation("📋 Response policy violations found:");
                foreach (var violation in invalidResult.Violations)
                {
                    logger.LogInformation("  • {PolicyName}: {Code} - {Message} (Severity: {Severity})",
                        violation.PolicyName, violation.Code, violation.Message, violation.Severity);
                }
            }
        }

        private static async Task DemonstrateViolationHandling(ILogger logger, SecurityPolicyEngine policyEngine)
        {
            logger.LogInformation("");
            logger.LogInformation("--- Violation Handling Demo ---");

            // Create a test violation
            var violation = new PolicyViolation(
                "Demo Policy",
                PolicyViolationSeverity.Warning,
                "DEMO_VIOLATION",
                "This is a demonstration violation for testing purposes",
                new Dictionary<string, object>
                {
                    ["DemoData"] = "Sample violation data",
                    ["Timestamp"] = DateTimeOffset.UtcNow
                });

            var securityContext = new SecurityContext
            {
                UserIdentity = "demo-user",
                ClientIpAddress = "192.168.1.100",
                OrganizationCode = "demo-org",
                CorrelationId = Guid.NewGuid().ToString()
            };

            logger.LogInformation("🚨 Handling demonstration violation...");
            await policyEngine.HandleViolationAsync(violation, securityContext);
            logger.LogInformation("✅ Violation handled successfully");

            // Demonstrate different severity levels
            var severityLevels = new[]
            {
                PolicyViolationSeverity.Info,
                PolicyViolationSeverity.Warning,
                PolicyViolationSeverity.Error,
                PolicyViolationSeverity.Critical
            };

            logger.LogInformation("📊 Demonstrating different violation severity levels:");
            foreach (var severity in severityLevels)
            {
                var testViolation = new PolicyViolation(
                    "Demo Policy",
                    severity,
                    $"DEMO_{severity.ToString().ToUpper()}",
                    $"Demonstration {severity} violation",
                    new Dictionary<string, object> { ["Severity"] = severity.ToString() });

                logger.LogInformation("  • {Severity}: {Code} - {Message}",
                    severity, testViolation.Code, testViolation.Message);
            }
        }
    }

    /// <summary>
    /// Custom security policy for demonstration purposes.
    /// </summary>
    internal class CustomSecurityPolicy : ISecurityPolicy
    {
        public string Name => "Custom Demo Policy";
        public int Priority => 50;
        public bool IsEnabled => true;

        public Task<PolicyValidationResult> ValidateRequestAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            // Simple validation: check for custom header
            var hasCustomHeader = request.Headers.Contains("X-Custom-Demo");
            
            if (!hasCustomHeader)
            {
                var violation = new PolicyViolation(
                    Name,
                    PolicyViolationSeverity.Info,
                    "MISSING_CUSTOM_HEADER",
                    "Custom demo header is missing",
                    new Dictionary<string, object>
                    {
                        ["ExpectedHeader"] = "X-Custom-Demo",
                        ["Recommendation"] = "Add X-Custom-Demo header for demonstration"
                    });

                return Task.FromResult(PolicyValidationResult.Failure(violation));
            }

            return Task.FromResult(PolicyValidationResult.Success());
        }

        public Task<PolicyValidationResult> ValidateResponseAsync(
            HttpResponseMessage response, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            // Always pass response validation for demo
            return Task.FromResult(PolicyValidationResult.Success());
        }

        public Task HandleViolationAsync(
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            // Simple logging for demo
            Console.WriteLine($"Custom policy violation handled: {violation.Code}");
            return Task.CompletedTask;
        }
    }
} 