using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prophy.ApiClient.Models.Entities
{

/// <summary>
/// Represents an author with contact information, affiliations, and publication metrics.
/// </summary>
public class Author
{
    /// <summary>
    /// Gets or sets the author's full name.
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author's first name.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the author's last name.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the author's middle name or initial.
    /// </summary>
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    /// <summary>
    /// Gets or sets the author's primary email address.
    /// </summary>
    [EmailAddress]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets additional email addresses for the author.
    /// </summary>
    [JsonPropertyName("emails")]
    public List<string>? Emails { get; set; }

    /// <summary>
    /// Gets or sets the author's ORCID identifier.
    /// </summary>
    [JsonPropertyName("orcid")]
    public string? Orcid { get; set; }

    /// <summary>
    /// Gets or sets additional ORCID identifiers for the author.
    /// </summary>
    [JsonPropertyName("orcids")]
    public List<string>? Orcids { get; set; }

    /// <summary>
    /// Gets or sets the author's primary institutional affiliation.
    /// </summary>
    [JsonPropertyName("affiliation")]
    public string? Affiliation { get; set; }

    /// <summary>
    /// Gets or sets the author's institutional affiliations.
    /// </summary>
    [JsonPropertyName("affiliations")]
    public List<string>? Affiliations { get; set; }

    /// <summary>
    /// Gets or sets the author's department within their institution.
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// Gets or sets the author's country.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the author's city.
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the author's h-index metric.
    /// </summary>
    [JsonPropertyName("hIndex")]
    public int? HIndex { get; set; }

    /// <summary>
    /// Gets or sets the author's total citation count.
    /// </summary>
    [JsonPropertyName("citationCount")]
    public int? CitationCount { get; set; }

    /// <summary>
    /// Gets or sets the author's publication count.
    /// </summary>
    [JsonPropertyName("publicationCount")]
    public int? PublicationCount { get; set; }

    /// <summary>
    /// Gets or sets the author's research interests or keywords.
    /// </summary>
    [JsonPropertyName("researchInterests")]
    public List<string>? ResearchInterests { get; set; }

    /// <summary>
    /// Gets or sets the author's academic position or title.
    /// </summary>
    [JsonPropertyName("position")]
    public string? Position { get; set; }

    /// <summary>
    /// Gets or sets the author's website or profile URL.
    /// </summary>
    [Url]
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the author.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the date when the author record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the author record was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
} 