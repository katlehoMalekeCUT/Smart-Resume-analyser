namespace SmartResumeAnalyzer.Models
{
    public class AnalyzeResultViewModel
    {
        public ResumeResult ResumeResult { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string SkillsInput { get; set; } = string.Empty;
        public bool SavedSuccessfully { get; set; }
        public string? Message { get; set; }
    }
}

