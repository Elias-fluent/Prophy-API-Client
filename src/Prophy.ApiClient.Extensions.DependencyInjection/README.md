# Prophy API Client Dependency Injection Extensions

This package provides dependency injection extensions for the Prophy API Client library, enabling easy integration with Microsoft.Extensions.DependencyInjection containers and comprehensive multi-tenancy support.

## Installation

```bash
dotnet add package Prophy.ApiClient.Extensions.DependencyInjection
```

## Basic Usage

### Simple Configuration

```csharp
using Prophy.ApiClient.Extensions.DependencyInjection;

// In Startup.cs or Program.cs
services.AddProphyApiClient(options =>
{
    options.BaseUrl = "https://api.prophy.science";
    options.ApiKey = "your-api-key";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.MaxRetryAttempts = 3;
});

// Inject and use the client
public class MyService
{
    private readonly ProphyApiClient _client;
    
    public MyService(ProphyApiClient client)
    {
        _client = client;
    }
    
    public async Task<string> GetManuscriptsAsync()
    {
        var manuscripts = await _client.Manuscripts.GetAllAsync();
        return manuscripts.ToString();
    }
}
```

### Configuration from appsettings.json

```json
{
  "ProphyApiClient": {
    "BaseUrl": "https://api.prophy.science",
    "ApiKey": "your-api-key",
    "Timeout": "00:00:30",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01",
    "EnableLogging": true,
    "UserAgent": "MyApp/1.0"
  }
}
```

```csharp
services.AddProphyApiClient(configuration);
// or with custom section name
services.AddProphyApiClient(configuration, "CustomApiClientSection");
```

## Multi-Tenancy Support

### Basic Multi-Tenancy

```csharp
// Register multi-tenant client
services.AddProphyApiClientWithMultiTenancy(configuration);

// Inject and use the multi-tenant client
public class TenantService
{
    private readonly MultiTenantProphyApiClient _client;
    
    public TenantService(MultiTenantProphyApiClient client)
    {
        _client = client;
    }
    
    public async Task<string> ProcessTenantDataAsync(string tenantId)
    {
        // Set tenant context
        await _client.SetContextAsync(tenantId);
        
        // All subsequent API calls will use tenant-specific configuration
        var manuscripts = await _client.Manuscripts.GetAllAsync();
        
        return manuscripts.ToString();
    }
}
```

### Named Client Configurations

For scenarios where you need multiple pre-configured clients:

```csharp
// Register named configurations
services.AddNamedProphyApiClient("tenant1", options =>
{
    options.BaseUrl = "https://tenant1.api.prophy.science";
    options.ApiKey = "tenant1-api-key";
});

services.AddNamedProphyApiClient("tenant2", configuration, "Tenant2Config");

// Use the named client factory
public class MultiTenantService
{
    private readonly INamedProphyApiClientFactory _factory;
    
    public MultiTenantService(INamedProphyApiClientFactory factory)
    {
        _factory = factory;
    }
    
    public async Task ProcessTenantAsync(string tenantName)
    {
        using var client = _factory.CreateNamedClient(tenantName);
        var manuscripts = await client.Manuscripts.GetAllAsync();
        // Process manuscripts...
    }
}
```

### Advanced Multi-Tenancy with Configuration Sections

For complex multi-tenant scenarios with configuration-driven tenant setup:

```json
{
  "ProphyApiClient": {
    "BaseUrl": "https://api.prophy.science",
    "ApiKey": "default-api-key",
    "Timeout": "00:00:30"
  },
  "Tenants": {
    "acme-corp": {
      "BaseUrl": "https://acme.api.prophy.science",
      "ApiKey": "acme-specific-key",
      "MaxRetryAttempts": 5
    },
    "globex": {
      "BaseUrl": "https://globex.api.prophy.science", 
      "ApiKey": "globex-specific-key",
      "Timeout": "00:01:00"
    }
  }
}
```

```csharp
// Register multiple tenant configurations from config
services.AddMultiTenantProphyApiClients(configuration, "Tenants");

// Or use the advanced registration method
services.AddAdvancedMultiTenantProphyApiClient(configuration, options =>
{
    options.DefaultConfigurationSection = "ProphyApiClient";
    options.TenantsConfigurationSection = "Tenants";
    options.EnableAutomaticTenantResolution = true;
    options.EnableConfigurationCaching = true;
});
```

### Tenant Resolution

The multi-tenant client automatically resolves tenant context from various sources:

1. **HTTP Headers**: `X-Organization-Code`, `X-Tenant-Id`, etc.
2. **JWT Tokens**: Claims like `org`, `organization`, `tenant_id`
3. **URL Patterns**: Subdomain-based tenant identification
4. **Manual Context Setting**: Explicit tenant context management

```csharp
public class TenantAwareController : ControllerBase
{
    private readonly MultiTenantProphyApiClient _client;
    
    public TenantAwareController(MultiTenantProphyApiClient client)
    {
        _client = client;
    }
    
    [HttpGet("manuscripts")]
    public async Task<IActionResult> GetManuscripts()
    {
        // Tenant context is automatically resolved from HTTP headers/JWT
        var manuscripts = await _client.Manuscripts.GetAllAsync();
        return Ok(manuscripts);
    }
    
    [HttpPost("switch-tenant/{tenantId}")]
    public async Task<IActionResult> SwitchTenant(string tenantId)
    {
        // Manually set tenant context
        await _client.SetContextAsync(tenantId);
        return Ok($"Switched to tenant: {tenantId}");
    }
}
```

