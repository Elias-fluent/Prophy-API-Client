using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AspNet48.Sample.Services;
using AspNet48.Sample.Models;
using Prophy.ApiClient.Exceptions;

namespace AspNet48.Sample.Controllers
{
    public class ManuscriptController : Controller
    {
        private readonly ProphyService _prophyService;
        private readonly ILogger<ManuscriptController> _logger;

        public ManuscriptController()
        {
            var serviceProvider = MvcApplication.ServiceProvider;
            _prophyService = serviceProvider.GetRequiredService<ProphyService>();
            _logger = serviceProvider.GetRequiredService<ILogger<ManuscriptController>>();
        }

        public ActionResult Upload()
        {
            return View(new ManuscriptUploadViewModel());
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Upload(ManuscriptUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation("Processing manuscript upload");

                // Validate file type
                if (model.ManuscriptFile != null)
                {
                    var extension = Path.GetExtension(model.ManuscriptFile.FileName)?.ToLower();
                    if (!new[] { ".pdf", ".docx", ".doc" }.Contains(extension))
                    {
                        ModelState.AddModelError("ManuscriptFile", "Only PDF, DOCX, and DOC files are supported");
                        return View(model);
                    }

                    // Check file size (limit to 50MB)
                    if (model.ManuscriptFile.ContentLength > 50 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ManuscriptFile", "File size cannot exceed 50MB");
                        return View(model);
                    }
                }

                // Process the upload using the service
                var result = await _prophyService.UploadManuscriptAsync(model);

                // Store success information in TempData for the next request
                if (result != null && !string.IsNullOrEmpty(result.ManuscriptIdString))
                {
                    TempData["UploadSuccess"] = true;
                    TempData["ManuscriptId"] = result.ManuscriptIdString;
                    TempData["OriginId"] = result.OriginId;
                    TempData["Title"] = model.Title;
                    TempData["CandidatesCount"] = result.Candidates?.Count ?? 0;
                    return RedirectToAction("Results", new { manuscriptId = result.ManuscriptIdString });
                }
                else
                {
                    ModelState.AddModelError("", "Upload failed: No manuscript ID received");
                    return View(model);
                }
            }
            catch (ProphyApiException ex)
            {
                _logger.LogError(ex, "Prophy API error during upload");
                ModelState.AddModelError("", $"API Error: {ex.Message}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during upload");
                ModelState.AddModelError("", $"Upload failed: {ex.Message}");
                return View(model);
            }
        }

        public async Task<ActionResult> Results(string manuscriptId)
        {
            if (string.IsNullOrEmpty(manuscriptId))
            {
                return RedirectToAction("Index");
            }

            try
            {
                _logger.LogInformation("Loading manuscript results for ID: {ManuscriptId}", manuscriptId);

                var model = new ManuscriptResultsViewModel
                {
                    ManuscriptId = manuscriptId,
                    OriginId = TempData["OriginId"]?.ToString(),
                    Title = TempData["Title"]?.ToString() ?? "Unknown Title"
                };

                // Get manuscript analysis
                try
                {
                    var analysis = await _prophyService.GetManuscriptAnalysisAsync(manuscriptId);
                    model.Analysis = analysis;
                    model.RefereeCount = analysis?.Candidates?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load manuscript analysis");
                    model.AnalysisError = $"Could not load analysis: {ex.Message}";
                }

                // Get referee candidates
                try
                {
                    var referees = await _prophyService.GetRefereeCandidatesAsync(manuscriptId);
                    model.RefereeRecommendations = referees;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load referee candidates");
                    model.RefereeError = $"Could not load referee candidates: {ex.Message}";
                }

                // Get journal recommendations
                try
                {
                    var journals = await _prophyService.GetJournalRecommendationsAsync(manuscriptId);
                    model.JournalRecommendations = journals;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load journal recommendations");
                    model.JournalError = $"Could not load journal recommendations: {ex.Message}";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading results");
                ViewBag.ErrorMessage = $"Error loading results: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetRefereeCandidates(string manuscriptId)
        {
            try
            {
                var candidates = await _prophyService.GetRefereeCandidatesAsync(manuscriptId);
                return Json(candidates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading referee candidates");
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetJournalRecommendations(string manuscriptId)
        {
            try
            {
                var recommendations = await _prophyService.GetJournalRecommendationsAsync(manuscriptId);
                return Json(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading journal recommendations");
                return Json(new { error = ex.Message });
            }
        }
    }
} 