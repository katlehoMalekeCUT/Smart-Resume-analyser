using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace SmartResumeAnalyzer.Services
{
    public class KeywordAnalyzerService
    {
        private readonly IMemoryCache _cache;
        private readonly VerbMatchingService? _verbMatchingService;

        public KeywordAnalyzerService(IMemoryCache cache, VerbMatchingService? verbMatchingService = null)
        {
            _cache = cache;
            _verbMatchingService = verbMatchingService;
        }

        public async Task<int> CalculateScoreAsync(string resumeText, string jobText)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new ArgumentException("Resume text cannot be empty", nameof(resumeText));
            if (string.IsNullOrWhiteSpace(jobText))
                throw new ArgumentException("Job text cannot be empty", nameof(jobText));

            var key = CreateCacheKey(resumeText, jobText, "kwscore");
            if (_cache.TryGetValue<int>(key, out var cached))
                return cached;

            var score = await Task.Run(async () =>
            {
                // Use verb-based matching if available, with 60% weight
                int verbScore = 0;
                if (_verbMatchingService != null)
                {
                    var verbResult = await _verbMatchingService.MatchVerbsToResumeAsync(jobText, resumeText);
                    verbScore = verbResult.Score;
                }

                // Traditional keyword matching with 40% weight
                var jobWords = ExtractKeywords(jobText);
                var resumeWords = ExtractKeywords(resumeText);

                int keywordMatches = jobWords.Count(word => resumeWords.Contains(word));
                int keywordScore = jobWords.Count == 0 ? 0 : (int)((double)keywordMatches / jobWords.Count * 100);

                // Combine scores: 60% verb matching + 40% keyword matching
                if (_verbMatchingService != null)
                {
                    return (int)(verbScore * 0.6 + keywordScore * 0.4);
                }
                else
                {
                    return keywordScore;
                }
            });

            _cache.Set(key, score, TimeSpan.FromMinutes(10));
            return score;
        }

        public async Task<List<string>> MissingKeywordsAsync(string resumeText, string jobText)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new ArgumentException("Resume text cannot be empty", nameof(resumeText));
            if (string.IsNullOrWhiteSpace(jobText))
                throw new ArgumentException("Job text cannot be empty", nameof(jobText));

            var key = CreateCacheKey(resumeText, jobText, "kwmiss");
            if (_cache.TryGetValue<List<string>>(key, out var cached))
                return cached ?? new List<string>();

            var missing = await Task.Run(async () =>
            {
                var results = new List<string>();

                // Get missing action verbs from job description (prioritized)
                if (_verbMatchingService != null)
                {
                    var verbResult = await _verbMatchingService.MatchVerbsToResumeAsync(jobText, resumeText);
                    if (verbResult.MissingVerbs.Count > 0)
                    {
                        // Add missing verbs as priority items
                        results.AddRange(verbResult.MissingVerbs.Take(3));
                    }
                }

                // Add traditional missing keywords to fill out the list
                var jobWords = ExtractKeywords(jobText);
                var resumeWords = ExtractKeywords(resumeText);

                var traditionalMissing = jobWords
                    .Where(word => !resumeWords.Contains(word))
                    .Except(results, StringComparer.OrdinalIgnoreCase)
                    .Take(Math.Max(0, 5 - results.Count))
                    .ToList();

                results.AddRange(traditionalMissing);
                
                return results
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            });

            _cache.Set(key, missing, TimeSpan.FromMinutes(10));
            return missing;
        }

        private static string CreateCacheKey(string a, string b, string prefix)
        {
            using var sha = SHA256.Create();
            var combined = (a ?? string.Empty) + "|" + (b ?? string.Empty);
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return prefix + ":" + Convert.ToHexString(hash);
        }

        private static readonly HashSet<string> CommonStopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "is", "are", "was", "were",
            "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
            "may", "might", "can", "must", "shall", "with", "from", "as", "by", "it", "its", "about", "into",
            "through", "during", "before", "after", "above", "below", "out", "off", "over", "under", "again",
            "further", "then", "once", "here", "there", "when", "where", "why", "how", "all", "each", "every",
            "both", "few", "more", "some", "such", "no", "nor", "not", "only", "own", "same", "so", "than",
            "too", "very", "just", "i", "you", "he", "she", "we", "they", "what", "which", "who", "whom", "this",
            "that", "these", "those", "am", "if", "your", "our", "their", "him", "her", "me", "us", "my",
            "you're", "he's", "she's", "it's", "we're", "they're", "i'm", "i've", "you've", "we've", "they've"
        };

        private List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Extract words that are meaningful (longer words, technical terms, proper nouns)
            var keywords = Regex.Matches(
                    text.ToLower(),
                    @"\b[a-zA-Z#\+\.]+\b")
                .Select(m => m.Value)
                .Where(w => 
                    w.Length > 2 &&                                    // Minimum length
                    !CommonStopwords.Contains(w) &&                    // Not a common stopword
                    (w.Length > 4 || Regex.IsMatch(w, @"[A-Z]")) &&   // Either longer or has capitals (technical terms)
                    Regex.IsMatch(w, @"[a-zA-Z]"))                     // Has letters
                .Distinct()
                .ToList();

            return keywords;
        }
    }
}
