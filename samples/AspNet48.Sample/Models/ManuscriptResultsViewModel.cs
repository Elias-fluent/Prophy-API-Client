using System;
using System.Collections.Generic;
using Prophy.ApiClient.Models.Responses;

namespace AspNet48.Sample.Models
{
    /// <summary>
    /// ViewModel for displaying manuscript analysis results from Prophy API
    /// </summary>
    public class ManuscriptResultsViewModel
    {
        /// <summary>
        /// Gets or sets the manuscript ID
        /// </summary>
        public string ManuscriptId { get; set; }

        /// <summary>
        /// Gets or sets the manuscript title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the origin ID of the manuscript
        /// </summary>
        public string OriginId { get; set; }

        /// <summary>
        /// Gets or sets the number of referee candidates found
        /// </summary>
        public int RefereeCount { get; set; }

        /// <summary>
        /// Gets or sets the manuscript upload response with referee candidates
        /// </summary>
        public ManuscriptUploadResponse ManuscriptAnalysis { get; set; }

        /// <summary>
        /// Gets or sets the manuscript analysis response
        /// </summary>
        public ManuscriptUploadResponse Analysis { get; set; }

        /// <summary>
        /// Gets or sets whether manuscript analysis is available
        /// </summary>
        public bool HasAnalysis { get; set; }

        /// <summary>
        /// Gets or sets the analysis error message if any
        /// </summary>
        public string AnalysisError { get; set; }

        /// <summary>
        /// Gets or sets the referee candidates response
        /// </summary>
        public RefereeRecommendationResponse RefereeCandidates { get; set; }

        /// <summary>
        /// Gets or sets the referee recommendations response
        /// </summary>
        public RefereeRecommendationResponse RefereeRecommendations { get; set; }

        /// <summary>
        /// Gets or sets whether referee candidates are available
        /// </summary>
        public bool HasRefereeCandidates { get; set; }

        /// <summary>
        /// Gets or sets the referee candidates error message if any
        /// </summary>
        public string RefereeCandidatesError { get; set; }

        /// <summary>
        /// Gets or sets the referee error message if any
        /// </summary>
        public string RefereeError { get; set; }

        /// <summary>
        /// Gets or sets the journal recommendations response  
        /// </summary>
        public JournalRecommendationResponse JournalRecommendations { get; set; }

        /// <summary>
        /// Gets or sets whether journal recommendations are available
        /// </summary>
        public bool HasJournalRecommendations { get; set; }

        /// <summary>
        /// Gets or sets the journal recommendations error message if any
        /// </summary>
        public string JournalRecommendationsError { get; set; }

        /// <summary>
        /// Gets or sets the journal error message if any
        /// </summary>
        public string JournalError { get; set; }

        /// <summary>
        /// Gets or sets a general error message for the entire operation
        /// </summary>
        public string GeneralError { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the results were loaded
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the manuscript
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the status of the overall operation
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ManuscriptResultsViewModel()
        {
            Timestamp = DateTime.Now;
            Metadata = new Dictionary<string, object>();
            Status = "Loading";
        }
    }
} 