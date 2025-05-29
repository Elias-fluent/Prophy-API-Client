# Prophy API Client - ASP.NET Framework 4.8 Sample Application

This sample application demonstrates comprehensive integration with the Prophy API using the `Prophy.ApiClient` library in an ASP.NET Framework 4.8 MVC web application. It showcases real-world usage patterns, error handling, and all major API features.

## Features Demonstrated

### 🔐 Authentication & Security
- **API Key Authentication**: Secure API key management and X-ApiKey header configuration
- **JWT Token Generation**: Create JWT tokens for seamless user authentication
- **Secure Configuration**: Environment-based configuration management
- **Error Handling**: Comprehensive error handling and user feedback

### 📄 Manuscript Management
- **File Upload**: Upload PDF/DOCX manuscripts with metadata
- **Referee Candidates**: Retrieve and display potential reviewers
- **Journal Recommendations**: Get journal suggestions based on manuscript content
- **Analysis Results**: Display comprehensive manuscript analysis

### 👥 Author Group Management
- **Group Operations**: Create and manage author groups
- **Author Management**: Add authors with affiliations and contact information
- **Partial Updates**: Demonstrate partial update capabilities

### ⚙️ Custom Fields
- **Field Discovery**: Retrieve and display custom field definitions
- **Dynamic Handling**: Show different field types and validation rules
- **Organization-specific**: Filter fields by entity type

### 🎯 Additional Features
- **Health Monitoring**: API connectivity and health checks
- **Responsive UI**: Modern Bootstrap 5 interface
- **Comprehensive Logging**: Detailed request/response logging
- **Real-time Feedback**: Progress indicators and status updates

## Prerequisites

- **.NET Framework 4.8** or higher
- **Visual Studio 2019** or higher (or Visual Studio Code with appropriate extensions)
- **IIS Express** (included with Visual Studio)
- **Prophy API Key** (contact Prophy support for access)

## Project Structure

```
AspNet48.Sample/
├── Controllers/
│   ├── HomeController.cs          # Dashboard and utility endpoints
│   └── ManuscriptController.cs    # Manuscript upload and processing
├── Models/
│   ├── DashboardViewModel.cs      # Dashboard data model
│   ├── JwtDemoViewModel.cs        # JWT generation form model
│   ├── ManuscriptUploadViewModel.cs   # File upload form model
│   └── ManuscriptResultsViewModel.cs  # Results display model
├── Services/
│   └── ProphyService.cs           # API client wrapper service
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml           # Dashboard and landing page
│   │   ├── About.cshtml           # Application information
│   │   ├── Contact.cshtml         # Support and contact info
│   │   ├── CustomFields.cshtml    # Custom fields discovery
│   │   └── JwtDemo.cshtml         # JWT token generation
│   ├── Manuscript/
│   │   ├── Upload.cshtml          # File upload interface
│   │   └── Results.cshtml         # Analysis results display
│   └── Shared/
│       └── _Layout.cshtml         # Main layout template
├── App_Start/
│   ├── RouteConfig.cs             # MVC routing configuration
│   └── FilterConfig.cs            # Global filters
├── Global.asax.cs                 # Application startup and DI setup
├── Web.config                     # Application configuration
└── packages.config                # NuGet package references
```

## Setup Instructions

### 1. Clone and Build

```bash
# Clone the repository (if not already done)
git clone <repository-url>
cd samples/AspNet48.Sample

# Restore NuGet packages
nuget restore

# Build the solution
msbuild AspNet48.Sample.csproj
```

### 2. Configure API Access

#### Option A: Web.config (Recommended for Development)
Add your Prophy API key to the `web.config` file:

```xml
<configuration>
  <appSettings>
    <add key="Prophy:ApiKey" value="your-api-key-here" />
    <add key="Prophy:BaseUrl" value="https://api.prophy.ai/" />
    <add key="Prophy:OrganizationCode" value="your-org-code" />
  </appSettings>
</configuration>
```

