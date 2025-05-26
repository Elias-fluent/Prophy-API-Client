# Prophy API Client Library

A lightweight .NET Standard 2.0 class library for seamless integration with the Prophy API. This library provides a clean, intuitive interface for .NET developers to interact with Prophy's Scientific Knowledge Management platform without dealing with low-level HTTP operations, JSON serialization, or authentication complexities.

## Features

- **🔐 Authentication Management**: Seamless API key and JWT-based authentication
- **📄 Manuscript Management**: Upload manuscripts and retrieve referee candidates
- **📚 Journal Recommendations**: Get journal suggestions based on manuscript analysis
- **👥 Author Groups**: Full CRUD operations for author group management
- **🔧 Custom Fields**: Dynamic handling of organization-specific fields
- **🔗 Webhook Support**: Process webhook notifications for manuscript events
- **🏢 Multi-Organization**: Support for multiple organizations with isolated contexts
- **⚡ Async/Await**: Full async support for all operations
- **🛡️ Type Safety**: Strongly typed models for all API interactions

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package Prophy.ApiClient
```

Or via Package Manager Console:

```powershell
Install-Package Prophy.ApiClient
```

## Quick Start

### Basic Setup

```csharp
using Prophy.ApiClient;

var client = new ProphyApiClient("your-api-key", "your-organization-code");
```

### Upload Manuscript and Get Referee Candidates

```csharp
var manuscript = new ManuscriptUploadRequest
{
    Title = "Advanced Machine Learning Techniques",
    Abstract = "This paper explores...",
    Authors = new[] { 
        new Author { Name = "Dr. John Doe", Email = "john@university.edu" }
    },
    SourceFile = await File.ReadAllBytesAsync("manuscript.pdf"),
    Folder = "ai-research"
};

var result = await client.Manuscripts.UploadAsync(manuscript);
var referees = result.Candidates; // List of qualified referee candidates
```

### Get Journal Recommendations

```csharp
var recommendations = await client.Journals.GetRecommendationsAsync("manuscript-id");
foreach (var journal in recommendations.Journals)
{
    Console.WriteLine($"{journal.Title} - Relevance: {journal.RelevanceScore}");
}
```

### Manage Author Groups

```csharp
var groupRequest = new CreateAuthorGroupRequest
{
    GroupName = "2024 Physics Experts",
    OwnerTeam = "Admin Team",
    EditorTeams = new[] { "Editorial Board" }
};

var group = await client.AuthorGroups.CreateAsync(groupRequest);

var author = new AuthorFromGroupRequest
{
    Name = "Dr. Jane Smith",
    Emails = new[] { "jane@university.edu" },
    Orcids = new[] { "0000-0000-0000-0001" }
};

await client.AuthorGroups.AddAuthorAsync(group.Id, "client-id-123", author);
```

### Generate JWT Login URL

```csharp
var jwtClaims = new JwtLoginClaims
{
    Subject = "Flexigrant",
    Organization = "Flexigrant",
    Email = "user@example.com",
    Folder = "manuscript-folder",
    OriginId = "manuscript-123"
};

var loginUrl = client.Authentication.GenerateLoginUrl(jwtClaims, "your-jwt-secret");
// Redirect user to loginUrl for seamless Prophy access
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

## API Coverage

This library covers all major Prophy API endpoints:

- ✅ **Authentication**: X-ApiKey header and JWT-based authentication
- ✅ **Manuscript Upload**: POST `/api/external/proposal/` with multipart form data
- ✅ **Journal Recommendations**: GET `/api/external/recommend-journals/{origin_id}/`
- ✅ **Custom Fields**: GET `/api/external/custom-fields/all/`
- ✅ **Author Groups**: POST `/api/external/authors-group/create/`
- ✅ **Authors from Groups**: Full CRUD at `/api/external/author-from-group/{group_id}/{client_id}/`
- ✅ **User Login**: JWT-based login at `/api/auth/api-jwt-login/`
- ✅ **Webhooks**: `mark_as_proposal_referee` event handling

## Requirements

- .NET Standard 2.0 compatible framework:
  - .NET Framework 4.6.1+
  - .NET Core 2.0+
  - .NET 5+

## Development

### Building the Project

```bash
git clone https://github.com/Elias-fluent/Prophy-API-Client.git
cd Prophy-API-Client
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Sample Applications

Check out the `samples/` directory for complete examples:

- **ConsoleApp.Sample**: Basic console application demonstrating core features
- **AspNetCore.Sample**: ASP.NET Core web application with dependency injection
- **WinForms.Sample**: Windows Forms application for desktop scenarios

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [API Documentation](https://www.prophy.ai/api-docs/)
- 🐛 [Report Issues](https://github.com/Elias-fluent/Prophy-API-Client/issues)
- 💬 [Discussions](https://github.com/Elias-fluent/Prophy-API-Client/discussions)

## Acknowledgments

- [Prophy](https://www.prophy.ai/) for providing the Scientific Knowledge Management platform
- The .NET community for excellent tooling and libraries 