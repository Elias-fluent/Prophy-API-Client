using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Configuration;
using AspNet48.Sample.Services;
using System.Configuration;

namespace AspNet48.Sample
{
    public class MvcApplication : HttpApplication
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected void Application_Start()
        {
            // Configure dependency injection
            ConfigureServices();

            // Standard MVC setup
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configure basic logging without console (to avoid System.Text.Json dependency issues)
            services.AddLogging(builder =>
            {
                // Just add the basic logging without console provider
                // Console logging requires System.Text.Json which can cause issues in .NET Framework 4.8
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Configure Prophy API Client
            var apiKey = ConfigurationManager.AppSettings["Prophy:ApiKey"];
            var organizationCode = ConfigurationManager.AppSettings["Prophy:OrganizationCode"];
            var baseUrl = ConfigurationManager.AppSettings["Prophy:BaseUrl"];
            var timeoutString = ConfigurationManager.AppSettings["Prophy:Timeout"];
            var maxRetriesString = ConfigurationManager.AppSettings["Prophy:MaxRetries"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Prophy API Key is not configured. Please set 'Prophy:ApiKey' in appSettings.");
            }

            if (string.IsNullOrEmpty(organizationCode))
            {
                throw new InvalidOperationException("Prophy Organization Code is not configured. Please set 'Prophy:OrganizationCode' in appSettings.");
            }

            var config = new ProphyApiClientConfiguration
            {
                ApiKey = apiKey,
                OrganizationCode = organizationCode,
                BaseUrl = !string.IsNullOrEmpty(baseUrl) ? baseUrl : "https://api.prophy.science"
            };

            // Register Prophy API Client
            services.AddSingleton(config);
            
            // Register application services with the new constructor signature
            services.AddTransient<ProphyService>(provider =>
            {
                var logger = provider.GetService<ILogger<ProphyService>>();
                return new ProphyService(apiKey, organizationCode, logger);
            });

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();
            
            // Set up dependency resolver for MVC
            System.Web.Mvc.DependencyResolver.SetResolver(new CustomDependencyResolver(ServiceProvider));
        }

        protected void Application_End()
        {
            // Properly dispose the service provider for .NET Framework 4.8
            if (ServiceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            var logger = ServiceProvider?.GetService<ILogger<MvcApplication>>();
            logger?.LogError(exception, "Unhandled application error occurred");
        }
    }

    // Custom dependency resolver for MVC
    public class CustomDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomDependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType);
        }
    }
} 