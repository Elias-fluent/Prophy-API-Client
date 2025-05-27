using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Manages and enforces security policies for API requests and responses.
    /// </summary>
    public class SecurityPolicyEngine : ISecurityPolicyEngine
    {
        private readonly ILogger<SecurityPolicyEngine> _logger;
        private readonly ISecurityAuditLogger _auditLogger;
        private readonly SecurityPolicyOptions _options;
        private readonly List<ISecurityPolicy> _policies;
        private readonly object _policiesLock = new object();

        /// <summary>
        /// Initializes a new instance of the SecurityPolicyEngine class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="auditLogger">The security audit logger.</param>
        /// <param name="options">The security policy options.</param>
        public SecurityPolicyEngine(
            ILogger<SecurityPolicyEngine> logger,
            ISecurityAuditLogger auditLogger,
            SecurityPolicyOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _options = options ?? new SecurityPolicyOptions();
            _policies = new List<ISecurityPolicy>();

            InitializeDefaultPolicies();
        }

        /// <inheritdoc />
        public void RegisterPolicy(ISecurityPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            lock (_policiesLock)
            {
                // Remove existing policy with the same name
                _policies.RemoveAll(p => p.Name.Equals(policy.Name, StringComparison.OrdinalIgnoreCase));
                
                // Add new policy and sort by priority
                _policies.Add(policy);
                _policies.Sort((p1, p2) => p2.Priority.CompareTo(p1.Priority));
            }

            _logger.LogInformation("Registered security policy: {PolicyName} with priority {Priority}", 
                policy.Name, policy.Priority);
        }

        /// <inheritdoc />
        public void UnregisterPolicy(string policyName)
        {
            if (string.IsNullOrWhiteSpace(policyName))
                throw new ArgumentException("Policy name cannot be null or empty", nameof(policyName));

            lock (_policiesLock)
            {
                var removed = _policies.RemoveAll(p => p.Name.Equals(policyName, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                {
                    _logger.LogInformation("Unregistered security policy: {PolicyName}", policyName);
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<ISecurityPolicy> GetRegisteredPolicies()
        {
            lock (_policiesLock)
            {
                return _policies.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public async Task<PolicyEnforcementResult> EnforceRequestPoliciesAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var violations = new List<PolicyViolation>();
            var metadata = new Dictionary<string, object>();
            var enabledPolicies = GetEnabledPolicies();

            _logger.LogDebug("Enforcing {PolicyCount} request policies for {RequestUri}", 
                enabledPolicies.Count, request.RequestUri);

            foreach (var policy in enabledPolicies)
            {
                try
                {
                    var validationResult = await policy.ValidateRequestAsync(request, context, cancellationToken);
                    
                    if (!validationResult.IsValid)
                    {
                        violations.AddRange(validationResult.Violations);
                        
                        // Handle violations based on severity
                        foreach (var violation in validationResult.Violations)
                        {
                            await HandlePolicyViolationAsync(policy, violation, context, cancellationToken);
                        }
                    }

                    // Merge metadata
                    foreach (var kvp in validationResult.Metadata)
                    {
                        metadata[$"{policy.Name}.{kvp.Key}"] = kvp.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing security policy {PolicyName}", policy.Name);
                    
                    var violation = new PolicyViolation(
                        policy.Name,
                        PolicyViolationSeverity.Error,
                        "POLICY_EXECUTION_ERROR",
                        $"Failed to execute policy: {ex.Message}",
                        new Dictionary<string, object> { ["Exception"] = ex.ToString() });
                    
                    violations.Add(violation);
                    await HandlePolicyViolationAsync(policy, violation, context, cancellationToken);
                }
            }

            var hasBlockingViolations = violations.Any(v => 
                v.Severity == PolicyViolationSeverity.Error || 
                v.Severity == PolicyViolationSeverity.Critical);

            var result = new PolicyEnforcementResult(
                !hasBlockingViolations,
                violations.AsReadOnly(),
                metadata);

            if (hasBlockingViolations)
            {
                _logger.LogWarning("Request blocked due to {ViolationCount} security policy violations", 
                    violations.Count(v => v.Severity >= PolicyViolationSeverity.Error));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<PolicyEnforcementResult> EnforceResponsePoliciesAsync(
            HttpResponseMessage response, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var violations = new List<PolicyViolation>();
            var metadata = new Dictionary<string, object>();
            var enabledPolicies = GetEnabledPolicies();

            _logger.LogDebug("Enforcing {PolicyCount} response policies for {StatusCode}", 
                enabledPolicies.Count, response.StatusCode);

            foreach (var policy in enabledPolicies)
            {
                try
                {
                    var result = await policy.ValidateResponseAsync(response, context, cancellationToken);
                    
                    if (!result.IsValid)
                    {
                        violations.AddRange(result.Violations);
                        
                        // Handle violations based on severity
                        foreach (var violation in result.Violations)
                        {
                            await HandlePolicyViolationAsync(policy, violation, context, cancellationToken);
                        }
                    }

                    // Merge metadata
                    foreach (var kvp in result.Metadata)
                    {
                        metadata[$"{policy.Name}.{kvp.Key}"] = kvp.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing security policy {PolicyName}", policy.Name);
                    
                    var violation = new PolicyViolation(
                        policy.Name,
                        PolicyViolationSeverity.Error,
                        "POLICY_EXECUTION_ERROR",
                        $"Failed to execute policy: {ex.Message}",
                        new Dictionary<string, object> { ["Exception"] = ex.ToString() });
                    
                    violations.Add(violation);
                    await HandlePolicyViolationAsync(policy, violation, context, cancellationToken);
                }
            }

            var hasBlockingViolations = violations.Any(v => 
                v.Severity == PolicyViolationSeverity.Error || 
                v.Severity == PolicyViolationSeverity.Critical);

            return new PolicyEnforcementResult(
                !hasBlockingViolations,
                violations.AsReadOnly(),
                metadata);
        }

        /// <inheritdoc />
        public async Task HandleViolationAsync(
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken = default)
        {
            if (violation == null)
                throw new ArgumentNullException(nameof(violation));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Find the policy that generated this violation
            var policy = GetEnabledPolicies().FirstOrDefault(p => 
                p.Name.Equals(violation.PolicyName, StringComparison.OrdinalIgnoreCase));

            if (policy != null)
            {
                await HandlePolicyViolationAsync(policy, violation, context, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Cannot find policy {PolicyName} to handle violation", violation.PolicyName);
                await LogViolationAsync(violation, context);
            }
        }

        private List<ISecurityPolicy> GetEnabledPolicies()
        {
            lock (_policiesLock)
            {
                return _policies.Where(p => p.IsEnabled).ToList();
            }
        }

        private async Task HandlePolicyViolationAsync(
            ISecurityPolicy policy, 
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken)
        {
            try
            {
                await policy.HandleViolationAsync(violation, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling violation for policy {PolicyName}", policy.Name);
            }

            // Always log the violation for audit purposes
            await LogViolationAsync(violation, context);
        }

        private async Task LogViolationAsync(PolicyViolation violation, SecurityContext context)
        {
            try
            {
                _auditLogger.LogSecurityViolation(
                    violation.Code,
                    violation.Message,
                    context.UserIdentity,
                    context.ClientIpAddress,
                    new Dictionary<string, object>
                    {
                        ["PolicyName"] = violation.PolicyName,
                        ["Severity"] = violation.Severity.ToString(),
                        ["Timestamp"] = violation.Timestamp,
                        ["CorrelationId"] = context.CorrelationId ?? "Unknown",
                        ["OrganizationCode"] = context.OrganizationCode ?? "Unknown",
                        ["Details"] = violation.Details
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security violation");
            }
        }

        private void InitializeDefaultPolicies()
        {
            if (_options.EnableDefaultPolicies)
            {
                // Register default policies
                RegisterPolicy(new TlsEnforcementPolicy(_logger, _options.TlsOptions));
                RegisterPolicy(new RequestThrottlingPolicy(_logger, _options.ThrottlingOptions));
                RegisterPolicy(new TokenValidationPolicy(_logger, _options.TokenValidationOptions));

                _logger.LogInformation("Initialized {PolicyCount} default security policies", 3);
            }
        }
    }

    /// <summary>
    /// Interface for the security policy enforcement engine.
    /// </summary>
    public interface ISecurityPolicyEngine
    {
        /// <summary>
        /// Registers a security policy with the engine.
        /// </summary>
        /// <param name="policy">The security policy to register.</param>
        void RegisterPolicy(ISecurityPolicy policy);

        /// <summary>
        /// Unregisters a security policy from the engine.
        /// </summary>
        /// <param name="policyName">The name of the policy to unregister.</param>
        void UnregisterPolicy(string policyName);

        /// <summary>
        /// Gets all registered security policies.
        /// </summary>
        /// <returns>A read-only list of registered policies.</returns>
        IReadOnlyList<ISecurityPolicy> GetRegisteredPolicies();

        /// <summary>
        /// Enforces security policies on a request.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="context">The security context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The policy enforcement result.</returns>
        Task<PolicyEnforcementResult> EnforceRequestPoliciesAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enforces security policies on a response.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="context">The security context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The policy enforcement result.</returns>
        Task<PolicyEnforcementResult> EnforceResponsePoliciesAsync(
            HttpResponseMessage response, 
            SecurityContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles a policy violation.
        /// </summary>
        /// <param name="violation">The policy violation.</param>
        /// <param name="context">The security context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the violation handling.</returns>
        Task HandleViolationAsync(
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of policy enforcement.
    /// </summary>
    public class PolicyEnforcementResult
    {
        /// <summary>
        /// Gets a value indicating whether all policies passed.
        /// </summary>
        public bool IsAllowed { get; }

        /// <summary>
        /// Gets the policy violations that occurred.
        /// </summary>
        public IReadOnlyList<PolicyViolation> Violations { get; }

        /// <summary>
        /// Gets metadata from policy execution.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the PolicyEnforcementResult class.
        /// </summary>
        /// <param name="isAllowed">Whether the request/response is allowed.</param>
        /// <param name="violations">The policy violations.</param>
        /// <param name="metadata">Metadata from policy execution.</param>
        public PolicyEnforcementResult(
            bool isAllowed,
            IReadOnlyList<PolicyViolation> violations,
            IReadOnlyDictionary<string, object> metadata)
        {
            IsAllowed = isAllowed;
            Violations = violations ?? Array.Empty<PolicyViolation>();
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }
} 