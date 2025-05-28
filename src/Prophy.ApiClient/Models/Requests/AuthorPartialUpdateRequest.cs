using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Request model for partially updating an author in an author group.
    /// Only properties that are set (non-null) will be updated in the API.
    /// </summary>
    public class AuthorPartialUpdateRequest
    {
        /// <summary>
        /// Gets or sets the full name of the author.
        /// If null, the name will not be updated.
        /// </summary>
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Author name must be between 1 and 200 characters")]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the first name of the author.
        /// If null, the first name will not be updated.
        /// </summary>
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the author.
        /// If null, the last name will not be updated.
        /// </summary>
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the list of email addresses for the author.
        /// If null, the emails will not be updated.
        /// </summary>
        [JsonPropertyName("emails")]
        public List<string>? Emails { get; set; }

        /// <summary>
        /// Gets or sets the list of ORCID identifiers for the author.
        /// If null, the ORCIDs will not be updated.
        /// </summary>
        [JsonPropertyName("orcids")]
        public List<string>? Orcids { get; set; }

        /// <summary>
        /// Gets or sets the list of affiliations for the author.
        /// If null, the affiliations will not be updated.
        /// </summary>
        [JsonPropertyName("affiliations")]
        public List<string>? Affiliations { get; set; }

        /// <summary>
        /// Gets or sets the primary affiliation of the author.
        /// If null, the primary affiliation will not be updated.
        /// </summary>
        [StringLength(500, ErrorMessage = "Primary affiliation cannot exceed 500 characters")]
        [JsonPropertyName("primary_affiliation")]
        public string? PrimaryAffiliation { get; set; }

        /// <summary>
        /// Gets or sets the country of the author.
        /// If null, the country will not be updated.
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the research areas or keywords for the author.
        /// If null, the research areas will not be updated.
        /// </summary>
        [JsonPropertyName("research_areas")]
        public List<string>? ResearchAreas { get; set; }

        /// <summary>
        /// Gets or sets the H-index of the author.
        /// If null, the H-index will not be updated.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "H-index must be a non-negative number")]
        [JsonPropertyName("h_index")]
        public int? HIndex { get; set; }

        /// <summary>
        /// Gets or sets the total number of citations for the author.
        /// If null, the citation count will not be updated.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Citation count must be a non-negative number")]
        [JsonPropertyName("citation_count")]
        public int? CitationCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of publications for the author.
        /// If null, the publication count will not be updated.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Publication count must be a non-negative number")]
        [JsonPropertyName("publication_count")]
        public int? PublicationCount { get; set; }

        /// <summary>
        /// Gets or sets whether the author is active in the group.
        /// If null, the active status will not be updated.
        /// </summary>
        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the author.
        /// If null, the metadata will not be updated.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets custom fields specific to the organization.
        /// If null, the custom fields will not be updated.
        /// </summary>
        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object>? CustomFields { get; set; }

        /// <summary>
        /// Validates the request to ensure all provided fields are valid.
        /// </summary>
        /// <returns>A list of validation errors, if any.</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Validate name if provided
            if (Name != null)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    errors.Add("Author name cannot be empty when provided");
                }
                else if (Name.Length > 200)
                {
                    errors.Add("Author name cannot exceed 200 characters");
                }
            }

            // Validate first name if provided
            if (FirstName != null && FirstName.Length > 100)
            {
                errors.Add("First name cannot exceed 100 characters");
            }

            // Validate last name if provided
            if (LastName != null && LastName.Length > 100)
            {
                errors.Add("Last name cannot exceed 100 characters");
            }

            // Validate primary affiliation if provided
            if (PrimaryAffiliation != null && PrimaryAffiliation.Length > 500)
            {
                errors.Add("Primary affiliation cannot exceed 500 characters");
            }

            // Validate country if provided
            if (Country != null && Country.Length > 100)
            {
                errors.Add("Country cannot exceed 100 characters");
            }

            // Validate email addresses if provided
            if (Emails != null)
            {
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
            }

            // Validate ORCID format if provided
            if (Orcids != null)
            {
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
            }

            // Validate numeric fields if provided
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
        /// Checks if any fields are set for update.
        /// </summary>
        /// <returns>True if at least one field is set for update, false otherwise.</returns>
        public bool HasUpdates()
        {
            return Name != null ||
                   FirstName != null ||
                   LastName != null ||
                   Emails != null ||
                   Orcids != null ||
                   Affiliations != null ||
                   PrimaryAffiliation != null ||
                   Country != null ||
                   ResearchAreas != null ||
                   HIndex.HasValue ||
                   CitationCount.HasValue ||
                   PublicationCount.HasValue ||
                   IsActive.HasValue ||
                   Metadata != null ||
                   CustomFields != null;
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

            // Remove any whitespace and convert to uppercase
            orcid = orcid.Trim().ToUpperInvariant();

            // Check if it matches the ORCID pattern: 0000-0000-0000-000X (where X can be 0-9 or X)
            if (orcid.Length != 19)
                return false;

            // Check the format: ####-####-####-####
            if (orcid[4] != '-' || orcid[9] != '-' || orcid[14] != '-')
                return false;

            // Check each digit group
            for (int i = 0; i < 19; i++)
            {
                if (i == 4 || i == 9 || i == 14)
                    continue; // Skip hyphens

                if (i == 18)
                {
                    // Last character can be 0-9 or X
                    if (!char.IsDigit(orcid[i]) && orcid[i] != 'X')
                        return false;
                }
                else
                {
                    // All other characters must be digits
                    if (!char.IsDigit(orcid[i]))
                        return false;
                }
            }

            return true;
        }
    }
} 