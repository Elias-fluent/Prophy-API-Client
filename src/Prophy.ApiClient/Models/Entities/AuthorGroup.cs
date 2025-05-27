using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Entities
{
    /// <summary>
    /// Represents an author group in the Prophy system.
    /// Author groups allow organizing authors with team-based permissions and configurations.
    /// </summary>
    public class AuthorGroup
    {
        /// <summary>
        /// Gets or sets the unique identifier for the author group.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the author group.
        /// </summary>
        [JsonPropertyName("group_name")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the author group.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the owner team for the author group.
        /// The owner team has full administrative rights over the group.
        /// </summary>
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
        /// Gets or sets the number of authors in the group.
        /// </summary>
        [JsonPropertyName("author_count")]
        public int AuthorCount { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the author group.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last modification date of the author group.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who created the author group.
        /// </summary>
        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified the author group.
        /// </summary>
        [JsonPropertyName("updated_by")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets whether the group is active.
        /// </summary>
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the organization code this group belongs to.
        /// </summary>
        [JsonPropertyName("organization")]
        public string? Organization { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the author group.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the list of authors in the group (when included in response).
        /// </summary>
        [JsonPropertyName("authors")]
        public List<Author>? Authors { get; set; }

        /// <summary>
        /// Returns a string representation of the author group.
        /// </summary>
        /// <returns>A string containing the group name and author count.</returns>
        public override string ToString()
        {
            return $"{GroupName} ({AuthorCount} authors)";
        }
    }
} 