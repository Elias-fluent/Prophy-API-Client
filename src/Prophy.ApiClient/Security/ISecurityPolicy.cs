using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Defines the contract for security policies that can be enforced by the API client.
    /// </summary>
    public interface ISecurityPolicy
    {
        /// <summary>
        /// Gets the name of the security policy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority of the policy. Higher values indicate higher priority.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets a value indicating whether this policy is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Validates a request before it is sent.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="context">The security context for the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the validation result.</returns>
        Task<PolicyValidationResult> ValidateRequestAsync(
            HttpRequestMessage request, 
            SecurityContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a response after it is received.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="context">The security context for the response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the validation result.</returns>
        Task<PolicyValidationResult> ValidateResponseAsync(
            HttpResponseMessage response, 
            SecurityContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles a policy violation.
        /// </summary>
        /// <param name="violation">The policy violation details.</param>
        /// <param name="context">The security context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the violation handling.</returns>
        Task HandleViolationAsync(
            PolicyViolation violation, 
            SecurityContext context, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of a security policy validation.
    /// </summary>
    public class PolicyValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the policy violations, if any.
        /// </summary>
        public IReadOnlyList<PolicyViolation> Violations { get; }

        /// <summary>
        /// Gets additional metadata about the validation.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the PolicyValidationResult class.
        /// </summary>
        /// <param name="isValid">Whether the validation passed.</param>
        /// <param name="violations">The policy violations.</param>
        /// <param name="metadata">Additional metadata.</param>
        public PolicyValidationResult(
            bool isValid, 
            IReadOnlyList<PolicyViolation>? violations = null, 
            IReadOnlyDictionary<string, object>? metadata = null)
        {
            IsValid = isValid;
            Violations = violations ?? Array.Empty<PolicyViolation>();
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <param name="metadata">Optional metadata.</param>
        /// <returns>A successful validation result.</returns>
        public static PolicyValidationResult Success(IReadOnlyDictionary<string, object>? metadata = null)
        {
            return new PolicyValidationResult(true, null, metadata);
        }

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        /// <param name="violations">The policy violations.</param>
        /// <param name="metadata">Optional metadata.</param>
        /// <returns>A failed validation result.</returns>
        public static PolicyValidationResult Failure(
            IReadOnlyList<PolicyViolation> violations, 
            IReadOnlyDictionary<string, object>? metadata = null)
        {
            return new PolicyValidationResult(false, violations, metadata);
        }

        /// <summary>
        /// Creates a failed validation result with a single violation.
        /// </summary>
        /// <param name="violation">The policy violation.</param>
        /// <param name="metadata">Optional metadata.</param>
        /// <returns>A failed validation result.</returns>
        public static PolicyValidationResult Failure(
            PolicyViolation violation, 
            IReadOnlyDictionary<string, object>? metadata = null)
        {
            return new PolicyValidationResult(false, new[] { violation }, metadata);
        }
    }

    /// <summary>
    /// Represents a security policy violation.
    /// </summary>
    public class PolicyViolation
    {
        /// <summary>
        /// Gets the name of the policy that was violated.
        /// </summary>
        public string PolicyName { get; }

        /// <summary>
        /// Gets the severity of the violation.
        /// </summary>
        public PolicyViolationSeverity Severity { get; }

        /// <summary>
        /// Gets the violation code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the violation message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets additional details about the violation.
        /// </summary>
        public IReadOnlyDictionary<string, object> Details { get; }

        /// <summary>
        /// Gets the timestamp when the violation occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the PolicyViolation class.
        /// </summary>
        /// <param name="policyName">The name of the policy that was violated.</param>
        /// <param name="severity">The severity of the violation.</param>
        /// <param name="code">The violation code.</param>
        /// <param name="message">The violation message.</param>
        /// <param name="details">Additional details about the violation.</param>
        public PolicyViolation(
            string policyName,
            PolicyViolationSeverity severity,
            string code,
            string message,
            IReadOnlyDictionary<string, object>? details = null)
        {
            PolicyName = policyName ?? throw new ArgumentNullException(nameof(policyName));
            Severity = severity;
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Details = details ?? new Dictionary<string, object>();
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Defines the severity levels for policy violations.
    /// </summary>
    public enum PolicyViolationSeverity
    {
        /// <summary>
        /// Informational violation that doesn't block the request.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning violation that should be logged but doesn't block the request.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error violation that blocks the request.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical violation that blocks the request and may trigger additional security measures.
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Represents the security context for a request or response.
    /// </summary>
    public class SecurityContext
    {
        /// <summary>
        /// Gets or sets the user identity.
        /// </summary>
        public string? UserIdentity { get; set; }

        /// <summary>
        /// Gets or sets the client IP address.
        /// </summary>
        public string? ClientIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the organization code.
        /// </summary>
        public string? OrganizationCode { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the request timestamp.
        /// </summary>
        public DateTimeOffset RequestTimestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets additional context properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the correlation ID for tracking requests.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        public string? SessionId { get; set; }
    }
} 