#### Option B: Environment Variables (Recommended for Production)
Set the following environment variables:

```bash
PROPHY_API_KEY=your-api-key-here
PROPHY_BASE_URL=https://api.prophy.ai/
PROPHY_ORGANIZATION_CODE=your-org-code
```

### 3. Run the Application

#### Using Visual Studio:
1. Open `AspNet48.Sample.csproj` in Visual Studio
2. Press `F5` or click "Start Debugging"
3. The application will launch in your default browser

#### Using IIS Express (Command Line):
```bash
# Navigate to the project directory
cd samples/AspNet48.Sample

# Start IIS Express
"C:\Program Files\IIS Express\iisexpress.exe" /path:. /port:8080
```

#### Using IIS:
1. Copy the application to your IIS web directory
2. Create a new application in IIS Manager
3. Ensure the application pool is set to .NET Framework 4.8
4. Configure appropriate permissions for file uploads

## Configuration Options

### API Settings
| Setting | Description | Required | Default |
|---------|-------------|----------|---------|
| `Prophy:ApiKey` | Your Prophy API key | Yes | - |
| `Prophy:BaseUrl` | Prophy API base URL | No | `https://api.prophy.ai/` |
| `Prophy:OrganizationCode` | Your organization code | No | - |
| `Prophy:Timeout` | Request timeout (seconds) | No | `30` |

### Logging Configuration
The application uses Microsoft.Extensions.Logging with console and debug providers. Adjust logging levels in `Global.asax.cs`:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

### File Upload Settings
Configure maximum file sizes in `web.config`:

```xml
<system.web>
  <httpRuntime maxRequestLength="51200" /> <!-- 50MB in KB -->
</system.web>
<system.webServer>
  <security>
    <requestFiltering>
      <requestLimits maxAllowedContentLength="52428800" /> <!-- 50MB in bytes -->
    </requestFiltering>
  </security>
</system.webServer>
```

## Usage Guide

### 1. Dashboard (Home Page)
- **Health Check**: Verify API connectivity
- **Author Groups Count**: Display current author groups
- **Quick Navigation**: Access all features from the main dashboard

### 2. Manuscript Upload
1. Navigate to "Upload Manuscript"
2. Fill in manuscript details (title, abstract, author information)
3. Select a PDF or DOCX file
4. Click "Upload and Analyze"
5. View results including referee candidates and journal recommendations

### 3. Custom Fields Discovery
1. Navigate to "Custom Fields"
2. View all custom field definitions for your organization
3. See field types, validation rules, and available options
4. Understand how to integrate custom fields in your applications

### 4. JWT Authentication Demo
1. Navigate to "JWT Demo"
2. Fill in user information (subject, organization, email)
3. Optionally provide folder and origin ID context
4. Generate a JWT login URL
5. Test the generated URL for authentication

## API Integration Examples

### Basic API Call Pattern
```csharp
public async Task<ActionResult> ExampleApiCall()
{
    try
    {
        _logger.LogInformation("Making API call");
        
        var result = await _prophyService.SomeApiMethodAsync();
        
        _logger.LogInformation("API call successful");
        return View(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "API call failed");
        ModelState.AddModelError("", $"Error: {ex.Message}");
        return View();
    }
}
```

### File Upload with Progress
```csharp
public async Task<ActionResult> UploadManuscript(ManuscriptUploadViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    try
    {
        using (var stream = model.ManuscriptFile.InputStream)
        {
            var response = await _prophyService.UploadManuscriptAsync(
                stream, 
                model.ManuscriptFile.FileName,
                model.Title,
                model.Abstract);
                
            return RedirectToAction("Results", new { originId = response.OriginId });
        }
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", $"Upload failed: {ex.Message}");
        return View(model);
    }
}
```

## Error Handling

The application implements comprehensive error handling at multiple levels:

### 1. Service Level
- API exceptions are caught and logged
- Meaningful error messages are returned
- Retry logic for transient failures