## Configuration Options

### ProphyApiClientOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | `string` | - | Base URL for the Prophy API |
| `ApiKey` | `string` | - | API key for authentication |
| `Timeout` | `TimeSpan` | 30 seconds | HTTP request timeout |
| `MaxRetryAttempts` | `int` | 3 | Maximum retry attempts for failed requests |
| `RetryDelay` | `TimeSpan` | 1 second | Delay between retry attempts |
| `EnableLogging` | `bool` | `true` | Enable detailed logging |
| `LogLevel` | `LogLevel` | `Information` | Logging level |
| `UserAgent` | `string` | - | Custom user agent string |
| `DefaultHeaders` | `Dictionary<string, string>` | Empty | Default headers for all requests |
| `AllowedIpAddresses` | `List<string>` | Empty | IP address whitelist |
| `BlockedIpAddresses` | `List<string>` | Empty | IP address blacklist |
| `AllowedCidrRanges` | `List<string>` | Empty | CIDR range whitelist |
| `BlockedCidrRanges` | `List<string>` | Empty | CIDR range blacklist |

### MultiTenancyOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultConfigurationSection` | `string` | "ProphyApiClient" | Default configuration section name |
| `TenantsConfigurationSection` | `string` | "Tenants" | Tenant configurations section name |
| `EnableAutomaticTenantResolution` | `bool` | `true` | Enable automatic tenant resolution |
| `EnableConfigurationCaching` | `bool` | `true` | Enable tenant configuration caching |
| `ConfigurationCacheExpirationMinutes` | `int` | 60 | Cache expiration time in minutes |
| `ValidateConfigurationsOnStartup` | `bool` | `true` | Validate configurations at startup |
| `FallbackBehavior` | `TenantFallbackBehavior` | `UseDefault` | Behavior when tenant config not found |

## Service Lifetimes

- **Core Services**: Registered as Singleton (HTTP client wrapper, authenticators, serializers)
- **Configuration**: Registered as Singleton with options pattern support
- **API Clients**: Registered as Scoped for proper disposal and isolation
- **Factories**: Registered as Singleton for efficient client creation

## Best Practices

### 1. Use Scoped Clients for Request Isolation

```csharp
// Good: Scoped registration ensures proper isolation
services.AddProphyApiClient(configuration);

// In controllers/services, inject the client directly
public class ManuscriptController : ControllerBase
{
    private readonly ProphyApiClient _client;
    
    public ManuscriptController(ProphyApiClient client)
    {
        _client = client; // This will be scoped per request
    }
}
```

### 2. Use Factories for Dynamic Client Creation

```csharp
// When you need to create clients dynamically
public class DynamicTenantService
{
    private readonly IProphyApiClientFactory _factory;
    
    public DynamicTenantService(IProphyApiClientFactory factory)
    {
        _factory = factory;
    }
    
    public async Task ProcessMultipleTenantsAsync(string[] tenantIds)
    {
        foreach (var tenantId in tenantIds)
        {
            using var client = _factory.CreateTenantClient(tenantId);
            await ProcessTenantDataAsync(client);
        }
    }
}
```

### 3. Configure Logging Appropriately

```csharp
services.AddProphyApiClient(options =>
{
    options.EnableLogging = true;
    options.LogLevel = LogLevel.Information; // Adjust based on environment
});

// Add logging configuration
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

### 4. Handle Configuration Validation

```csharp
services.AddProphyApiClient(options =>
{
    options.BaseUrl = configuration["ProphyApiClient:BaseUrl"];
    options.ApiKey = configuration["ProphyApiClient:ApiKey"];
    
    // Validate configuration
    if (!options.IsValid())
    {
        var errors = options.GetValidationErrors();
        throw new InvalidOperationException($"Invalid configuration: {string.Join(", ", errors)}");
    }
});
```

## Troubleshooting

### Common Issues

1. **Missing API Key**: Ensure the API key is properly configured in your configuration source.

2. **Tenant Resolution Failures**: Check that tenant identifiers are properly included in HTTP headers or JWT tokens.

3. **Configuration Not Found**: Verify that configuration sections exist and are properly named.

4. **Service Registration Order**: Register base services before multi-tenancy services.

### Debugging

Enable detailed logging to troubleshoot issues:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFilter("Prophy.ApiClient", LogLevel.Debug);
    builder.AddFilter("Prophy.ApiClient.MultiTenancy", LogLevel.Trace);
});
```

## Examples

See the `samples/` directory for complete working examples:

- **AspNetCore.Sample**: ASP.NET Core web application with multi-tenancy
- **ConsoleApp.Sample**: Console application with basic client usage
- **WinForms.Sample**: Windows Forms application with dependency injection

## License

This package is licensed under the MIT License. See the LICENSE file for details. 