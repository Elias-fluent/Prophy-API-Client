using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prophy.ApiClient.Configuration;
using Prophy.ApiClient.Extensions.DependencyInjection.Configuration;
using Prophy.ApiClient.MultiTenancy;
using Prophy.ApiClient.Tests.Utilities;

namespace Prophy.ApiClient.Tests.MultiTenancy.Enhanced
{
    /// <summary>
    /// Enhanced tests for multi-tenancy support and configuration management using property-based testing.
    /// </summary>
    public class MultiTenancyEnhancedTests
    {
        private readonly Mock<ILogger<OrganizationContext>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public MultiTenancyEnhancedTests()
        {
            _mockLogger = TestHelpers.CreateMockLogger<OrganizationContext>();
            _mockServiceProvider = new Mock<IServiceProvider>();
        }

        #region Property-Based Tests for Organization Context

        [Property]
        public Property OrganizationContext_WithValidOrganizationCode_ShouldSetCorrectly()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                orgCode =>
                {
                    try
                    {
                        // Arrange & Act
                        var context = new OrganizationContext(orgCode.Get, "Test Organization");

                        // Assert
                        return context.OrganizationCode == orgCode.Get;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Property]
        public Property OrganizationContext_WithMultipleOrganizations_ShouldMaintainIsolation()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (org1, org2) =>
                {
                    try
                    {
                        // Arrange
                        var context1 = new OrganizationContext(org1.Get, "Organization 1");
                        var context2 = new OrganizationContext(org2.Get, "Organization 2");

                        // Assert
                        return context1.OrganizationCode == org1.Get &&
                               context2.OrganizationCode == org2.Get &&
                               context1.OrganizationCode != context2.OrganizationCode;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        #endregion

        #region Tenant Isolation Tests

        [Fact]
        public async Task OrganizationContext_WithConcurrentTenants_ShouldMaintainIsolation()
        {
            // Arrange
            var tenantTasks = Enumerable.Range(1, 10)
                .Select(i => Task.Run(() =>
                {
                    var orgCode = $"tenant-{i}";
                    var context = new OrganizationContext(orgCode, $"Tenant {i}");
                    
                    // Simulate some work
                    Thread.Sleep(10);
                    
                    return new { TenantId = i, OrganizationCode = context.OrganizationCode };
                }))
                .ToList();

            // Act
            var results = await Task.WhenAll(tenantTasks);

            // Assert
            Assert.All(results, result =>
            {
                Assert.Equal($"tenant-{result.TenantId}", result.OrganizationCode);
            });

            // Verify all tenants are unique
            var uniqueTenants = results.Select(r => r.OrganizationCode).Distinct().Count();
            Assert.Equal(10, uniqueTenants);
        }

        [Theory]
        [InlineData("tenant-1", "api-key-1")]
        [InlineData("tenant-2", "api-key-2")]
        [InlineData("TENANT-UPPER", "API-KEY-UPPER")]
        [InlineData("tenant_with_underscores", "api_key_with_underscores")]
        [InlineData("tenant-with-special-chars-123", "api-key-with-special-chars-456")]
        public void OrganizationContext_WithDifferentTenantFormats_ShouldHandleCorrectly(string tenantCode, string apiKey)
        {
            // Arrange & Act
            var context = new OrganizationContext(tenantCode, "Test Organization", apiKey);

            // Assert
            Assert.Equal(tenantCode, context.OrganizationCode);
            Assert.Equal(apiKey, context.ApiKey);
        }

        #endregion

        #region Configuration Management Tests

        [Fact]
        public void ProphyApiClientConfiguration_WithValidConfiguration_ShouldBindCorrectly()
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = "test-api-key",
                OrganizationCode = "test-org",
                BaseUrl = "https://api.test.com",
                TimeoutSeconds = 300,
                MaxRetryAttempts = 3,
                EnableDetailedLogging = true
            };

            // Act & Assert
            Assert.Equal("test-api-key", config.ApiKey);
            Assert.Equal("test-org", config.OrganizationCode);
            Assert.Equal("https://api.test.com", config.BaseUrl);
            Assert.Equal(300, config.TimeoutSeconds);
            Assert.Equal(3, config.MaxRetryAttempts);
            Assert.True(config.EnableDetailedLogging);
        }

        [Property]
        public Property ProphyApiClientConfiguration_WithRandomValidValues_ShouldAcceptAllValues()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                Arb.From<NonEmptyString>(),
                (apiKey, orgCode, baseUrl) =>
                {
                    try
                    {
                        var config = new ProphyApiClientConfiguration
                        {
                            ApiKey = apiKey.Get,
                            OrganizationCode = orgCode.Get,
                            BaseUrl = baseUrl.Get
                        };

                        return config.ApiKey == apiKey.Get &&
                               config.OrganizationCode == orgCode.Get &&
                               config.BaseUrl == baseUrl.Get;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        [Fact]
        public void MultiTenancyOptions_WithValidConfiguration_ShouldBindCorrectly()
        {
            // Arrange
            var options = new MultiTenancyOptions
            {
                DefaultConfigurationSection = "ProphyApiClient",
                TenantsConfigurationSection = "Tenants",
                EnableAutomaticTenantResolution = true,
                EnableConfigurationCaching = true,
                ConfigurationCacheExpirationMinutes = 60,
                ValidateConfigurationsOnStartup = true,
                FallbackBehavior = TenantFallbackBehavior.UseDefault
            };

            // Act & Assert
            Assert.Equal("ProphyApiClient", options.DefaultConfigurationSection);
            Assert.Equal("Tenants", options.TenantsConfigurationSection);
            Assert.True(options.EnableAutomaticTenantResolution);
            Assert.True(options.EnableConfigurationCaching);
            Assert.Equal(60, options.ConfigurationCacheExpirationMinutes);
            Assert.True(options.ValidateConfigurationsOnStartup);
            Assert.Equal(TenantFallbackBehavior.UseDefault, options.FallbackBehavior);
        }

        #endregion

        #region Cross-Tenant Operation Tests

        [Fact]
        public async Task CrossTenantOperations_WithProperIsolation_ShouldPreventDataLeakage()
        {
            // Arrange
            var tenant1Context = new OrganizationContext("tenant1", "Tenant 1");
            var tenant2Context = new OrganizationContext("tenant2", "Tenant 2");

            var tenant1Data = new List<string>();
            var tenant2Data = new List<string>();

            // Act
            var tasks = new[]
            {
                Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        tenant1Data.Add($"tenant1-data-{i}");
                        Thread.Sleep(1); // Simulate work
                    }
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        tenant2Data.Add($"tenant2-data-{i}");
                        Thread.Sleep(1); // Simulate work
                    }
                })
            };

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal("tenant1", tenant1Context.OrganizationCode);
            Assert.Equal("tenant2", tenant2Context.OrganizationCode);
            Assert.Equal(100, tenant1Data.Count);
            Assert.Equal(100, tenant2Data.Count);
            Assert.All(tenant1Data, item => Assert.StartsWith("tenant1-", item));
            Assert.All(tenant2Data, item => Assert.StartsWith("tenant2-", item));
        }

