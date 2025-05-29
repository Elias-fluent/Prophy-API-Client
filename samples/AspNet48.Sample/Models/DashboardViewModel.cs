using System;

namespace AspNet48.Sample.Models
{
    public class DashboardViewModel
    {
        public bool IsHealthy { get; set; }
        public DateTime LastChecked { get; set; }
        public int AuthorGroupsCount { get; set; }
        public string ErrorMessage { get; set; }
        
        public string HealthStatus => IsHealthy ? "Healthy" : "Unhealthy";
        public string HealthStatusClass => IsHealthy ? "success" : "danger";
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }
} 