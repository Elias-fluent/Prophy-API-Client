using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Requests
{
    /// <summary>
    /// Request model for creating a new author group.
    /// </summary>
    public class CreateAuthorGroupRequest
    {
        /// <summary>
        /// Gets or sets the name of the author group.
        /// </summary>
        [Required(ErrorMessage = "Group name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Group name must be between 1 and 200 characters")]
        [JsonPropertyName("group_name")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the author group.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the owner team for the author group.
        /// The owner team has full administrative rights over the group.
        /// </summary>
        [Required(ErrorMessage = "Owner team is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Owner team must be between 1 and 100 characters")]
        [JsonPropertyName("owner_team")]
        public string OwnerTeam { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of editor teams.
        /// Editor teams can modify authors and group settings.
        /// </summary>
        [JsonPropertyName("editor_teams")]
        public List<string> EditorTeams { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of viewer teams.
        /// Viewer teams can only view the group and its authors.
        /// </summary>
        [JsonPropertyName("viewer_teams")]
        public List<string> ViewerTeams { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets additional metadata for the author group.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Validates the request to ensure all required fields are present and valid.
        /// </summary>
        /// <returns>A list of validation errors, if any.</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(GroupName))
            {
                errors.Add("Group name is required and cannot be empty");
            }
            else if (GroupName.Length > 200)
            {
                errors.Add("Group name cannot exceed 200 characters");
            }

            if (string.IsNullOrWhiteSpace(OwnerTeam))
            {
                errors.Add("Owner team is required and cannot be empty");
            }
            else if (OwnerTeam.Length > 100)
            {
                errors.Add("Owner team cannot exceed 100 characters");
            }

            if (!string.IsNullOrEmpty(Description) && Description.Length > 1000)
            {
                errors.Add("Description cannot exceed 1000 characters");
            }

            // Validate team names
            foreach (var team in EditorTeams)
            {
                if (string.IsNullOrWhiteSpace(team))
                {
                    errors.Add("Editor team names cannot be empty");
                    break;
                }
                if (team.Length > 100)
                {
                    errors.Add($"Editor team name '{team}' exceeds 100 characters");
                }
            }

            foreach (var team in ViewerTeams)
            {
                if (string.IsNullOrWhiteSpace(team))
                {
                    errors.Add("Viewer team names cannot be empty");
                    break;
                }
                if (team.Length > 100)
                {
                    errors.Add($"Viewer team name '{team}' exceeds 100 characters");
                }
            }

            return errors;
        }
    }
} 