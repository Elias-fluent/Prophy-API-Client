using System;
using System.Collections.Generic;

namespace Prophy.ApiClient.Security
{
    /// <summary>
    /// Configuration options for security policy enforcement.
    /// </summary>
    public class SecurityPolicyOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether default security policies should be enabled.
        /// Default is true.
        /// </summary>
        public bool EnableDefaultPolicies { get; set; } = true;

        /// <summary>
        /// Gets or sets the TLS enforcement options.
        /// </summary>
        public TlsEnforcementOptions TlsOptions { get; set; } = new TlsEnforcementOptions();

        /// <summary>
        /// Gets or sets the request throttling options.
        /// </summary>
        public RequestThrottlingOptions ThrottlingOptions { get; set; } = new RequestThrottlingOptions();

        /// <summary>
        /// Gets or sets the token validation options.
        /// </summary>
        public TokenValidationOptions TokenValidationOptions { get; set; } = new TokenValidationOptions();

        /// <summary>
        /// Gets or sets the global security policy settings.
        /// </summary>
        public GlobalSecuritySettings GlobalSettings { get; set; } = new GlobalSecuritySettings();
    }

    /// <summary>
    /// Configuration options for TLS enforcement policy.
    /// </summary>
    public class TlsEnforcementOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether TLS enforcement is enabled.
        /// Default is true.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum TLS version required.
        /// Default is TLS 1.2.
        /// </summary>
        public TlsVersion MinimumTlsVersion { get; set; } = TlsVersion.Tls12;

        /// <summary>
        /// Gets or sets a value indicating whether to require valid certificates.
        /// Default is true.
        /// </summary>
        public bool RequireValidCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets the allowed certificate authorities.
        /// Empty list means all valid CAs are accepted.
        /// </summary>
        public List<string> AllowedCertificateAuthorities { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether to allow self-signed certificates.
        /// Default is false.
        /// </summary>
        public bool AllowSelfSignedCertificates { get; set; } = false;

        /// <summary>
        /// Gets or sets the certificate pinning configuration.
        /// </summary>
        public CertificatePinningOptions CertificatePinning { get; set; } = new CertificatePinningOptions();
    }

    /// <summary>
    /// Configuration options for request throttling policy.
    /// </summary>
    public class RequestThrottlingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether request throttling is enabled.
        /// Default is true.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of requests per minute.
        /// Default is 60.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; } = 60;

        /// <summary>
        /// Gets or sets the maximum number of requests per hour.
        /// Default is 1000.
        /// </summary>
        public int MaxRequestsPerHour { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum number of concurrent requests.
        /// Default is 10.
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 10;

        /// <summary>
        /// Gets or sets the throttling strategy.
        /// Default is sliding window.
        /// </summary>
        public ThrottlingStrategy Strategy { get; set; } = ThrottlingStrategy.SlidingWindow;

        /// <summary>
        /// Gets or sets the burst allowance for short-term spikes.
        /// Default is 5.
        /// </summary>
        public int BurstAllowance { get; set; } = 5;

        /// <summary>
        /// Gets or sets the time window for burst detection in seconds.
        /// Default is 10 seconds.
        /// </summary>
        public int BurstWindowSeconds { get; set; } = 10;
    }

    /// <summary>
    /// Configuration options for token validation policy.
    /// </summary>
    public class TokenValidationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether token validation is enabled.
        /// Default is true.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to validate API key format.
        /// Default is true.
        /// </summary>
        public bool ValidateApiKeyFormat { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to validate JWT tokens.
        /// Default is true.
        /// </summary>
        public bool ValidateJwtTokens { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum token age in minutes.
        /// Default is 60 minutes.
        /// </summary>
        public int MaxTokenAgeMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the required token claims.
        /// </summary>
        public List<string> RequiredClaims { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the allowed token issuers.
        /// Empty list means all issuers are accepted.
        /// </summary>
        public List<string> AllowedIssuers { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the allowed token audiences.
        /// Empty list means all audiences are accepted.
        /// </summary>
        public List<string> AllowedAudiences { get; set; } = new List<string>();
    }

    /// <summary>
    /// Global security settings that apply to all policies.
    /// </summary>
    public class GlobalSecuritySettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to fail fast on security violations.
        /// Default is true.
        /// </summary>
        public bool FailFastOnViolations { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of violations before blocking requests.
        /// Default is 3.
        /// </summary>
        public int MaxViolationsBeforeBlock { get; set; } = 3;

        /// <summary>
        /// Gets or sets the violation tracking window in minutes.
        /// Default is 15 minutes.
        /// </summary>
        public int ViolationTrackingWindowMinutes { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether to log all security events.
        /// Default is true.
        /// </summary>
        public bool LogAllSecurityEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets the correlation ID header name.
        /// Default is "X-Correlation-ID".
        /// </summary>
        public string CorrelationIdHeaderName { get; set; } = "X-Correlation-ID";

        /// <summary>
        /// Gets or sets the session ID header name.
        /// Default is "X-Session-ID".
        /// </summary>
        public string SessionIdHeaderName { get; set; } = "X-Session-ID";
    }

    /// <summary>
    /// Configuration options for certificate pinning.
    /// </summary>
    public class CertificatePinningOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether certificate pinning is enabled.
        /// Default is false.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the pinned certificate thumbprints.
        /// </summary>
        public List<string> PinnedThumbprints { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the pinned public key hashes.
        /// </summary>
        public List<string> PinnedPublicKeyHashes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether to allow backup certificates.
        /// Default is true.
        /// </summary>
        public bool AllowBackupCertificates { get; set; } = true;
    }

    /// <summary>
    /// Defines the supported TLS versions.
    /// </summary>
    public enum TlsVersion
    {
        /// <summary>
        /// TLS 1.0 (deprecated, not recommended).
        /// </summary>
        Tls10 = 0,

        /// <summary>
        /// TLS 1.1 (deprecated, not recommended).
        /// </summary>
        Tls11 = 1,

        /// <summary>
        /// TLS 1.2 (recommended minimum).
        /// </summary>
        Tls12 = 2,

        /// <summary>
        /// TLS 1.3 (latest and most secure).
        /// </summary>
        Tls13 = 3
    }

    /// <summary>
    /// Defines the throttling strategies.
    /// </summary>
    public enum ThrottlingStrategy
    {
        /// <summary>
        /// Fixed window throttling.
        /// </summary>
        FixedWindow = 0,

        /// <summary>
        /// Sliding window throttling.
        /// </summary>
        SlidingWindow = 1,

        /// <summary>
        /// Token bucket throttling.
        /// </summary>
        TokenBucket = 2,

        /// <summary>
        /// Leaky bucket throttling.
        /// </summary>
        LeakyBucket = 3
    }
} 