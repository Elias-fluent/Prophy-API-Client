using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Request model for adding or updating an author in an author group.
    /// </summary>
    public class AuthorFromGroupRequest
    {
        /// <summary>
        /// Gets or sets the full name of the author.
        /// </summary>
        [Required(ErrorMessage = "Author name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Author name must be between 1 and 200 characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first name of the author.
        /// </summary>
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the author.
        /// </summary>
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the list of email addresses for the author.
        /// </summary>
        [JsonPropertyName("emails")]
        public List<string> Emails { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of ORCID identifiers for the author.
        /// </summary>
        [JsonPropertyName("orcids")]
        public List<string> Orcids { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of affiliations for the author.
        /// </summary>
        [JsonPropertyName("affiliations")]
        public List<string> Affiliations { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the primary affiliation of the author.
        /// </summary>
        [StringLength(500, ErrorMessage = "Primary affiliation cannot exceed 500 characters")]
        [JsonPropertyName("primary_affiliation")]
        public string? PrimaryAffiliation { get; set; }

        /// <summary>
        /// Gets or sets the country of the author.
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the research areas or keywords for the author.
        /// </summary>
        [JsonPropertyName("research_areas")]
        public List<string> ResearchAreas { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the H-index of the author.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "H-index must be a non-negative number")]
        [JsonPropertyName("h_index")]
        public int? HIndex { get; set; }

        /// <summary>
        /// Gets or sets the total number of citations for the author.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Citation count must be a non-negative number")]
        [JsonPropertyName("citation_count")]
        public int? CitationCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of publications for the author.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Publication count must be a non-negative number")]
        [JsonPropertyName("publication_count")]
        public int? PublicationCount { get; set; }

        /// <summary>
        /// Gets or sets whether the author is active in the group.
        /// </summary>
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets additional metadata for the author.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets custom fields specific to the organization.
        /// </summary>
        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object>? CustomFields { get; set; }

        /// <summary>
        /// Validates the request to ensure all required fields are present and valid.
        /// </summary>
        /// <returns>A list of validation errors, if any.</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("Author name is required and cannot be empty");
            }
            else if (Name.Length > 200)
            {
                errors.Add("Author name cannot exceed 200 characters");
            }

            // Validate email addresses
            foreach (var email in Emails)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    errors.Add("Email addresses cannot be empty");
                    break;
                }
                if (!IsValidEmail(email))
                {
                    errors.Add($"Invalid email format: {email}");
                }
            }

            // Validate ORCID format
            foreach (var orcid in Orcids)
            {
                if (string.IsNullOrWhiteSpace(orcid))
                {
                    errors.Add("ORCID identifiers cannot be empty");
                    break;
                }
                if (!IsValidOrcid(orcid))
                {
                    errors.Add($"Invalid ORCID format: {orcid}. Expected format: 0000-0000-0000-0000");
                }
            }

            // Validate numeric fields
            if (HIndex.HasValue && HIndex.Value < 0)
            {
                errors.Add("H-index must be a non-negative number");
            }

            if (CitationCount.HasValue && CitationCount.Value < 0)
            {
                errors.Add("Citation count must be a non-negative number");
            }

            if (PublicationCount.HasValue && PublicationCount.Value < 0)
            {
                errors.Add("Publication count must be a non-negative number");
            }

            return errors;
        }

        /// <summary>
        /// Validates email format using a simple regex pattern.
        /// </summary>
        /// <param name="email">The email to validate.</param>
        /// <returns>True if the email format is valid, false otherwise.</returns>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates ORCID format (0000-0000-0000-0000).
        /// </summary>
        /// <param name="orcid">The ORCID to validate.</param>
        /// <returns>True if the ORCID format is valid, false otherwise.</returns>
        private static bool IsValidOrcid(string orcid)
        {
            if (string.IsNullOrWhiteSpace(orcid))
                return false;

            // ORCID format: 0000-0000-0000-0000 (with optional https://orcid.org/ prefix)
            var cleanOrcid = orcid.Replace("https://orcid.org/", "").Replace("http://orcid.org/", "");
            
            return System.Text.RegularExpressions.Regex.IsMatch(cleanOrcid, @"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$");
        }
    }
} 