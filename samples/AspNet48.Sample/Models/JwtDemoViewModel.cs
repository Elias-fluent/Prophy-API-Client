using System.ComponentModel.DataAnnotations;

namespace AspNet48.Sample.Models
{
    public class JwtDemoViewModel
    {
        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        [StringLength(100, ErrorMessage = "Subject cannot exceed 100 characters")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Organization is required")]
        [Display(Name = "Organization Code")]
        [StringLength(50, ErrorMessage = "Organization code cannot exceed 50 characters")]
        public string Organization { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Folder")]
        [StringLength(100, ErrorMessage = "Folder cannot exceed 100 characters")]
        public string Folder { get; set; }

        [Display(Name = "Origin ID")]
        [StringLength(100, ErrorMessage = "Origin ID cannot exceed 100 characters")]
        public string OriginId { get; set; }

        // Output properties
        public string GeneratedUrl { get; set; }
        public bool IsGenerated { get; set; }
    }
} 