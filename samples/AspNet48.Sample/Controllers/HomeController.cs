using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AspNet48.Sample.Services;
using AspNet48.Sample.Models;

namespace AspNet48.Sample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProphyService _prophyService;
        private readonly ILogger<HomeController> _logger;

        public HomeController()
        {
            var serviceProvider = MvcApplication.ServiceProvider;
            _prophyService = serviceProvider.GetRequiredService<ProphyService>();
            _logger = serviceProvider.GetRequiredService<ILogger<HomeController>>();
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading dashboard");

                var model = new DashboardViewModel
                {
                    IsHealthy = await _prophyService.IsHealthyAsync(),
                    LastChecked = DateTime.Now
                };

                // Try to get author groups count for dashboard
                try
                {
                    var authorGroups = await _prophyService.GetAuthorGroupsAsync();
                    model.AuthorGroupsCount = authorGroups.Data?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve author groups count for dashboard");
                    model.AuthorGroupsCount = 0;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                
                var model = new DashboardViewModel
                {
                    IsHealthy = false,
                    LastChecked = DateTime.Now,
                    ErrorMessage = "Unable to connect to Prophy API. Please check your configuration."
                };

                return View(model);
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Prophy API Client Library - ASP.NET Framework 4.8 Sample Application";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact information for Prophy API support.";
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> HealthCheck()
        {
            try
            {
                var isHealthy = await _prophyService.IsHealthyAsync();
                return Json(new { success = true, healthy = isHealthy, timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Json(new { success = false, healthy = false, error = ex.Message, timestamp = DateTime.Now });
            }
        }

        public async Task<ActionResult> CustomFields()
        {
            try
            {
                _logger.LogInformation("Loading custom fields");

                // Get custom fields from the API
                var customFields = await _prophyService.GetCustomFieldsAsync();
                
                return View(customFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading custom fields");
                ViewBag.ErrorMessage = $"Error loading custom fields: {ex.Message}";
                return View();
            }
        }

        public ActionResult JwtDemo()
        {
            _logger.LogInformation("Loading JWT demo");
            
            var model = new JwtDemoViewModel
            {
                Subject = "Demo Organization",
                Organization = "demo-org",
                Email = "demo@example.com",
                Folder = "demo-folder",
                OriginId = "demo-manuscript-123"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateJwt(JwtDemoViewModel model)
        {
            try
            {
                _logger.LogInformation("Generating JWT for demo");

                if (!ModelState.IsValid)
                {
                    return View("JwtDemo", model);
                }

                // Generate JWT token using the service - correct method signature
                var loginUrl = await _prophyService.GenerateJwtLoginUrlAsync(
                    model.OriginId ?? "demo-manuscript",
                    model.Email,
                    model.Folder);

                model.GeneratedUrl = loginUrl;
                model.IsGenerated = true;

                return View("JwtDemo", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT");
                ModelState.AddModelError("", $"Error generating JWT: {ex.Message}");
                return View("JwtDemo", model);
            }
        }
    }
} 