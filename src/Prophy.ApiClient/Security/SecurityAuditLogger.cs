using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient.Diagnostics;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Provides comprehensive audit logging for security-sensitive operations with tamper protection.
    /// </summary>
    public class SecurityAuditLogger : ISecurityAuditLogger
    {
        private readonly ILogger<SecurityAuditLogger> _logger;
        private readonly SecurityAuditOptions _options;
        private readonly string _instanceId;

        /// <summary>
        /// Initializes a new instance of the SecurityAuditLogger class.
        /// </summary>
        /// <param name="logger">The logger instance for audit logging.</param>
        /// <param name="options">Configuration options for audit logging.</param>
        public SecurityAuditLogger(ILogger<SecurityAuditLogger> logger, SecurityAuditOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new SecurityAuditOptions();
            _instanceId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        /// <inheritdoc />
        public void LogAuthenticationAttempt(string organizationCode, string? userIdentity, bool success, string? ipAddress = null, string? userAgent = null)
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.Authentication,
                EventCategory = "Authentication",
                Action = "Login",
                Success = success,
                OrganizationCode = organizationCode,
                UserIdentity = userIdentity,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = new Dictionary<string, object>
                {
                    ["AuthenticationMethod"] = "ApiKey",
                    ["Result"] = success ? "Success" : "Failed"
                }
            };

            LogAuditEvent(auditEvent);
        }

        /// <inheritdoc />
        public void LogConfigurationAccess(string configurationKey, string action, string? userIdentity = null, bool success = true)
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.ConfigurationAccess,
                EventCategory = "Configuration",
                Action = action,
                Success = success,
                UserIdentity = userIdentity,
                ResourceId = configurationKey,
                Details = new Dictionary<string, object>
                {
                    ["ConfigurationKey"] = configurationKey,
                    ["AccessType"] = action
                }
            };

            LogAuditEvent(auditEvent);
        }

        /// <inheritdoc />
        public void LogSecretAccess(string secretName, string action, string? userIdentity = null, bool success = true, string? providerName = null)
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.SecretAccess,
                EventCategory = "SecretManagement",
                Action = action,
                Success = success,
                UserIdentity = userIdentity,
                ResourceId = secretName,
                Details = new Dictionary<string, object>
                {
                    ["SecretName"] = secretName,
                    ["Provider"] = providerName ?? "Unknown",
                    ["Operation"] = action
                }
            };

            LogAuditEvent(auditEvent);
        }

        /// <inheritdoc />
        public void LogApiAccess(string endpoint, string method, int statusCode, string? userIdentity = null, string? ipAddress = null, TimeSpan? duration = null)
        {
            var success = statusCode >= 200 && statusCode < 400;
            
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.ApiAccess,
                EventCategory = "ApiAccess",
                Action = method,
                Success = success,
                UserIdentity = userIdentity,
                IpAddress = ipAddress,
                ResourceId = endpoint,
                Details = new Dictionary<string, object>
                {
                    ["Endpoint"] = endpoint,
                    ["HttpMethod"] = method,
                    ["StatusCode"] = statusCode,
                    ["DurationMs"] = duration?.TotalMilliseconds ?? 0
                }
            };

            LogAuditEvent(auditEvent);
        }

        /// <inheritdoc />
        public void LogSecurityViolation(string violationType, string description, string? userIdentity = null, string? ipAddress = null, Dictionary<string, object>? additionalData = null)
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.SecurityViolation,
                EventCategory = "Security",
                Action = violationType,
                Success = false,
                UserIdentity = userIdentity,
                IpAddress = ipAddress,
                Details = new Dictionary<string, object>
                {
                    ["ViolationType"] = violationType,
                    ["Description"] = description,
                    ["Severity"] = "High"
                }
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    auditEvent.Details[kvp.Key] = kvp.Value;
                }
            }

            LogAuditEvent(auditEvent);
        }

        /// <inheritdoc />
        public void LogDataAccess(string resourceType, string resourceId, string action, string? userIdentity = null, bool success = true)
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.DataAccess,
                EventCategory = "DataAccess",
                Action = action,
                Success = success,
                UserIdentity = userIdentity,
                ResourceId = resourceId,
                Details = new Dictionary<string, object>
                {
                    ["ResourceType"] = resourceType,
                    ["ResourceId"] = resourceId,
                    ["Operation"] = action
                }
            };

            LogAuditEvent(auditEvent);
        }

        /// <inheritdoc />
        public void LogPermissionCheck(string permission, string? resourceId, bool granted, string? userIdentity = null)
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = SecurityEventType.Authorization,
                EventCategory = "Authorization",
                Action = "PermissionCheck",
                Success = granted,
                UserIdentity = userIdentity,
                ResourceId = resourceId,
                Details = new Dictionary<string, object>
                {
                    ["Permission"] = permission,
                    ["Granted"] = granted,
                    ["Resource"] = resourceId ?? "Global"
                }
            };

            LogAuditEvent(auditEvent);
        }

        private void LogAuditEvent(SecurityAuditEvent auditEvent)
        {
            // Set common properties
            auditEvent.Timestamp = DateTimeOffset.UtcNow;
            auditEvent.InstanceId = _instanceId;
            auditEvent.CorrelationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();

            // Add integrity hash if enabled
            if (_options.EnableIntegrityCheck)
            {
                auditEvent.IntegrityHash = ComputeIntegrityHash(auditEvent);
            }

            // Determine log level based on event type and success
            var logLevel = DetermineLogLevel(auditEvent);

            // Create structured log entry
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["AuditEvent"] = true,
                ["EventType"] = auditEvent.EventType.ToString(),
                ["EventCategory"] = auditEvent.EventCategory,
                ["CorrelationId"] = auditEvent.CorrelationId,
                ["InstanceId"] = auditEvent.InstanceId
            });

            // Log the audit event
            _logger.Log(logLevel, DiagnosticEvents.EventIds.PerformanceMetrics,
                "Security audit: {EventType} - {Action} {Result} for {UserIdentity} from {IpAddress} on {ResourceId}",
                auditEvent.EventType,
                auditEvent.Action,
                auditEvent.Success ? "succeeded" : "failed",
                auditEvent.UserIdentity ?? "Unknown",
                auditEvent.IpAddress ?? "Unknown",
                auditEvent.ResourceId ?? "N/A");

            // Log detailed audit information at debug level
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var auditJson = JsonSerializer.Serialize(auditEvent, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogDebug("Audit event details: {AuditEventJson}", auditJson);
            }

            // Record metrics
            DiagnosticEvents.Metrics.IncrementCounter($"security.audit.{auditEvent.EventType.ToString().ToLowerInvariant()}");
            DiagnosticEvents.Metrics.IncrementCounter($"security.audit.{auditEvent.EventCategory.ToLowerInvariant()}");
            
            if (!auditEvent.Success)
            {
                DiagnosticEvents.Metrics.IncrementCounter("security.audit.failures");
            }
        }

        private LogLevel DetermineLogLevel(SecurityAuditEvent auditEvent)
        {
            return auditEvent.EventType switch
            {
                SecurityEventType.SecurityViolation => LogLevel.Critical,
                SecurityEventType.Authentication when !auditEvent.Success => LogLevel.Warning,
                SecurityEventType.Authorization when !auditEvent.Success => LogLevel.Warning,
                SecurityEventType.SecretAccess => LogLevel.Information,
                SecurityEventType.ConfigurationAccess => LogLevel.Information,
                _ => LogLevel.Information
            };
        }

        private string ComputeIntegrityHash(SecurityAuditEvent auditEvent)
        {
            // Create a deterministic string representation for hashing
            var hashInput = $"{auditEvent.Timestamp:O}|{auditEvent.EventType}|{auditEvent.Action}|{auditEvent.Success}|{auditEvent.UserIdentity}|{auditEvent.ResourceId}|{auditEvent.IpAddress}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Interface for security audit logging.
    /// </summary>
    public interface ISecurityAuditLogger
    {
        /// <summary>
        /// Logs an authentication attempt.
        /// </summary>
        /// <param name="organizationCode">The organization code being authenticated against.</param>
        /// <param name="userIdentity">The user identity attempting authentication.</param>
        /// <param name="success">Whether the authentication was successful.</param>
        /// <param name="ipAddress">The IP address of the authentication attempt.</param>
        /// <param name="userAgent">The user agent string of the client.</param>
        void LogAuthenticationAttempt(string organizationCode, string? userIdentity, bool success, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Logs access to configuration settings.
        /// </summary>
        /// <param name="configurationKey">The configuration key being accessed.</param>
        /// <param name="action">The action being performed (Read, Write, Delete).</param>
        /// <param name="userIdentity">The user identity performing the action.</param>
        /// <param name="success">Whether the action was successful.</param>
        void LogConfigurationAccess(string configurationKey, string action, string? userIdentity = null, bool success = true);

        /// <summary>
        /// Logs access to secrets or sensitive data.
        /// </summary>
        /// <param name="secretName">The name of the secret being accessed.</param>
        /// <param name="action">The action being performed (Read, Write, Delete).</param>
        /// <param name="userIdentity">The user identity performing the action.</param>
        /// <param name="success">Whether the action was successful.</param>
        /// <param name="providerName">The name of the provider used to access the secret.</param>
        void LogSecretAccess(string secretName, string action, string? userIdentity = null, bool success = true, string? providerName = null);

        /// <summary>
        /// Logs API access attempts.
        /// </summary>
        /// <param name="endpoint">The API endpoint being accessed.</param>
        /// <param name="method">The HTTP method used.</param>
        /// <param name="statusCode">The HTTP status code returned.</param>
        /// <param name="userIdentity">The user identity making the request.</param>
        /// <param name="ipAddress">The IP address of the request.</param>
        /// <param name="duration">The duration of the request.</param>
        void LogApiAccess(string endpoint, string method, int statusCode, string? userIdentity = null, string? ipAddress = null, TimeSpan? duration = null);

        /// <summary>
        /// Logs security violations or suspicious activities.
        /// </summary>
        /// <param name="violationType">The type of security violation.</param>
        /// <param name="description">A description of the violation.</param>
        /// <param name="userIdentity">The user identity associated with the violation.</param>
        /// <param name="ipAddress">The IP address associated with the violation.</param>
        /// <param name="additionalData">Additional data related to the violation.</param>
        void LogSecurityViolation(string violationType, string description, string? userIdentity = null, string? ipAddress = null, Dictionary<string, object>? additionalData = null);

        /// <summary>
        /// Logs data access operations.
        /// </summary>
        /// <param name="resourceType">The type of resource being accessed.</param>
        /// <param name="resourceId">The identifier of the resource being accessed.</param>
        /// <param name="action">The action being performed.</param>
        /// <param name="userIdentity">The user identity performing the action.</param>
        /// <param name="success">Whether the action was successful.</param>
        void LogDataAccess(string resourceType, string resourceId, string action, string? userIdentity = null, bool success = true);

        /// <summary>
        /// Logs permission checks and authorization decisions.
        /// </summary>
        /// <param name="permission">The permission being checked.</param>
        /// <param name="resourceId">The resource the permission applies to.</param>
        /// <param name="granted">Whether the permission was granted.</param>
        /// <param name="userIdentity">The user identity being checked.</param>
        void LogPermissionCheck(string permission, string? resourceId, bool granted, string? userIdentity = null);
    }

    /// <summary>
    /// Represents a security audit event.
    /// </summary>
    public class SecurityAuditEvent
    {
        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the type of security event.
        /// </summary>
        public SecurityEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the category of the event.
        /// </summary>
        public string EventCategory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the action that was performed.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the action was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the user identity associated with the event.
        /// </summary>
        public string? UserIdentity { get; set; }

        /// <summary>
        /// Gets or sets the organization code associated with the event.
        /// </summary>
        public string? OrganizationCode { get; set; }

        /// <summary>
        /// Gets or sets the IP address associated with the event.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string associated with the event.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier associated with the event.
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracking related events.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the instance identifier of the application.
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional details about the event.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the integrity hash for tamper detection.
        /// </summary>
        public string? IntegrityHash { get; set; }
    }

    /// <summary>
    /// Enumeration of security event types.
    /// </summary>
    public enum SecurityEventType
    {
        /// <summary>
        /// Authentication-related events.
        /// </summary>
        Authentication,

        /// <summary>
        /// Authorization-related events.
        /// </summary>
        Authorization,

        /// <summary>
        /// Configuration access events.
        /// </summary>
        ConfigurationAccess,

        /// <summary>
        /// Secret access events.
        /// </summary>
        SecretAccess,

        /// <summary>
        /// API access events.
        /// </summary>
        ApiAccess,

        /// <summary>
        /// Data access events.
        /// </summary>
        DataAccess,

        /// <summary>
        /// Security violation events.
        /// </summary>
        SecurityViolation
    }

    /// <summary>
    /// Configuration options for security audit logging.
    /// </summary>
    public class SecurityAuditOptions
    {
        /// <summary>
        /// Gets or sets whether to enable integrity checking for audit logs.
        /// Default is true.
        /// </summary>
        public bool EnableIntegrityCheck { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log successful authentication attempts.
        /// Default is true.
        /// </summary>
        public bool LogSuccessfulAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log all API access attempts.
        /// Default is false (only logs failures and sensitive operations).
        /// </summary>
        public bool LogAllApiAccess { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum log level for audit events.
        /// Default is Information.
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets whether to include detailed information in audit logs.
        /// Default is true.
        /// </summary>
        public bool IncludeDetailedInformation { get; set; } = true;
    }
} 