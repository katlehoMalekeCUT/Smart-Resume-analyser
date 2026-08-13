using System.Text.Json.Serialization;

namespace SmartResumeAnalyzer.Models
{
    public class UserRecord
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("user_email")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonPropertyName("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("suggestions")]
        public string Suggestions { get; set; } = string.Empty;

        [JsonPropertyName("ats_score")]
        public int? ATSScore { get; set; }

        [JsonPropertyName("job_description")]
        public string JobDescription { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}

