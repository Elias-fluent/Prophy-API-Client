# Getting Started with Prophy API Client Library

## Installation

Install the Prophy API Client Library via NuGet:

```bash
dotnet add package Prophy.ApiClient
```

## Basic Setup

```csharp
using Prophy.ApiClient;

// Initialize the client with your API key and organization code
var client = new ProphyApiClient("your-api-key", "your-organization-code");
```

## Configuration

### Using appsettings.json

```json
{
  "Prophy": {
    "ApiKey": "your-api-key",
    "OrganizationCode": "your-org-code",
    "BaseUrl": "https://www.prophy.ai/api/",
    "Timeout": "00:01:00"
  }
}
```

### Dependency Injection (ASP.NET Core)

```csharp
// In Startup.cs or Program.cs
services.AddProphyApiClient(configuration);

// Or with options
services.AddProphyApiClient(options =>
{
    options.ApiKey = "your-api-key";
    options.OrganizationCode = "your-org-code";
});
```

## Quick Examples

### Upload a Manuscript

```csharp
var manuscript = new ManuscriptUploadRequest
{
    Title = "Research Paper Title",
    Abstract = "Paper abstract...",
    Authors = new[] { 
        new Author { Name = "Dr. John Doe", Email = "john@university.edu" }
    },
    SourceFile = await File.ReadAllBytesAsync("manuscript.pdf"),
    Folder = "journal-name"
};

var result = await client.Manuscripts.UploadAsync(manuscript);
var referees = result.Candidates;
```

### Get Journal Recommendations

```csharp
var recommendations = await client.Journals.GetRecommendationsAsync("manuscript-id");
foreach (var journal in recommendations.Journals)
{
    Console.WriteLine($"{journal.Title} - Relevance: {journal.RelevanceScore}");
}
```

### Create Author Group

```csharp
var groupRequest = new CreateAuthorGroupRequest
{
    GroupName = "2024 Physics Experts",
    OwnerTeam = "Admin Team",
    EditorTeams = new[] { "Editorial Board" }
};

var group = await client.AuthorGroups.CreateAsync(groupRequest);
```

## Next Steps

- Check out the [API Reference](api-reference/) for detailed documentation
- Explore the [sample applications](../samples/) for complete examples
- Review the [examples](examples/) for specific use cases 