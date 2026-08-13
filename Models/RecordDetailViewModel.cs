namespace SmartResumeAnalyzer.Models
{
    public class RecordDetailViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int ATSScore { get; set; }
        public string Suggestions { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}