### 2. Controller Level
- Model validation errors
- User-friendly error messages
- Graceful degradation

### 3. View Level
- Error alerts and notifications
- Form validation feedback
- Loading states and progress indicators

## Security Considerations

### API Key Management
- **Never commit API keys to source control**
- Use environment variables or secure configuration
- Rotate keys regularly
- Monitor API usage for anomalies

### File Upload Security
- Validate file types and sizes
- Scan uploaded files for malware
- Store files in secure locations
- Implement proper access controls

### JWT Security
- Use strong secret keys (minimum 256 bits)
- Set appropriate expiration times
- Validate tokens on the server side
- Use HTTPS for all token transmission

## Troubleshooting

### Common Issues

#### 1. API Key Authentication Fails
```
Error: Unauthorized (401)
```
**Solution**: Verify your API key is correct and has proper permissions.

#### 2. File Upload Fails
```
Error: Request entity too large
```
**Solution**: Increase `maxRequestLength` and `maxAllowedContentLength` in web.config.

#### 3. Dependency Injection Errors
```
Error: No service for type 'IProphyApiClient' has been registered
```
**Solution**: Ensure services are properly registered in `Global.asax.cs`.

### Debugging Tips

1. **Enable Detailed Logging**: Set log level to `Debug` for verbose output
2. **Check Network Connectivity**: Verify API endpoint accessibility
3. **Validate Configuration**: Ensure all required settings are present
4. **Monitor API Responses**: Check response headers and status codes

## Performance Optimization

### 1. Async/Await Pattern
All API calls use async/await for non-blocking operations:

```csharp
public async Task<ActionResult> Index()
{
    var healthTask = _prophyService.IsHealthyAsync();
    var groupsTask = _prophyService.GetAuthorGroupsAsync();
    
    await Task.WhenAll(healthTask, groupsTask);
    
    // Process results...
}
```

### 2. Caching Strategies
Consider implementing caching for:
- Custom field definitions
- Author group lists
- Health check results

### 3. Connection Pooling
The HttpClient is configured for connection reuse and pooling.

## Testing

### Manual Testing Checklist
- [ ] Dashboard loads and shows health status
- [ ] File upload accepts PDF/DOCX files
- [ ] Manuscript analysis returns results
- [ ] Custom fields display correctly
- [ ] JWT generation works
- [ ] Error handling displays user-friendly messages
- [ ] All navigation links work
- [ ] Responsive design works on mobile

### API Testing
Use tools like Postman or curl to test API endpoints directly:

```bash
curl -X GET "https://api.prophy.ai/external/health" \
  -H "X-ApiKey: your-api-key"
```

## Deployment

### IIS Deployment
1. Build the application in Release mode
2. Copy files to IIS web directory
3. Configure application pool (.NET Framework 4.8)
4. Set environment variables or update web.config
5. Configure SSL certificate for HTTPS

### Azure App Service
1. Create an App Service with .NET Framework 4.8
2. Configure application settings for API keys
3. Deploy using Visual Studio or Azure DevOps
4. Enable Application Insights for monitoring

## Support and Resources

### Documentation
- [Prophy API Documentation](https://docs.prophy.ai)
- [ASP.NET MVC Documentation](https://docs.microsoft.com/en-us/aspnet/mvc/)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)

### Getting Help
- **API Issues**: Contact Prophy support at support@prophy.ai
- **Technical Issues**: Check the GitHub repository issues
- **Feature Requests**: Submit via the developer portal

### Sample Data
For testing purposes, you can use the sample files in the `SampleFiles/` directory:
- `sample-manuscript.pdf`: Example research paper
- `sample-manuscript.docx`: Example Word document

## License

This sample application is provided under the same license as the Prophy.ApiClient library. See the main repository LICENSE file for details.

## Contributing

Contributions to improve this sample application are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

---

**Note**: This is a demonstration application. For production use, implement additional security measures, error handling, and monitoring as appropriate for your environment. 