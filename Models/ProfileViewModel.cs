namespace SmartResumeAnalyzer.Models
{
    public class ProfileAnalysisEntry
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ATSScore { get; set; }
        public DateTime? Date { get; set; }
    }

    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public DateTime MemberSince { get; set; }
        public int TotalResumesAnalyzed { get; set; }
        public int TotalResumesUploaded { get; set; }
        public int AverageATSScore { get; set; }
        public int HighestATSScore { get; set; }
        public DateTime? LastAnalysisDate { get; set; }
        public List<ProfileAnalysisEntry> RecentAnalyses { get; set; } = new();
        public List<string> TopSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public List<string> ResumeSuggestions { get; set; } = new();
        public string? LoadMessage { get; set; }
        public bool HasRecords => RecentAnalyses.Any();
    }
}

