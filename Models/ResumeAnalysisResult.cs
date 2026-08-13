namespace SmartResumeAnalyzer.Models
{
    public class ResumeResult
    {
        // ATS Score
        public int ATSScore { get; set; }

        // Number of matched skills
        public int MatchedSkills { get; set; }

        // Missing skills list
        public List<string> MissingSkills { get; set; } = new();

        // Suggestions list
        public List<string> Suggestions { get; set; } = new();

        // Career recommendation
        public string CareerRecommendation { get; set; } = string.Empty;
    }
}