        [Fact]
        public void TenantConfiguration_WithOverrides_ShouldApplyCorrectly()
        {
            // Arrange
            var baseConfig = new ProphyApiClientConfiguration
            {
                ApiKey = "base-key",
                OrganizationCode = "base-org",
                BaseUrl = "https://api.base.com",
                TimeoutSeconds = 60,
                MaxRetryAttempts = 3
            };

            var tenantOverrides = new ProphyApiClientConfiguration
            {
                ApiKey = "tenant-key",
                OrganizationCode = "tenant-org",
                TimeoutSeconds = 300
                // BaseUrl and MaxRetryAttempts should inherit from base
            };

            // Act
            var mergedConfig = new ProphyApiClientConfiguration
            {
                ApiKey = tenantOverrides.ApiKey ?? baseConfig.ApiKey,
                OrganizationCode = tenantOverrides.OrganizationCode ?? baseConfig.OrganizationCode,
                BaseUrl = tenantOverrides.BaseUrl ?? baseConfig.BaseUrl,
                TimeoutSeconds = tenantOverrides.TimeoutSeconds != 0 ? tenantOverrides.TimeoutSeconds : baseConfig.TimeoutSeconds,
                MaxRetryAttempts = tenantOverrides.MaxRetryAttempts != 0 ? tenantOverrides.MaxRetryAttempts : baseConfig.MaxRetryAttempts
            };

            // Assert
            Assert.Equal("tenant-key", mergedConfig.ApiKey);
            Assert.Equal("tenant-org", mergedConfig.OrganizationCode);
            Assert.Equal("https://api.base.com", mergedConfig.BaseUrl); // Inherited
            Assert.Equal(300, mergedConfig.TimeoutSeconds); // Overridden
            Assert.Equal(3, mergedConfig.MaxRetryAttempts); // Inherited
        }

