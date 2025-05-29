using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace AspNet48.Sample.Models
{
    public class ManuscriptUploadViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [Display(Name = "Manuscript Title")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Display(Name = "Abstract")]
        [StringLength(5000, ErrorMessage = "Abstract cannot exceed 5000 characters")]
        public string Abstract { get; set; }

        [Display(Name = "Manuscript File")]
        [Required(ErrorMessage = "Please select a file to upload")]
        public HttpPostedFileBase ManuscriptFile { get; set; }

        [Display(Name = "Folder/Journal")]
        [StringLength(100, ErrorMessage = "Folder name cannot exceed 100 characters")]
        public string Folder { get; set; }

        /// <summary>
        /// Gets or sets the journal or folder for the manuscript
        /// </summary>
        [Display(Name = "Target Journal")]
        [StringLength(100, ErrorMessage = "Journal name cannot exceed 100 characters")]
        public string Journal { get; set; }

        /// <summary>
        /// Gets or sets the origin ID for tracking purposes
        /// </summary>
        [Display(Name = "Origin ID")]
        [StringLength(100, ErrorMessage = "Origin ID cannot exceed 100 characters")]
        public string OriginId { get; set; }

        /// <summary>
        /// Gets or sets the list of authors for the manuscript
        /// </summary>
        public List<AuthorInfo> Authors { get; set; } = new List<AuthorInfo>();

        [Display(Name = "Author Name")]
        [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters")]
        public string AuthorName { get; set; }

        [Display(Name = "Author Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string AuthorEmail { get; set; }

        [Display(Name = "Author Affiliation")]
        [StringLength(200, ErrorMessage = "Affiliation cannot exceed 200 characters")]
        public string AuthorAffiliation { get; set; }

        /// <summary>
        /// Gets or sets the manuscript keywords
        /// </summary>
        [Display(Name = "Keywords")]
        [StringLength(500, ErrorMessage = "Keywords cannot exceed 500 characters")]
        public string Keywords { get; set; }

        /// <summary>
        /// Gets or sets the manuscript subject area
        /// </summary>
        [Display(Name = "Subject Area")]
        [StringLength(100, ErrorMessage = "Subject cannot exceed 100 characters")]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the manuscript language
        /// </summary>
        [Display(Name = "Language")]
        [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
        public string Language { get; set; } = "en";

        /// <summary>
        /// Gets or sets the file content as byte array (populated from ManuscriptFile)
        /// </summary>
        public byte[] FileContent { get; set; }

        /// <summary>
        /// Gets or sets the original file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the uploaded file
        /// </summary>
        public string MimeType { get; set; }

        // Referee candidate filtering properties
        /// <summary>
        /// Gets or sets the minimum H-index for referee candidates
        /// </summary>
        [Display(Name = "Minimum H-Index")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimum H-Index must be a non-negative number")]
        public int? MinHIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum H-index for referee candidates
        /// </summary>
        [Display(Name = "Maximum H-Index")]
        [Range(0, int.MaxValue, ErrorMessage = "Maximum H-Index must be a non-negative number")]
        public int? MaxHIndex { get; set; }

        /// <summary>
        /// Gets or sets the minimum academic age for referee candidates
        /// </summary>
        [Display(Name = "Minimum Academic Age")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimum Academic Age must be a non-negative number")]
        public int? MinAcademicAge { get; set; }

        /// <summary>
        /// Gets or sets the maximum academic age for referee candidates
        /// </summary>
        [Display(Name = "Maximum Academic Age")]
        [Range(0, int.MaxValue, ErrorMessage = "Maximum Academic Age must be a non-negative number")]
        public int? MaxAcademicAge { get; set; }

        /// <summary>
        /// Gets or sets the minimum articles count for referee candidates
        /// </summary>
        [Display(Name = "Minimum Articles Count")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimum Articles Count must be a non-negative number")]
        public int? MinArticlesCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum articles count for referee candidates
        /// </summary>
        [Display(Name = "Maximum Articles Count")]
        [Range(0, int.MaxValue, ErrorMessage = "Maximum Articles Count must be a non-negative number")]
        public int? MaxArticlesCount { get; set; }

        /// <summary>
        /// Gets or sets the list of candidates to exclude (by author ID or name)
        /// </summary>
        [Display(Name = "Exclude Candidates (comma-separated)")]
        public string ExcludeCandidates { get; set; }
    }

    /// <summary>
    /// Represents author information for manuscript upload
    /// </summary>
    public class AuthorInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Affiliation { get; set; }
    }
} 