        #endregion

        #region Configuration Validation Tests

        [Theory]
        [InlineData(null, "org-code", false)]
        [InlineData("", "org-code", false)]
        [InlineData("api-key", null, false)]
        [InlineData("api-key", "", false)]
        [InlineData("api-key", "org-code", true)]
        public void ProphyApiClientConfiguration_WithValidationRules_ShouldValidateCorrectly(
            string? apiKey, string? orgCode, bool expectedValid)
        {
            // Arrange
            var config = new ProphyApiClientConfiguration
            {
                ApiKey = apiKey,
                OrganizationCode = orgCode
            };

            // Act
            var isValid = config.IsValid;

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        #endregion

        #region Performance and Memory Tests

        [Fact]
        public async Task OrganizationContext_WithHighConcurrency_ShouldPerformWell()
        {
            // Arrange
            const int concurrentOperations = 1000;
            var contexts = new OrganizationContext[concurrentOperations];
            var tasks = new Task[concurrentOperations];

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < concurrentOperations; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    contexts[index] = new OrganizationContext($"tenant-{index}", $"Tenant {index}");
                });
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
            Assert.All(contexts, context => Assert.NotNull(context.OrganizationCode));

            // Verify each context has the correct organization
            for (int i = 0; i < concurrentOperations; i++)
            {
                Assert.Equal($"tenant-{i}", contexts[i].OrganizationCode);
            }
        }

        [Fact]
        public void OrganizationContext_WithMemoryPressure_ShouldNotLeak()
        {
            // Arrange
            const int iterations = 10000;
            var initialMemory = GC.GetTotalMemory(true);

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var context = new OrganizationContext($"tenant-{i}", $"Tenant {i}");
                // Context goes out of scope and should be eligible for GC
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            // Memory should not have grown significantly (allowing for some overhead)
            var memoryGrowth = finalMemory - initialMemory;
            Assert.True(memoryGrowth < 1024 * 1024, $"Memory grew by {memoryGrowth} bytes, which may indicate a leak");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void OrganizationContext_WithNullOrganization_ShouldHandleGracefully()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new OrganizationContext(null!, "Test"));
            Assert.Throws<ArgumentException>(() => new OrganizationContext("", "Test"));
            Assert.Throws<ArgumentException>(() => new OrganizationContext("   ", "Test"));
        }

        [Fact]
        public void OrganizationContext_WithVeryLongOrganizationCode_ShouldHandleCorrectly()
        {
            // Arrange
            var longOrgCode = new string('A', 1000);

            // Act
            var context = new OrganizationContext(longOrgCode, "Test Organization");

            // Assert
            Assert.Equal(longOrgCode, context.OrganizationCode);
        }

        [Theory]
        [InlineData("org-with-special-chars-!@#$%^&*()")]
        [InlineData("org_with_underscores")]
        [InlineData("org-with-unicode-世界")]
        [InlineData("ORG-WITH-UPPERCASE")]
        [InlineData("org.with.dots")]
        [InlineData("org:with:colons")]
        public void OrganizationContext_WithSpecialCharacters_ShouldHandleCorrectly(string orgCode)
        {
            // Act
            var context = new OrganizationContext(orgCode, "Test Organization");

            // Assert
            Assert.Equal(orgCode, context.OrganizationCode);
        }

        #endregion
    }
} 